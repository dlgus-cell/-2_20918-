using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 불 장판 기믹 — 밟고 있는 동안 주기적으로 피해를 준다.
///
/// [영구성] 안개(FogZone)의 Clear 같은 "영구 해제"가 없다. 저장도 안 한다.
///          → 불은 끌 수 없다(요구사항). 단 해제 보상(코어 충전 등)으로
///            Suppress(seconds) 동안만 일시 차단된다.
///
/// [면역]   해제 보상으로 받은 함정 피해 면역(PlayerStatusEffects.HazardImmune)
///          중에는 피해를 건너뛴다.
///
/// [부착]   불 영역 큐브의 트리거 콜라이더(IsTrigger)에 부착.
/// </summary>
[RequireComponent(typeof(Collider))]
public class FireZone : MonoBehaviour
{
    [Header("피해")]
    [SerializeField] private int   damagePerTick = 6;
    [SerializeField] private float tickInterval  = 0.5f;

    [Header("설정")]
    [SerializeField] private string playerTag = "Player";

    [Header("피해 줄 때 실행 (이펙트/사운드 연결용)")]
    public UnityEvent onBurn;

    private bool  _playerInside;
    private float _tickTimer;
    private float _suppressTimer;   // > 0 이면 일시 차단(해제 보상)

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag)) { _playerInside = true; _tickTimer = 0f; }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag)) _playerInside = false;
    }

    void Update()
    {
        if (_suppressTimer > 0f) { _suppressTimer -= Time.deltaTime; return; }
        if (!_playerInside) return;

        // 함정 피해 면역 중이면 건너뜀(내 영역에서 확인).
        if (PlayerStatusEffects.Instance != null && PlayerStatusEffects.Instance.HazardImmune) return;

        _tickTimer -= Time.deltaTime;
        if (_tickTimer > 0f) return;
        _tickTimer = tickInterval;

        if (PlayerVitals.Instance != null)
        {
            PlayerVitals.Instance.TakeDamage(damagePerTick);
            onBurn?.Invoke();
        }
    }

    /// <summary>
    /// 해제 보상으로 seconds 동안 불을 일시 차단한다(영구 아님).
    /// GimmickCore.onCharged 등에 연결.
    /// </summary>
    public void Suppress(float seconds) => _suppressTimer = Mathf.Max(_suppressTimer, seconds);
}
