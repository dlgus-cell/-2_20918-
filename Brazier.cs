using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 등불꽃 기믹 (스테이지2). 공격이나 상호작용으로 불을 켜면 일정 시간 동안
/// 주위를 밝히고, 시간이 지나면 자동으로 꺼진다.
/// </summary>
public class Brazier : MonoBehaviour, IInteractable
{
    [Header("점등 시간(초). 0이면 끄지 않고 계속 켜짐")]
    [SerializeField] private float litDuration = 5f;

    [Header("켜고 끌 빛(선택)")]
    [SerializeField] private Light litLight;

    [Header("E키로도 켜기 허용")]
    [SerializeField] private bool allowInteract = true;
    [SerializeField] private string interactPrompt = "[E] 불 붙이기";

    [Header("켜질 때 / 꺼질 때 실행")]
    public UnityEvent onActivated;
    public UnityEvent onDeactivated;

    public bool IsLit { get; private set; }
    private float _offTimer;

    /// <summary>불을 켠다(전투 공격 명중 또는 스크립트에서 호출). 점등 시간 갱신.</summary>
    public void Activate()
    {
        _offTimer = litDuration;
        if (IsLit) return;
        IsLit = true;
        if (litLight != null) litLight.enabled = true;
        onActivated?.Invoke();
    }

    /// <summary>불을 끈다.</summary>
    public void Deactivate()
    {
        if (!IsLit) return;
        IsLit = false;
        if (litLight != null) litLight.enabled = false;
        onDeactivated?.Invoke();
    }

    void Update()
    {
        if (!IsLit || litDuration <= 0f) return;
        _offTimer -= Time.deltaTime;
        if (_offTimer <= 0f) Deactivate();
    }

    // IInteractable
    public Transform InteractionPoint => transform;
    public void Interact(GameObject interactor) => Activate();
    public string GetInteractionPrompt() => interactPrompt;
    public bool CanInteract(GameObject interactor) => allowInteract && !IsLit;
}
