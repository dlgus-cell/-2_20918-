using System;
using System.Collections.Generic;

/// <summary>슬롯 메타 정보만 담은 경량 구조체 (슬롯 목록 표시용).</summary>
[Serializable]
public class SaveSlotMeta
{
    public bool   isEmpty     = true;
    public int    slotIndex;
    public string saveName;
    public string saveDateTime;
    public string questSummary;   // 예: "진행 2  완료 5"
    public string levelSummary;   // 예: "Lv.12  골드 3,400G"
    public float  totalPlayTimeSeconds;

    public string FormattedPlayTime()
    {
        int h = (int)(totalPlayTimeSeconds / 3600);
        int m = (int)(totalPlayTimeSeconds % 3600 / 60);
        int s = (int)(totalPlayTimeSeconds % 60);
        return h > 0 ? $"{h}시간 {m:00}분" : $"{m}분 {s:00}초";
    }
}
