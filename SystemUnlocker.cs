using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ─────────────────────────────────────────────────────────────────
/// <summary>
/// NPC 퀘스트 완료 시 시스템을 해금하는 트리거 컴포넌트.
/// (구버전 SystemUnlockTrigger 통합)
///
/// [사용법 A — 퀘스트 완료 자동 해금]
///   NPC 오브젝트에 추가 → questIDToWatch 입력 → systemsToUnlock 선택
///
/// [사용법 B — 외부에서 수동 호출]
///   GetComponent&lt;SystemUnlocker&gt;().TriggerUnlock();
/// </summary>
public class SystemUnlocker : MonoBehaviour
{
    [Header("해금할 시스템 목록")]
    [SerializeField] private HUDTopBar.SystemType[] systemsToUnlock;

    [Header("퀘스트 완료 자동 해금 (선택)")]
    [Tooltip("이 퀘스트 ID가 완료되면 자동으로 해금합니다. 비워두면 수동 호출.")]
    [SerializeField] private string questIDToWatch = "";

    [Header("HUDTopBar 참조 (비우면 자동 탐색)")]
    [SerializeField] private HUDTopBar hudTopBar;

    void Start()
    {
        if (hudTopBar == null)
            hudTopBar = FindObjectOfType<HUDTopBar>();

        if (!string.IsNullOrEmpty(questIDToWatch) && QuestManager.Instance != null)
            QuestManager.Instance.OnQuestCompleted += OnQuestCompleted;
    }

    void OnDestroy()
    {
        if (QuestManager.Instance != null)
            QuestManager.Instance.OnQuestCompleted -= OnQuestCompleted;
    }

    void OnQuestCompleted(ActiveQuest quest)
    {
        if (quest.questData.questID != questIDToWatch) return;
        TriggerUnlock();
    }

    /// <summary>시스템을 해금합니다.</summary>
    public void TriggerUnlock()
    {
        if (hudTopBar == null)
        {
            Debug.LogWarning("[SystemUnlocker] HUDTopBar를 찾을 수 없습니다.");
            return;
        }
        hudTopBar.UnlockSystems(systemsToUnlock);
    }

#if UNITY_EDITOR
    [ContextMenu("테스트: 해금 실행")]
    void DEBUG_Unlock() => TriggerUnlock();
#endif
}
