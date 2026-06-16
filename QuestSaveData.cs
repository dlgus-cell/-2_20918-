using System;
using System.Collections.Generic;

/// <summary>퀘스트 전체 상태.</summary>
[Serializable]
public class QuestSaveData
{
    public List<ActiveQuestRecord>  activeQuests     = new();
    public List<string>             completedQuestIDs = new();
}
