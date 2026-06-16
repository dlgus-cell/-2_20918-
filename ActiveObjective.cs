using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ActiveObjective
{
    public string objectiveID;
    public string description;
    public int    requiredAmount;
    public int    currentAmount;
    public bool   IsCompleted => currentAmount >= requiredAmount;
}
