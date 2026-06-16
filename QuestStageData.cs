using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class QuestStageData
{
    [Header("단계 정보")]
    public string stageTitle       = "단계 제목";

    [TextArea(2, 4)]
    public string stageDescription = "이 단계에서 해야 할 일을 설명하세요.";

    [Header("목표 목록")]
    public List<QuestObjectiveData> objectives = new List<QuestObjectiveData>();

    [Header("목적지 마커")]
    public bool    hasDestination           = true;
    public string  destinationName          = "목적지";
    public Vector3 destinationWorldPosition = Vector3.zero;
}
