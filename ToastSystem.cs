using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

// ═══════════════════════════════════════════════════════════════════════
//  ToastSystem.cs — 화면 토스트 알림 시스템
//
//  [역할]
//    골드 획득 / 퀘스트 수주·완료 / 아이템 사용 등의 이벤트를 받아
//    화면 한쪽에 잠깐 떴다 사라지는 알림(토스트)을 표시한다.
//    어디서든 ToastSystem.Show("메시지") 로 수동 호출도 가능.
//
//  [부착 위치]
//    Screen Space Canvas 아래의 토스트 컨테이너 오브젝트에 부착.
//    - toastPrefab: 토스트 한 줄(프리팹). 안에 TMP_Text 포함.
//    - container  : 토스트들이 쌓일 부모 Transform (보통 이 오브젝트).
//
//  [통합 메모]
//    - 외부에서 ToastSystem.Show(...) 정적 호출 가능 (입력/시스템 무관).
//    - 자동 구독은 SubscribeAll()/UnsubscribeAll() 한 곳에 모았다.
//      이벤트를 추가·제거할 때 그 두 메서드만 손대면 된다.
//    - GoldSystem / Inventory 이벤트는 static, QuestManager 이벤트는
//      인스턴스(Instance) 기반이라 처리 방식이 다르다(아래 주석 참고).
//    - ★ 아이템 "획득" 토스트는 현재 미연결:
//        Inventory에 "어떤 아이템이 획득됐는지" 알려주는 이벤트가 없어서다.
//        Inventory(통합 대상)를 지금 수정하지 않기 위해, 획득 토스트는
//        줍는 쪽에서 ToastSystem.Show("○○ 획득")을 호출하는 방식으로 둔다.
//        나중에 Inventory에 획득 이벤트가 생기면 SubscribeAll에 한 줄 추가하면 된다.
// ═══════════════════════════════════════════════════════════════════════

public class ToastSystem : MonoBehaviour
{
    // ─── 싱글톤 ──────────────────────────────────────────────
    public static ToastSystem Instance { get; private set; }

    [Header("토스트 UI")]
    [Tooltip("토스트 한 줄 프리팹. 안에 TMP_Text가 있어야 한다.")]
    [SerializeField] private GameObject toastPrefab;

    [Tooltip("토스트가 쌓일 부모. 비우면 이 오브젝트의 transform 사용.")]
    [SerializeField] private Transform container;

    [Header("표시 설정")]
    [Tooltip("토스트 1개가 화면에 머무는 시간(초).")]
    [SerializeField] private float displayDuration = 2.5f;

    [Tooltip("페이드 인/아웃 시간(초).")]
    [SerializeField] private float fadeDuration = 0.3f;

    [Tooltip("동시에 표시할 최대 토스트 개수. 초과 시 가장 오래된 것부터 제거.")]
    [SerializeField] private int maxToasts = 4;

    [Header("자동 구독 옵션")]
    [Tooltip("골드 획득 시 자동 토스트. (기본 꺼짐 — 골드는 화면 숫자로 표시되므로)")]
    [SerializeField] private bool autoGold = false;
    [Tooltip("퀘스트 수주/완료 시 자동 토스트.")]
    [SerializeField] private bool autoQuest = true;
    [Tooltip("아이템 사용 시 자동 토스트.")]
    [SerializeField] private bool autoItemUse = true;

    // ─── 런타임 상태 ─────────────────────────────────────────
    private readonly List<GameObject> _active = new();
    private bool _questSubscribed = false;   // QuestManager는 인스턴스라 별도 추적

