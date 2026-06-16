using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 상점 아이템 데이터 + 구매 로직 ScriptableObject.
/// (구버전 ShopItem)
///
/// [생성 방법]
///   Project 우클릭 → Create → RPGShop → ShopItem
/// </summary>
[CreateAssetMenu(fileName = "NewShopItem", menuName = "RPGShop/ShopItem")]
public class ShopItemDef : ScriptableObject
{
    [Header("기본 정보")]
    public string itemName    = "아이템 이름";

    [TextArea(2, 5)]
    public string description = "아이템 설명을 입력하세요.";

    public Sprite icon;

    [Header("카테고리")]
    public ItemCategory category = ItemCategory.기타;

    [Header("가격")]
    public int price = 100;

    [Header("구매 시 지급할 아이템 (Inventory 연동)")]
    [Tooltip("연결하면 구매 성공 시 Inventory에 자동으로 추가됩니다.\n비워두면 골드만 차감됩니다.")]
    public ItemData linkedItem;

    [Tooltip("지급 수량 (기본 1)")]
    public int linkedItemAmount = 1;

    public bool CanAffordGold =>
        GoldSystem.Instance != null && GoldSystem.Instance.CanAfford(price);

    public bool HasInventorySpace =>
        linkedItem == null ||
        (Inventory.Instance != null && Inventory.Instance.HasSpaceFor(linkedItem, linkedItemAmount));

    /// <summary>골드도 충분하고 인벤토리 공간도 있을 때만 true</summary>
    public bool CanBuy => CanAffordGold && HasInventorySpace;

    /// <summary>
    /// 구매를 시도합니다. 골드 차감 전에 인벤토리 공간을 먼저 확인.
    /// </summary>
    public bool TryPurchase(Action onSuccess = null, Action<PurchaseResult> onFail = null)
    {
        if (GoldSystem.Instance == null)
        {
            Debug.LogError("[ShopItemDef] GoldSystem 인스턴스가 없습니다.");
            onFail?.Invoke(PurchaseResult.NoGoldSystem);
            return false;
        }

        if (linkedItem != null && !HasInventorySpace)
        {
            Debug.Log($"[ShopItemDef] 인벤토리 공간 부족: '{itemName}'");
            onFail?.Invoke(PurchaseResult.InventoryFull);
            return false;
        }

        bool paid = GoldSystem.Instance.SpendGold(price, GoldChangeReason.Purchase);
        if (!paid)
        {
            Debug.Log($"[ShopItemDef] 골드 부족: '{itemName}'  필요 {price}G");
            onFail?.Invoke(PurchaseResult.NotEnoughGold);
            return false;
        }

        if (linkedItem != null)
        {
            bool added = Inventory.Instance?.AddItem(linkedItem, linkedItemAmount) ?? false;
            if (!added)
            {
                GoldSystem.Instance.AddGold(price, GoldChangeReason.Misc);
                Debug.LogWarning($"[ShopItemDef] 아이템 지급 실패 → 골드 환불: +{price}G");
                onFail?.Invoke(PurchaseResult.InventoryFull);
                return false;
            }
        }

        Debug.Log($"[ShopItemDef] 구매 성공: '{itemName}'  -{price}G");
        onSuccess?.Invoke();
        return true;
    }

    /// <summary>단순 콜백 오버로드</summary>
    public bool TryPurchase(Action onSuccess, Action onFail)
        => TryPurchase(onSuccess, _ => onFail?.Invoke());
}
