using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// ─────────────────────────────────────────────────────────────────
/// <summary>
/// 게임에 존재하는 모든 ItemData 를 등록하는 데이터베이스.
/// QuestDb(퀘스트)와 동일한 패턴으로, "문자열 ID → ItemData" 매핑을 제공한다.
///
/// ── 왜 필요한가 ──────────────────────────────────────────────────
///   AI NPC 의 GIVE_ITEM, 전투 드롭(Coin/Heart/Weapon 등), 퀘스트 보상이
///   모두 "아이템 ID(문자열)"만 들고 있고, 그 ID 로 실제 ItemData 를 찾을
///   방법이 어디에도 없었다. 그 빈자리를 채우는 조회 DB.
///
/// ── 사용법 ───────────────────────────────────────────────────────
///   1) Project 우클릭 → Create → RPG/Item Database 로 에셋 생성
///   2) 생성된 에셋의 allItems 리스트에 모든 ItemData 를 드래그 등록
///   3) 각 ItemData 의 itemId 가 비어 있지 않은지 확인(고유해야 함)
///   4) 이 DB 를 사용하는 쪽(브리지/드롭 수집기 등)의 Inspector 슬롯에 연결
///      또는 씬에서 한 번 MarkActive() 를 호출하면 ItemDb.Active 로도 접근 가능.
/// </summary>
[CreateAssetMenu(fileName = "ItemDatabase", menuName = "RPG/Item Database")]
public class ItemDb : ScriptableObject
{
    [Tooltip("게임에 존재하는 모든 ItemData 를 여기에 등록하세요.")]
    public List<ItemData> allItems = new();

    /// <summary>
    /// (선택) 전역 접근용. Inspector 참조를 일일이 연결하기 번거로운
    /// 호출부(예: AI/전투 브리지)를 위해, 활성 ItemDb 를 하나 들고 있는다.
    /// QuestDb 처럼 Inspector 참조 방식이 기본이며, 이건 보조 수단이다.
    /// </summary>
    public static ItemDb Active { get; private set; }

    /// <summary>이 DB 를 전역 활성 DB 로 지정한다(씬 설치 시 1회 호출).</summary>
    public void MarkActive() => Active = this;

    /// <summary>itemId 로 ItemData 를 찾는다. 없으면 null.</summary>
    public ItemData FindById(string itemId)
        => allItems.FirstOrDefault(i => i != null && i.itemId == itemId);

    /// <summary>itemId 로 찾아 인벤토리에 추가까지 시도한다. 성공 여부 반환.</summary>
    public bool TryGiveToInventory(string itemId, int amount = 1)
    {
        var data = FindById(itemId);
        if (data == null)
        {
            Debug.LogWarning($"[ItemDb] itemId '{itemId}' 를 찾을 수 없음.");
            return false;
        }
        if (Inventory.Instance == null)
        {
            Debug.LogWarning("[ItemDb] Inventory.Instance 없음 — 지급 실패.");
            return false;
        }
        return Inventory.Instance.AddItem(data, amount);
    }

#if UNITY_EDITOR
    [ContextMenu("등록된 아이템 목록 출력")]
    void PrintAll()
    {
        foreach (var i in allItems)
            Debug.Log(i != null ? $"  {i.itemId}  :  {i.itemName}" : "  (null)");
    }

    [ContextMenu("itemId 중복/누락 검사")]
    void ValidateIds()
    {
        var seen = new HashSet<string>();
        foreach (var i in allItems)
        {
            if (i == null) { Debug.LogWarning("  (null 항목)"); continue; }
            if (string.IsNullOrEmpty(i.itemId)) Debug.LogWarning($"  itemId 누락: {i.itemName}");
            else if (!seen.Add(i.itemId))       Debug.LogWarning($"  itemId 중복: {i.itemId}");
        }
        Debug.Log("[ItemDb] 검사 완료.");
    }
#endif
}
