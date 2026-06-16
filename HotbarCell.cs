using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// 핫바 슬롯 UI.
/// - 호버: 살짝 커짐(유지).
/// - 누름(키 1~9 / 클릭): 더 크게 커지며 잠깐 옅게 어두워졌다가 원래(또는 호버)대로 복귀 = "팝".
/// 선택/글로우 같은 강조는 없음. 아이템 아이콘은 따로 지정(ItemData.icon).
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class HotbarCell : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("References")]
    [SerializeField] private Image    bgImage;
    [SerializeField] private Image    iconImage;
    [SerializeField] private TMP_Text slotNumberText;

    [Header("크기 (1 = 원래)")]
    [Tooltip("호버 시 크기(살짝).")]
    [SerializeField] private float hoverScale = 1.06f;
    [Tooltip("누름 팝의 최대 크기(호버보다 큼).")]
    [SerializeField] private float pressScale = 1.16f;

    [Header("누름 팝 타이밍(초)")]
    [SerializeField] private float popUpTime   = 0.07f;
    [SerializeField] private float popDownTime = 0.16f;

    [Header("누름 시 잠깐 입히는 틴트 (회색빛은 '느낌' — 색은 자유)")]
    [SerializeField] private Color pressTint = new Color(0.72f, 0.72f, 0.76f, 1f);

    [Header("호버 이동 속도")]
    [SerializeField] private float hoverLerp = 16f;

    private int  _slotIndex;
    private bool _hover;
    private bool _popping;
    private RectTransform _rt;
    private Coroutine _hoverC, _popC;

    void Awake()
    {
        _rt = GetComponent<RectTransform>();
        if (bgImage) bgImage.color = Color.white;
    }

    void OnEnable()  => HotbarManager.OnSlotPressed += OnSlotPressed;
    void OnDisable() => HotbarManager.OnSlotPressed -= OnSlotPressed;

    public void Init(int index)
    {
        _slotIndex = index;
        if (slotNumberText) slotNumberText.text = (index + 1).ToString();
        Refresh(HotbarManager.Instance?.GetSlotItem(index));
    }

    void Refresh(ItemData item)
    {
        if (!iconImage) return;
        iconImage.sprite = item?.icon;
        iconImage.color  = item != null ? Color.white : new Color(1, 1, 1, 0);
    }

    void OnSlotPressed(int index) { if (index == _slotIndex) PlayPop(); }

    public void OnPointerEnter(PointerEventData _) { _hover = true;  if (!_popping) HoverTo(hoverScale); }
    public void OnPointerExit (PointerEventData _) { _hover = false; if (!_popping) HoverTo(1f); }
    public void OnPointerClick(PointerEventData _) { HotbarManager.Instance?.UseSlot(_slotIndex); }

    // ── 호버: 부드럽게 목표 크기로 ──
    void HoverTo(float target)
    {
        if (_hoverC != null) StopCoroutine(_hoverC);
        _hoverC = StartCoroutine(LerpScale(target));
    }

    IEnumerator LerpScale(float target)
    {
        var tv = Vector3.one * target;
        while (_rt && Vector3.Distance(_rt.localScale, tv) > 0.001f)
        {
            _rt.localScale = Vector3.Lerp(_rt.localScale, tv, Time.unscaledDeltaTime * hoverLerp);
            yield return null;
        }
        if (_rt) _rt.localScale = tv;
    }

    // ── 누름 팝: 크게 + 틴트 → 원래(또는 호버)대로 ──
    void PlayPop()
    {
        if (_hoverC != null) StopCoroutine(_hoverC);
        if (_popC   != null) StopCoroutine(_popC);
        _popC = StartCoroutine(PopRoutine());
    }

    IEnumerator PopRoutine()
    {
        _popping = true;
        float startScale = _rt ? _rt.localScale.x : 1f;
        Color startTint  = bgImage ? bgImage.color : Color.white;

        // 커지며 틴트 입힘
        for (float t = 0f; t < popUpTime; t += Time.unscaledDeltaTime)
        {
            float k = t / popUpTime;
            SetScale(Mathf.Lerp(startScale, pressScale, k));
            SetTint(Color.Lerp(startTint, pressTint, k));
            yield return null;
        }
        SetScale(pressScale); SetTint(pressTint);

        // 복귀: 호버 중이면 hoverScale, 아니면 1
        float rest = _hover ? hoverScale : 1f;
        for (float t = 0f; t < popDownTime; t += Time.unscaledDeltaTime)
        {
            float k = t / popDownTime;
            SetScale(Mathf.Lerp(pressScale, rest, k));
            SetTint(Color.Lerp(pressTint, Color.white, k));
            yield return null;
        }
        SetScale(rest); SetTint(Color.white);
        _popping = false;
    }

    void SetScale(float s) { if (_rt) _rt.localScale = Vector3.one * s; }
    void SetTint(Color c)  { if (bgImage) bgImage.color = c; }
}
