using System;
using UnityEngine;

// ═══════════════════════════════════════════════════════════════════════
//  PlayerInteractor.cs — 플레이어 상호작용 감지·실행 컴포넌트
//
//  [역할]
//    플레이어 주변의 IInteractable 오브젝트를 감지하고,
//    가장 가까운(상호작용 가능한) 대상을 골라 E키로 Interact()를 호출한다.
//    현재 대상이 바뀌면 이벤트로 알려, 프롬프트 UI가 갱신되게 한다.
//
//  [부착 위치]
//    플레이어 GameObject (또는 그 자식)에 부착.
//
//  ★ 임시 컴포넌트 — 나중에 플레이어 담당 팀원의 입력/제어 시스템과 통합 예정.
//    통합 시 교체할 지점은 본문에서 "[통합 지점]" 주석으로 표시했다.
//    - 입력 감지(E키)만 팀원 입력 시스템으로 갈아끼우면 나머지는 그대로 재사용 가능.
//    - 외부에서 입력 없이 강제 실행하려면 public TryInteract()를 호출하면 된다.
// ═══════════════════════════════════════════════════════════════════════

public class PlayerInteractor : MonoBehaviour
{
    [Header("감지 설정")]
    [Tooltip("이 반경 안의 IInteractable을 상호작용 후보로 감지한다.")]
    [SerializeField] private float detectRadius = 2.5f;

    [Tooltip("감지할 레이어. 기본값(Everything)이면 모든 레이어를 검사한다.")]
    [SerializeField] private LayerMask detectLayers = ~0;

    [Tooltip("자기 자신(플레이어)도 후보에서 제외할 때 사용하는 태그. 보통 'Player'.")]
    [SerializeField] private string selfTag = "Player";

    [Header("입력 설정")]
    [Tooltip("상호작용 키. ★ 통합 시 팀원 입력 시스템으로 대체될 예정.")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    // ─── 이벤트 ──────────────────────────────────────────────
    //   프롬프트 UI 등 외부 시스템이 구독한다. UI와 직접 결합하지 않기 위함(통합 용이).

    /// <summary>
    /// 현재 상호작용 대상이 바뀔 때 발생.
    /// 대상이 생기면 해당 IInteractable, 사라지면 null을 전달한다.
    /// </summary>
    public static event Action<IInteractable> OnFocusChanged;

    // ─── 런타임 상태 ─────────────────────────────────────────
    private IInteractable _current;        // 현재 포커스된 대상 (없으면 null)
    private readonly Collider[] _hits = new Collider[16];  // OverlapSphere 결과 버퍼(GC 방지)

    /// <summary>현재 포커스된 상호작용 대상 (없으면 null). 외부 조회용.</summary>
    public IInteractable Current => _current;

    // ═════════════════════════════════════════════════════════════════

    void Update()
    {
        // 1) 매 프레임 가장 적합한 대상을 갱신
        UpdateFocus();

        // 2) 입력 감지
        //    [통합 지점] 아래 한 줄이 입력 감지부. 팀원 입력 시스템과 통합할 때
        //               이 조건만 해당 시스템의 "상호작용 버튼 눌림"으로 교체하면 된다.
        if (Input.GetKeyDown(interactKey))
            TryInteract();
    }

    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 현재 포커스 대상에게 상호작용을 실행한다.
    /// 입력 방식과 무관하게 외부에서 직접 호출할 수도 있다(통합 용이).
    /// </summary>
    public void TryInteract()
    {
        if (_current == null) return;
        if (!_current.CanInteract(gameObject)) return;

        _current.Interact(gameObject);
    }

    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 주변을 탐색해 가장 가까운 "상호작용 가능한" 대상을 찾아 _current에 반영.
    /// 대상이 바뀌면 OnFocusChanged 이벤트를 발생시킨다.
    /// </summary>
    void UpdateFocus()
    {
        IInteractable best = FindBestTarget();

        if (!ReferenceEquals(best, _current))
        {
            _current = best;
            OnFocusChanged?.Invoke(_current);
        }
    }

    /// <summary>
    /// detectRadius 안에서 CanInteract가 true인 IInteractable 중
    /// 가장 가까운 것을 반환. 없으면 null.
    /// </summary>
    IInteractable FindBestTarget()
    {
        int count = Physics.OverlapSphereNonAlloc(
            transform.position, detectRadius, _hits, detectLayers, QueryTriggerInteraction.Collide);

        IInteractable best = null;
        float bestSqrDist = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            Collider col = _hits[i];
            if (col == null) continue;

            // 자기 자신(플레이어)은 제외
            if (!string.IsNullOrEmpty(selfTag) && col.CompareTag(selfTag)) continue;

            // IInteractable을 구현한 컴포넌트 탐색 (콜라이더 자신 또는 부모에서)
            IInteractable candidate = col.GetComponentInParent<IInteractable>();
            if (candidate == null) continue;

            // 지금 상호작용 가능한 대상만 후보로
            if (!candidate.CanInteract(gameObject)) continue;

            // 거리 비교 기준점: 인터페이스가 제공하는 InteractionPoint
            Transform point = candidate.InteractionPoint;
            Vector3 targetPos = point != null ? point.position : col.transform.position;

            float sqrDist = (targetPos - transform.position).sqrMagnitude;
            if (sqrDist < bestSqrDist)
            {
                bestSqrDist = sqrDist;
                best = candidate;
            }
        }

        return best;
    }

    // ─────────────────────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, detectRadius);
    }
}
