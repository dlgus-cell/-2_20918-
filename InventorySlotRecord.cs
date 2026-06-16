using System;

/// <summary>인벤토리 한 슬롯의 저장 데이터. 슬롯 위치(slotIndex)와 itemId, 개수를 보존.</summary>
[Serializable]
public class InventorySlotRecord
{
    public int    slotIndex;
    public string itemId;
    public int    amount;
}
