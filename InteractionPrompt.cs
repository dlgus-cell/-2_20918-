using UnityEngine;
using TMPro;

// ═══════════════════════════════════════════════════════════════════════
//  InteractionPrompt.cs — 상호작용 프롬프트 UI ("[E] 대화하기")
//
//  [역할]
//    PlayerInteractor가 포커스한 대상의 머리 위에 프롬프트 텍스트를 띄운다.
//    대상이 없으면 숨긴다.
//
//  [표시 방식]
//    Screen Space Canvas 위의 텍스트를, 대상의 월드 좌표를 화면 좌표로
//    변환해 따라가게 한다. (쿼터뷰에서 글자 크기가 일정하고 항상 또렷함)
//
//  [부착 위치]
//    Screen Space - Overlay (또는 Camera) Canvas 아래의 프롬프트 오브젝트에 부착.
//    promptText에 자식 TMP 텍스트를 연결.
//
//  [통합 메모]
//    - PlayerInteractor.OnFocusChanged 이벤트만 구독하므로 결합도가 낮다.
//    - 카메라는 Camera.main을 자동 사용하되 인스펙터에서 교체 가능.
//      팀원 카메라 세팅이 정해지면 targetCamera에 연결하면 된다.
// ═══════════════════════════════════════════════════════════════════════

public class InteractionPrompt : MonoBehaviour
{
    [Header("UI 참조")]
    [Tooltip("프롬프트로 표시할 TMP 텍스트. 보통 이 오브젝트의 자식.")]
    [SerializeField] private TMP_Text promptText;

    [Tooltip("프롬프트 전체를 켜고 끌 루트 오브젝트. 비우면 이 GameObject를 사용.")]
    [SerializeField] private GameObject promptRoot;

    [Header("카메라")]
    [Tooltip("월드→화면 좌표 변환에 쓸 카메라. 비우면 Camera.main 사용.")]
    [SerializeField] private Camera targetCamera;

    [Header("위치 보정")]
    [Tooltip("대상 InteractionPoint 기준, 월드 공간에서 위로 띄울 높이.")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.5f, 0f);

    // ─── 런타임 상태 ─────────────────────────────────────────
    private IInteractable _target;   // 현재 따라다닐 대상 (없으면 null)

    // ═════════════════════════════════════════════════════════════════

    void OnEnable()  => PlayerInteractor.OnFocusChanged += HandleFocusChanged;
    void OnDisable() => PlayerInteractor.OnFocusChanged -= HandleFocusChanged;

    void Start()
    {
        if (promptRoot == null) promptRoot = gameObject;
        if (targetCamera == null) targetCamera = Camera.main;
        Hide();
    }

    // ─────────────────────────────────────────────────────────────────

    /// <summary>PlayerInteractor가 포커스 대상을 바꿀 때 호출된다.</summary>
    void HandleFocusChanged(IInteractable target)
    {
        _target = target;

        if (_target == null)
        {
            Hide();
            return;
        }

        // 프롬프트 텍스트 갱신 후 표시
        if (promptText != null)
            promptText.text = _target.GetInteractionPrompt();

        Show();
    }

    // ─────────────────────────────────────────────────────────────────

    void LateUpdate()
    {
        // 대상이 없으면 따라갈 필요 없음
        if (_target == null) return;

        // 카메라가 없으면(씬 전환 등) 다시 확보 시도
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            if (targetCamera == null) return;
        }

        Transform point = _target.InteractionPoint;
        if (point == null) return;

        Vector3 worldPos   = point.position + worldOffset;
        Vector3 screenPos  = targetCamera.WorldToScreenPoint(worldPos);

        // 대상이 카메라 뒤에 있으면(z < 0) 숨김 처리
        if (screenPos.z < 0f)
        {
            if (promptRoot.activeSelf) promptRoot.SetActive(false);
            return;
        }

        if (!promptRoot.activeSelf) promptRoot.SetActive(true);
        transform.position = screenPos;
    }

    // ─────────────────────────────────────────────────────────────────

    void Show()
    {
        if (promptRoot != null) promptRoot.SetActive(true);
    }

    void Hide()
    {
        if (promptRoot != null) promptRoot.SetActive(false);
    }
}
