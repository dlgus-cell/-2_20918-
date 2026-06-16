using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 기믹 코어 — 맵 어딘가에 두고, 몬스터를 유도해 공격받으면(충전) 연결된 함정을
/// 일시 해제하고 보상을 발동시킨다. (감전·불 해제용)
///
/// [경계] "몬스터가 코어를 공격하는 행동"은 몬스터 AI(팀원) 소관이다. 코어는
///        맞은 양을 받는 public 훅(ReceiveHit)만 열어 둔다. 몬스터 연결 전에도
///        E키 상호작용으로 충전(테스트/대체 경로)할 수 있다.
///
/// [연결] onCharged 에 → 끌 함정의 Suppress()/SetActive(false) + HazardWard.Trigger()
///        를 인스펙터로 연결한다. resetSeconds 뒤 다시 무장(onRearmed).
///
/// [부착] 코어 큐브(또는 그 콜라이더)에 부착.
/// </summary>
public class GimmickCore : MonoBehaviour, IInteractable
{
    [Header("충전 조건")]
    [Tooltip("이만큼 누적 피격되면 충전(해제 발동).")]
    [SerializeField] private int   requiredHits = 5;
    [Tooltip("충전 후 다시 무장될 때까지의 시간(초).")]
    [SerializeField] private float resetSeconds = 8f;

    [Header("E키 상호작용으로도 충전 허용 (몬스터 연결 전 테스트/대체)")]
    [SerializeField] private bool   allowInteractCharge = true;
    [SerializeField] private string interactPrompt = "[E] 코어 가동";

    [Header("충전 완료 시 실행 (함정 Suppress + 버프 연결)")]
    public UnityEvent onCharged;
    [Header("다시 무장될 때 실행")]
    public UnityEvent onRearmed;

    private int   _hits;
    private bool  _charged;
    private float _resetTimer;

    /// <summary>현재 충전(해제 발동) 상태인지.</summary>
    public bool IsCharged => _charged;

    /// <summary>
    /// [연동 지점] 몬스터/전투가 코어를 때릴 때 호출.
    ///   예: // core.GetComponent&lt;GimmickCore&gt;()?.ReceiveHit(damage);
    /// </summary>
    public void ReceiveHit(int amount = 1)
    {
        if (_charged || amount <= 0) return;
        _hits += amount;
        if (_hits >= requiredHits) Charge();
    }

    /// <summary>강제 충전(외부/테스트).</summary>
    public void Charge()
    {
        if (_charged) return;
        _charged = true;
        _hits = 0;
        _resetTimer = resetSeconds;
        onCharged?.Invoke();
        Debug.Log($"[GimmickCore] '{name}' 충전 완료 → 함정 해제/버프");
    }

    void Update()
    {
        if (!_charged) return;
        _resetTimer -= Time.deltaTime;
        if (_resetTimer <= 0f)
        {
            _charged = false;
            onRearmed?.Invoke();
        }
    }

    // ── IInteractable (E키 충전 = 몬스터 연결 전 대체 경로) ──
    public Transform InteractionPoint => transform;
    public void Interact(GameObject interactor) { if (allowInteractCharge) ReceiveHit(requiredHits); }
    public string GetInteractionPrompt() => interactPrompt;
    public bool CanInteract(GameObject interactor) => allowInteractCharge && !_charged;
}
