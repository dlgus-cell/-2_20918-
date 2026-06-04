// 골드가 변동된 이유 종류 목록

public enum GoldChangeReason
{
    MonsterDrop,    // 몬스터 드랍인 경우
    QuestReward,    // 퀘스트 완료 보상인 경우
    PickUp,         // 바닥의 골드를 주운 경우
    Purchase,       // 상점에서 물건을 산 경우
    Load,           // 저장 데이터를 불러온 경우
    Misc            // 위의 해당하지 않는 경우
}
