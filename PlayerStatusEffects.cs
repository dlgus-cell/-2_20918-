using System;
using UnityEngine;

/// <summary>
/// 플레이어의 "일시 상태"(느림 / 기절 / 이속·공격 버프 / 함정 피해 면역)를
/// 한곳에서 들고 있는 허브.
///
/// ── 왜 이 파일이 필요한가 ────────────────────────────────────────────
///   함정(덩굴=느림, 감전=기절)과 해제 보상(안개=이속↑, 가시=공격력↑)은
///   "플레이어의 이동/전투"에 영향을 준다. 그런데 이동·데미지 계산은 팀원
///   (허정욱 컨트롤러 / 이연 전투) 코드 소관이라 내가 직접 못 건드린다.
///   그래서 이 컴포넌트는 상태 "값"만 들고 있고(타이머로 자동 만료),
///   실제 반영은 팀원 코드가 이 값을 "읽어" 가도록 열어 둔다.
///   (PlayerVitals 와 동일한 "훅만 열어둔다" 설계)
///
/// ── 부착 ─────────────────────────────────────────────────────────────
///   PlayerVitals / PlayerStats 와 같은 오브젝트(플레이어)에 부착.
///
/// ── 팀원 연동 지점 요약 (이 값들을 읽어서 반영) ───────────────────────
///   1) 이동(허정욱): 실제이동속도 = 기본속도 * MoveSpeedMultiplier,
///                    IsStunned 면 이동/입력 차단
///   2) 전투(이연):   플레이어 데미지 계산 시 AttackMultiplier 곱
///   (파일 하단 [연동 예시] 주석 코드 참고)
/// </summary>
public class PlayerStatusEffects : MonoBehaviour
{
    public static PlayerStatusEffects Instance { get; private set; }

    // ── 채널별 배율 / 잔여 시간 (각 채널 독립, 마지막 적용값 우선) ──
    private float _slowMult    = 1f, _slowTimer;
    private float _spdBuffMult = 1f, _spdBuffTimer;
    private float _atkBuffMult = 1f, _atkBuffTimer;
    private float _stunTimer;
    private float _immuneTimer;

    // ── 팀원/함정이 읽어가는 값 ──
    /// <summary>이동 속도 배율(느림 × 이속버프). 기본 1. 컨트롤러가 곱해서 사용.</summary>
    public float MoveSpeedMultiplier => _slowMult * _spdBuffMult;
    /// <summary>기절 중인지. 컨트롤러가 true 면 이동/입력 차단.</summary>
    public bool  IsStunned           => _stunTimer > 0f;
    /// <summary>공격력 배율. 전투 코드가 플레이어 데미지에 곱해서 사용.</summary>
    public float AttackMultiplier    => _atkBuffMult;
    /// <summary>함정 피해 면역 중인지. 함정 기믹들이 피해 전에 확인(내 영역에서 사용).</summary>
    public bool  HazardImmune        => _immuneTimer > 0f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        Tick(ref _slowTimer,    () => _slowMult    = 1f);
        Tick(ref _spdBuffTimer, () => _spdBuffMult = 1f);
        Tick(ref _atkBuffTimer, () => _atkBuffMult = 1f);
        if (_stunTimer   > 0f) _stunTimer   -= Time.deltaTime;
        if (_immuneTimer > 0f) _immuneTimer -= Time.deltaTime;
    }

    static void Tick(ref float timer, Action onExpire)
    {
        if (timer <= 0f) return;
        timer -= Time.deltaTime;
        if (timer <= 0f) onExpire();
    }

    // ── 함정 / 해제 장치가 호출하는 진입점 ──────────────────────────────

    /// <summary>느림 적용. mult 가 작을수록 느려짐(예: 0.5 = 절반 속도).</summary>
    public void ApplySlow(float mult, float seconds)
    { _slowMult = Mathf.Clamp(mult, 0.05f, 1f); _slowTimer = seconds; }

    /// <summary>기절(조작 잠금) 적용.</summary>
    public void ApplyStun(float seconds)
    { _stunTimer = Mathf.Max(_stunTimer, seconds); }

    /// <summary>이속 버프. mult 가 1 보다 클수록 빨라짐.</summary>
    public void ApplyMoveSpeedBuff(float mult, float seconds)
    { _spdBuffMult = Mathf.Max(1f, mult); _spdBuffTimer = seconds; }

    /// <summary>공격력 버프. mult 가 1 보다 큼.</summary>
    public void ApplyAttackBuff(float mult, float seconds)
    { _atkBuffMult = Mathf.Max(1f, mult); _atkBuffTimer = seconds; }

    /// <summary>함정 피해 면역(내 영역 함정들이 HazardImmune 로 확인).</summary>
    public void ApplyHazardImmunity(float seconds)
    { _immuneTimer = Mathf.Max(_immuneTimer, seconds); }

    // ════════════════════════════════════════════════════════════════════
    //  [연동 예시 — 팀원이 자기 코드에서 아래처럼 "읽어" 가면 됨]
    //
    //  ▶ 허정욱 플레이어 컨트롤러 (이동 처리부):
    //      // var fx = PlayerStatusEffects.Instance;
    //      // if (fx != null && fx.IsStunned) return;        // 기절 중 이동/입력 차단
    //      // float speed = baseMoveSpeed;
    //      // if (fx != null) speed *= fx.MoveSpeedMultiplier; // 느림/이속버프 반영
    //      // transform.position += moveDir * speed * Time.deltaTime;
    //
    //  ▶ 이연 전투 (플레이어 공격 데미지 계산부):
    //      // int dmg = baseDamage;
    //      // var fx = PlayerStatusEffects.Instance;
    //      // if (fx != null) dmg = Mathf.RoundToInt(dmg * fx.AttackMultiplier);
    //      // enemy.TakeDamage(dmg);
    // ════════════════════════════════════════════════════════════════════

    [ContextMenu("테스트/느림 0.5 (3초)")]      void _TestSlow()   => ApplySlow(0.5f, 3f);
    [ContextMenu("테스트/기절 (2초)")]           void _TestStun()   => ApplyStun(2f);
    [ContextMenu("테스트/이속버프 1.5 (5초)")]   void _TestSpd()    => ApplyMoveSpeedBuff(1.5f, 5f);
    [ContextMenu("테스트/공격버프 1.5 (5초)")]   void _TestAtk()    => ApplyAttackBuff(1.5f, 5f);
    [ContextMenu("테스트/면역 (5초)")]           void _TestImmune() => ApplyHazardImmunity(5f);
}
