using UnityEngine;

/// <summary>
/// 매니저 자동 설치 도구. 빈 GameObject 하나에 이 컴포넌트만 붙여 두면,
/// 씬에 없는 매니저 싱글톤들을 시작 시 자동 생성한다.
/// (저장/인벤토리/골드/퀘스트/토스트/기믹 허브)
/// → "매니저를 일일이 만들어 배치" 하는 수작업을 줄이기 위한 편의용.
///
/// 주의: 자동 생성은 객체 "존재"만 보장한다. 일부 매니저는 인스펙터에서
///       참조(예: SaveSystem 의 questDatabase, 플레이어 Transform)를
///       추가로 연결해야 모든 기능이 동작한다.
/// </summary>
public class GameBootstrap : MonoBehaviour
{
    [Header("플레이어가 아직 없을 때, 임시 플레이어 생성")]
    [Tooltip("켜면 PlayerStats + PlayerVitals 가 붙은 임시 Player 오브젝트를 만든다(테스트용).")]
    [SerializeField] private bool createTempPlayer = false;

    void Awake()
    {
        Ensure<GoldSystem>("GoldSystem");
        Ensure<Inventory>("Inventory");
        Ensure<QuestManager>("QuestManager");
        Ensure<ToastSystem>("ToastSystem");
        Ensure<SaveSystem>("SaveSystem");
        Ensure<GimmickSaveHub>("GimmickSaveHub");

        if (createTempPlayer && Find<PlayerStats>() == null)
        {
            var p = new GameObject("Player(임시)");
            p.tag = "Player";
            p.AddComponent<PlayerStats>();
            p.AddComponent<PlayerVitals>();
        }
    }

    static void Ensure<T>(string objectName) where T : Component
    {
        if (Find<T>() != null) return;          // 이미 있으면 건너뜀(중복 방지)
        new GameObject(objectName).AddComponent<T>();
    }

    static T Find<T>() where T : Object
    {
#if UNITY_2023_1_OR_NEWER
        return Object.FindAnyObjectByType<T>();
#else
        return Object.FindObjectOfType<T>();
#endif
    }
}
