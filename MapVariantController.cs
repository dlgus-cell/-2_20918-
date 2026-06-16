using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 맵 변환 컨트롤러 — 같은 방(맵 구조)은 그대로 두고, "미리 몬스터·함정·장식이
/// 배치된" 변형(variant) 프리팹을 마운트 지점에 불러온다(인스턴스화). 씬 교체 아님.
///
/// [형식]
///   variants[i] = 미리 배치가 끝난 프리팹. LoadVariant(i) 하면 이전 변형을 치우고
///   i 번 프리팹을 새로 깐다(매번 새 상태 → 재방문 시 몬스터도 새로).
///
/// [트리거] 외부에서 public 메서드 호출.
///   - AI(허정욱 GeminiNPC)가 대화 결과로 LoadVariant()/Next() 호출하도록 연결.
///   - 문/방 진입 트리거의 UnityEvent 에 LoadVariant 연결 → "문으로 이동 시 로드".
///   (파일 하단 [연동 지점] 주석 참고)
///
/// [부착] 방의 마운트 지점(또는 방 루트)에 부착. mountPoint 비우면 자기 Transform.
/// </summary>
public class MapVariantController : MonoBehaviour
{
    /// <summary>인덱스를 넘기는 UnityEvent (인스펙터 노출용).</summary>
    [System.Serializable] public class IntEvent : UnityEvent<int> {}

    [Header("변형 프리팹 (인덱스 = 변형 번호)")]
    [Tooltip("미리 몬스터·함정·장식이 배치된 프리팹들.")]
    [SerializeField] private List<GameObject> variants = new();

    [Header("마운트 지점 (비우면 자기 Transform)")]
    [SerializeField] private Transform mountPoint;

    [Header("불러올 때 이전 변형 제거")]
    [SerializeField] private bool clearPreviousOnLoad = true;

    [Header("시작 시 자동 로드 (-1 = 안 함)")]
    [SerializeField] private int startIndex = -1;

    [Header("부활 시 기본 맵으로 (변형 제거)")]
    [Tooltip("켜면 SaveService 부활 시 현재 변형을 치워 기본(빈) 맵으로 되돌린다.")]
    [SerializeField] private bool clearOnRespawn = true;

    [Header("이벤트")]
    [Tooltip("로드된 변형 인덱스 전달 — 맵 변환 시 퀘스트 부여 등에 연결.")]
    public IntEvent   onVariantLoaded;
    public UnityEvent onCleared;

    /// <summary>현재 로드된 변형 인덱스. 없으면 -1.</summary>
    public int CurrentIndex { get; private set; } = -1;

    private GameObject _current;

    void Awake() { if (mountPoint == null) mountPoint = transform; }

    void Start() { if (startIndex >= 0) LoadVariant(startIndex); }

    void OnEnable()  => SaveService.OnRespawned += HandleRespawn;
    void OnDisable() => SaveService.OnRespawned -= HandleRespawn;

    void HandleRespawn() { if (clearOnRespawn) Clear(); }

    /// <summary>
    /// i 번 변형을 불러온다(이전 변형은 설정에 따라 제거). AI/문/트리거가 호출.
    /// </summary>
    public void LoadVariant(int index)
    {
        if (index < 0 || index >= variants.Count || variants[index] == null)
        {
            Debug.LogWarning($"[MapVariantController] 잘못된 변형 인덱스: {index}");
            return;
        }

        if (clearPreviousOnLoad) ClearCurrent();

        _current = Instantiate(variants[index], mountPoint);
        _current.transform.localPosition = Vector3.zero;
        _current.transform.localRotation = Quaternion.identity;
        _current.transform.localScale    = variants[index].transform.localScale; // 마운트 스케일 왜곡 방지
        CurrentIndex = index;

        onVariantLoaded?.Invoke(index);
        Debug.Log($"[MapVariantController] 변형 {index} 로드");
    }

    /// <summary>다음 변형으로(끝이면 처음으로 순환).</summary>
    public void Next()
    {
        if (variants.Count == 0) return;
        LoadVariant((CurrentIndex + 1) % variants.Count);
    }

    /// <summary>현재 변형을 새로 다시 깐다(리스폰 느낌).</summary>
    public void ReloadCurrent() { if (CurrentIndex >= 0) LoadVariant(CurrentIndex); }

    /// <summary>현재 변형을 치운다(빈 방으로).</summary>
    public void Clear()
    {
        ClearCurrent();
        CurrentIndex = -1;
        onCleared?.Invoke();
    }

    void ClearCurrent()
    {
        if (_current == null) return;
        if (Application.isPlaying) Destroy(_current);
        else                       DestroyImmediate(_current);
        _current = null;
    }

    // ────────────────────────────────────────────────────────────────
    //  [연동 지점] AI(허정욱) 대화 액션에서 맵을 바꿀 때:
    //    // var ctrl = currentRoom.GetComponent<MapVariantController>();
    //    // ctrl?.LoadVariant(요청된_변형_번호);   // 또는 ctrl?.Next();
    //
    //  문/트리거 → 로드: 문 컴포넌트의 UnityEvent 에 LoadVariant(int) 연결.
    // ────────────────────────────────────────────────────────────────

    [ContextMenu("테스트/다음 변형")]   void _TestNext()   => Next();
    [ContextMenu("테스트/현재 재로드")] void _TestReload() => ReloadCurrent();
    [ContextMenu("테스트/비우기")]      void _TestClear()  => Clear();
}
