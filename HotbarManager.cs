using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 핫바 로직 싱글톤.
/// 칸을 미리 여러 개 띄우지 않고 "장착된 아이템 수만큼만" 칸이 생긴다(최대 maxSlots, 9).
/// 아이템을 장착하면 칸이 하나 늘고, 사용해서 소진되면 칸이 하나 줄며 뒤 칸이 당겨진다.
/// 키 1~9 / 클릭으로 즉시 사용. (선택/강조 없음)
/// </summary>
public class HotbarManager : MonoBehaviour
{
    public static HotbarManager Instance { get; private set; }

    /// <summary>핫바 구성이 바뀜(추가/제거) → 패널이 다시 그린다.</summary>
    public static event Action OnHotbarChanged;
    /// <summary>슬롯을 눌렀을 때(키 1~9 / 클릭) — 시각 피드백용.</summary>
    public static event Action<int> OnSlotPressed;
    /// <summary>슬롯 아이템 사용 성공 (인덱스, 아이템).</summary>
    public static event Action<int, ItemData> OnSlotUsed;

    [Header("최대 핫바 칸 수 (1~9)")]
    [SerializeField, Range(1, 9)] private int maxSlots = 9;

    private readonly List<ItemData> _items = new();

    private static readonly KeyCode[] _numberKeys =
    {
        KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3,
        KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6,
        KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9
    };

    public int MaxSlots => maxSlots;
    public int Count     => _items.Count;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update() => HandleKeyInput();

    /// <summary>아이템을 핫바에 추가한다(이미 있으면 무시, 가득 차면 실패).</summary>
    public bool EquipItem(ItemData item)
    {
        if (item == null) return false;
        if (_items.Contains(item)) return true;
        if (_items.Count >= maxSlots) return false;

        _items.Add(item);
        OnHotbarChanged?.Invoke();
        Debug.Log($"[Hotbar] 장착: {item.itemName} (칸 {_items.Count}/{maxSlots})");
        return true;
    }

    /// <summary>해당 칸을 제거한다(뒤 칸이 앞으로 당겨짐).</summary>
    public void UnequipAt(int index)
    {
        if (index < 0 || index >= _items.Count) return;
        _items.RemoveAt(index);
        OnHotbarChanged?.Invoke();
    }

    /// <summary>칸의 아이템을 즉시 사용. 소진되면 칸을 제거. 소비·열쇠만 실제 동작.</summary>
    public void UseSlot(int index)
    {
        if (index < 0 || index >= _items.Count) return;

        OnSlotPressed?.Invoke(index);                 // 눌림 피드백

        var item = _items[index];
        bool used = Inventory.Instance != null && Inventory.Instance.UseItemByData(item);
        if (!used) return;

        OnSlotUsed?.Invoke(index, item);
        if (Inventory.Instance != null && !Inventory.Instance.HasItem(item))
            UnequipAt(index);                          // 다 쓰면 칸 제거
    }

    public ItemData GetSlotItem(int index) =>
        (index >= 0 && index < _items.Count) ? _items[index] : null;

    private void HandleKeyInput()
    {
        int n = Mathf.Min(_items.Count, _numberKeys.Length);
        for (int i = 0; i < n; i++)
            if (Input.GetKeyDown(_numberKeys[i])) { UseSlot(i); return; }
    }
}
