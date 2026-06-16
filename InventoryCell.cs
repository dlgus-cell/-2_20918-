using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// 인벤토리 그리드 한 칸의 UI.
/// 호버/누름 시 배경 스프라이트를 교체한다(색 강조·스케일 없음).
/// 아이템 아이콘은 따로 지정(ItemData.icon).
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class InventoryCell : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler,
    IPointerClickHandler
{
    [Header("References")]
    [SerializeField] private Image    backgroundImage;
    [SerializeField] private Image    iconImage;
    [SerializeField] private TMP_Text amountText;

    [Header("상태 스프라이트 (배경 교체)")]
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite hoverSprite;
    [SerializeField] private Sprite pressedSprite;

    [Header("아이콘 표시")]
    [SerializeField] private Color emptyIconColor  = new Color(1, 1, 1, 0);
    [SerializeField] private Color filledIconColor = Color.white;

    private InventorySlot _slot;
    private int           _slotIndex;
    private bool          _isEmpty = true;
    private bool          _isHover;

    /// <summary>클릭 시 외부에서 구독 (슬롯 인덱스 + 자신)</summary>
    public event Action<int, InventoryCell> OnClicked;

    void Start()
    {
        SetBg(normalSprite);
        if (iconImage)  iconImage.color = emptyIconColor;
        if (amountText) amountText.text = "";
    }

    public void Setup(int index, InventorySlot slot)
    {
        _slotIndex = index;
        _slot      = slot;
        _isEmpty   = slot == null || slot.IsEmpty;

        if (iconImage)
        {
            iconImage.sprite = _isEmpty ? null : slot.item.icon;
            iconImage.color  = _isEmpty ? emptyIconColor : filledIconColor;
        }
        if (amountText)
            amountText.text = (!_isEmpty && slot.amount > 1) ? slot.amount.ToString() : "";
    }

    public void OnPointerEnter(PointerEventData _) { _isHover = true;  SetBg(hoverSprite); }
    public void OnPointerExit (PointerEventData _) { _isHover = false; SetBg(normalSprite); }
    public void OnPointerDown (PointerEventData _) { SetBg(pressedSprite); }
    public void OnPointerUp   (PointerEventData _) { SetBg(_isHover ? hoverSprite : normalSprite); }

    public void OnPointerClick(PointerEventData _)
    {
        if (_isEmpty) return;
        OnClicked?.Invoke(_slotIndex, this);
    }

    void SetBg(Sprite s)
    {
        if (backgroundImage && s != null) backgroundImage.sprite = s;
    }
}
