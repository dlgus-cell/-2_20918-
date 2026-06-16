using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 버튼용 코드 기반 호버/클릭 연출(스프라이트 교체 없이).
/// - 호버: 살짝 커짐.
/// - 누름: 더 크게 커지며 잠깐 옅게 틴트 → 원래(또는 호버)대로 = "팝".
/// 아무 UI 오브젝트에 붙이면 동작(Button 컴포넌트 없어도 됨).
/// 핫바 셀과 같은 느낌이지만 어떤 버튼에도 재사용 가능.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class UIButtonFx : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler
{
    [Header("틴트 대상 (비우면 자기 Graphic 자동)")]
    [SerializeField] private Graphic tintTarget;

    [Header("크기 (1 = 원래)")]
    [SerializeField] private float hoverScale = 1.05f;
    [SerializeField] private float pressScale = 1.12f;

    [Header("누름 팝 타이밍(초)")]
    [SerializeField] private float popUpTime   = 0.06f;
    [SerializeField] private float popDownTime = 0.14f;

    [Header("호버 이동 속도")]
    [SerializeField] private float hoverLerp = 16f;

    [Header("누름 틴트 (느낌)")]
    [SerializeField] private Color pressTint = new Color(0.75f, 0.75f, 0.78f, 1f);

    private RectTransform _rt;
    private bool _hover, _popping;
    private Coroutine _hoverC, _popC;

    void Awake()
    {
        _rt = GetComponent<RectTransform>();
        if (tintTarget == null) tintTarget = GetComponent<Graphic>();
    }

    public void OnPointerEnter(PointerEventData _) { _hover = true;  if (!_popping) HoverTo(hoverScale); }
    public void OnPointerExit (PointerEventData _) { _hover = false; if (!_popping) HoverTo(1f); }
    public void OnPointerDown (PointerEventData _) { Pop(); }
    public void OnPointerUp   (PointerEventData _) { }

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

    void Pop()
    {
        if (_hoverC != null) StopCoroutine(_hoverC);
        if (_popC   != null) StopCoroutine(_popC);
        _popC = StartCoroutine(PopRoutine());
    }

    IEnumerator PopRoutine()
    {
        _popping = true;
        float startScale = _rt ? _rt.localScale.x : 1f;
        Color startTint  = tintTarget ? tintTarget.color : Color.white;

        for (float t = 0f; t < popUpTime; t += Time.unscaledDeltaTime)
        {
            float k = t / popUpTime;
            SetScale(Mathf.Lerp(startScale, pressScale, k));
            SetTint(Color.Lerp(startTint, pressTint, k));
            yield return null;
        }
        SetScale(pressScale); SetTint(pressTint);

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
    void SetTint(Color c)  { if (tintTarget) tintTarget.color = c; }
}
