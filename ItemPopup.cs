using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

// ─────────────────────────────────────────────────────────────────
/// <summary>
/// 아이템 슬롯 클릭 시 나타나는 컨텍스트 팝업.
/// 설명은 항상, 사용/장착은 아이템 타입에 따라 표시.
/// (구버전 ItemContextMenuUI)
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class ItemPopup : MonoBehaviour
{
    [Header("텍스트")]
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text descriptionText;

    [Header("버튼")]
    [SerializeField] private Button useButton;
    [SerializeField] private Button equipButton;
    [SerializeField] private Button closeButton;

    [Header("팝업 오프셋 (슬롯 기준)")]
    [SerializeField] private Vector2 popupOffset = new Vector2(110f, 0f);

    private CanvasGroup _cg;
    private int         _currentSlotIndex = -1;
    private Canvas      _rootCanvas;

    void Awake()
    {
        _cg         = GetComponent<CanvasGroup>();
        _rootCanvas = GetComponentInParent<Canvas>();

        useButton?.onClick.AddListener(OnUseClicked);
        equipButton?.onClick.AddListener(OnEquipClicked);
        closeButton?.onClick.AddListener(Hide);

        Hide();
    }

    void Update()
    {
        if (_cg.alpha > 0 && Input.GetKeyDown(KeyCode.Escape))
            Hide();
    }

    public void Show(int slotIndex, InventorySlot slot, RectTransform anchorRT)
    {
        if (slot == null || slot.IsEmpty) return;

        _currentSlotIndex = slotIndex;

        if (itemNameText)    itemNameText.text    = slot.item.itemName;
        if (descriptionText) descriptionText.text = slot.item.description;

        if (useButton)
            useButton.gameObject.SetActive(slot.item.itemType == ItemType.Consumable);

        if (equipButton)
            equipButton.gameObject.SetActive(slot.item.itemType == ItemType.Equippable);

        PositionNearSlot(anchorRT);
        SetVisible(true);

        Canvas.ForceUpdateCanvases();
        ClampToScreen();
    }

    public void Hide()
    {
        SetVisible(false);
        _currentSlotIndex = -1;
    }

    void OnUseClicked()
    {
        if (_currentSlotIndex < 0) return;
        Inventory.Instance?.UseItem(_currentSlotIndex);
        Hide();
    }

    void OnEquipClicked()
    {
        if (_currentSlotIndex < 0) return;
        var slot = Inventory.Instance?.GetSlot(_currentSlotIndex);
        if (slot == null || slot.IsEmpty) return;

        bool equipped = HotbarManager.Instance?.EquipItem(slot.item) ?? false;

        if (equipped)
            Debug.Log($"[ItemPopup] 장착: {slot.item.itemName} → 핫바");
        else
            Debug.Log("[ItemPopup] 핫바가 가득 찼습니다.");

        Hide();
    }

    void PositionNearSlot(RectTransform anchorRT)
    {
        var rt = transform as RectTransform;
        if (rt == null || anchorRT == null) return;

        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, anchorRT.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rootCanvas.transform as RectTransform,
            screenPos,
            _rootCanvas.worldCamera,
            out Vector2 localPos
        );

        rt.anchoredPosition = localPos + popupOffset;
    }

    void ClampToScreen()
    {
        var rt = transform as RectTransform;
        if (rt == null || _rootCanvas == null) return;

        var canvasRT   = _rootCanvas.transform as RectTransform;
        var canvasSize = canvasRT.rect.size;
        var panelSize  = rt.rect.size;
        var pos        = rt.anchoredPosition;

        float halfW = canvasSize.x * 0.5f;
        float halfH = canvasSize.y * 0.5f;

        pos.x = Mathf.Clamp(pos.x, -halfW + panelSize.x * 0.5f, halfW - panelSize.x * 0.5f);
        pos.y = Mathf.Clamp(pos.y, -halfH + panelSize.y * 0.5f, halfH - panelSize.y * 0.5f);

        rt.anchoredPosition = pos;
    }

    void SetVisible(bool v)
    {
        _cg.alpha          = v ? 1f : 0f;
        _cg.blocksRaycasts = v;
        _cg.interactable   = v;
    }
}
