using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

// ─────────────────────────────────────────────────────────────────
/// <summary>
/// HUD 아이콘 버튼 호버/클릭 강조 효과.
/// 인벤토리·지도·상태창·저장 등 HUD 버튼에 부착.
/// (구버전 HUDIconButton)
/// </summary>
[RequireComponent(typeof(Button))]
public class UIIconFx : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler
{
    [Header("아이콘 / 배경 이미지")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Image bgImage;

    [Header("색상")]
    [SerializeField] private Color normalIcon  = Color.white;
    [SerializeField] private Color hoverIcon   = new Color(1f, 0.9f, 0.5f, 1f);
    [SerializeField] private Color pressedIcon = new Color(0.8f, 0.7f, 0.3f, 1f);
    [SerializeField] private Color normalBg    = new Color(0.1f,  0.1f,  0.15f, 0.75f);
    [SerializeField] private Color hoverBg     = new Color(0.2f,  0.2f,  0.30f, 0.90f);
    [SerializeField] private Color pressedBg   = new Color(0.05f, 0.05f, 0.08f, 1.00f);

    [Header("스케일")]
    [SerializeField] private float hoverScale = 1.12f;
    [SerializeField] private float pressScale = 0.90f;
    [SerializeField] private float animSpeed  = 16f;

    private RectTransform _rt;
    private Coroutine _iconC, _bgC, _scaleC;

    void Awake()
    {
        _rt = GetComponent<RectTransform>();
        if (iconImage == null) iconImage = GetComponentInChildren<Image>();
        if (iconImage) iconImage.color = normalIcon;
        if (bgImage)   bgImage.color   = normalBg;
    }

    public void OnPointerEnter(PointerEventData _) => Anim(hoverIcon,   hoverBg,   hoverScale);
    public void OnPointerExit (PointerEventData _) => Anim(normalIcon,  normalBg,  1f);
    public void OnPointerDown (PointerEventData _) => Anim(pressedIcon, pressedBg, pressScale);
    public void OnPointerUp   (PointerEventData _) => Anim(normalIcon,  normalBg,  1f);

    void Anim(Color ic, Color bc, float sc)
    {
        if (_iconC  != null) StopCoroutine(_iconC);
        if (_bgC    != null) StopCoroutine(_bgC);
        if (_scaleC != null) StopCoroutine(_scaleC);
        if (iconImage) _iconC  = StartCoroutine(UIAnimUtil.LerpColor(iconImage, ic, animSpeed));
        if (bgImage)   _bgC    = StartCoroutine(UIAnimUtil.LerpColor(bgImage,   bc, animSpeed));
        _scaleC = StartCoroutine(UIAnimUtil.LerpScale(_rt, sc, animSpeed));
    }
}
