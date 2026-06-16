using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

/// <summary>
/// 범용 닫기 버튼 컴포넌트.
/// 닫을 패널을 직접 지정하거나, 부모 패널을 자동 탐색합니다.
/// (구버전 UICloseButton)
/// </summary>
[RequireComponent(typeof(Button))]
public class UIPanelCloser : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler
{
    public enum CloseMode { SetInactive, DestroyPanel, CanvasGroupHide }

    [Header("닫을 패널 (비워두면 부모 자동 탐색)")]
    [SerializeField] private GameObject targetPanel;
    [SerializeField] private CloseMode  closeMode = CloseMode.SetInactive;

    [Header("버튼 이미지 (선택)")]
    [SerializeField] private Image buttonImage;

    [Header("시각 효과 색상")]
    [SerializeField] private Color normalColor  = new Color(0.75f, 0.75f, 0.75f, 1f);
    [SerializeField] private Color hoverColor   = new Color(1.00f, 0.35f, 0.35f, 1f);
    [SerializeField] private Color pressedColor = new Color(0.60f, 0.10f, 0.10f, 1f);
    [SerializeField] private float animSpeed    = 18f;

    [Header("스케일 효과")]
    [SerializeField] private float hoverScale = 1.15f;
    [SerializeField] private float pressScale = 0.88f;

    private RectTransform _rt;
    private Coroutine     _colorRoutine;
    private Coroutine     _scaleRoutine;

    void Awake()
    {
        _rt = GetComponent<RectTransform>();

        if (buttonImage == null) buttonImage = GetComponent<Image>();
        if (buttonImage != null) buttonImage.color = normalColor;

        if (targetPanel == null && transform.parent != null)
            targetPanel = transform.parent.gameObject;

        GetComponent<Button>().onClick.AddListener(Close);
    }

    public void Close()
    {
        if (targetPanel == null) return;

        switch (closeMode)
        {
            case CloseMode.SetInactive:
                targetPanel.SetActive(false);
                break;
            case CloseMode.DestroyPanel:
                Destroy(targetPanel);
                break;
            case CloseMode.CanvasGroupHide:
                if (targetPanel.TryGetComponent<CanvasGroup>(out var cg))
                {
                    cg.alpha = 0f;
                    cg.blocksRaycasts = false;
                    cg.interactable   = false;
                }
                break;
        }
    }

    public void SetTarget(GameObject panel) => targetPanel = panel;

    public void OnPointerEnter(PointerEventData _) { AnimColor(hoverColor);   AnimScale(hoverScale); }
    public void OnPointerExit (PointerEventData _) { AnimColor(normalColor);  AnimScale(1f); }
    public void OnPointerDown (PointerEventData _) { AnimColor(pressedColor); AnimScale(pressScale); }
    public void OnPointerUp   (PointerEventData _) { AnimColor(normalColor);  AnimScale(1f); }

    void AnimColor(Color target)
    {
        if (buttonImage == null) return;
        if (_colorRoutine != null) StopCoroutine(_colorRoutine);
        _colorRoutine = StartCoroutine(UIAnimUtil.LerpColor(buttonImage, target, animSpeed));
    }

    void AnimScale(float target)
    {
        if (_scaleRoutine != null) StopCoroutine(_scaleRoutine);
        _scaleRoutine = StartCoroutine(UIAnimUtil.LerpScale(_rt, target, animSpeed));
    }
}
