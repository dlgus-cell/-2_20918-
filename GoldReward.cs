using UnityEngine;

/// <summary>
/// 퀘스트 보상 헬퍼.
/// 정적 호출: GoldReward.Grant(500);
/// 또는 MonoBehaviour로 씬에 배치 후 GiveReward() 호출.
/// </summary>
public class GoldReward : MonoBehaviour
{
    [Header("이 퀘스트의 골드 보상")]
    [SerializeField] private int rewardAmount = 100;

    [Header("퀘스트 이름 (로그용)")]
    [SerializeField] private string questName = "퀘스트";

    public static void Grant(int amount, string questNameLog = "")
    {
        if (GoldSystem.Instance == null)
        {
            Debug.LogError("[GoldReward] GoldSystem 인스턴스가 없습니다.");
            return;
        }

        GoldSystem.Instance.AddGold(amount, GoldChangeReason.QuestReward);
        if (!string.IsNullOrEmpty(questNameLog))
            Debug.Log($"[GoldReward] 퀘스트 완료: '{questNameLog}' → +{amount}G");
    }

    public void GiveReward() => Grant(rewardAmount, questName);

    public void SetReward(int amount, string name = "")
    {
        rewardAmount = amount;
        if (!string.IsNullOrEmpty(name)) questName = name;
    }
}
