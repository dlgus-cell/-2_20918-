using System;
using System.Collections.Generic;

/// <summary>플레이어 상태. 실제 게임에 맞게 필드를 추가·수정하세요.</summary>
[Serializable]
public class PlayerSaveData
{
    // 위치
    public float posX;
    public float posY;
    public float posZ;
    // 기본 스탯 (예시 — 실제 값은 PlayerController 등에서 채워주세요)
    public int   gold       = 0;
    public int   experience = 0;
    public int   level      = 1;
    // 현재 체력/기력 — 이어하기 시 저장 당시 값 복원용 (PlayerVitals 와 연동)
    // -1 = 미저장(구버전 세이브) → 로드 시 현재 최대치로 시작
    public int   currentHP      = -1;
    public int   currentStamina = -1;
    // 인벤토리 내용(슬롯 위치 보존). itemId 로 저장 → 로드 시 ItemDb 로 복원.
    public List<InventorySlotRecord> inventory = new();
}

// ─────────────────────────────────────────────────────────────────────
