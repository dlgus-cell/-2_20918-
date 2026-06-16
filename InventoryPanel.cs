using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// 인벤토리 패널 컨트롤러. B키로 토글.
/// (구버전 InventoryUI)
/// </summary>
public class InventoryPanel : MonoBehaviour
{
    [Header("패널 루트 (열고/닫힐 GameObject)")]
    [SerializeField] private GameObject panelRoot;

    [Header("슬롯 프리팹 + 부모")]
    [SerializeField] private InventoryCell slotPrefab;
    [SerializeField] private Transform     gridParent;

    [Header("컨텍스트 메뉴")]
    [SerializeField] private ItemPopup contextMenu;

    [Header("열기 키")]
    [SerializeField] private KeyCode toggleKey = KeyCode.B;

    [Header("해금 게이팅")]
    [Tooltip("켜면 시스템이 해금되기 전엔 인벤토리가 열리지 않는다.")]
    [SerializeField] private bool requireUnlock = true;
    [SerializeField] private HUDTopBar.SystemType systemType = HUDTopBar.SystemType.Inventory;

    private List<InventoryCell> _slotUIs = new();
    private bool _isOpen = false;

    void OnEnable()
    {
        Inventory.OnInventoryChanged += RefreshAll;
        Inventory.OnSlotUpdated      += RefreshSlot;
    }

    void OnDisable()
    {
        Inventory.OnInventoryChanged -= RefreshAll;
        Inventory.OnSlotUpdated      -= RefreshSlot;
    }

    void Start()
    {
        BuildSlots();
        SetPanelVisible(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey)) Toggle();
    }

    public void Toggle()
    {
        // 해금 전에는 열기 차단(닫기는 허용).
        if (!_isOpen && requireUnlock && !HUDTopBar.IsSystemUnlocked(systemType))
        {
            Debug.Log($"[InventoryPanel] '{systemType}' 미해금 — 열기 차단");
            return;
        }
        _isOpen = !_isOpen;
        SetPanelVisible(_isOpen);

        if (_isOpen) RefreshAll();
        else         contextMenu?.Hide();
    }

    public void Close()
    {
        _isOpen = false;
        SetPanelVisible(false);
        contextMenu?.Hide();
    }

    void SetPanelVisible(bool visible)
    {
        if (panelRoot) panelRoot.SetActive(visible);
    }

    void BuildSlots()
    {
        if (slotPrefab == null || gridParent == null) return;

        int count = Inventory.Instance != null ? Inventory.Instance.MaxSlots : 30;

        for (int i = 0; i < count; i++)
        {
            var slotUI = Instantiate(slotPrefab, gridParent);
            slotUI.OnClicked += OnSlotClicked;
            slotUI.Setup(i, null);
            _slotUIs.Add(slotUI);
        }
    }

    void RefreshAll()
    {
        if (Inventory.Instance == null) return;
        var slots = Inventory.Instance.GetAllSlots();

        for (int i = 0; i < _slotUIs.Count; i++)
        {
            var slot = i < slots.Count ? slots[i] : null;
            _slotUIs[i].Setup(i, slot);
        }
    }

    void RefreshSlot(int index, InventorySlot slot)
    {
        if (index < 0 || index >= _slotUIs.Count) return;
        _slotUIs[index].Setup(index, slot);
    }

    void OnSlotClicked(int index, InventoryCell slotUI)
    {
        var slot = Inventory.Instance?.GetSlot(index);
        if (slot == null || slot.IsEmpty) return;

        contextMenu?.Show(index, slot, slotUI.GetComponent<RectTransform>());
    }
}
