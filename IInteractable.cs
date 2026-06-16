using UnityEngine;

// ═══════════════════════════════════════════════════════════════════════
//  IInteractable.cs — 상호작용 가능 오브젝트 공통 인터페이스
//
//  [역할]
//    NPC, 골드, 잠긴문, 태엽, 비콘 등 "플레이어가 E키로 상호작용할 수 있는"
//    모든 오브젝트가 구현하는 공통 규약.
//    PlayerInteractor가 이 인터페이스만 보고 일괄 처리한다.
//
//  [구현 예]
//    public class QuestNpc : MonoBehaviour, IInteractable { ... }
//    public class GoldDrop : MonoBehaviour, IInteractable { ... }
// ═══════════════════════════════════════════════════════════════════════

/// <summary>
/// 플레이어가 상호작용할 수 있는 오브젝트가 구현하는 인터페이스.
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// 상호작용을 실행한다. (E키 입력 시 PlayerInteractor가 호출)
    /// </summary>
    /// <param name="interactor">상호작용을 시도한 주체 (보통 플레이어 GameObject)</param>
    void Interact(GameObject interactor);

    /// <summary>
    /// 화면에 표시할 프롬프트 텍스트. 예: "[E] 대화하기", "[E] 줍기"
    /// </summary>
    string GetInteractionPrompt();

    /// <summary>
    /// 지금 상호작용이 가능한지 여부. (예: 열쇠 보유 여부, 이미 완료된 퀘스트 등)
    /// false면 PlayerInteractor가 프롬프트를 띄우지 않거나 잠금 표시한다.
    /// </summary>
    /// <param name="interactor">상호작용을 시도한 주체 (보통 플레이어 GameObject)</param>
    bool CanInteract(GameObject interactor);

    /// <summary>
    /// 상호작용 기준 위치. 프롬프트 UI를 띄울 좌표·거리 계산에 사용.
    /// 보통 구현체에서 자기 transform을 반환한다.
    /// </summary>
    Transform InteractionPoint { get; }
}
