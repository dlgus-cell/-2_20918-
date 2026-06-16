using UnityEngine;

// ═══════════════════════════════════════════════════════════════════════
//  ItemPickup.cs — 월드에 떨어진 아이템 줍기
//
//  [역할]
//    바닥에 놓인 아이템 오브젝트. 플레이어가 트리거로 자동 줍거나(autoPickup),
//    E키로 직접 주워(IInteractable) 인벤토리에 넣는다.
//    GoldDrop(GoldFx.cs)의 패턴을 그대로 따른다.
//
//  [부착 위치]
//    줍기 가능한 월드 아이템 프리팹에 부착. Collider 필요(트리거로 사용).
//
//  [설계 메모]
//    - 인벤토리가 가득 차면 AddItem이 false를 반환 → 오브젝트를 그대로 두어
//      "주웠는데 사라지고 아이템은 안 들어오는" 손실을 방지한다.
//    - 획득 시 ToastSystem.Show("○○ 획득")으로 알림. (Inventory에는 '무슨
//      아이템이 들어왔는지' 알려주는 이벤트가 없어, 줍는 쪽에서 직접 띄운다)
// ═══════════════════════════════════════════════════════════════════════

[RequireComponent(typeof(Collider))]
public class ItemPickup : MonoBehaviour, IInteractable, ISaveableGimmick
{
    [Header("저장용 고유 ID (기믹 상태 저장)")]
    [Tooltip("씬 안에서 유일한 ID. 이미 주운 월드 아이템이 이어하기 시 다시 안 생기게 함. 비우면 저장 안 됨.")]
    [SerializeField] private string saveId = "";

    [Header("줍을 아이템")]
    [SerializeField] private ItemData item;
    [SerializeField] private int amount = 1;

    [Header("자동 줍기 (트리거에 닿으면 즉시 획득)")]
    [SerializeField] private bool autoPickup = true;

    [Header("플레이어 태그")]
    [SerializeField] private string playerTag = "Player";

    [Header("자동 소멸 시간 (0 = 무제한)")]
    [SerializeField] private float despawnTime = 0f;

    [Header("토스트 알림 표시")]
    [SerializeField] private bool showToast = true;

    private bool _collected = false;

    // ═════════════════════════════════════════════════════════════════

    void Start()
    {
        GetComponent<Collider>().isTrigger = true;
        if (despawnTime > 0f) Destroy(gameObject, despawnTime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!autoPickup || _collected) return;
        if (!other.CompareTag(playerTag)) return;
        TryCollect();
    }

    // ─── IInteractable 구현 ───────────────────────────────────────────

    /// <summary>프롬프트 UI를 띄울 기준 위치 (자기 자신).</summary>
    public Transform InteractionPoint => transform;

    /// <summary>E키 상호작용 = 줍기.</summary>
    public void Interact(GameObject interactor) => TryCollect();

    /// <summary>화면에 표시할 프롬프트. 아이템 이름을 함께 보여준다.</summary>
    public string GetInteractionPrompt()
        => item != null ? $"[E] {item.itemName} 줍기" : "[E] 줍기";

    /// <summary>
    /// 수동 줍기 가능 여부.
    /// autoPickup이 켜져 있으면 트리거로 자동 획득되므로 수동 대상 아님.
    /// </summary>
    public bool CanInteract(GameObject interactor)
        => !autoPickup && !_collected && item != null;

    // ─── 줍기 처리 ────────────────────────────────────────────────────

    /// <summary>
    /// 인벤토리에 넣기를 시도한다. 성공해야만 오브젝트를 제거한다.
    /// 인벤토리가 가득 차면(false) 그대로 두어 아이템 손실을 막는다.
    /// </summary>
    void TryCollect()
    {
        if (_collected) return;
        if (item == null)
        {
            Debug.LogWarning("[ItemPickup] item이 지정되지 않았습니다.");
            return;
        }

        if (Inventory.Instance == null)
        {
            Debug.LogWarning("[ItemPickup] Inventory.Instance가 없습니다.");
            return;
        }

        bool added = Inventory.Instance.AddItem(item, amount);
        if (!added)
        {
            // 인벤토리가 가득 참 → 줍지 않고 유지
            if (showToast) ToastSystem.Show("인벤토리가 가득 찼습니다");
            return;
        }

        _collected = true;

        if (showToast)
        {
            string msg = amount > 1 ? $"{item.itemName} x{amount} 획득" : $"{item.itemName} 획득";
            ToastSystem.Show(msg);
        }

        // TODO: SoundManager.Play("item_pickup");
        // TODO: EffectManager.Spawn("fx_pickup", transform.position);
        // 파괴 직전 '주움' 상태를 허브에 맡긴다(이어하기 시 다시 안 생기게).
        GimmickSaveHub.Instance?.NotifyDetachedState(saveId, "collected");
        Destroy(gameObject);
    }

    /// <summary>아이템/수량을 외부에서 설정 (스폰 직후 호출용).</summary>
    public void Setup(ItemData newItem, int newAmount)
    {
        item   = newItem;
        amount = Mathf.Max(1, newAmount);
    }

    // ═════════════════════════════════════════════════════════════════
    #region ISaveableGimmick 구현 (기믹 상태 저장)

    public string SaveId => saveId;

    public string CaptureState() => _collected ? "collected" : "";   // 안 주움은 기본값(생략)

    public void RestoreState(string state)
    {
        // 이미 주운 아이템이면 이어하기 시 화면에서 제거(다시 줍지 못하게).
        if (state == "collected")
        {
            _collected = true;
            GimmickSaveHub.Instance?.NotifyDetachedState(saveId, "collected");
            Destroy(gameObject);
        }
    }

    #endregion
}
