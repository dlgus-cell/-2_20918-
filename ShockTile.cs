using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 감전 기믹 (명세서 스테이지2: "바닥에 전류가 흐르는 부분").
///
/// [역할]
///   플레이어가 트리거 위에 있는 동안 일정 간격으로 피해를 준다.
///   피해는 정본 PlayerVitals.TakeDamage 로 전달(플레이어 담당 코드와 분리).
///
/// [켜고 끄기]
///   SetActive(false) 로 비활성화 가능(예: 태엽/스위치로 전류 차단).
///
/// [부착] 바닥 트리거 콜라이더(IsTrigger)에 부착.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ShockTile : MonoBehaviour
{
    [Header("피해")]
    [SerializeField] private int   damagePerTick = 5;
    [SerializeField] private float tickInterval  = 0.5f;

    [Header("기절 (피해 시 플레이어 조작 잠금 — 훅: 컨트롤러가 IsStunned 읽음)")]
    [Tooltip("0 이면 기절 없음. 기절 1회의 지속 시간(초).")]
    [SerializeField] private float stunSeconds  = 1.5f;
    [Tooltip("기절 재발동 쿨다운(초). 이 시간마다 최대 1번만 기절(그 사이엔 피해만).")]
    [SerializeField] private float stunCooldown = 3f;

    [Header("설정")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool   active    = true;

    [Header("피해 줄 때 실행 (이펙트/사운드 연결용)")]
    public UnityEvent onShock;

    private bool  _playerInside;
    private float _tickTimer;
    private float _suppressTimer;   // > 0 이면 일시 차단(해제 보상)
    private float _stunCdTimer;     // > 0 이면 기절 쿨다운 중

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
        if (_stunCdTimer > 0f) _stunCdTimer -= Time.deltaTime;

        if (_suppressTimer > 0f) { _suppressTimer -= Time.deltaTime; return; }
        if (!active || !_playerInside) return;

        // 함정 피해 면역 중이면 건너뜀(내 영역에서 확인).
        if (PlayerStatusEffects.Instance != null && PlayerStatusEffects.Instance.HazardImmune) return;

        _tickTimer -= Time.deltaTime;
        if (_tickTimer > 0f) return;
        _tickTimer = tickInterval;

        if (PlayerVitals.Instance != null)
        {
            PlayerVitals.Instance.TakeDamage(damagePerTick);
            onShock?.Invoke();

            // 기절은 플레이어 이동(허정욱) 소관이라 값만 넣어둔다(훅).
            // 쿨다운 중이 아닐 때만 1회 기절시키고, 다시 쿨다운을 건다.
            if (stunSeconds > 0f && _stunCdTimer <= 0f)
            {
                PlayerStatusEffects.Instance?.ApplyStun(stunSeconds);
                _stunCdTimer = stunCooldown;
            }
        }
    }

    /// <summary>전류를 켜거나 끈다(스위치/태엽 연동용). 끄면 Update에서 피해가 멈춘다.</summary>
    public void SetActive(bool on) => active = on;

    /// <summary>해제 보상으로 seconds 동안 전류를 일시 차단(코어 onCharged 등에 연결).</summary>
    public void Suppress(float seconds) => _suppressTimer = Mathf.Max(_suppressTimer, seconds);
}
