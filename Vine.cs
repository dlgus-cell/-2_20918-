using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 덩굴 기믹 (용도 변경) — 닿아(트리거 안에) 있는 동안 플레이어를 느리게 만든다.
/// 걸리(느려지)기 전에 일반공격으로 잘라(Cut) 무력화하면 더 이상 느려지지 않고,
/// 베기 해제 보상(onCut)이 발동한다.
///
/// [경계]
///   "느림"은 플레이어 이동(허정욱) 소관이라 직접 못 멈춘다. 여기서는
///   PlayerStatusEffects 에 느림 값만 넣어두고(훅), 실제 감속은 컨트롤러가
///   MoveSpeedMultiplier 를 읽어 반영한다(PlayerStatusEffects 주석 참고).
///   "공격으로 자르기" 판정은 전투 코드가 Cut() 을 호출(기존 패턴 유지).
///
/// [참고] 예전의 "길을 막는 벽" 동작(blockingCollider)은 제거됨 — 이제 통과하며
///        느려지는 영역이다.
///
/// [부착] 덩굴 큐브의 트리거 콜라이더(IsTrigger)에 부착.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Vine : MonoBehaviour, IInteractable
{
    [Header("느림 (닿아 있는 동안)")]
    [Tooltip("이동 속도 배율. 0.5 = 절반 속도.")]
    [SerializeField] private float slowMultiplier = 0.5f;
    [SerializeField] private string playerTag = "Player";

    [Header("자르는 데 필요한 타격 수")]
    [SerializeField] private int hitsToCut = 1;

    [Header("E키 상호작용으로도 자르기 허용")]
    [SerializeField] private bool allowInteract = false;
    [SerializeField] private string interactPrompt = "[E] 베기";

    [Header("덩굴 비주얼(선택)")]
    [SerializeField] private GameObject vineVisual;

    [Header("잘렸을 때 실행 (베기 해제 보상 연결)")]
    public UnityEvent onCut;

    public bool IsCut { get; private set; }
    private int  _hits;
    private bool _playerInside;

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other) { if (other.CompareTag(playerTag)) _playerInside = true; }
    void OnTriggerExit(Collider other)  { if (other.CompareTag(playerTag)) _playerInside = false; }

    void Update()
    {
        if (IsCut || !_playerInside) return;
        // 닿아 있는 동안 느림을 짧게 계속 갱신 → 영역을 벗어나면 자동 만료된다.
        PlayerStatusEffects.Instance?.ApplySlow(slowMultiplier, 0.2f);
    }

    /// <summary>공격 1회. 누적 타격이 기준에 도달하면 잘린다. 전투 코드가 호출.</summary>
    public void Cut()
    {
        if (IsCut) return;
        _hits++;
        if (_hits >= hitsToCut) DoCut();
    }

    void DoCut()
    {
        IsCut = true;
        _playerInside = false;
        if (vineVisual != null) vineVisual.SetActive(false);
        onCut?.Invoke();   // 베기 해제 보상(예: 피해 면역 / 회복) 연결
        Debug.Log($"[Vine] '{name}' 잘림 → 느림 해제 + 보상");
    }

    // IInteractable
    public Transform InteractionPoint => transform;
    public void Interact(GameObject interactor) => Cut();
    public string GetInteractionPrompt() => interactPrompt;
    public bool CanInteract(GameObject interactor) => allowInteract && !IsCut;
}
