using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 저장 "정책"과 사망 후 부활을 담당하는 조정 레이어.
///
/// ── 왜 이 파일이 필요한가 ─────────────────────────────────────────────
///   기존 SaveSystem 은 "슬롯에 저장/불러오기"라는 기계적인 일만 한다.
///   하지만 게임 규칙은 그 위에 더 있다:
///     · 플레이어가 원하는 곳에서 저장 (단, 전투 중·보스방에서는 금지)
///     · 죽으면 마지막 저장 위치에서 풀피로 부활
///   이 규칙을 SaveSystem 을 수정하지 않고 바깥에서 얹는다.
///
/// ── 설계 원칙 ────────────────────────────────────────────────────────
///   - SaveSystem / PlayerSaveData 등 기존 파일은 수정하지 않는다.
///   - 다른 담당자(전투팀·플레이어팀)가 호출할 자리는 public 으로 열어둔다.
///     ("[연동 지점]" 주석 참고)
///   - SaveSystem.Instance / PlayerVitals.Instance 의 공개 API 만 사용.
///
/// ── 외부 연결 지점 요약 ───────────────────────────────────────────────
///   1. 전투 시작/종료(전투팀) → SetCombatActive(true/false)
///   2. 보스방 등 저장 금지 구역 진입/이탈 → BlockSaving() / UnblockSaving()
///   3. 세이브 지점 UI/상호작용 → TrySave(slot)
///   4. 불러오기 메뉴 → Load(slot)
/// </summary>
public class SaveService : MonoBehaviour
{
    public static SaveService Instance { get; private set; }

    // ───── 이벤트 ─────
    /// <summary>저장이 막혀 실패했을 때 (사유 문자열).</summary>
    public static event Action<string> OnSaveBlocked;
    /// <summary>부활이 끝났을 때.</summary>
    public static event Action         OnRespawned;

    // ───── Inspector ─────
    [Header("플레이어 참조 (없으면 Tag=Player 로 자동 탐색)")]
    [SerializeField] private Transform playerTransform;

    [Header("사망 처리")]
    [Tooltip("사망 시 자동으로 부활을 처리할지. 플레이어팀이 직접 처리한다면 끈다.")]
    [SerializeField] private bool  autoRespawnOnDeath = true;
    [Tooltip("사망 연출이 재생될 시간(초). 이만큼 기다린 뒤 부활.")]
    [SerializeField] private float respawnDelay = 1.5f;
    [Tooltip("켜면 사망 시 마지막 슬롯을 통째로 다시 불러온다(골드·퀘스트까지 롤백). "
           + "끄면 위치 이동 + 풀피만 하고 진행 상황은 유지한다.")]
    [SerializeField] private bool  fullReloadOnDeath = false;

    [Header("저장 슬롯")]
    [SerializeField] private int defaultSlot = 0;

    // ───── 내부 상태 ─────
    private int     _saveBlockCount;       // 0보다 크면 저장 금지 (여러 곳에서 독립적으로 막을 수 있게 카운터)
    private bool    _hasSavePoint;
    private int     _lastSaveSlot = -1;
    private Vector3 _lastSavePosition;

    /// <summary>지금 이 자리에서 저장 가능한가?</summary>
    public bool CanSaveHere => _saveBlockCount <= 0;

