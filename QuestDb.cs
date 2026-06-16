using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// ─────────────────────────────────────────────────────────────────
/// <summary>
/// 게임에 존재하는 모든 QuestDef를 등록하는 데이터베이스.
/// (구버전 QuestDatabase 통합)
///
/// [사용법]
///   Project 우클릭 → Create → Quest System → Quest Database
///   생성된 에셋에 모든 QuestDef를 드래그해서 등록하세요.
///   SaveSystem Inspector의 questDatabase 슬롯에 연결하세요.
/// </summary>
[CreateAssetMenu(fileName = "QuestDatabase", menuName = "Quest System/Quest Database")]
public class QuestDb : ScriptableObject
{
    [Tooltip("게임에 존재하는 모든 QuestDef를 여기에 등록하세요.")]
    public List<QuestDef> allQuests = new();

    public QuestDef FindByID(string questID)
        => allQuests.FirstOrDefault(q => q != null && q.questID == questID);

#if UNITY_EDITOR
    [ContextMenu("등록된 퀘스트 목록 출력")]
    void PrintAll()
    {
        foreach (var q in allQuests)
            Debug.Log(q != null ? $"  {q.questID}  :  {q.questName}" : "  (null)");
    }
#endif
}
