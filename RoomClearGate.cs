using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 몬스터 방 클리어 게이트 — 방 안의 몬스터를 모두 잡으면(공통 퀘스트) 문을 작동시킨다.
///
/// [퀘스트] 몬스터 방에서만 발생. 임무는 "맵의 몬스터 전멸". 클리어 시 onCleared 발동 →
///          DoorPortal.Activate() 에 연결해 문을 열고 연속 진행.
///
/// [감지 방식 — 전제]
///   monsterTag("Enemy") 로 태그된, monsterRoot 하위의 "살아있는(활성)" 몬스터 수를
///   주기적으로 센다. 한 번이라도 몬스터가 있었고(=무장) 그 수가 0이 되면 클리어.
///   → 몬스터가 사망 시 파괴/비활성된다는 전제(전투 팀 코드가 처리). 별도 연동 불필요.
///   monsterRoot 를 비우면 씬 전체의 태그 대상으로 계산.
///   (정밀 제어가 필요하면 전투 코드가 NotifyMonsterKilled() 를 호출해도 됨)
///
/// [부착] 몬스터 방(변형) 루트에 부착. onArmed/onProgress 로 퀘스트 표시를 연결할 수 있다.
/// </summary>
public class RoomClearGate : MonoBehaviour
{
    /// <summary>남은 몬스터 수를 넘기는 UnityEvent (인스펙터 노출용).</summary>
    [System.Serializable] public class IntEvent : UnityEvent<int> {}

    [Header("몬스터 식별")]
    [Tooltip("이 루트 하위의 몬스터만 센다. 비우면 씬 전체.")]
    [SerializeField] private Transform monsterRoot;
    [SerializeField] private string    monsterTag = "Enemy";

    [Header("검사 주기(초)")]
    [SerializeField] private float pollInterval = 0.5f;

    [Header("퀘스트 표시용 라벨(선택)")]
    [SerializeField] private string questLabel = "맵의 몬스터를 모두 처치";

    [Header("이벤트")]
    public UnityEvent onArmed;       // 첫 몬스터 감지 = 퀘스트 시작(표시 연결)
    public IntEvent   onProgress;    // 남은 수 변경
    public UnityEvent onCleared;     // 전멸 = 문 작동(DoorPortal.Activate 연결)

    public bool   IsArmed    { get; private set; }
    public bool   IsCleared  { get; private set; }
    public int    Remaining  { get; private set; }
    public string QuestLabel => questLabel;

    private float _timer;
    private bool  _tagOk = true;

    void OnEnable() { Rearm(); }

    void Update()
    {
        if (IsCleared || !_tagOk) return;

        _timer -= Time.deltaTime;
        if (_timer > 0f) return;
        _timer = pollInterval;

        int count = CountMonsters();

        if (!IsArmed)
        {
            if (count > 0)
            {
                IsArmed = true;
                Remaining = count;
                onArmed?.Invoke();
                onProgress?.Invoke(Remaining);
            }
            return;
        }

        if (count != Remaining)
        {
            Remaining = count;
            onProgress?.Invoke(Remaining);
        }

        if (count == 0)
        {
            IsCleared = true;
            onCleared?.Invoke();
            Debug.Log($"[RoomClearGate] '{name}' 클리어 — 문 작동");
        }
    }

    int CountMonsters()
    {
        GameObject[] all;
        try { all = GameObject.FindGameObjectsWithTag(monsterTag); }
        catch
        {
            _tagOk = false;   // 태그 미정의 → 더 이상 시도 안 함
            Debug.LogWarning($"[RoomClearGate] 태그 '{monsterTag}' 가 프로젝트에 정의돼 있지 않습니다.");
            return 0;
        }

        if (monsterRoot == null) return all.Length;

        int n = 0;
        foreach (var go in all)
            if (go.transform.IsChildOf(monsterRoot)) n++;
        return n;
    }

    /// <summary>[선택] 전투 코드가 몬스터 사망 직후 호출하면 즉시 재검사한다.</summary>
    public void NotifyMonsterKilled() => _timer = 0f;

    /// <summary>방을 다시 무장(변형 재로드/리스폰 시).</summary>
    public void Rearm()
    {
        IsArmed = false;
        IsCleared = false;
        Remaining = 0;
        _timer = 0f;
    }

    /// <summary>[테스트] 강제로 클리어 처리해 문 작동을 확인한다.</summary>
    public void ForceClear()
    {
        if (IsCleared) return;
        IsArmed = true;
        IsCleared = true;
        Remaining = 0;
        onCleared?.Invoke();
        Debug.Log($"[RoomClearGate] (테스트) 강제 클리어 → 문 작동");
    }
}
