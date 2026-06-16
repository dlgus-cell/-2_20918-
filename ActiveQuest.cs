using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// ═══════════════════════════════════════════════════════════════════════
//  런타임 퀘스트 상태 클래스 (원본 ScriptableObject를 변경하지 않음)
// ═══════════════════════════════════════════════════════════════════════

public class ActiveQuest
{
    public QuestDef questData;
    public int       currentStageIndex;
    public bool      isCompleted;
    public List<ActiveQuestStage> stages;

    public ActiveQuest(QuestDef data)
    {
        questData         = data;
        currentStageIndex = 0;
        isCompleted       = false;
        stages = data.stages.Select(s => new ActiveQuestStage
        {
            stageTitle               = s.stageTitle,
            stageDescription         = s.stageDescription,
            destinationName          = s.destinationName,
            destinationWorldPosition = s.destinationWorldPosition,
            hasDestination           = s.hasDestination,
            objectives = s.objectives.Select(o => new ActiveObjective
            {
                objectiveID    = o.objectiveID,
                description    = o.description,
                requiredAmount = o.requiredAmount,
                currentAmount  = 0
            }).ToList()
        }).ToList();
    }

    public ActiveQuestStage GetCurrentStage()
    {
        if (stages == null || currentStageIndex >= stages.Count) return null;
        return stages[currentStageIndex];
    }
}
