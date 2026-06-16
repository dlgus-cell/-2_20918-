using System;
using UnityEngine;

/// <summary>
/// 플레이어의 "현재" 체력(HP)과 기력(Stamina)을 관리하는 시스템.
///
/// ── 왜 이 파일이 필요한가 ─────────────────────────────────────────────
///   기존 PlayerStats 는 MaxHP / MaxStamina (최대치, 파생값)만 가지고 있고,
///   "지금 남은 체력/기력"을 들고 있는 주체가 어디에도 없었다.
///   체력/기력 HUD(필수 요구사항), Heart 드롭 회복, 소비아이템 효과가
///   전부 이 "현재값"을 필요로 하므로, 그 빈자리를 채우는 컴포넌트다.
///
/// ── 설계 원칙 ────────────────────────────────────────────────────────
///   - 다른 담당자(AI팀/전투팀)의 코드는 절대 수정하지 않는다.
///   - 외부에서 호출할 자리는 public 메서드 + 이벤트로 "열어만" 둔다.
///     실제 연결(누가 언제 부를지)은 각 담당자가 판단한다.
///     → 연결 지점은 모두 "[연동 지점]" 주석으로 표시했다.
///   - 최대치는 PlayerStats 에서 읽는다. PlayerStats 가 씬에 없어도
///     standalone 값으로 단독 테스트가 가능하다.
///
/// ── 외부 연결 지점 요약 (다른 코드가 호출) ───────────────────────────
///   1. 전투팀 Enemy/Boss 공격     → PlayerVitals.Instance.TakeDamage(dmg)
///   2. 전투팀 Item(Type.Heart)    → PlayerVitals.Instance.Heal(value)
///   3. 내 Inventory 소비아이템 사용 → PlayerVitals.Instance.ApplyConsumable(effect)
///   4. 플레이어 사망 처리(허정욱)   → PlayerVitals.OnDeath 구독
///   5. 능력/스킬 사용              → PlayerVitals.Instance.UseStamina(cost)
/// </summary>
public class PlayerVitals : MonoBehaviour
{
    // ───── 싱글톤 (PlayerStats / GoldSystem 등 기존 패턴과 동일) ─────
    public static PlayerVitals Instance { get; private set; }

    // ───── 이벤트 (HUD 등이 구독. PlayerStats 와 동일하게 static) ─────
    /// <summary>(현재 HP, 최대 HP)</summary>
    public static event Action<int, int> OnHPChanged;
    /// <summary>(현재 기력, 최대 기력)</summary>
    public static event Action<int, int> OnStaminaChanged;
    /// <summary>체력이 0이 되어 사망한 순간 1회 발생.</summary>
    public static event Action           OnDeath;
    /// <summary>부활(Revive) 시 발생.</summary>
    public static event Action           OnRevived;

    // ───── Inspector ─────
    [Header("PlayerStats 가 없을 때 단독 테스트용 최대치")]
    [SerializeField] private bool useStandaloneWhenNoStats = true;
    [SerializeField] private int  standaloneMaxHP      = 100;
    [SerializeField] private int  standaloneMaxStamina = 50;

    [Header("시작 시 가득 채운 상태로 시작")]
    [SerializeField] private bool startFull = true;

    [Header("기력 자동 회복 (초당, 0이면 비활성)")]
    [SerializeField] private float staminaRegenPerSecond = 0f;

    // ───── 현재값 ─────
    public int  CurrentHP      { get; private set; }
    public int  CurrentStamina { get; private set; }
    public bool IsDead         { get; private set; }

    private float _staminaRegenAccum;

    // ───── 최대치 (PlayerStats 우선, 없으면 standalone) ─────
    public int MaxHP =>
        (PlayerStats.Instance != null) ? PlayerStats.Instance.MaxHP
                                       : (useStandaloneWhenNoStats ? standaloneMaxHP : 1);

    public int MaxStamina =>
        (PlayerStats.Instance != null) ? PlayerStats.Instance.MaxStamina
                                       : (useStandaloneWhenNoStats ? standaloneMaxStamina : 1);

    public float HPProgress      => MaxHP      > 0 ? (float)CurrentHP      / MaxHP      : 0f;
    public float StaminaProgress => MaxStamina > 0 ? (float)CurrentStamina / MaxStamina : 0f;

