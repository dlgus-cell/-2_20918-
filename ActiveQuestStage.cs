using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ActiveQuestStage
{
    public string stageTitle;
    public string stageDescription;
    public List<ActiveObjective> objectives;
    public string  destinationName;
    public Vector3 destinationWorldPosition;
    public bool    hasDestination;
}
