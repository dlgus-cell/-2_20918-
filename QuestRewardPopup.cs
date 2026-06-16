using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ─────────────────────────────────────────────────────────────────
/// <summary>
/// 퀘스트 완료 시 보상 팝업 표시 + Gold·XP 실제 지급.
/// (구버전 QuestRewardUI)
/// </summary>
public class QuestRewardPopup : MonoBehaviour
{
    [Header("UI 레퍼런스")]
    [SerializeField] private GameObject      rewardPanel;
    [SerializeField] private TextMeshProUGUI completeTitle;
    [SerializeField] private TextMeshProUGUI questNameText;
    [SerializeField] private TextMeshProUGUI rewardText;
    [SerializeField] private TextMeshProUGUI goldDeltaText;
    [SerializeField] private TextMeshProUGUI xpDeltaText;
    [SerializeField] private Button          closeButton;

    [Header("애니메이션 (선택)")]
    [SerializeField] private Animator panelAnimator;
    [SerializeField] private string   showTrigger = "Show";

    [Header("옵션")]
    [SerializeField] private float autoCloseSeconds = 0f;

    void Start()
    {
        if (rewardPanel) rewardPanel.SetActive(false);
        closeButton?.onClick.AddListener(ClosePanel);

        if (QuestManager.Instance == null)
        {
            Debug.LogWarning("[QuestRewardPopup] QuestManager 인스턴스 없음 — 이벤트 구독 실패");
            return;
        }

        QuestManager.Instance.OnQuestCompleted += OnQuestCompleted;
    }

    void OnDestroy()
    {
        if (QuestManager.Instance != null)
            QuestManager.Instance.OnQuestCompleted -= OnQuestCompleted;
    }

    void OnQuestCompleted(ActiveQuest quest)
    {
        var reward = quest.questData.reward;

        int goldGranted = 0;
        if (reward.gold > 0 && GoldSystem.Instance != null)
        {
            GoldSystem.Instance.AddGold(reward.gold, GoldChangeReason.QuestReward);
            goldGranted = reward.gold;
        }

        int xpGranted = 0;
        if (reward.experience > 0 && PlayerStats.Instance != null)
        {
            PlayerStats.Instance.AddXP(reward.experience);
            xpGranted = reward.experience;
        }

        ShowPopup(quest, goldGranted, xpGranted);
    }

    void ShowPopup(ActiveQuest quest, int goldGranted, int xpGranted)
    {
        if (rewardPanel) rewardPanel.SetActive(true);

        if (panelAnimator && !string.IsNullOrEmpty(showTrigger))
            panelAnimator.SetTrigger(showTrigger);

        if (completeTitle) completeTitle.text = "퀘스트 완료!";
        if (questNameText) questNameText.text = quest.questData.questName;

        if (rewardText)
        {
            var r  = quest.questData.reward;
            var sb = new StringBuilder("─── 보상 획득 ───");
            if (!string.IsNullOrEmpty(r.additionalDescription))
                sb.AppendLine($"\n{r.additionalDescription}");
            rewardText.text = sb.ToString().TrimEnd();
        }

        if (goldDeltaText)
        {
            goldDeltaText.gameObject.SetActive(goldGranted > 0);
            goldDeltaText.text = $"🪙  +{goldGranted:N0} G";
        }

        if (xpDeltaText)
        {
            xpDeltaText.gameObject.SetActive(xpGranted > 0);
            xpDeltaText.text = $"✨  +{xpGranted:N0} XP";
        }

        if (autoCloseSeconds > 0)
            StartCoroutine(AutoClose(autoCloseSeconds));
    }

    IEnumerator AutoClose(float delay)
    {
        yield return new WaitForSeconds(delay);
        ClosePanel();
    }

    void ClosePanel()
    {
        if (rewardPanel) rewardPanel.SetActive(false);
    }
}
