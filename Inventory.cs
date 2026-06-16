using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 인벤토리 핵심 싱글톤.
///
/// ★ 통폐합 변경 (6단계):
///   - HasSpaceFor(ItemData, int) 추가
///     → 구매 전 인벤토리 여유 공간을 사전 체크하여
///       "골드 차감 후 아이템 지급 실패" 버그 방지
/// </summary>
public class Inventory : MonoBehaviour
{
    // ─── 싱글톤 ───────────────────────────────────────────────────
    public static Inventory Instance { get; private set; }

    // ─── 이벤트 ───────────────────────────────────────────────────
    public static event Action<int, InventorySlot> OnSlotUpdated;
    public static event Action                     OnInventoryChanged;
    public static event Action<ItemData>           OnItemUsed;

    // ─── Inspector ────────────────────────────────────────────────
    [Header("슬롯 최대 개수")]
    [SerializeField] private int maxSlots = 30;

    // ─── 내부 상태 ────────────────────────────────────────────────
    private List<InventorySlot> _slots = new();

    public int MaxSlots  => maxSlots;
    public int SlotCount => _slots.Count;

    // ─────────────────────────────────────────────────────────────
    #region Unity Lifecycle

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        for (int i = 0; i < maxSlots; i++)
            _slots.Add(new InventorySlot(null, 0));
    }

    #endregion

    // ─────────────────────────────────────────────────────────────
    #region 공개 API

    /// <summary>아이템을 인벤토리에 추가합니다.</summary>
    public bool AddItem(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0) return false;

        // 스택 가능 아이템: 기존 슬롯에 합산
        if (item.maxStack > 1)
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].item != item || _slots[i].amount >= item.maxStack) continue;

                int add = Mathf.Min(amount, item.maxStack - _slots[i].amount);
                _slots[i].amount += add;
                amount -= add;
                OnSlotUpdated?.Invoke(i, _slots[i]);
                if (amount <= 0) { OnInventoryChanged?.Invoke(); return true; }
            }
        }

        // 빈 슬롯에 배치
        while (amount > 0)
        {
            int emptyIdx = _slots.FindIndex(s => s.IsEmpty);
            if (emptyIdx < 0)
            {
                Debug.Log("[Inventory] 인벤토리가 가득 찼습니다.");
                OnInventoryChanged?.Invoke();
                return false;
            }

            int stackAmt      = Mathf.Min(amount, item.maxStack);
            _slots[emptyIdx]  = new InventorySlot(item, stackAmt);
            amount           -= stackAmt;
            OnSlotUpdated?.Invoke(emptyIdx, _slots[emptyIdx]);
        }

        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>아이템을 인벤토리에서 제거합니다.</summary>
    public bool RemoveItem(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0) return false;

        for (int i = 0; i < _slots.Count && amount > 0; i++)
        {
            if (_slots[i].item != item) continue;

            int remove       = Mathf.Min(amount, _slots[i].amount);
            _slots[i].amount -= remove;
            amount           -= remove;

            if (_slots[i].amount <= 0)
                _slots[i] = new InventorySlot(null, 0);

            OnSlotUpdated?.Invoke(i, _slots[i]);
        }

        OnInventoryChanged?.Invoke();
        return amount == 0;
    }

    /// <summary>슬롯 인덱스의 아이템을 사용합니다. 소비/열쇠만 동작.</summary>
    public bool UseItem(int slotIndex)
    {
        if (!IsValidIndex(slotIndex)) return false;
        var slot = _slots[slotIndex];
        if (slot.IsEmpty) return false;

        if (slot.item.itemType == ItemType.Consumable)
        {
            OnItemUsed?.Invoke(slot.item);
            ApplyConsumableEffect(slot.item);
            RemoveItem(slot.item, 1);
            Debug.Log($"[Inventory] 사용: {slot.item.itemName}");
            return true;
        }

        // 열쇠: 효과 적용은 없고, 구독자(KeyUnlockHandler)가 OnItemUsed 로 받아 해금. 사용 시 1개 소모.
        if (slot.item.itemType == ItemType.Key)
        {
            OnItemUsed?.Invoke(slot.item);
            RemoveItem(slot.item, 1);
            Debug.Log($"[Inventory] 열쇠 사용: {slot.item.itemName}");
            return true;
        }

        return false;
    }

    /// <summary>해당 아이템을 가진 첫 슬롯을 사용한다(핫바에서 호출). 소비/열쇠만 동작.</summary>
    public bool UseItemByData(ItemData item)
    {
        if (item == null) return false;
        for (int i = 0; i < _slots.Count; i++)
            if (!_slots[i].IsEmpty && _slots[i].item == item)
                return UseItem(i);
        return false;
    }

    // ── 조회 ──────────────────────────────────────────────────────

    public InventorySlot           GetSlot(int index) => IsValidIndex(index) ? _slots[index] : null;
    public IReadOnlyList<InventorySlot> GetAllSlots() => _slots;
    public int  GetItemCount(ItemData item)            => _slots.Where(s => s.item == item).Sum(s => s.amount);
    public bool HasItem(ItemData item, int amount = 1) => GetItemCount(item) >= amount;

    // ── ★ 신규: 구매 전 여유 공간 사전 체크 ──────────────────────

    /// <summary>
    /// 해당 아이템을 amount개 추가할 공간이 있는지 확인합니다.
    /// ShopItemDef.TryPurchase()에서 골드 차감 전에 호출하여
    /// "골드만 사라지는 버그"를 방지합니다.
    /// </summary>
    public bool HasSpaceFor(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0) return false;

        int remaining = amount;

        // 기존 스택에 넣을 수 있는 양 계산
        if (item.maxStack > 1)
        {
            foreach (var slot in _slots)
            {
                if (slot.item != item || slot.amount >= item.maxStack) continue;
                remaining -= (item.maxStack - slot.amount);
                if (remaining <= 0) return true;
            }
        }

        // 추가로 필요한 빈 슬롯 수
        int emptyCount  = _slots.Count(s => s.IsEmpty);
        int slotsNeeded = Mathf.CeilToInt((float)remaining / Mathf.Max(1, item.maxStack));
        return emptyCount >= slotsNeeded;
    }

    // ── 저장/복원 ─────────────────────────────────────────────────

    /// <summary>인벤토리 내용을 저장용 레코드로 내보낸다(빈 슬롯 제외, 위치 보존).</summary>
    public List<InventorySlotRecord> GetSaveData()
    {
        var list = new List<InventorySlotRecord>();
        for (int i = 0; i < _slots.Count; i++)
        {
            var s = _slots[i];
            if (s.IsEmpty) continue;
            if (string.IsNullOrEmpty(s.item.itemId))
            {
                Debug.LogWarning($"[Inventory] '{s.item.itemName}' 에 itemId 가 없어 저장에서 제외됩니다.");
                continue;
            }
            list.Add(new InventorySlotRecord { slotIndex = i, itemId = s.item.itemId, amount = s.amount });
        }
        return list;
    }

    /// <summary>저장된 인벤토리 내용을 복원한다. itemId 조회를 위해 ItemDb 가 필요.</summary>
    public void LoadSaveData(List<InventorySlotRecord> records, ItemDb db)
    {
        // 전체 비우기
        for (int i = 0; i < _slots.Count; i++)
        {
            _slots[i] = new InventorySlot(null, 0);
            OnSlotUpdated?.Invoke(i, _slots[i]);
        }

        if (db == null)
        {
            Debug.LogWarning("[Inventory] ItemDb 가 없어 인벤토리를 복원할 수 없습니다.");
            OnInventoryChanged?.Invoke();
            return;
        }

        if (records != null)
        {
            foreach (var r in records)
            {
                if (r == null || string.IsNullOrEmpty(r.itemId)) continue;
                var item = db.FindById(r.itemId);
                if (item == null)
                {
                    Debug.LogWarning($"[Inventory] itemId '{r.itemId}' 를 ItemDb 에서 찾을 수 없어 건너뜁니다.");
                    continue;
                }
                int amt = Mathf.Max(1, r.amount);
                if (r.slotIndex >= 0 && r.slotIndex < _slots.Count)
                {
                    _slots[r.slotIndex] = new InventorySlot(item, amt);
                    OnSlotUpdated?.Invoke(r.slotIndex, _slots[r.slotIndex]);
                }
                else
                {
                    AddItem(item, amt); // 슬롯 인덱스가 범위를 벗어나면 그냥 추가
                }
            }
        }

        OnInventoryChanged?.Invoke();
    }

    #endregion

    // ─────────────────────────────────────────────────────────────
    #region 내부

    bool IsValidIndex(int idx) => idx >= 0 && idx < _slots.Count;

    void ApplyConsumableEffect(ItemData item)
    {
        var fx = item.consumableEffect;
        if (fx == null) return;

        if (fx.addGold > 0)
            GoldSystem.Instance?.AddGold(fx.addGold, GoldChangeReason.Misc);

        // ◀ [연결 완료] HP/기력 회복을 정본 PlayerVitals로 연결.
        //   골드는 위에서 이미 처리하므로 여기선 HP/기력만 적용(중복 지급 방지).
        if (PlayerVitals.Instance != null)
        {
            if (fx.restoreHp > 0) PlayerVitals.Instance.Heal(fx.restoreHp);
            if (fx.restoreMp > 0) PlayerVitals.Instance.RestoreStamina(fx.restoreMp);
        }
        Debug.Log($"[Inventory] 효과 적용 → HP+{fx.restoreHp} MP+{fx.restoreMp} G+{fx.addGold}");
    }

    #endregion
}
