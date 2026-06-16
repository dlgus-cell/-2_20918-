using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class QuestObjectiveData
{
    [Tooltip("UpdateObjective() 호출 시 사용할 고유 ID")]
    public string objectiveID    = "obj_001";
    public string description    = "목표 설명";
    public int    requiredAmount = 1;
}
