using System;
using System.Collections.Generic;

/// <summary>진행 중 퀘스트 하나의 스냅샷.</summary>
[Serializable]
public class ActiveQuestRecord
{
    public string              questID;
    public int                 currentStageIndex;
    public List<ObjectiveRecord> objectives = new();
}
