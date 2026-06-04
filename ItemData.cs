// 아이템 한 종류의 정보를 담는 데이터 묶음임

using UnityEngine;

// [CreateAssetMenu]는 Unity 프로젝트 창에서 데이터를 만들 수 있게 함
// fileName은 새 파일의 기본 이름, menuName은 메뉴에 표시될 위치/이름
[CreateAssetMenu(fileName = "NewItem", menuName = "RPG/Item")]
// ScriptableObject는 게임 오브젝트에 넣는 코드가 아닌, 파일로 따로 저장하는 데이터임
public class ItemData : ScriptableObject
{
    [Header("기본 정보")]
    // 아이템의 이름 변수
    public string itemName = "아이템";

    // [TextArea]는 Unity Inspector에서 여러 줄의 텍스트 입력 칸으로 표시한다는 표시
    [TextArea(2, 5)]
    public string description = "아이템 설명";

    // Sprite는 2D 이미지 자료형. 인벤토리에 표시할 아이콘
    public Sprite icon;

    [Header("타입")]
    // 아이템의 종류를 정하는 변수
    public ItemType itemType = ItemType.Misc;

    [Header("스택")]
    // 한 슬롯에 겹쳐서 쌓을 수 있는 최대 개수 변수
    public int maxStack = 1;

    [Header("구매 가격 (상점 연동)")]
    // 상점에서 구매할 때의 가격 변수
    public int buyPrice = 0;

    [Header("소비 아이템 전용")]
    // 사용 시 효과를 담은 데이터
    public ConsumableEffect consumableEffect;

    [Header("장착 아이템 전용")]
    // 아이템이 장착되는 부위
    public EquipSlot equipSlot = EquipSlot.None;
}
