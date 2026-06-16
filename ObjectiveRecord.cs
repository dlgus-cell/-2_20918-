using System;
using System.Collections.Generic;

/// <summary>목표 진행도 스냅샷.</summary>
[Serializable]
public class ObjectiveRecord
{
    public int    stageIndex;
    public string objectiveID;
    public int    currentAmount;
}

// ─────────────────────────────────────────────────────────────────────
