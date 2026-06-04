// 아이템을 사용했을 때 발생하는 효과 데이터 묶음

// [System.Serializable]은 이 클래스를 Unity Inspector에 표시 가능하고 파일로 저장 가능하게 함
[System.Serializable]
public class ConsumableEffect
{
    // 사용 시 회복되는 체력 양
    public int restoreHp    = 0;
    // 사용 시 회복되는 마나 양
    public int restoreMp    = 0;
    // 사용 시 얻게 되는 골드 양
    public int addGold      = 0;
    // 기본 효과로 표현할 수 없는 효과를 위한 변수
    public string customTag = "";
}