    // ─────────────────────────────────────────────────────────────────
    #region Unity Lifecycle

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (playerTransform == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }
    }

    void OnEnable()  => PlayerVitals.OnDeath += HandleDeath;
    void OnDisable() => PlayerVitals.OnDeath -= HandleDeath;

    #endregion

    // ─────────────────────────────────────────────────────────────────
    #region 저장 차단 (전투/보스방 등)

    /// <summary>[연동 지점 2] 저장 금지 시작. 보스방 진입·전투 시작 등에서 호출.</summary>
    public void BlockSaving()   => _saveBlockCount++;

    /// <summary>[연동 지점 2] 저장 금지 해제. 막은 횟수만큼 풀어야 한다.</summary>
    public void UnblockSaving() => _saveBlockCount = Mathf.Max(0, _saveBlockCount - 1);

    /// <summary>
    /// [연동 지점 1] 전투 상태 토글 편의 메서드.
    /// 전투팀이 전투 시작 시 true, 종료 시 false 로 호출하면 된다.
    /// (내부적으로 BlockSaving/UnblockSaving 을 1회씩 사용)
    /// </summary>
    public void SetCombatActive(bool active)
    {
        if (active) BlockSaving();
        else        UnblockSaving();
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────
    #region 저장 / 불러오기

    /// <summary>
    /// [연동 지점 3] 세이브 지점/메뉴에서 호출. 저장 가능하면 저장하고 부활 지점을 갱신.
    /// 전투 중·보스방이면 막고 false 반환.
    /// </summary>
    public bool TrySave(int slot = -1, string saveName = null)
    {
        if (slot < 0) slot = defaultSlot;

        if (!CanSaveHere)
        {
            const string reason = "전투 중이거나 저장할 수 없는 구역입니다.";
            Debug.Log($"[SaveService] 저장 차단: {reason}");
            OnSaveBlocked?.Invoke(reason);
            return false;
        }

        if (SaveSystem.Instance == null)
        {
            Debug.LogWarning("[SaveService] SaveSystem 이 씬에 없습니다.");
            return false;
        }

        bool ok = SaveSystem.Instance.SaveToSlot(slot, saveName);
        if (ok)
        {
            _lastSaveSlot = slot;
            _hasSavePoint = true;
            if (playerTransform != null) _lastSavePosition = playerTransform.position;
            Debug.Log($"[SaveService] 저장 완료 → 부활 지점 갱신 (슬롯 {slot})");
        }
        return ok;
    }

    /// <summary>[연동 지점 4] 불러오기. SaveSystem 으로 그대로 위임.</summary>
    public bool Load(int slot = -1)
    {
        if (slot < 0) slot = defaultSlot;
        if (SaveSystem.Instance == null) return false;

        bool ok = SaveSystem.Instance.LoadFromSlot(slot);
        if (ok)
        {
            _lastSaveSlot = slot;
            _hasSavePoint = true;
            if (playerTransform != null) _lastSavePosition = playerTransform.position;
        }
        return ok;
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────
    #region 사망 → 부활

    void HandleDeath()
    {
        if (!autoRespawnOnDeath) return;   // 플레이어팀이 직접 처리하는 경우
        StartCoroutine(RespawnRoutine());
    }

    IEnumerator RespawnRoutine()
    {
        // 사망 연출이 재생될 시간을 준다. (플레이어팀의 OnDeath 핸들러와 공존)
        if (respawnDelay > 0f) yield return new WaitForSeconds(respawnDelay);

        if (fullReloadOnDeath && _hasSavePoint)
        {
            // 마지막 저장을 통째로 다시 불러옴 → 위치·골드·퀘스트까지 그 시점으로.
            Load(_lastSaveSlot);
            if (PlayerVitals.Instance != null) PlayerVitals.Instance.Revive(1f);
        }
        else
        {
            // 위치만 마지막 저장 지점으로 옮기고 풀피 부활. 진행 상황은 유지.
            if (_hasSavePoint && playerTransform != null)
                playerTransform.position = _lastSavePosition;

            if (PlayerVitals.Instance != null) PlayerVitals.Instance.Revive(1f);
        }

        Debug.Log("[SaveService] 부활 완료");
        OnRespawned?.Invoke();
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────
    #region 에디터 테스트 (우클릭 컨텍스트 메뉴 — 키 충돌 없음)

    [ContextMenu("테스트/여기서 저장(슬롯0)")]
    void _TestSave() => Debug.Log($"[SaveService] TrySave = {TrySave(0)}");

    [ContextMenu("테스트/전투 시작(저장 차단)")]
    void _TestCombatOn() { SetCombatActive(true); Debug.Log($"CanSaveHere = {CanSaveHere}"); }

    [ContextMenu("테스트/전투 종료(차단 해제)")]
    void _TestCombatOff() { SetCombatActive(false); Debug.Log($"CanSaveHere = {CanSaveHere}"); }

    [ContextMenu("테스트/플레이어 즉사시키기")]
    void _TestKill()
    {
        if (PlayerVitals.Instance != null)
            PlayerVitals.Instance.TakeDamage(PlayerVitals.Instance.CurrentHP);
    }

    #endregion
}
