using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ─────────────────────────────────────────────────────────────────
/// <summary>
/// 상점 아이템 한 줄(행) UI.
/// (구버전 ShopItemUI)
/// </summary>
public class ShopRow : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private Image           iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descText;
    [SerializeField] private TextMeshProUGUI categoryText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Button          buyButton;
    [SerializeField] private TextMeshProUGUI buyButtonText;

    [Header("버튼 색상")]
    [SerializeField] private Color colorCanBuy        = new Color(0.2f,  0.6f,  1f);
    [SerializeField] private Color colorNoGold        = new Color(0.5f,  0.5f,  0.5f);
    [SerializeField] private Color colorInventoryFull = new Color(0.7f,  0.45f, 0.1f);

    private ShopItemDef _data;

    void OnEnable()
    {
        GoldSystem.OnGoldChanged     += OnGoldChanged;
        Inventory.OnInventoryChanged += OnInventoryChanged;
    }

    void OnDisable()
    {
        GoldSystem.OnGoldChanged     -= OnGoldChanged;
        Inventory.OnInventoryChanged -= OnInventoryChanged;
    }

    /// <summary>데이터를 주입하고 UI를 초기화합니다.</summary>
    public void Initialize(ShopItemDef item)
    {
        _data = item;

        if (iconImage)
        {
            iconImage.sprite  = item.icon;
            iconImage.enabled = item.icon != null;
        }

        if (nameText)     nameText.text     = item.itemName;
        if (descText)     descText.text     = item.description;
        if (categoryText) categoryText.text = item.category.ToString();
        if (priceText)    priceText.text    = $"{item.price:N0} G";

        if (buyButton)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(OnBuyClicked);
        }

        if (buyButtonText) buyButtonText.text = "구매";

        RefreshAffordability();
    }

    /// <summary>구버전 호환 오버로드</summary>
    public void Initialize(ShopItemDef item, Action<ShopItemDef> onBuy)
    {
        Initialize(item);
        buyButton?.onClick.AddListener(() => onBuy?.Invoke(_data));
    }

    public void RefreshAffordability()
    {
        if (_data == null || buyButton == null) return;

        var img = buyButton.GetComponent<Image>();

        if (!_data.CanAffordGold)
        {
            buyButton.interactable = false;
            if (img) img.color = colorNoGold;
            SetStatus("골드 부족", new Color(1f, 0.4f, 0.4f));
        }
        else if (!_data.HasInventorySpace)
        {
            buyButton.interactable = false;
            if (img) img.color = colorInventoryFull;
            SetStatus("인벤토리 가득참", new Color(1f, 0.7f, 0.2f));
        }
        else
        {
            buyButton.interactable = true;
            if (img) img.color = colorCanBuy;
            SetStatus("", Color.white);
        }
    }

    void OnBuyClicked()
    {
        if (_data == null) return;

        _data.TryPurchase(
            onSuccess: () =>
            {
                if (buyButtonText) buyButtonText.text = "구매 완료";
                SetStatus("", Color.white);
                RefreshAffordability();
            },
            onFail: result =>
            {
                switch (result)
                {
                    case PurchaseResult.NotEnoughGold:
                        SetStatus("골드 부족!", new Color(1f, 0.4f, 0.4f));
                        break;
                    case PurchaseResult.InventoryFull:
                        SetStatus("인벤토리 가득참!", new Color(1f, 0.7f, 0.2f));
                        break;
                    default:
                        SetStatus("구매 실패", Color.gray);
                        break;
                }
                RefreshAffordability();
            }
        );
    }

    void OnGoldChanged(int total, int delta, GoldChangeReason reason) => RefreshAffordability();
    void OnInventoryChanged() => RefreshAffordability();

    void SetStatus(string msg, Color color)
    {
        if (statusText == null) return;
        statusText.text  = msg;
        statusText.color = color;
        statusText.gameObject.SetActive(!string.IsNullOrEmpty(msg));
    }
}
