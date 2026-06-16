using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 퀘스트 데이터 정의 + 퀘스트 데이터베이스 통합.
/// (구버전 QuestData + QuestDatabase 통합)
///
/// [생성 방법]
///   Project 우클릭 → Create → Quest System → Quest
/// </summary>
[CreateAssetMenu(fileName = "NewQuest", menuName = "Quest System/Quest")]
public class QuestDef : ScriptableObject
{
    [Header("기본 정보")]
    [Tooltip("고유 ID. 다른 퀘스트와 절대 겹치면 안 됩니다.")]
    public string questID   = "quest_001";
    public string questName = "새 퀘스트";

    [TextArea(3, 6)]
    public string description = "퀘스트 설명을 입력하세요.";

    [Header("퀘스트 단계 (순서대로 진행됩니다)")]
    public List<QuestStageData> stages = new List<QuestStageData>();

    [Header("완료 보상")]
    public QuestRewardData reward;
}
