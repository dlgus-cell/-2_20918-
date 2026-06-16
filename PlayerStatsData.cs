using System;
using System.Collections.Generic;
using UnityEngine;

// ─────────────────────────────────────────────────────────────────
/// <summary>
/// 저장용 스탯 데이터.
/// ★ 5단계 변경: SerializableDictionary(Dictionary 상속) →
///               List&lt;StatInvestment&gt; 로 교체 (JsonUtility 호환)
/// </summary>
[Serializable]
public class PlayerStatsData
{
    public int level      = 1;
    public int currentXP  = 0;
    public int statPoints = 0;

    // ★ JsonUtility는 Dictionary를 직렬화 못하므로 List 사용
    public List<StatInvestment> invested = new();

    [Serializable]
    public class StatInvestment
    {
        public string stat;
        public int    amount;
    }

    public int GetInvested(string statName)
    {
        var entry = invested.Find(s => s.stat == statName);
        return entry != null ? entry.amount : 0;
    }

    public void SetInvested(string statName, int amount)
    {
        var entry = invested.Find(s => s.stat == statName);
        if (entry != null) entry.amount = amount;
        else invested.Add(new StatInvestment { stat = statName, amount = amount });
    }
}