    // ═════════════════════════════════════════════════════════════════
    #region Unity Lifecycle

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (container == null) container = transform;
    }

    void OnEnable()  => SubscribeAll();
    void OnDisable() => UnsubscribeAll();

    void Update()
    {
        // QuestManager.Instance가 늦게 생성될 수 있어, 구독 전이면 재시도.
        if (autoQuest && !_questSubscribed)
            TrySubscribeQuest();
    }

    #endregion

    // ═════════════════════════════════════════════════════════════════
    #region 공개 API

    /// <summary>토스트를 표시한다. (어디서든 정적 호출 가능)</summary>
    public static void Show(string message)
    {
        if (Instance == null)
        {
            Debug.LogWarning($"[ToastSystem] 인스턴스가 없습니다. 메시지: {message}");
            return;
        }
        Instance.ShowInternal(message);
    }

    #endregion

    // ═════════════════════════════════════════════════════════════════
    #region 이벤트 구독 (통합 시 여기만 손대면 됨)

    void SubscribeAll()
    {
        // ── GoldSystem (static 이벤트) ──
        if (autoGold)
            GoldSystem.OnGoldChanged += HandleGoldChanged;

        // ── Inventory (static 이벤트) ──
        if (autoItemUse)
            Inventory.OnItemUsed += HandleItemUsed;

        // ── QuestManager (인스턴스 이벤트) ──
        //    Instance가 준비된 뒤에만 가능 → Update에서 재시도. 여기서도 한 번 시도.
        if (autoQuest)
            TrySubscribeQuest();
    }

    void UnsubscribeAll()
    {
        if (autoGold)
            GoldSystem.OnGoldChanged -= HandleGoldChanged;

        if (autoItemUse)
            Inventory.OnItemUsed -= HandleItemUsed;

        if (_questSubscribed && QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestAccepted  -= HandleQuestAccepted;
            QuestManager.Instance.OnQuestCompleted -= HandleQuestCompleted;
        }
        _questSubscribed = false;
    }

    void TrySubscribeQuest()
    {
        if (_questSubscribed) return;
        if (QuestManager.Instance == null) return;

        QuestManager.Instance.OnQuestAccepted  += HandleQuestAccepted;
        QuestManager.Instance.OnQuestCompleted += HandleQuestCompleted;
        _questSubscribed = true;
    }

    #endregion

    // ═════════════════════════════════════════════════════════════════
    #region 이벤트 핸들러

    /// <summary>
    /// 골드 변화 → 획득(delta>0)이고 reason이 실제 '얻은' 경우에만 토스트.
    /// 구매(Purchase)·로드(Load)는 제외.
    /// </summary>
    void HandleGoldChanged(int newTotal, int delta, GoldChangeReason reason)
    {
        if (delta <= 0) return;

        switch (reason)
        {
            case GoldChangeReason.PickUp:
            case GoldChangeReason.MonsterDrop:
            case GoldChangeReason.QuestReward:
                ShowInternal($"+{delta} 골드");
                break;
            // Purchase / Load / Misc 는 토스트하지 않음
        }
    }

    void HandleItemUsed(ItemData item)
    {
        if (item == null) return;
        ShowInternal($"{item.itemName} 사용");
    }

    void HandleQuestAccepted(ActiveQuest quest)
    {
        if (quest?.questData == null) return;
        ShowInternal($"퀘스트 수주: {quest.questData.questName}");
    }

    void HandleQuestCompleted(ActiveQuest quest)
    {
        if (quest?.questData == null) return;
        ShowInternal($"퀘스트 완료: {quest.questData.questName}");
    }

    #endregion

    // ═════════════════════════════════════════════════════════════════
    #region 토스트 표시 처리

    void ShowInternal(string message)
    {
        if (toastPrefab == null)
        {
            Debug.LogWarning($"[ToastSystem] toastPrefab이 없습니다. 메시지: {message}");
            return;
        }

        // 최대 개수 초과 시 가장 오래된 것 제거
        while (_active.Count >= maxToasts && _active.Count > 0)
        {
            GameObject oldest = _active[0];
            _active.RemoveAt(0);
            if (oldest != null) Destroy(oldest);
        }

        GameObject toast = Instantiate(toastPrefab, container);
        _active.Add(toast);

        var text = toast.GetComponentInChildren<TMP_Text>();
        if (text != null) text.text = message;

        StartCoroutine(ToastRoutine(toast));
    }

    IEnumerator ToastRoutine(GameObject toast)
    {
        CanvasGroup cg = toast.GetComponent<CanvasGroup>();
        if (cg == null) cg = toast.AddComponent<CanvasGroup>();

        // 페이드 인
        yield return Fade(cg, 0f, 1f, fadeDuration);

        // 유지
        yield return new WaitForSeconds(displayDuration);

        // 페이드 아웃
        yield return Fade(cg, 1f, 0f, fadeDuration);

        _active.Remove(toast);
        if (toast != null) Destroy(toast);
    }

    IEnumerator Fade(CanvasGroup cg, float from, float to, float duration)
    {
        if (cg == null) yield break;
        if (duration <= 0f) { cg.alpha = to; yield break; }

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        cg.alpha = to;
    }

    #endregion

    // ═════════════════════════════════════════════════════════════════

    [ContextMenu("테스트: 토스트 표시")]
    void DEBUG_Show() => ShowInternal("테스트 토스트입니다.");
}
