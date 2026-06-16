using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ═══════════════════════════════════════════════════════════════════════
//  HealthBarHUD.cs — 체력/기력(스태미나) HUD 바
//
//  [역할]
//    PlayerVitals의 이벤트를 구독하여 체력 바·기력 바를 실시간 갱신한다.
//    명세서 기능요구사항 5번(체력/기력 UI, 필수)에 해당.
//
//  [부착 위치]
//    HUD Canvas 아래의 적당한 오브젝트에 부착.
//    - hpFill / staminaFill : Image (Image Type = Filled, Fill Method = Horizontal)
//    - hpText / staminaText : (선택) 숫자 표시용 TMP
//
//  [통합 메모]
//    - PlayerVitals의 정적 이벤트(OnHPChanged/OnStaminaChanged)만 구독하므로
//      PlayerVitals가 나중에 팀원 캐릭터 시스템으로 교체돼도,
//      같은 시그니처 (현재값, 최대값) 이벤트만 쏘면 이 HUD는 그대로 동작한다.
// ═══════════════════════════════════════════════════════════════════════

public class HealthBarHUD : MonoBehaviour
{
    [Header("체력 바")]
    [Tooltip("체력 게이지 Image (Type = Filled).")]
    [SerializeField] private Image hpFill;
    [Tooltip("체력 수치 텍스트 (선택).")]
    [SerializeField] private TMP_Text hpText;

    [Header("기력(스태미나) 바")]
    [Tooltip("기력 게이지 Image (Type = Filled).")]
    [SerializeField] private Image staminaFill;
    [Tooltip("기력 수치 텍스트 (선택).")]
    [SerializeField] private TMP_Text staminaText;

    [Header("표시 옵션")]
    [Tooltip("게이지가 부드럽게 차고 줄어들게 보간한다.")]
    [SerializeField] private bool smoothFill = true;
    [Tooltip("보간 속도 (값이 클수록 빠르게 따라감).")]
    [SerializeField] private float smoothSpeed = 8f;
    [Tooltip("수치 텍스트 형식. {0}=현재, {1}=최대")]
    [SerializeField] private string numberFormat = "{0} / {1}";

    // ─── 런타임 상태 ─────────────────────────────────────────
    private float _hpTarget = 1f;        // 목표 fill (0~1)
    private float _staminaTarget = 1f;

    // ═════════════════════════════════════════════════════════════════

    void OnEnable()
    {
        PlayerVitals.OnHPChanged      += HandleHPChanged;
        PlayerVitals.OnStaminaChanged += HandleStaminaChanged;
    }

    void OnDisable()
    {
        PlayerVitals.OnHPChanged      -= HandleHPChanged;
        PlayerVitals.OnStaminaChanged -= HandleStaminaChanged;
    }

    void Start()
    {
        // PlayerVitals가 이미 초기화돼 있으면 현재값으로 1회 동기화.
        // (이벤트를 놓쳤을 경우 대비)
        if (PlayerVitals.Instance != null)
        {
            var ph = PlayerVitals.Instance;
            HandleHPChanged(ph.CurrentHP, ph.MaxHP);
            HandleStaminaChanged(ph.CurrentStamina, ph.MaxStamina);

            // 시작 시엔 보간 없이 즉시 반영
            if (hpFill != null)      hpFill.fillAmount      = _hpTarget;
            if (staminaFill != null) staminaFill.fillAmount = _staminaTarget;
        }
    }

    void Update()
    {
        if (!smoothFill) return;

        if (hpFill != null)
            hpFill.fillAmount = Mathf.MoveTowards(
                hpFill.fillAmount, _hpTarget, smoothSpeed * Time.deltaTime);

        if (staminaFill != null)
            staminaFill.fillAmount = Mathf.MoveTowards(
                staminaFill.fillAmount, _staminaTarget, smoothSpeed * Time.deltaTime);
    }

    // ─────────────────────────────────────────────────────────────────

    void HandleHPChanged(int current, int max)
    {
        _hpTarget = max > 0 ? (float)current / max : 0f;

        if (!smoothFill && hpFill != null)
            hpFill.fillAmount = _hpTarget;

        if (hpText != null)
            hpText.text = string.Format(numberFormat, current, max);
    }

    void HandleStaminaChanged(int current, int max)
    {
        _staminaTarget = max > 0 ? (float)current / max : 0f;

        if (!smoothFill && staminaFill != null)
            staminaFill.fillAmount = _staminaTarget;

        if (staminaText != null)
            staminaText.text = string.Format(numberFormat, current, max);
    }
}
