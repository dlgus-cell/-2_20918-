// 맵에 떨어진 골드 오브젝트를 관리하는 코드임
using UnityEngine;

// [RequireComponent]는 이 코드가 부착된 오브젝트에 반드시 지정한 다른 코드가 함께 있어야 한다는 표시
// Collider는 다른 오브젝트와의 충돌을 감지함
[RequireComponent(typeof(Collider))]
public class GoldDrop : MonoBehaviour
{
    [Header("골드 양")]
    // 오브젝트가 가진 골드 양 변수
    [SerializeField] private int goldAmount = 10;

    [Header("자동 줍기 (트리거에 닿으면 즉시 획득)")]
    // 플레이어가 닿으면 획득하게 하는 변수
    [SerializeField] private bool autoPickup = true;

    [Header("플레이어 태그")]
    // 플레이어를 식별하는 태그
    [SerializeField] private string playerTag = "Player";

    [Header("자동 소멸 시간 (0 = 무제한)")]
    // 자동으로 사라지는 시간 변수
    [SerializeField] private float despawnTime = 30f;

    [Header("자기력 반경 (0 = 비활성)")]
    // 골드가 플레이어에게 빨려가는 거리 변수
    [SerializeField] private float magnetRadius = 2f;
    // 플레이어에게 빨려가는 속도 변수
    [SerializeField] private float magnetSpeed = 8f;

    // 중복 획득 방지 변수
    private bool _collected = false;
    // 골드가 빨려갈 대상의 위치 변수
    private Transform _magnetTarget;

    void Start()
    {
        // GetComponent는 오브젝트의 지정 코드를 가져옴
        // isTrigger = true는 지정 코드에 있는 설정값임
        GetComponent<Collider>().isTrigger = true;
        // 소멸 시간 후에 오브젝트를 파괴함
        if (despawnTime > 0f) Destroy(gameObject, despawnTime);
    }

    // Update는 계속해서 호출되는 함수임
    void Update()
    {
        // 획득되었거나 자동 줍기가 꺼져 있으면 종료함
        if (_collected || !autoPickup) return;

        // 끌려갈 대상으로 이동함
        if (_magnetTarget != null)
        {
            // Vector3.MoveTowards는 현재 위치에서 목표 위치로 일정한 속도로 이동시킴
            // Time.deltaTime을 곱하면 프레임 속도와 상관없이 일정한 속도로 움직임
            transform.position = Vector3.MoveTowards(
                transform.position, _magnetTarget.position, magnetSpeed * Time.deltaTime);
        }
        // 대상이 없고 자기력 반경이 있으면 주변을 탐색함
        else if (magnetRadius > 0f)
        {
            // Physics.OverlapSphere는 지정한 구 모양 범위 안의 모든 Collider를 찾아 목록으로 저장함
            Collider[] hits = Physics.OverlapSphere(transform.position, magnetRadius);
            // foreach는 목록의 각 항목을 차례대로 처리함
            foreach (var hit in hits)
            {
                // CompareTag로 플레이어 태그를 확인하고, 찾으면 대상으로 설정한 후에 반복을 종료함
                if (hit.CompareTag(playerTag)) { _magnetTarget = hit.transform; break; }
            }
        }
    }

    // OnTriggerEnter는 다른 Collider가 이 코드가 있는 오브젝트의 Collider에 들어왔을 때 호출되는 함수
    void OnTriggerEnter(Collider other)
    {
        if (!autoPickup || _collected) return;
        // 들어온 것이 플레이어가 아니면 무시함
        if (!other.CompareTag(playerTag)) return;
        Collect();
    }

    // 줍기 키를 눌렀을 때 호출하는 함수
    public void Interact()
    {
        if (_collected) return;
        Collect();
    }

    // 다른 코드에서 오브젝트의 골드 양을 설정하는 함수
    public void SetAmount(int amount) => goldAmount = amount;

    // 골드를 획득 처리하는 함수
    void Collect()
    {
        _collected = true;
        // GoldSystem이 존재하면 골드를 추가함
        GoldSystem.Instance?.AddGold(goldAmount, GoldChangeReason.PickUp);
        // 오브젝트를 파괴함
        Destroy(gameObject);
    }

    // OnDrawGizmosSelected는 화면에 도형, 범위를 그려주는 함수임
    void OnDrawGizmosSelected()
    {
        if (magnetRadius > 0f)
        {
            Gizmos.color = new Color(1f, 0.85f, 0f, 0.3f);
            Gizmos.DrawSphere(transform.position, magnetRadius);
        }
    }
}
