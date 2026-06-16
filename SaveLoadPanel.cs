using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// ─────────────────────────────────────────────────────────────────
/// <summary>
/// 우측 상단 저장 버튼 + 저장/불러오기 창 UI 컨트롤러.
/// (구버전 SaveLoadUI)
/// </summary>
public class SaveLoadPanel : MonoBehaviour
{
    [Header("루트 UI")]
    [SerializeField] private Button          saveButton;
    [SerializeField] private GameObject      saveLoadPanel;
    [SerializeField] private Button          closePanelButton;
    [SerializeField] private TextMeshProUGUI panelTitleText;

    [Header("탭 버튼")]
    [SerializeField] private Button saveTabButton;
    [SerializeField] private Button loadTabButton;

    [Header("탭 색상")]
    [SerializeField] private Color activeTabColor   = new Color(0.2f, 0.6f, 1f);
    [SerializeField] private Color inactiveTabColor = new Color(0.35f, 0.35f, 0.35f);

    [Header("슬롯")]
    [SerializeField] private Transform  slotContainer;
    [SerializeField] private GameObject slotItemPrefab;

    [Header("상태 텍스트")]
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("확인 다이얼로그 (선택)")]
    [SerializeField] private GameObject      confirmDialog;
    [SerializeField] private TextMeshProUGUI confirmMessage;
    [SerializeField] private Button          confirmYesButton;
    [SerializeField] private Button          confirmNoButton;

    private bool             _isSaveMode  = true;
    private bool             _isOpen      = false;
    private List<GameObject> _slotObjects = new();
    private Action           _pendingConfirmAction;

    void Start()
    {
        if (saveLoadPanel) saveLoadPanel.SetActive(false);
        if (confirmDialog) confirmDialog.SetActive(false);

        saveButton?.onClick.AddListener(TogglePanel);
        closePanelButton?.onClick.AddListener(ClosePanel);
        saveTabButton?.onClick.AddListener(() => SwitchTab(true));
        loadTabButton?.onClick.AddListener(() => SwitchTab(false));
        confirmYesButton?.onClick.AddListener(ConfirmYes);
        confirmNoButton?.onClick.AddListener(ConfirmNo);

        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.OnSaveCompleted += OnSaveDone;
            SaveSystem.Instance.OnLoadCompleted += OnLoadDone;
            SaveSystem.Instance.OnSlotDeleted   += OnDeleteDone;
            SaveSystem.Instance.OnError         += ShowStatus;
        }
    }

    void OnDestroy()
    {
        if (SaveSystem.Instance == null) return;
        SaveSystem.Instance.OnSaveCompleted -= OnSaveDone;
        SaveSystem.Instance.OnLoadCompleted -= OnLoadDone;
        SaveSystem.Instance.OnSlotDeleted   -= OnDeleteDone;
        SaveSystem.Instance.OnError         -= ShowStatus;
    }

    public void TogglePanel() { if (_isOpen) ClosePanel(); else OpenPanel(); }

    void OpenPanel()
    {
        _isOpen = true;
        if (saveLoadPanel) saveLoadPanel.SetActive(true);
        SwitchTab(_isSaveMode);
    }

    void ClosePanel()
    {
        _isOpen = false;
        if (saveLoadPanel) saveLoadPanel.SetActive(false);
        if (confirmDialog) confirmDialog.SetActive(false);
        ClearStatus();
    }

    void SwitchTab(bool saveMode)
    {
        _isSaveMode = saveMode;
        if (panelTitleText) panelTitleText.text = saveMode ? "게임 저장" : "불러오기";
        SetTabColor(saveTabButton,  saveMode);
        SetTabColor(loadTabButton, !saveMode);
        RefreshSlots();
    }

    void SetTabColor(Button btn, bool active)
    {
        if (btn == null) return;
        var img = btn.GetComponent<Image>();
        if (img) img.color = active ? activeTabColor : inactiveTabColor;
    }

    void RefreshSlots()
    {
        foreach (var go in _slotObjects) if (go) Destroy(go);
        _slotObjects.Clear();

        if (SaveSystem.Instance == null) return;

        foreach (var meta in SaveSystem.Instance.GetAllSlotMeta())
            BuildSlotItem(meta);
    }

    void BuildSlotItem(SaveSlotMeta meta)
    {
        if (slotItemPrefab == null || slotContainer == null) return;

        var go = Instantiate(slotItemPrefab, slotContainer);
        _slotObjects.Add(go);

        SetChildText(go, "SlotNumberText", $"슬롯 {meta.slotIndex + 1}");

        if (meta.isEmpty)
        {
            SetChildText(go, "SaveNameText",     "— 빈 슬롯 —");
            SetChildText(go, "DateTimeText",     "");
            SetChildText(go, "PlayTimeText",     "");
            SetChildText(go, "LevelSummaryText", "");
            SetChildText(go, "QuestSummaryText", "");
        }
        else
        {
            SetChildText(go, "SaveNameText",     meta.saveName);
            SetChildText(go, "DateTimeText",     meta.saveDateTime);
            SetChildText(go, "PlayTimeText",     $"플레이: {meta.FormattedPlayTime()}");
            SetChildText(go, "LevelSummaryText", meta.levelSummary);
            SetChildText(go, "QuestSummaryText", meta.questSummary);
        }

        int idx = meta.slotIndex;

        var saveBtn = FindChildButton(go, "SaveButton");
        if (saveBtn)
        {
            saveBtn.gameObject.SetActive(_isSaveMode);
            saveBtn.onClick.AddListener(() => OnClickSave(idx, meta));
        }

        var loadBtn = FindChildButton(go, "LoadButton");
        if (loadBtn)
        {
            loadBtn.gameObject.SetActive(!_isSaveMode);
            loadBtn.interactable = !meta.isEmpty;
            loadBtn.onClick.AddListener(() => OnClickLoad(idx));
        }

        var deleteBtn = FindChildButton(go, "DeleteButton");
        if (deleteBtn)
        {
            deleteBtn.interactable = !meta.isEmpty;
            deleteBtn.onClick.AddListener(() => OnClickDelete(idx));
        }
    }

    void OnClickSave(int idx, SaveSlotMeta meta)
    {
        if (!meta.isEmpty)
            ShowConfirm($"슬롯 {idx + 1}을 덮어쓰시겠습니까?\n\"{meta.saveName}\"",
                        () => DoSave(idx));
        else
            DoSave(idx);
    }

    void OnClickLoad(int idx)
    {
        ShowConfirm($"슬롯 {idx + 1}을 불러오시겠습니까?\n현재 진행 상황은 사라집니다.",
                    () => DoLoad(idx));
    }

    void OnClickDelete(int idx)
    {
        ShowConfirm($"슬롯 {idx + 1}의 데이터를 삭제하시겠습니까?",
                    () => DoDelete(idx));
    }

    void DoSave(int idx)
    {
        SaveSystem.Instance?.SaveToSlot(idx);
        RefreshSlots();
    }

    void DoLoad(int idx)
    {
        SaveSystem.Instance?.LoadFromSlot(idx);
        QuestManager.Instance?.NotifyRestoreComplete();
        ClosePanel();
    }

    void DoDelete(int idx)
    {
        SaveSystem.Instance?.DeleteSlot(idx);
        RefreshSlots();
    }

    void ShowConfirm(string message, Action onConfirm)
    {
        if (confirmDialog == null) { onConfirm?.Invoke(); return; }
        _pendingConfirmAction = onConfirm;
        if (confirmMessage) confirmMessage.text = message;
        confirmDialog.SetActive(true);
    }

    void ConfirmYes()
    {
        confirmDialog?.SetActive(false);
        _pendingConfirmAction?.Invoke();
        _pendingConfirmAction = null;
    }

    void ConfirmNo()
    {
        confirmDialog?.SetActive(false);
        _pendingConfirmAction = null;
    }

    void OnSaveDone(int idx)   { ShowStatus($"슬롯 {idx + 1}에 저장했습니다."); RefreshSlots(); }
    void OnLoadDone(int idx)   => ShowStatus($"슬롯 {idx + 1}을 불러왔습니다.");
    void OnDeleteDone(int idx) => ShowStatus($"슬롯 {idx + 1}을 삭제했습니다.");

    void ShowStatus(string msg)
    {
        if (statusText) statusText.text = msg;
        StopAllCoroutines();
        StartCoroutine(ClearAfter(3f));
    }

    void ClearStatus() { StopAllCoroutines(); if (statusText) statusText.text = ""; }

    IEnumerator ClearAfter(float t) { yield return new WaitForSeconds(t); if (statusText) statusText.text = ""; }

    static void SetChildText(GameObject root, string childName, string text)
    {
        var t = root.transform.Find(childName);
        if (t == null) return;
        var tmp = t.GetComponent<TextMeshProUGUI>();
        if (tmp) tmp.text = text;
    }

    static Button FindChildButton(GameObject root, string childName)
    {
        var t = root.transform.Find(childName);
        return t ? t.GetComponent<Button>() : null;
    }
}
