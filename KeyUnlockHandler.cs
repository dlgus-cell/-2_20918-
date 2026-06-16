using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 열쇠(ItemType.Key)를 "사용"하면 시스템을 즉시 해금한다.
/// (열쇠 방 없이 — 인벤토리/핫바에서 열쇠 사용 → 해금)
///
/// [동작] Inventory.OnItemUsed 구독 → 사용된 게 Key 면:
///   1) mappings 에 itemId 매핑이 있으면 그 시스템들을 해금
///   2) 매핑이 없으면 sequentialOrder 에서 아직 안 풀린 첫 시스템을 해금
///   (둘 다 지원하니, 열쇠 종류별 지정/순차 어느 방식이든 인스펙터에서 선택)
///
/// [부착] 씬에 하나(예: 매니저 오브젝트나 HUDTopBar 옆).
/// </summary>
public class KeyUnlockHandler : MonoBehaviour
{
    [System.Serializable]
    public class KeyMapping
    {
        [Tooltip("ItemData.itemId")]
        public string itemId;
        public HUDTopBar.SystemType[] systems;
    }

    [Header("열쇠별 해금 대상 (itemId → 시스템들). 우선 적용")]
    [SerializeField] private List<KeyMapping> mappings = new();

    [Header("매핑 없는 열쇠는 순서대로 다음 잠긴 시스템 하나 해금")]
    [SerializeField] private HUDTopBar.SystemType[] sequentialOrder =
    {
        HUDTopBar.SystemType.Inventory,
        HUDTopBar.SystemType.Shop,
        HUDTopBar.SystemType.StatusWindow,
        HUDTopBar.SystemType.Save
    };

    [Header("HUDTopBar 참조 (비우면 자동 탐색)")]
    [SerializeField] private HUDTopBar hud;

    void Awake()
    {
        if (hud == null)
#if UNITY_2023_1_OR_NEWER
            hud = FindAnyObjectByType<HUDTopBar>();
#else
            hud = FindObjectOfType<HUDTopBar>();
#endif
    }

    void OnEnable()  => Inventory.OnItemUsed += OnItemUsed;
    void OnDisable() => Inventory.OnItemUsed -= OnItemUsed;

    void OnItemUsed(ItemData item)
    {
        if (item == null || item.itemType != ItemType.Key) return;

        if (hud == null)
        {
#if UNITY_2023_1_OR_NEWER
            hud = FindAnyObjectByType<HUDTopBar>();
#else
            hud = FindObjectOfType<HUDTopBar>();
#endif
        }
        if (hud == null) { Debug.LogWarning("[KeyUnlockHandler] HUDTopBar 없음 — 해금 불가"); return; }

        // 1) itemId 매핑 우선
        foreach (var m in mappings)
        {
            if (m != null && m.itemId == item.itemId && m.systems != null && m.systems.Length > 0)
            {
                hud.UnlockSystems(m.systems);
                Debug.Log($"[KeyUnlockHandler] '{item.itemName}' → 지정 시스템 해금");
                return;
            }
        }

        // 2) 매핑 없으면 순서상 다음 잠긴 시스템 하나 해금
        foreach (var sys in sequentialOrder)
        {
            if (!hud.IsUnlocked(sys))
            {
                hud.UnlockSystem(sys);
                Debug.Log($"[KeyUnlockHandler] '{item.itemName}' → 다음 시스템 해금: {sys}");
                return;
            }
        }

        Debug.Log("[KeyUnlockHandler] 해금할 시스템이 더 없음(이미 전부 해금).");
    }
}
