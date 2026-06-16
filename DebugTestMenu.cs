using UnityEngine;

/// <summary>
/// 런타임 테스트 메뉴 (OnGUI). 빈 오브젝트 하나에 붙이면 끝 — UI/프리팹 세팅 불필요.
/// 기본 F12 로 토글. 현재 기능 전반을 버튼으로 테스트한다.
///   - 시스템 임시 전체 해금(저장 안 함) / 개별 해금 / 해금 초기화
///   - 골드 ±, 경험치 +, 레벨/포인트 표시
///   - HP/기력 ±
///   - 테스트 아이템 지급(선택)
///   - 방 강제 클리어(문 작동) / 맵 변형 로드
/// 배포 전 이 컴포넌트(또는 오브젝트)는 빼면 된다.
/// </summary>
public class DebugTestMenu : MonoBehaviour
{
    [Header("열기 키 / 시작 상태")]
    [SerializeField] private KeyCode toggleKey = KeyCode.F12;
    [SerializeField] private bool    startOpen = false;

    [Header("증감 단위")]
    [SerializeField] private int goldStep = 100;
    [SerializeField] private int xpStep   = 50;
    [SerializeField] private int hpStep   = 10;

    [Header("테스트용 아이템(선택)")]
    [SerializeField] private ItemData[] testItems;

    private bool    _open;
    private Vector2 _scroll;

    void Awake() { _open = startOpen; }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey)) _open = !_open;
    }

    void OnGUI()
    {
        if (!_open) return;

        GUILayout.BeginArea(new Rect(10, 10, 290, Screen.height - 20), GUI.skin.box);
        _scroll = GUILayout.BeginScrollView(_scroll);

        GUILayout.Label($"테스트 메뉴  ({toggleKey} 토글)");

        // ── 시스템 해금 ──
        GUILayout.Space(6);
        GUILayout.Label("■ 시스템 해금");
        bool dbg  = HUDTopBar.DebugUnlockAll;
        bool ndbg = GUILayout.Toggle(dbg, " 임시 전체 해금(저장 안 함)");
        if (ndbg != dbg) HUDTopBar.SetDebugUnlockAll(ndbg);

        var hud = HUDTopBar.Instance;
        if (hud != null)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("인벤")) hud.UnlockSystem(HUDTopBar.SystemType.Inventory);
            if (GUILayout.Button("상점")) hud.UnlockSystem(HUDTopBar.SystemType.Shop);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("상태")) hud.UnlockSystem(HUDTopBar.SystemType.StatusWindow);
            if (GUILayout.Button("저장")) hud.UnlockSystem(HUDTopBar.SystemType.Save);
            GUILayout.EndHorizontal();
            if (GUILayout.Button("해금 전체 초기화")) hud.ResetAllUnlocks();
        }
        else GUILayout.Label("(HUDTopBar 없음)");

        // ── 재화/성장 ──
        GUILayout.Space(6);
        GUILayout.Label("■ 재화/성장");
        if (GoldSystem.Instance != null)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button($"+{goldStep} 골드")) GoldSystem.Instance.AddGold(goldStep);
            if (GUILayout.Button($"-{goldStep} 골드")) GoldSystem.Instance.SpendGold(goldStep);
            GUILayout.EndHorizontal();
        }
        if (PlayerStats.Instance != null)
        {
            GUILayout.Label($"Lv {PlayerStats.Instance.Level}   포인트 {PlayerStats.Instance.StatPoints}");
            if (GUILayout.Button($"+{xpStep} XP")) PlayerStats.Instance.AddXP(xpStep);
        }

        // ── 체력/기력 ──
        GUILayout.Space(6);
        GUILayout.Label("■ 체력/기력");
        if (PlayerVitals.Instance != null)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button($"-{hpStep} HP")) PlayerVitals.Instance.TakeDamage(hpStep);
            if (GUILayout.Button($"+{hpStep} HP")) PlayerVitals.Instance.Heal(hpStep);
            GUILayout.EndHorizontal();
            if (GUILayout.Button($"+{hpStep} 기력")) PlayerVitals.Instance.RestoreStamina(hpStep);
        }

        // ── 아이템 ──
        if (testItems != null && testItems.Length > 0 && Inventory.Instance != null)
        {
            GUILayout.Space(6);
            GUILayout.Label("■ 아이템 지급");
            foreach (var it in testItems)
                if (it != null && GUILayout.Button($"+ {it.itemName}"))
                    Inventory.Instance.AddItem(it, 1);
        }

        // ── 방/맵 ──
        GUILayout.Space(6);
        GUILayout.Label("■ 방/맵");
        if (GUILayout.Button("방 강제 클리어(문 작동)"))
            foreach (var g in FindGates()) g.ForceClear();
        if (GUILayout.Button("맵 변형 0번 로드"))
            foreach (var m in FindVariants()) m.LoadVariant(0);

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    RoomClearGate[] FindGates()
    {
#if UNITY_2023_1_OR_NEWER
        return Object.FindObjectsByType<RoomClearGate>(FindObjectsSortMode.None);
#else
        return Object.FindObjectsOfType<RoomClearGate>();
#endif
    }

    MapVariantController[] FindVariants()
    {
#if UNITY_2023_1_OR_NEWER
        return Object.FindObjectsByType<MapVariantController>(FindObjectsSortMode.None);
#else
        return Object.FindObjectsOfType<MapVariantController>();
#endif
    }
}
