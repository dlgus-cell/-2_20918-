using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 감전 해제 보상 — 주위 몬스터를 일정 시간 기절시킨다.
///
/// [경계] 몬스터/적 클래스는 팀원(이연/허정욱) 코드라 내 코드엔 없다. 그래서
///        여기서는 "범위 안의 적을 찾는 것"까지만 하고, 실제로 멈추는 호출은
///        아래 주석 코드처럼 팀원이 자기 몬스터 코드에 맞게 연결해야 한다.
///
/// [연결] HazardWard(StunMonsters) 또는 함정 해제 이벤트의 UnityEvent 에
///        이 컴포넌트의 Trigger() 를 연결.
/// </summary>
public class MonsterStunField : MonoBehaviour
{
    [Header("범위 / 시간")]
    [SerializeField] private float     radius      = 6f;
    [SerializeField] private float     stunSeconds = 3f;

    [Header("적 식별")]
    [SerializeField] private string    enemyTag    = "Enemy";
    [SerializeField] private LayerMask enemyMask   = ~0;

    public UnityEvent onTriggered;

    /// <summary>코어 충전/해제 시 호출(UnityEvent 로 연결).</summary>
    public void Trigger()
    {
        var hits = Physics.OverlapSphere(transform.position, radius, enemyMask);
        foreach (var h in hits)
        {
            if (!h.CompareTag(enemyTag)) continue;

            // ── [연동 지점] 실제 기절은 몬스터 코드(팀원)가 처리. 아래 중 택1로 연결 ──
            //   A) 몬스터가 public void Stun(float s) 를 가진 경우:
            //        // h.GetComponent<MonsterAI>()?.Stun(stunSeconds);
            //   B) 공용 인터페이스(IStunnable 등)를 쓰는 경우:
            //        // h.GetComponent<IStunnable>()?.Stun(stunSeconds);
            //   C) 메시지로 받는 경우:
            //        // h.SendMessage("Stun", stunSeconds, SendMessageOptions.DontRequireReceiver);

            Debug.Log($"[MonsterStunField] '{h.name}' 기절 대상 (팀원 연결 시 실제 정지) {stunSeconds}s");
        }
        onTriggered?.Invoke();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
