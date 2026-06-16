/// <summary>인벤토리 슬롯 런타임 데이터</summary>
[System.Serializable]
public class InventorySlot
{
    public ItemData item;
    public int      amount;

    public InventorySlot(ItemData item, int amount)
    {
        this.item   = item;
        this.amount = amount;
    }

    public bool IsEmpty => item == null || amount <= 0;
}