    // ─────────────────────────────────────────────────────────────────
    #region Unity Lifecycle

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        // 최대치가 바뀌면(레벨업 / Vitality·Stamina 투자) 현재값을 맞춰준다.
        PlayerStats.OnMaxHPChanged      += HandleMaxHPChanged;
        PlayerStats.OnMaxStaminaChanged += HandleMaxStaminaChanged;
    }

    void OnDisable()
    {
        PlayerStats.OnMaxHPChanged      -= HandleMaxHPChanged;
        PlayerStats.OnMaxStaminaChanged -= HandleMaxStaminaChanged;
    }

    void Start()
    {
        if (startFull)
        {
            CurrentHP      = MaxHP;
            CurrentStamina = MaxStamina;
        }
        else
        {
            CurrentHP      = Mathf.Clamp(CurrentHP,      0, MaxHP);
            CurrentStamina = Mathf.Clamp(CurrentStamina, 0, MaxStamina);
        }

        _lastMaxHP = MaxHP;

        // HUD 가 초기값을 그릴 수 있도록 한 번 알림.
        OnHPChanged?.Invoke(CurrentHP, MaxHP);
        OnStaminaChanged?.Invoke(CurrentStamina, MaxStamina);
    }

    void Update()
    {
        if (staminaRegenPerSecond > 0f && !IsDead && CurrentStamina < MaxStamina)
        {
            _staminaRegenAccum += staminaRegenPerSecond * Time.deltaTime;
            if (_staminaRegenAccum >= 1f)
            {
                int gain = Mathf.FloorToInt(_staminaRegenAccum);
                _staminaRegenAccum -= gain;
                RestoreStamina(gain);
            }
        }
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────
    #region 체력 (HP)

    /// <summary>[연동 지점 1] 전투팀 Enemy/Boss 가 플레이어 타격 시 호출.</summary>
    public void TakeDamage(int amount)
    {
        if (IsDead || amount <= 0) return;

        CurrentHP = Mathf.Max(0, CurrentHP - amount);
        OnHPChanged?.Invoke(CurrentHP, MaxHP);

        if (CurrentHP == 0) Die();
    }

    /// <summary>[연동 지점 2] 전투팀 Item(Type.Heart) 줍기 / 회복 시 호출.</summary>
    public void Heal(int amount)
    {
        if (IsDead || amount <= 0) return;

        CurrentHP = Mathf.Min(MaxHP, CurrentHP + amount);
        OnHPChanged?.Invoke(CurrentHP, MaxHP);
    }

    /// <summary>
    /// [연동 지점 6] HP 를 특정 값으로 직접 설정. 저장 데이터 로드(이어하기) 시
    /// 저장됐던 현재 HP 를 그대로 복원하는 용도. 0 이하로 설정되면 사망 처리.
    /// </summary>
    public void SetHP(int value)
    {
        CurrentHP = Mathf.Clamp(value, 0, MaxHP);
        OnHPChanged?.Invoke(CurrentHP, MaxHP);
        if (CurrentHP == 0 && !IsDead) Die();
    }

    void Die()
    {
        if (IsDead) return;
        IsDead = true;
        Debug.Log("[PlayerVitals] 플레이어 사망 (HP 0)");
        // [연동 지점 4] 실제 사망 연출/리스폰은 플레이어 담당(허정욱) 코드가
        //              OnDeath 를 구독하여 처리한다. 여기서는 신호만 보낸다.
        OnDeath?.Invoke();
    }

    /// <summary>부활. hpFraction = 회복할 체력 비율(0~1).</summary>
    public void Revive(float hpFraction = 1f)
    {
        IsDead = false;
        CurrentHP      = Mathf.Clamp(Mathf.RoundToInt(MaxHP * hpFraction), 1, MaxHP);
        CurrentStamina = MaxStamina;
        OnHPChanged?.Invoke(CurrentHP, MaxHP);
        OnStaminaChanged?.Invoke(CurrentStamina, MaxStamina);
        OnRevived?.Invoke();
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────
    #region 기력 (Stamina / 기력)

    /// <summary>
    /// [연동 지점 5] 능력/스킬 사용 시 호출. 기력이 충분하면 소모하고 true,
    /// 부족하면 아무 일도 하지 않고 false 를 반환한다.
    /// </summary>
    public bool UseStamina(int amount)
    {
        if (amount <= 0) return true;
        if (CurrentStamina < amount) return false;

        CurrentStamina -= amount;
        OnStaminaChanged?.Invoke(CurrentStamina, MaxStamina);
        return true;
    }

    public void RestoreStamina(int amount)
    {
        if (amount <= 0) return;
        CurrentStamina = Mathf.Min(MaxStamina, CurrentStamina + amount);
        OnStaminaChanged?.Invoke(CurrentStamina, MaxStamina);
    }

    /// <summary>기력을 특정 값으로 직접 설정. 저장 데이터 로드 복원용.</summary>
    public void SetStamina(int value)
    {
        CurrentStamina = Mathf.Clamp(value, 0, MaxStamina);
        OnStaminaChanged?.Invoke(CurrentStamina, MaxStamina);
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────
    #region 소비아이템 연결 (내 도메인 안에서 처리)

    /// <summary>
    /// [연동 지점 3] 내 Inventory 의 소비아이템 사용 시 호출.
    /// ItemData.consumableEffect 를 그대로 넘기면 효과를 적용한다.
    ///   - restoreHp  → Heal
    ///   - restoreMp  → RestoreStamina  (ConsumableEffect 는 'Mp' 로 부르지만
    ///                  실제 자원은 기력/Stamina 바와 동일하게 취급)
    ///   - addGold    → GoldSystem.AddGold
    ///   - customTag  → 특수 효과. 별도 시스템 필요 시 여기서 분기.
    /// </summary>
    public void ApplyConsumable(ConsumableEffect effect)
    {
        if (effect == null) return;

        if (effect.restoreHp > 0) Heal(effect.restoreHp);
        if (effect.restoreMp > 0) RestoreStamina(effect.restoreMp);

        if (effect.addGold > 0 && GoldSystem.Instance != null)
            GoldSystem.Instance.AddGold(effect.addGold);

        if (!string.IsNullOrEmpty(effect.customTag))
            Debug.Log($"[PlayerVitals] customTag 효과 미처리: '{effect.customTag}' " +
                      $"(특수 효과 시스템이 생기면 여기서 분기)");
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────
    #region 최대치 변동 반영 (PlayerStats 이벤트 핸들러)

    // 최대 HP 의 직전 값. 증가분만큼 현재 HP 도 올려주기 위해 추적한다.
    private int _lastMaxHP = -1;

    void HandleMaxHPChanged(int newMax)
    {
        if (_lastMaxHP < 0) _lastMaxHP = newMax;   // 첫 호출 보호
        int delta = newMax - _lastMaxHP;
        _lastMaxHP = newMax;

        // 최대치가 늘면 늘어난 만큼 현재 HP 도 채워준다(흔한 RPG 동작).
        if (delta > 0 && !IsDead) CurrentHP += delta;
        CurrentHP = Mathf.Clamp(CurrentHP, 0, newMax);

        OnHPChanged?.Invoke(CurrentHP, newMax);
    }

    void HandleMaxStaminaChanged(int newMax)
    {
        CurrentStamina = Mathf.Clamp(CurrentStamina, 0, newMax);
        OnStaminaChanged?.Invoke(CurrentStamina, newMax);
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────
    #region 에디터 테스트용 (키 충돌 없이 우클릭 → 컨텍스트 메뉴로 실행)

    [ContextMenu("테스트/데미지 10")]
    void _TestDamage10() => TakeDamage(10);

    [ContextMenu("테스트/회복 10")]
    void _TestHeal10() => Heal(10);

    [ContextMenu("테스트/기력 5 사용")]
    void _TestUseStamina5() => Debug.Log($"[PlayerVitals] UseStamina(5) = {UseStamina(5)}");

    [ContextMenu("테스트/기력 10 회복")]
    void _TestRestoreStamina10() => RestoreStamina(10);

    [ContextMenu("테스트/즉사")]
    void _TestKill() => TakeDamage(CurrentHP);

    [ContextMenu("테스트/부활(풀피)")]
    void _TestRevive() => Revive(1f);

    [ContextMenu("테스트/현재 상태 출력")]
    void _TestDump() =>
        Debug.Log($"[PlayerVitals] HP {CurrentHP}/{MaxHP}  STA {CurrentStamina}/{MaxStamina}  Dead={IsDead}");

    #endregion
}
