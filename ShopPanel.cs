using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ─────────────────────────────────────────────────────────────────
/// <summary>
/// 상점 패널 전체 관리 매니저.
/// (구버전 ShopManager)
/// </summary>
public class ShopPanel : MonoBehaviour
{
    [Header("패널 & 버튼")]
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private Button     toggleButton;
    [SerializeField] private Button     closeButton;

    [Header("스크롤 리스트")]
    [SerializeField] private Transform  itemContainer;
    [SerializeField] private GameObject itemRowPrefab;

    [Header("아이템 데이터")]
    [SerializeField] private ShopItemDef[] items;

    [Header("해금 게이팅")]
    [Tooltip("켜면 시스템이 해금되기 전엔 상점이 열리지 않는다.")]
    [SerializeField] private bool requireUnlock = true;
    [SerializeField] private HUDTopBar.SystemType systemType = HUDTopBar.SystemType.Shop;

    private bool _isOpen = false;
    private readonly List<ShopRow> _spawnedRows = new();

    void Awake()
    {
        if (toggleButton != null) toggleButton.onClick.AddListener(ToggleShop);
        if (closeButton  != null) closeButton.onClick.AddListener(CloseShop);
    }

    void Start()
    {
        SetPanelVisible(false);
        BuildItemList();
    }

    public void ToggleShop() => SetPanelVisible(!_isOpen);
    public void OpenShop()   => SetPanelVisible(true);
    public void CloseShop()  => SetPanelVisible(false);

    void SetPanelVisible(bool visible)
    {
        // 해금 전에는 열기 차단(닫기는 항상 허용).
        if (visible && requireUnlock && !HUDTopBar.IsSystemUnlocked(systemType))
        {
            Debug.Log($"[ShopPanel] '{systemType}' 미해금 — 열기 차단");
            return;
        }
        _isOpen = visible;
        if (shopPanel != null) shopPanel.SetActive(visible);
    }

    void BuildItemList()
    {
        if (itemContainer == null || itemRowPrefab == null) return;

        foreach (var row in _spawnedRows)
            if (row != null) Destroy(row.gameObject);
        _spawnedRows.Clear();

        if (items == null) return;

        foreach (var item in items)
        {
            if (item == null) continue;

            GameObject go = Instantiate(itemRowPrefab, itemContainer);
            ShopRow row   = go.GetComponent<ShopRow>();

            if (row != null)
            {
                row.Initialize(item);
                _spawnedRows.Add(row);
            }
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
            ToggleShop();

        if (Input.GetKeyDown(KeyCode.Escape) && _isOpen)
            CloseShop();
    }
}
