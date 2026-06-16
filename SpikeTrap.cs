using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 가시 트랩 — 대기 → 경고 → 솟음(피해) 주기를 반복한다.
///
/// [해제]  솟아(노출) 있는 동안 플레이어가 공격하면(OnAttacked 호출) 일정 시간
///         일시 정지하고, onDisabled(가시 해제 보상 = 공격력 버프)가 발동한다.
///
/// [경계]  "플레이어가 가시를 공격" 판정은 전투 코드(이연)가 OnAttacked() 를
///         호출하는 방식(Vine.Cut 과 동일 패턴). 파일 하단 주석 예시 참고.
///
/// [면역]  함정 피해 면역(PlayerStatusEffects.HazardImmune) 중에는 솟음 피해를
///         건너뛴다.
///
/// [부착]  가시 큐브의 트리거 콜라이더(IsTrigger)에 부착.
/// </summary>
[RequireComponent(typeof(Collider))]
public class SpikeTrap : MonoBehaviour
{
    private enum Phase { Idle, Warn, Strike }

    [Header("주기 (초)")]
    [SerializeField] private float idleTime   = 1.5f;
    [SerializeField] private float warnTime   = 0.5f;
    [SerializeField] private float strikeTime = 0.8f;

    [Header("피해")]
    [SerializeField] private int    damage    = 10;
    [SerializeField] private string playerTag = "Player";

    [Header("공격으로 해제된 뒤 재가동까지 정지 시간(초)")]
    [SerializeField] private float suppressSeconds = 6f;

    [Header("단계 진입 시 실행 (이펙트/사운드)")]
    public UnityEvent onWarn;      // 경고(곧 솟음)
    public UnityEvent onStrike;    // 솟음
    public UnityEvent onRetract;   // 들어감
    [Header("공격으로 해제됐을 때 (공격력 버프 등 연결)")]
    public UnityEvent onDisabled;

    private Phase _phase = Phase.Idle;
    private float _timer;
    private bool  _playerInside;
    private bool  _struckThisCycle;
    private float _suppressTimer;

    /// <summary>솟아 있어 공격으로 해제 가능한 상태인지.</summary>
    public bool IsExposed => _phase == Phase.Strike && _suppressTimer <= 0f;

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    void Start() => _timer = idleTime;

    void OnTriggerEnter(Collider other) { if (other.CompareTag(playerTag)) _playerInside = true; }
    void OnTriggerExit(Collider other)  { if (other.CompareTag(playerTag)) _playerInside = false; }

    void Update()
    {
        if (_suppressTimer > 0f) { _suppressTimer -= Time.deltaTime; return; }

        _timer -= Time.deltaTime;
        if (_timer > 0f) return;

        switch (_phase)
        {
            case Phase.Idle:
                _phase = Phase.Warn;   _timer = warnTime;   onWarn?.Invoke();
                break;
            case Phase.Warn:
                _phase = Phase.Strike; _timer = strikeTime; _struckThisCycle = false; onStrike?.Invoke();
                break;
            case Phase.Strike:
                _phase = Phase.Idle;   _timer = idleTime;   onRetract?.Invoke();
                break;
        }
    }

    void LateUpdate()
    {
        // 솟은 동안 플레이어가 위에 있으면 사이클당 1회 피해.
        if (_phase != Phase.Strike || _struckThisCycle || !_playerInside) return;
        if (PlayerStatusEffects.Instance != null && PlayerStatusEffects.Instance.HazardImmune) return;

        _struckThisCycle = true;
        PlayerVitals.Instance?.TakeDamage(damage);
    }

    /// <summary>
    /// [연동 지점] 솟아 있는 가시를 플레이어가 공격하면 호출(전투 코드).
    ///   예: // if (spike.IsExposed) spike.OnAttacked();
    ///   또는 명중 대상이 SpikeTrap 이면:
    ///        // hit.GetComponent&lt;SpikeTrap&gt;()?.OnAttacked();
    /// </summary>
    public void OnAttacked()
    {
        if (!IsExposed) return;
        _suppressTimer = suppressSeconds;
        _phase = Phase.Idle;
        _timer = idleTime;
        onRetract?.Invoke();
        onDisabled?.Invoke();   // 가시 해제 보상(공격력 버프) 연결
        Debug.Log($"[SpikeTrap] '{name}' 공격으로 일시 해제 {suppressSeconds}s");
    }
}
