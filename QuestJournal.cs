using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ─────────────────────────────────────────────────────────────────
/// <summary>
/// J 키로 열고 닫는 퀘스트 저널 창.
/// (구버전 QuestJournalUI)
/// </summary>
public class QuestJournal : MonoBehaviour
{
    [Header("루트 패널")]
    [SerializeField] private GameObject journalPanel;

    [Header("왼쪽 퀘스트 목록")]
    [SerializeField] private Transform  questListContainer;
    [SerializeField] private GameObject questListItemPrefab;

    [Header("탭 버튼")]
    [SerializeField] private Button activeTab;
    [SerializeField] private Button completedTab;

    [Header("오른쪽 상세 정보")]
    [SerializeField] private TextMeshProUGUI detailQuestName;
    [SerializeField] private TextMeshProUGUI detailDescription;
    [SerializeField] private TextMeshProUGUI detailProgress;
    [SerializeField] private TextMeshProUGUI detailStageName;
    [SerializeField] private TextMeshProUGUI detailStageDesc;
    [SerializeField] private TextMeshProUGUI detailObjectives;
    [SerializeField] private TextMeshProUGUI detailReward;

    [Header("닫기 버튼")]
    [SerializeField] private Button closeButton;

    [Header("옵션")]
    [SerializeField] private KeyCode toggleKey = KeyCode.J;

    private readonly List<GameObject> _listItems = new();
    private bool _isOpen = false;
    private bool _showingCompleted = false;

    void Start()
    {
        if (journalPanel) journalPanel.SetActive(false);

        activeTab?.onClick.AddListener(() => SwitchTab(false));
        completedTab?.onClick.AddListener(() => SwitchTab(true));
        closeButton?.onClick.AddListener(Close);

        var qm = QuestManager.Instance;
        if (qm == null) return;

        qm.OnQuestAccepted      += OnQuestEvent;
        qm.OnQuestUpdated       += OnQuestEvent;
        qm.OnQuestStageAdvanced += OnQuestEvent;
        qm.OnQuestCompleted     += OnQuestEvent;
    }

    void OnDestroy()
    {
        var qm = QuestManager.Instance;
        if (qm == null) return;

        qm.OnQuestAccepted      -= OnQuestEvent;
        qm.OnQuestUpdated       -= OnQuestEvent;
        qm.OnQuestStageAdvanced -= OnQuestEvent;
        qm.OnQuestCompleted     -= OnQuestEvent;
    }

    void OnQuestEvent(ActiveQuest _) { if (_isOpen) RefreshList(); }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey)) Toggle();
    }

    void Toggle() { if (_isOpen) Close(); else Open(); }

    void Open()
    {
        _isOpen = true;
        if (journalPanel) journalPanel.SetActive(true);
        RefreshList();
    }

    void Close()
    {
        _isOpen = false;
        if (journalPanel) journalPanel.SetActive(false);
    }

    void SwitchTab(bool showCompleted)
    {
        _showingCompleted = showCompleted;
        RefreshList();
    }

    void RefreshList()
    {
        foreach (var go in _listItems) if (go) Destroy(go);
        _listItems.Clear();

        var quests = _showingCompleted
            ? QuestManager.Instance.GetCompletedQuests()
            : QuestManager.Instance.GetActiveQuests();

        if (quests.Count == 0) { ClearDetail("퀘스트 없음"); return; }

        ActiveQuest firstQuest = quests[0];

        foreach (var quest in quests)
        {
            var go  = Instantiate(questListItemPrefab, questListContainer);
            var btn = go.GetComponent<Button>() ?? go.GetComponentInChildren<Button>();
            var tmp = go.GetComponent<TextMeshProUGUI>() ?? go.GetComponentInChildren<TextMeshProUGUI>();

            if (tmp != null)
                tmp.text = quest.isCompleted ? $"✓ {quest.questData.questName}" : quest.questData.questName;

            var captured = quest;
            btn?.onClick.AddListener(() => ShowDetail(captured));

            _listItems.Add(go);
        }

        ShowDetail(firstQuest);
    }

    void ShowDetail(ActiveQuest quest)
    {
        var data  = quest.questData;
        var stage = quest.GetCurrentStage();

        SetText(detailQuestName,   data.questName);
        SetText(detailDescription, data.description);

        if (quest.isCompleted)
        {
            SetText(detailProgress,   "완료 ★");
            SetText(detailStageName,  "모든 단계 완료");
            SetText(detailStageDesc,  "");
            SetText(detailObjectives, "모든 목표를 달성했습니다.");
        }
        else if (stage != null)
        {
            SetText(detailProgress,  $"단계  {quest.currentStageIndex + 1} / {quest.stages.Count}");
            SetText(detailStageName, stage.stageTitle);
            SetText(detailStageDesc, stage.stageDescription);

            var sb = new StringBuilder();
            foreach (var obj in stage.objectives)
            {
                string check  = obj.IsCompleted ? "✓" : "○";
                string amount = obj.requiredAmount > 1
                    ? $"  ({obj.currentAmount}/{obj.requiredAmount})" : "";
                sb.AppendLine($"{check}  {obj.description}{amount}");
            }
            if (stage.hasDestination)
                sb.AppendLine($"\n📍 목적지:  {stage.destinationName}");
            SetText(detailObjectives, sb.ToString().TrimEnd());
        }
        else
        {
            SetText(detailProgress,   "");
            SetText(detailStageName,  "");
            SetText(detailStageDesc,  "");
            SetText(detailObjectives, "");
        }

        var r      = data.reward;
        string reward = $"🪙 골드      {r.gold}\n✨ 경험치   {r.experience}";
        if (!string.IsNullOrEmpty(r.additionalDescription))
            reward += $"\n{r.additionalDescription}";
        SetText(detailReward, reward);
    }

    void ClearDetail(string message = "")
    {
        SetText(detailQuestName,   message);
        SetText(detailDescription, "");
        SetText(detailProgress,    "");
        SetText(detailStageName,   "");
        SetText(detailStageDesc,   "");
        SetText(detailObjectives,  "");
        SetText(detailReward,      "");
    }

    static void SetText(TextMeshProUGUI tmp, string text)
    {
        if (tmp) tmp.text = text;
    }
}
