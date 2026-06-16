using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

// ─────────────────────────────────────────────────────────────────
/// <summary>
/// 상태창 스탯 한 줄 UI.
/// 이름 / 현재 값 / [+] 버튼 / 투자 포인트 표시 / 숫자 애니메이션.
/// (구버전 StatRowUI)
/// </summary>
public class StatRow : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text    statNameText;
    [SerializeField] private TMP_Text    statValueText;
    [SerializeField] private TMP_Text    investedText;
    [SerializeField] private TMP_Text    perPointText;
    [SerializeField] private GameObject  plusButtonObj;
    [SerializeField] private Button      plusButton;
    [SerializeField] private Image       plusBtnImage;

    [Header("+ 버튼 색상")]
    [SerializeField] private Color plusNormal  = new Color(0.25f, 0.80f, 0.35f, 1f);
    [SerializeField] private Color plusHover   = new Color(0.45f, 1.00f, 0.55f, 1f);
    [SerializeField] private Color plusPressed = new Color(0.15f, 0.55f, 0.20f, 1f);

    [Header("값 텍스트 강조 색상")]
    [SerializeField] private Color valueNormal    = Color.white;
    [SerializeField] private Color valueHighlight = new Color(1f, 0.95f, 0.4f, 1f);

    [Header("숫자 카운트 애니메이션")]
    [SerializeField] private float countDuration = 0.35f;
    [SerializeField] private float animSpeed     = 16f;

    private StatType _statType;
    private int      _displayedValue;
    private bool     _initialized;

    private Coroutine _countRoutine;
    private Coroutine _flashRoutine;
    private Coroutine _btnColorRoutine;
    private Coroutine _btnScaleRoutine;
    private RectTransform _btnRT;

    public void Init(StatType type)
    {
        _statType    = type;
        _initialized = true;

        if (plusButton != null)
            plusButton.onClick.AddListener(OnPlusClicked);

        if (plusBtnImage == null && plusButtonObj != null)
            plusBtnImage = plusButtonObj.GetComponent<Image>();

        if (plusButtonObj != null)
            _btnRT = plusButtonObj.GetComponent<RectTransform>();

        SetupPointerEvents();

        Refresh(instant: true);
    }

    void OnEnable()
    {
        PlayerStats.OnStatsChanged      += OnStatsChanged;
        PlayerStats.OnStatPointsChanged += OnStatPointsChanged;
    }

    void OnDisable()
    {
        PlayerStats.OnStatsChanged      -= OnStatsChanged;
        PlayerStats.OnStatPointsChanged -= OnStatPointsChanged;
    }

    void OnStatsChanged()              { if (_initialized) Refresh(); }
    void OnStatPointsChanged(int _)    { if (_initialized) UpdatePlusButton(); }

    public void Refresh(bool instant = false)
    {
        if (PlayerStats.Instance == null) return;

        var ps = PlayerStats.Instance;

        if (statNameText) statNameText.text = GetStatDisplayName(_statType);

        int newVal = ps.GetStatValue(_statType);
        if (instant)
        {
            _displayedValue = newVal;
            if (statValueText) statValueText.text = FormatValue(_statType, newVal);
        }
        else if (newVal != _displayedValue)
        {
            if (_countRoutine != null) StopCoroutine(_countRoutine);
            _countRoutine = StartCoroutine(CountTo(newVal));
            FlashValueHighlight();
        }

        int invested = ps.GetInvested(_statType);
        if (investedText)
        {
            investedText.gameObject.SetActive(invested > 0);
            investedText.text = $"(+{invested})";
        }

        if (perPointText)
            perPointText.text = $"+{ps.GetPerPoint(_statType)}/pt";

        UpdatePlusButton();
    }

    void UpdatePlusButton()
    {
        if (plusButtonObj == null) return;
        bool hasPoints = PlayerStats.Instance != null && PlayerStats.Instance.StatPoints > 0;
        plusButtonObj.SetActive(hasPoints);
    }

    void OnPlusClicked()
    {
        PlayerStats.Instance?.InvestPoint(_statType);
        AnimBtnScale(0.88f, then: 1f);
    }

    void SetupPointerEvents()
    {
        if (plusButtonObj == null) return;

        var trigger = plusButtonObj.GetComponent<EventTrigger>();
        if (trigger == null) trigger = plusButtonObj.AddComponent<EventTrigger>();
        trigger.triggers.Clear();

        Add(trigger, EventTriggerType.PointerEnter, _ => AnimBtnColor(plusHover));
        Add(trigger, EventTriggerType.PointerExit,  _ => AnimBtnColor(plusNormal));
        Add(trigger, EventTriggerType.PointerDown,  _ => { AnimBtnColor(plusPressed); AnimBtnScale(0.88f); });
        Add(trigger, EventTriggerType.PointerUp,    _ => { AnimBtnColor(plusHover);  AnimBtnScale(1f); });
    }

    static void Add(EventTrigger et, EventTriggerType type, Action<BaseEventData> cb)
    {
        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(d => cb(d));
        et.triggers.Add(entry);
    }

    void AnimBtnColor(Color target)
    {
        if (plusBtnImage == null) return;
        if (_btnColorRoutine != null) StopCoroutine(_btnColorRoutine);
        _btnColorRoutine = StartCoroutine(LerpColor(plusBtnImage, target, animSpeed));
    }

    void AnimBtnScale(float to, float then = -1f)
    {
        if (_btnRT == null) return;
        if (_btnScaleRoutine != null) StopCoroutine(_btnScaleRoutine);
        _btnScaleRoutine = StartCoroutine(ScaleSeq(to, then));
    }

    IEnumerator ScaleSeq(float to, float then)
    {
        yield return LerpScale(_btnRT, to, animSpeed * 2f);
        if (then >= 0f) yield return LerpScale(_btnRT, then, animSpeed);
    }

    IEnumerator CountTo(int target)
    {
        int    start   = _displayedValue;
        float  elapsed = 0f;

        while (elapsed < countDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / countDuration);
            _displayedValue = Mathf.RoundToInt(Mathf.Lerp(start, target, t));
            if (statValueText) statValueText.text = FormatValue(_statType, _displayedValue);
            yield return null;
        }

        _displayedValue = target;
        if (statValueText) statValueText.text = FormatValue(_statType, target);
    }

    void FlashValueHighlight()
    {
        if (statValueText == null) return;
        if (_flashRoutine != null) StopCoroutine(_flashRoutine);
        _flashRoutine = StartCoroutine(FlashColor(statValueText, valueHighlight, valueNormal));
    }

    IEnumerator FlashColor(TMP_Text txt, Color flash, Color normal)
    {
        txt.color = flash;
        yield return new WaitForSecondsRealtime(0.25f);
        float t = 0f;
        while (t < 0.4f)
        {
            t += Time.unscaledDeltaTime;
            txt.color = Color.Lerp(flash, normal, t / 0.4f);
            yield return null;
        }
        txt.color = normal;
    }

    static string GetStatDisplayName(StatType t) => t switch
    {
        StatType.Strength     => "힘 (공격력)",
        StatType.Vitality     => "체력 (최대 HP)",
        StatType.Stamina      => "기력 (최대 기력)",
        StatType.Agility      => "민첩 (이동속도)",
        StatType.Intelligence => "지능 (마법 공격력)",
        StatType.Defense      => "방어 (방어력)",
        _                     => t.ToString()
    };

    static string FormatValue(StatType t, int v) => t switch
    {
        StatType.Agility => $"{v}  ({1f + v * 0.02f:P0})",
        _                => v.ToString()
    };

    static IEnumerator LerpColor(Image img, Color target, float speed)
    {
        while (img && Vector4.Distance(img.color, target) > 0.001f)
        {
            img.color = Color.Lerp(img.color, target, Time.unscaledDeltaTime * speed);
            yield return null;
        }
        if (img) img.color = target;
    }

    static IEnumerator LerpScale(RectTransform rt, float target, float speed)
    {
        var tv = Vector3.one * target;
        while (rt && Vector3.Distance(rt.localScale, tv) > 0.001f)
        {
            rt.localScale = Vector3.Lerp(rt.localScale, tv, Time.unscaledDeltaTime * speed);
            yield return null;
        }
        if (rt) rt.localScale = tv;
    }
}
