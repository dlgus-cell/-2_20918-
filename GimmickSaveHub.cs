using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 기믹 상태를 모으고(저장) 나눠주는(불러오기) 중간 다리. (폴더3 3단계 설계)
///
/// ── SaveSystem 과의 관계 ─────────────────────────────────────────
///   SaveSystem 은 BuildSaveData 에서 CaptureAll(), ApplySaveData 에서
///   RestoreAll() 만 호출한다(최소 수정). 나머지는 전부 이 허브가 담당.
///
/// ── 살아있는 기믹 vs 파괴된 기믹 ─────────────────────────────────
///   - 문/태엽처럼 씬에 계속 존재하는 기믹: 저장 시 씬을 스캔해 현재 상태를 읽음.
///   - 주운 아이템(ItemPickup)처럼 파괴되는 기믹: 객체가 사라지면 스캔으로
///     못 잡으므로, 파괴 직전 NotifyDetachedState() 로 상태를 허브에 맡긴다.
///     허브는 이를 _detached 에 보관하여 다음 저장에도 유지한다.
///
/// ── 부착 ─────────────────────────────────────────────────────────
///   빈 GameObject 에 부착(씬에 1개). SaveSystem 과 함께 두면 됨.
/// </summary>
public class GimmickSaveHub : MonoBehaviour
{
    public static GimmickSaveHub Instance { get; private set; }

    // 파괴되어 씬 스캔으로 못 잡는 기믹의 상태(예: 주운 아이템). saveId → state
    private readonly Dictionary<string, string> _detached = new();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>파괴 직전 기믹이 자기 상태를 허브에 맡긴다(ItemPickup 등).</summary>
    public void NotifyDetachedState(string saveId, string state)
    {
        if (string.IsNullOrEmpty(saveId)) return;
        _detached[saveId] = state;
    }

    /// <summary>현재 모든 기믹 상태를 수집한다.</summary>
    public List<GimmickRecord> CaptureAll()
    {
        // 파괴된 기믹 상태로 시작 → 살아있는 기믹 상태를 덮어씌움
        var map = new Dictionary<string, string>(_detached);

        foreach (var g in FindSaveables())
        {
            if (string.IsNullOrEmpty(g.SaveId)) continue;
            string state = g.CaptureState();
            if (string.IsNullOrEmpty(state)) { map.Remove(g.SaveId); continue; } // 기본값 → 생략
            map[g.SaveId] = state;
        }

        var list = new List<GimmickRecord>();
        foreach (var kv in map)
            list.Add(new GimmickRecord { saveId = kv.Key, state = kv.Value });
        return list;
    }

    /// <summary>저장됐던 기믹 상태를 씬에 복원한다.</summary>
    public void RestoreAll(List<GimmickRecord> records)
    {
        _detached.Clear();
        if (records == null) return;

        var map = new Dictionary<string, string>();
        foreach (var r in records)
            if (r != null && !string.IsNullOrEmpty(r.saveId))
                map[r.saveId] = r.state;

        // 살아있는 기믹 복원 (RestoreState 가 Destroy 를 부를 수 있으므로 스냅샷 순회)
        var live = FindSaveables();
        var liveIds = new HashSet<string>();
        foreach (var g in live)
        {
            if (string.IsNullOrEmpty(g.SaveId)) continue;
            liveIds.Add(g.SaveId);
            if (map.TryGetValue(g.SaveId, out string state))
                g.RestoreState(state);
        }

        // 기록엔 있으나 씬에 없는 항목은 보존(다음 저장에 유지)
        foreach (var kv in map)
            if (!liveIds.Contains(kv.Key))
                _detached[kv.Key] = kv.Value;
    }

    static List<ISaveableGimmick> FindSaveables()
    {
        var result = new List<ISaveableGimmick>();
        // 비활성 오브젝트는 제외(활성 기믹만 대상).
#if UNITY_2023_1_OR_NEWER
        var all = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
#else
        var all = Object.FindObjectsOfType<MonoBehaviour>();
#endif
        foreach (var mb in all)
            if (mb is ISaveableGimmick g) result.Add(g);
        return result;
    }
}
