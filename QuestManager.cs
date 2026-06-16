using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 퀘스트 시스템의 핵심 매니저. DontDestroyOnLoad 싱글톤.
/// ★ 저장 시스템과 연동하기 위해 ClearAllQuests / RestoreActiveQuest /
///    RestoreCompletedQuest 메서드가 추가된 버전입니다. ★
/// </summary>
public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    // ── 런타임 상태 ──────────────────────────────────────────────────
    private readonly List<ActiveQuest> _activeQuests    = new();
    private readonly List<ActiveQuest> _completedQuests = new();

    // ── 이벤트 ───────────────────────────────────────────────────────
    public event System.Action<ActiveQuest> OnQuestAccepted;
    public event System.Action<ActiveQuest> OnQuestUpdated;
    public event System.Action<ActiveQuest> OnQuestStageAdvanced;
    public event System.Action<ActiveQuest> OnQuestCompleted;

    // 저장·불러오기 이벤트
    public event System.Action OnQuestsCleared;
    public event System.Action OnQuestsRestored;

    // ─────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ════════════════════════════════════════════════════════════════
    //  공개 API — 기존과 동일
    // ════════════════════════════════════════════════════════════════

    public bool AcceptQuest(QuestDef data)
    {
        if (data == null) { Debug.LogWarning("[QuestManager] QuestDef가 null입니다."); return false; }

        if (_activeQuests.Any(q => q.questData.questID == data.questID))
        {
            Debug.Log($"[QuestManager] 이미 진행 중: {data.questName}");
            return false;
        }
        if (_completedQuests.Any(q => q.questData.questID == data.questID))
        {
            Debug.Log($"[QuestManager] 이미 완료됨: {data.questName}");
            return false;
        }

        var quest = new ActiveQuest(data);
        _activeQuests.Add(quest);
        OnQuestAccepted?.Invoke(quest);
        Debug.Log($"[QuestManager] 퀘스트 수락 ▶ {data.questName}");
        return true;
    }

    public void UpdateObjective(string questID, string objectiveID, int amount = 1)
    {
        var quest = _activeQuests.FirstOrDefault(q => q.questData.questID == questID);
        if (quest == null) return;

        var stage = quest.GetCurrentStage();
        if (stage == null) return;

        var obj = stage.objectives.FirstOrDefault(o => o.objectiveID == objectiveID);
        if (obj == null) return;

        obj.currentAmount = Mathf.Min(obj.currentAmount + amount, obj.requiredAmount);
        Debug.Log($"[QuestManager] 목표 진행: {obj.description}  {obj.currentAmount}/{obj.requiredAmount}");

        OnQuestUpdated?.Invoke(quest);

        if (stage.objectives.All(o => o.IsCompleted))
            AdvanceOrComplete(quest);
    }

    public List<ActiveQuest> GetActiveQuests()    => new(_activeQuests);
    public List<ActiveQuest> GetCompletedQuests() => new(_completedQuests);
    public ActiveQuest GetActiveQuest(string questID)
        => _activeQuests.FirstOrDefault(q => q.questData.questID == questID);

    public bool IsQuestActive(string questID)
        => _activeQuests.Any(q => q.questData.questID == questID);

    public bool IsQuestCompleted(string questID)
        => _completedQuests.Any(q => q.questData.questID == questID);

    // ════════════════════════════════════════════════════════════════
    //  ★ 저장 시스템 연동용 메서드 (신규 추가) ★
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 불러오기 전에 모든 퀘스트를 초기화합니다.
    /// SaveSystem에서 호출합니다.
    /// </summary>
    public void ClearAllQuests()
    {
        _activeQuests.Clear();
        _completedQuests.Clear();
        OnQuestsCleared?.Invoke();
        Debug.Log("[QuestManager] 퀘스트 상태 초기화됨 (불러오기 준비)");
    }

    /// <summary>
    /// 저장 데이터에서 활성 퀘스트를 복원합니다.
    /// SaveSystem에서 호출합니다.
    /// </summary>
    public void RestoreActiveQuest(QuestDef data, ActiveQuestRecord record)
    {
        var quest = new ActiveQuest(data);
        quest.currentStageIndex = Mathf.Clamp(record.currentStageIndex, 0, quest.stages.Count - 1);

        // 목표 진행도 복원
        foreach (var objRec in record.objectives)
        {
            if (objRec.stageIndex < quest.stages.Count)
            {
                var obj = quest.stages[objRec.stageIndex].objectives
                    .FirstOrDefault(o => o.objectiveID == objRec.objectiveID);
                if (obj != null)
                    obj.currentAmount = objRec.currentAmount;
            }
        }

        _activeQuests.Add(quest);
        Debug.Log($"[QuestManager] 복원 (활성): {data.questName}  단계 {quest.currentStageIndex + 1}");
    }

    /// <summary>
    /// 저장 데이터에서 완료된 퀘스트를 복원합니다.
    /// SaveSystem에서 호출합니다.
    /// </summary>
    public void RestoreCompletedQuest(QuestDef data)
    {
        var quest = new ActiveQuest(data);
        quest.isCompleted = true;
        _completedQuests.Add(quest);
        Debug.Log($"[QuestManager] 복원 (완료): {data.questName}");
    }

    /// <summary>복원 완료 후 UI를 갱신하기 위해 호출합니다.</summary>
    public void NotifyRestoreComplete()
    {
        OnQuestsRestored?.Invoke();

        // HUD·저널 등이 이벤트를 통해 갱신되도록
        foreach (var q in _activeQuests)
            OnQuestAccepted?.Invoke(q);
    }

    // ════════════════════════════════════════════════════════════════
    //  내부
    // ════════════════════════════════════════════════════════════════

    void AdvanceOrComplete(ActiveQuest quest)
    {
        if (quest.currentStageIndex < quest.stages.Count - 1)
        {
            quest.currentStageIndex++;
            Debug.Log($"[QuestManager] 다음 단계 ▶ {quest.GetCurrentStage()?.stageTitle}");
            OnQuestStageAdvanced?.Invoke(quest);
            OnQuestUpdated?.Invoke(quest);
        }
        else
        {
            _activeQuests.Remove(quest);
            quest.isCompleted = true;
            _completedQuests.Add(quest);
            Debug.Log($"[QuestManager] 퀘스트 완료 ★ {quest.questData.questName}");
            OnQuestCompleted?.Invoke(quest);
        }
    }

    // ─────────────────────────────────────────────────────────────────
    //  Context Menu 테스트
    // ─────────────────────────────────────────────────────────────────

    [ContextMenu("테스트: 첫 번째 활성 퀘스트의 목표 1개 완료")]
    public void DEBUG_CompleteOneObjective()
    {
        if (_activeQuests.Count == 0) { Debug.Log("활성 퀘스트 없음"); return; }
        var q   = _activeQuests[0];
        var obj = q.GetCurrentStage()?.objectives.FirstOrDefault(o => !o.IsCompleted);
        if (obj != null) UpdateObjective(q.questData.questID, obj.objectiveID, obj.requiredAmount);
        else             Debug.Log("완료할 목표 없음");
    }

    [ContextMenu("테스트: 첫 번째 활성 퀘스트 전체 강제 완료")]
    public void DEBUG_ForceCompleteQuest()
    {
        if (_activeQuests.Count == 0) { Debug.Log("활성 퀘스트 없음"); return; }
        var q = _activeQuests[0];
        while (_activeQuests.Contains(q))
        {
            var stage = q.GetCurrentStage();
            if (stage == null) break;
            foreach (var obj in stage.objectives) obj.currentAmount = obj.requiredAmount;
            AdvanceOrComplete(q);
        }
    }

    [ContextMenu("테스트: 상태 출력")]
    public void DEBUG_PrintStatus()
    {
        Debug.Log($"=== 퀘스트 상태 === 활성: {_activeQuests.Count}  완료: {_completedQuests.Count}");
        foreach (var q in _activeQuests)
        {
            var s = q.GetCurrentStage();
            Debug.Log($"  [진행] {q.questData.questName}  단계 {q.currentStageIndex + 1}/{q.stages.Count}: {s?.stageTitle}");
        }
    }
}
