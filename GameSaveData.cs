using System;
using System.Collections.Generic;

/// <summary>슬롯 하나에 저장되는 전체 게임 데이터.</summary>
[Serializable]
public class GameSaveData
{
    // ── 메타 정보 ──────────────────────────────────────────────────────
    public string saveVersion   = "1.0";
    public int    slotIndex;
    public string saveName;          // 사용자 정의 이름 (기본: "저장 {n}")
    public string saveDateTime;      // "2025-01-01 18:30"
    public float  totalPlayTimeSeconds;

    // ── 게임 데이터 ────────────────────────────────────────────────────
    public PlayerSaveData player  = new();
    public QuestSaveData  quests  = new();
    // 기믹 상태(열린 문/감긴 태엽/주운 아이템 등). "이어하기 시 문 열린 채" 규칙용.
    public List<GimmickRecord> gimmicks = new();
}

// ─────────────────────────────────────────────────────────────────────
