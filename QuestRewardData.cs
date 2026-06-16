using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 퀘스트 완료 보상 데이터.
/// QuestRewardPopup가 완료 시 자동으로 GoldSystem·PlayerStats에 지급합니다.
/// </summary>
[System.Serializable]
public class QuestRewardData
{
    [Tooltip("완료 시 GoldSystem.AddGold()로 자동 지급")]
    public int gold       = 100;

    [Tooltip("완료 시 PlayerStats.AddXP()로 자동 지급")]
    public int experience = 200;

    [TextArea(1, 3)]
    [Tooltip("팝업에 추가로 표시할 텍스트 (아이템 이름 등)")]
    public string additionalDescription = "";
}
