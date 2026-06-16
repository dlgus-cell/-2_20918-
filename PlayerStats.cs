using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 핵심 스탯 시스템 싱글톤.
///
/// ★ 통폐합 변경 (5단계):
///   - PlayerStatsData.SerializableDictionary → List&lt;StatInvestment&gt; 로 교체
///     (JsonUtility는 Dictionary를 직렬화하지 못함 — 저장 버그 수정)
///   - GetSaveData() / LoadSaveData() 내부 로직을 새 구조에 맞게 수정
/// </summary>
public class PlayerStats : MonoBehaviour
{
    // ─── 싱글톤 ──────────────────────────────────────────────
    public static PlayerStats Instance { get; private set; }

    // ─── 이벤트 ──────────────────────────────────────────────
    public static event Action            OnStatsChanged;
    public static event Action<int>       OnStatPointsChanged;
    public static event Action<int, int>  OnLevelUp;
    public static event Action<int, int>  OnXPChanged;
    public static event Action<int>       OnMaxHPChanged;
    public static event Action<int>       OnMaxStaminaChanged;

    // ─── Inspector ───────────────────────────────────────────
    [Header("플레이어 정보")]
    [SerializeField] private string playerName       = "플레이어";

    [Header("레벨 & 경험치")]
    [SerializeField] private int   startLevel        = 1;
    [SerializeField] private int   baseXPRequired    = 100;
    [SerializeField] private float xpScaling         = 1.5f;
    [SerializeField] private int   statPointsPerLevel = 3;

    [Header("스탯 기본값 (레벨 1 기준)")]
    [SerializeField] private int baseAttack      = 10;
    [SerializeField] private int baseMaxHP       = 100;
    [SerializeField] private int baseMaxStamina  = 50;
    [SerializeField] private int baseAgility     = 5;
    [SerializeField] private int baseMagicAttack = 5;
    [SerializeField] private int baseDefense     = 5;

    [Header("스탯 1포인트당 증가량")]
    [SerializeField] private int attackPerPoint  = 5;
    [SerializeField] private int hpPerPoint      = 20;
    [SerializeField] private int staminaPerPoint = 15;
    [SerializeField] private int agilityPerPoint = 2;
    [SerializeField] private int magicPerPoint   = 4;
    [SerializeField] private int defensePerPoint = 3;

    // ─── 런타임 상태 ─────────────────────────────────────────
    public string PlayerName => playerName;
    public int    Level      { get; private set; }
    public int    CurrentXP  { get; private set; }
    public int    StatPoints { get; private set; }

    private readonly Dictionary<StatType, int> _invested = new();

    // ─── 파생 스탯 ───────────────────────────────────────────
    public int   AttackPower         => baseAttack      + GetInvested(StatType.Strength)     * attackPerPoint;
    public int   MaxHP               => baseMaxHP       + GetInvested(StatType.Vitality)      * hpPerPoint;
    public int   MaxStamina          => baseMaxStamina  + GetInvested(StatType.Stamina)       * staminaPerPoint;
    public int   AgilityValue        => baseAgility     + GetInvested(StatType.Agility)       * agilityPerPoint;
    public int   MagicAttack         => baseMagicAttack + GetInvested(StatType.Intelligence)  * magicPerPoint;
    public int   Defense             => baseDefense     + GetInvested(StatType.Defense)       * defensePerPoint;
    public float MoveSpeedMultiplier => 1f + GetInvested(StatType.Agility) * 0.02f;

