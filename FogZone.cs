using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 안개 기믹 (명세서 스테이지2: "안개 속에 오래 있으면 피해를 입음").
///
/// [역할]
///   플레이어가 안개(트리거) 안에 graceSeconds 이상 머무르면, 그 후부터
///   일정 간격으로 피해를 준다. 안개 밖으로 나가면 머문 시간이 초기화된다.
///   피해는 정본 PlayerVitals 로 전달.
///
/// [해제]
///   Clear() 로 안개를 끌 수 있다(등불꽃 Brazier.onActivated 에 연결해서
///   "공격으로 등불을 켜면 안개가 사라짐"을 만든다).
///
/// [부착] 안개 영역 트리거 콜라이더(IsTrigger)에 부착.
/// </summary>
[RequireComponent(typeof(Collider))]
public class FogZone : MonoBehaviour, ISaveableGimmick
{
    [Header("저장용 고유 ID (이어하기 시 해제 상태 유지)")]
    [SerializeField] private string saveId = "";

    [Header("피해")]
    [SerializeField] private int   damagePerTick = 3;
    [SerializeField] private float tickInterval  = 1f;
    [Tooltip("이 시간(초) 이상 머물러야 피해가 시작된다.")]
    [SerializeField] private float graceSeconds  = 2f;

    [Header("설정")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool   active    = true;

    [Header("표시(선택) — 안개 비주얼 오브젝트")]
    [Tooltip("Clear() 시 함께 끌 안개 비주얼. 비워도 됨.")]
    [SerializeField] private GameObject fogVisual;

    [Header("이벤트")]
    public UnityEvent onCleared;

    private bool  _playerInside;
    private float _dwellTime;
    private float _tickTimer;
    private float _suppressTimer;   // > 0 이면 일시 차단(해제 보상)

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag)) { _playerInside = true; _dwellTime = 0f; _tickTimer = 0f; }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag)) { _playerInside = false; _dwellTime = 0f; }
    }

    void Update()
    {
        if (_suppressTimer > 0f) { _suppressTimer -= Time.deltaTime; return; }
        if (!active || !_playerInside) return;

        _dwellTime += Time.deltaTime;
        if (_dwellTime < graceSeconds) return;

        // 함정 피해 면역 중이면 건너뜀(내 영역에서 확인).
        if (PlayerStatusEffects.Instance != null && PlayerStatusEffects.Instance.HazardImmune) return;

        _tickTimer -= Time.deltaTime;
        if (_tickTimer > 0f) return;
        _tickTimer = tickInterval;

        PlayerVitals.Instance?.TakeDamage(damagePerTick);
    }

    /// <summary>안개를 걷어낸다(피해 중단 + 비주얼 끄기).</summary>
    public void Clear()
    {
        if (!active) return;
        active = false;
        _playerInside = false;
        if (fogVisual != null) fogVisual.SetActive(false);
        onCleared?.Invoke();
        Debug.Log($"[FogZone] '{name}' 안개 해제됨");
    }

    /// <summary>해제 보상으로 seconds 동안 피해를 일시 차단(영구 Clear 와 별개).</summary>
    public void Suppress(float seconds) => _suppressTimer = Mathf.Max(_suppressTimer, seconds);

    // ISaveableGimmick — 해제 상태를 저장/복원(이어하기 시 유지)
    public string SaveId => saveId;
    public string CaptureState() => active ? "" : "cleared";
    public void RestoreState(string state) { if (state == "cleared") Clear(); }
}
