using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 화면 왼쪽에 현재 퀘스트 목표를 표시하는 HUD.
/// (구버전 QuestHUD)
/// </summary>
public class QuestHud : MonoBehaviour
{
    [Header("패널 루트")]
    [SerializeField] private GameObject hudPanel;

    [Header("텍스트 레퍼런스")]
    [SerializeField] private TextMeshProUGUI questTitleText;
    [SerializeField] private TextMeshProUGUI stageTitleText;

    [Header("목표 목록")]
    [SerializeField] private Transform  objectiveContainer;
    [SerializeField] private GameObject objectiveItemPrefab;

    [Header("옵션")]
    [SerializeField] private int maxDisplayedQuests = 1;

    private readonly List<GameObject> _spawnedItems = new();

    void Start()
    {
        if (hudPanel) hudPanel.SetActive(false);

        var qm = QuestManager.Instance;
        if (qm == null) return;

        qm.OnQuestAccepted      += OnQuestEvent;
        qm.OnQuestUpdated       += OnQuestEvent;
        qm.OnQuestStageAdvanced += OnQuestEvent;
        qm.OnQuestCompleted     += OnQuestEvent;
        qm.OnQuestsRestored     += Refresh;
        qm.OnQuestsCleared      += Refresh;
    }

    void OnDestroy()
    {
        var qm = QuestManager.Instance;
        if (qm == null) return;

        qm.OnQuestAccepted      -= OnQuestEvent;
        qm.OnQuestUpdated       -= OnQuestEvent;
        qm.OnQuestStageAdvanced -= OnQuestEvent;
        qm.OnQuestCompleted     -= OnQuestEvent;
        qm.OnQuestsRestored     -= Refresh;
        qm.OnQuestsCleared      -= Refresh;
    }

    void OnQuestEvent(ActiveQuest _) => Refresh();

    void Refresh()
    {
        ClearItems();

        var activeQuests = QuestManager.Instance.GetActiveQuests();

        if (activeQuests.Count == 0)
        {
            if (hudPanel) hudPanel.SetActive(false);
            return;
        }

        if (hudPanel) hudPanel.SetActive(true);

        int shown = Mathf.Min(activeQuests.Count, maxDisplayedQuests);
        for (int i = 0; i < shown; i++)
            BuildQuestSection(activeQuests[i], i > 0);
    }

    void BuildQuestSection(ActiveQuest quest, bool addSeparator)
    {
        var stage = quest.GetCurrentStage();
        if (stage == null) return;

        if (addSeparator)
            SpawnLabel("───────────────", Color.gray * 0.8f);

        if (questTitleText != null) questTitleText.text = quest.questData.questName;
        else SpawnLabel($"[ {quest.questData.questName} ]", new Color(1f, 0.85f, 0.3f));

        if (stageTitleText != null) stageTitleText.text = $"▸ {stage.stageTitle}";
        else SpawnLabel($"▸ {stage.stageTitle}", new Color(0.8f, 0.9f, 1f));

        foreach (var obj in stage.objectives)
        {
            string icon   = obj.IsCompleted ? "✓" : "○";
            string amount = obj.requiredAmount > 1 ? $" ({obj.currentAmount}/{obj.requiredAmount})" : "";
            SpawnLabel($"  {icon} {obj.description}{amount}",
                       obj.IsCompleted ? Color.green : Color.white);
        }

        if (stage.hasDestination && !string.IsNullOrEmpty(stage.destinationName))
            SpawnLabel($"  📍 {stage.destinationName}", new Color(1f, 1f, 0.4f));
    }

    void SpawnLabel(string text, Color color)
    {
        if (objectiveItemPrefab == null || objectiveContainer == null) return;
        var go  = Instantiate(objectiveItemPrefab, objectiveContainer);
        var tmp = go.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null) { tmp.text = text; tmp.color = color; }
        _spawnedItems.Add(go);
    }

    void ClearItems()
    {
        foreach (var go in _spawnedItems) if (go) Destroy(go);
        _spawnedItems.Clear();
    }
}
