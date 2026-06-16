using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 문간 이동 기믹 — 트리거에 플레이어가 들어오면 짝(exitPortal) 위치로 순간이동.
/// 양쪽을 서로 exitPortal 로 지정하면 양방향 이동.
///
/// [작동 게이트]
///   active=false 면 문이 작동하지 않는다(이동 X). 몬스터 방을 클리어하면
///   RoomClearGate.onCleared → 이 문의 Activate() 가 호출돼 열리고, 다음 방으로
///   연속 진행한다. (예전 태엽 기반 되돌림 루프는 제거됨)
///
/// [부착] 입구 트리거 콜라이더(IsTrigger)에 부착하고 exitPortal 을 짝으로 지정.
///        연속 방의 "다음 문"은 active=false 로 두고 RoomClearGate 에 연결.
/// </summary>
[RequireComponent(typeof(Collider))]
public class DoorPortal : MonoBehaviour
{
    [Header("도착 포털 (짝)")]
    [SerializeField] private DoorPortal exitPortal;

    [Tooltip("도착 지점. 비우면 도착 포털 자기 위치로 나온다.")]
    [SerializeField] private Transform exitPoint;

    [Header("작동 게이트")]
    [Tooltip("꺼져 있으면 문이 작동하지 않는다(이동 X). 방 클리어 시 Activate() 로 켠다.")]
    [SerializeField] private bool active = true;

    [Header("설정")]
    [SerializeField] private string playerTag = "Player";

    [Tooltip("이동 직후 재발동 방지 시간(초). 도착하자마자 다시 빨려가는 핑퐁 방지.")]
    [SerializeField] private float reuseDelay = 0.5f;

    [Header("이벤트")]
    public UnityEvent onTeleported;
    public UnityEvent onActivated;

    private float _reuseUntil;

    public bool IsActive => active;

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!active) return;                       // 작동 전엔 이동 없음
        if (Time.time < _reuseUntil) return;
        if (!other.CompareTag(playerTag)) return;

        if (exitPortal == null)
        {
            Debug.LogWarning($"[DoorPortal] '{name}' 의 도착 포털이 비어 있습니다.");
            return;
        }

        // 양쪽 모두 잠깐 잠가서 도착 즉시 재발동 방지
        LockBriefly();
        exitPortal.LockBriefly();

        MoveTo(other, exitPortal.GetArrivalPoint());
        onTeleported?.Invoke();
    }

    /// <summary>문을 작동 상태로 켠다(RoomClearGate.onCleared 에 연결).</summary>
    public void Activate()
    {
        if (active) return;
        active = true;
        onActivated?.Invoke();
        Debug.Log($"[DoorPortal] '{name}' 작동 시작");
    }

    /// <summary>작동 상태를 직접 설정.</summary>
    public void SetActive(bool on) => active = on;

    /// <summary>잠깐 발동을 막는다(핑퐁 방지).</summary>
    public void LockBriefly() => _reuseUntil = Time.time + reuseDelay;

    /// <summary>이 포털의 도착 좌표.</summary>
    public Vector3 GetArrivalPoint() => exitPoint != null ? exitPoint.position : transform.position;

    static void MoveTo(Collider target, Vector3 dest)
    {
        // CharacterController 가 있으면 끄고 옮긴다(내부 위치 캐시 충돌 방지).
        var cc = target.GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
            target.transform.position = dest;
            cc.enabled = true;
        }
        else
        {
            target.transform.position = dest;
        }
    }
}