    // ─────────────────────────────────────────────────────────
    #region Unity Lifecycle

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Level = startLevel;
        foreach (StatType t in Enum.GetValues(typeof(StatType)))
            _invested[t] = 0;
    }

    #endregion

    // ─────────────────────────────────────────────────────────
    #region XP & Level

    public void AddXP(int amount)
    {
        if (amount <= 0) return;
        CurrentXP += amount;
        Debug.Log($"[PlayerStats] +{amount} XP → {CurrentXP}/{GetXPRequired(Level)}");

        while (CurrentXP >= GetXPRequired(Level))
        {
            CurrentXP -= GetXPRequired(Level);
            LevelUp();
        }

        OnXPChanged?.Invoke(CurrentXP, GetXPRequired(Level));
    }

    public int   GetXPRequired(int lv) =>
        Mathf.RoundToInt(baseXPRequired * Mathf.Pow(lv, xpScaling));

    public float XPProgress =>
        GetXPRequired(Level) > 0 ? (float)CurrentXP / GetXPRequired(Level) : 0f;

    void LevelUp()
    {
        int prev = Level;
        Level++;
        StatPoints += statPointsPerLevel;

        Debug.Log($"[PlayerStats] ★ 레벨업! {prev} → {Level}  (포인트 +{statPointsPerLevel})");

        OnLevelUp?.Invoke(prev, Level);
        OnStatPointsChanged?.Invoke(StatPoints);
        OnStatsChanged?.Invoke();
        OnMaxHPChanged?.Invoke(MaxHP);
        OnMaxStaminaChanged?.Invoke(MaxStamina);
    }

    #endregion

    // ─────────────────────────────────────────────────────────
    #region Stat Investment

    public bool InvestPoint(StatType stat)
    {
        if (StatPoints <= 0)
        {
            Debug.Log("[PlayerStats] 스탯 포인트가 없습니다.");
            return false;
        }

        _invested[stat]++;
        StatPoints--;

        Debug.Log($"[PlayerStats] {stat} 투자 → {GetInvested(stat)}pt  (남은: {StatPoints})");

        OnStatPointsChanged?.Invoke(StatPoints);
        OnStatsChanged?.Invoke();
        FireDerivedEvents(stat);
        return true;
    }

    public int GetInvested(StatType stat) =>
        _invested.TryGetValue(stat, out int v) ? v : 0;

    public int GetStatValue(StatType stat) => stat switch
    {
        StatType.Strength     => AttackPower,
        StatType.Vitality     => MaxHP,
        StatType.Stamina      => MaxStamina,
        StatType.Agility      => AgilityValue,
        StatType.Intelligence => MagicAttack,
        StatType.Defense      => Defense,
        _                     => 0
    };

    public int GetBaseValue(StatType stat) => stat switch
    {
        StatType.Strength     => baseAttack,
        StatType.Vitality     => baseMaxHP,
        StatType.Stamina      => baseMaxStamina,
        StatType.Agility      => baseAgility,
        StatType.Intelligence => baseMagicAttack,
        StatType.Defense      => baseDefense,
        _                     => 0
    };

    public int GetPerPoint(StatType stat) => stat switch
    {
        StatType.Strength     => attackPerPoint,
        StatType.Vitality     => hpPerPoint,
        StatType.Stamina      => staminaPerPoint,
        StatType.Agility      => agilityPerPoint,
        StatType.Intelligence => magicPerPoint,
        StatType.Defense      => defensePerPoint,
        _                     => 1
    };

    void FireDerivedEvents(StatType stat)
    {
        if (stat == StatType.Vitality) OnMaxHPChanged?.Invoke(MaxHP);
        if (stat == StatType.Stamina)  OnMaxStaminaChanged?.Invoke(MaxStamina);
    }

    #endregion

    // ─────────────────────────────────────────────────────────
    #region Save / Load  ★ JsonUtility 호환 버전

    /// <summary>현재 상태를 직렬화 가능한 데이터로 반환합니다.</summary>
    public PlayerStatsData GetSaveData()
    {
        var data = new PlayerStatsData
        {
            level      = Level,
            currentXP  = CurrentXP,
            statPoints = StatPoints
        };

        // ★ Dictionary → List<StatInvestment> 로 변환 (JsonUtility 호환)
        foreach (StatType t in Enum.GetValues(typeof(StatType)))
            data.SetInvested(t.ToString(), GetInvested(t));

        return data;
    }

    /// <summary>저장 데이터를 로드하여 상태를 복원합니다.</summary>
    public void LoadSaveData(PlayerStatsData data)
    {
        if (data == null) return;

        Level      = Mathf.Max(1, data.level);
        CurrentXP  = Mathf.Max(0, data.currentXP);
        StatPoints = Mathf.Max(0, data.statPoints);

        // ★ List<StatInvestment> → Dictionary 로 복원
        foreach (StatType t in Enum.GetValues(typeof(StatType)))
            _invested[t] = data.GetInvested(t.ToString());

        OnStatsChanged?.Invoke();
        OnXPChanged?.Invoke(CurrentXP, GetXPRequired(Level));
        OnStatPointsChanged?.Invoke(StatPoints);
        OnMaxHPChanged?.Invoke(MaxHP);
        OnMaxStaminaChanged?.Invoke(MaxStamina);

        Debug.Log($"[PlayerStats] 데이터 복원 완료  Lv.{Level}  XP:{CurrentXP}  포인트:{StatPoints}");
    }

    #endregion
}
