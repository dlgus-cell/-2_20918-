using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 저장 시스템 핵심 싱글톤.
/// JSON 파일을 Application.persistentDataPath/saves/ 에 저장.
/// (구버전 SaveManager)
/// </summary>
public class SaveSystem : MonoBehaviour
{
    public static SaveSystem Instance { get; private set; }

    [Header("설정")]
    [SerializeField] private int        slotCount        = 3;
    [SerializeField] private QuestDb    questDatabase;
    [SerializeField] private ItemDb     itemDatabase;   // 인벤토리 복원용(itemId→ItemData)
    [SerializeField] private bool       autoSaveEnabled  = true;
    [SerializeField] private float      autoSaveInterval = 300f;

    [Header("플레이어 (선택)")]
    [Tooltip("플레이어 Transform — 위치 저장에 사용. 없어도 동작합니다.")]
    [SerializeField] private Transform  playerTransform;

    public event Action<int>    OnSaveCompleted;
    public event Action<int>    OnLoadCompleted;
    public event Action<int>    OnSlotDeleted;
    public event Action<string> OnError;

    private string _saveDirectory;
    private float  _playTimeAccumulator;
    private float  _autoSaveTimer;
    private int    _lastSavedSlot = -1;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _saveDirectory = Path.Combine(Application.persistentDataPath, "saves");
        Directory.CreateDirectory(_saveDirectory);
        Debug.Log($"[SaveSystem] 저장 경로: {_saveDirectory}");
    }

    void Update()
    {
        _playTimeAccumulator += Time.deltaTime;

        if (autoSaveEnabled)
        {
            _autoSaveTimer += Time.deltaTime;
            if (_autoSaveTimer >= autoSaveInterval && _lastSavedSlot >= 0)
            {
                _autoSaveTimer = 0f;
                SaveToSlot(_lastSavedSlot, autoSave: true);
            }
        }
    }

    /// <summary>지정 슬롯에 저장합니다.</summary>
    public bool SaveToSlot(int slotIndex, string saveName = null, bool autoSave = false)
    {
        if (!IsValidSlot(slotIndex)) return false;

        try
        {
            var data    = BuildSaveData(slotIndex, saveName, autoSave);
            string json = JsonUtility.ToJson(data, prettyPrint: true);
            string path = SlotPath(slotIndex);
            File.WriteAllText(path, json);

            _lastSavedSlot       = slotIndex;
            _playTimeAccumulator = 0f;

            Debug.Log($"[SaveSystem] 저장 완료 → 슬롯 {slotIndex}  ({(autoSave ? "자동저장" : "수동저장")})");
            OnSaveCompleted?.Invoke(slotIndex);
            return true;
        }
        catch (Exception e)
        {
            string msg = $"저장 실패 (슬롯 {slotIndex}): {e.Message}";
            Debug.LogError($"[SaveSystem] {msg}");
            OnError?.Invoke(msg);
            return false;
        }
    }

    /// <summary>지정 슬롯에서 불러옵니다.</summary>
    public bool LoadFromSlot(int slotIndex)
    {
        if (!IsValidSlot(slotIndex)) return false;

        string path = SlotPath(slotIndex);
        if (!File.Exists(path))
        {
            OnError?.Invoke($"슬롯 {slotIndex}에 저장 데이터가 없습니다.");
            return false;
        }

        try
        {
            string json = File.ReadAllText(path);
            var data    = JsonUtility.FromJson<GameSaveData>(json);

            ApplySaveData(data);

            _lastSavedSlot       = slotIndex;
            _playTimeAccumulator = 0f;

            Debug.Log($"[SaveSystem] 불러오기 완료 ← 슬롯 {slotIndex}");
            OnLoadCompleted?.Invoke(slotIndex);
            return true;
        }
        catch (Exception e)
        {
            string msg = $"불러오기 실패 (슬롯 {slotIndex}): {e.Message}";
            Debug.LogError($"[SaveSystem] {msg}");
            OnError?.Invoke(msg);
            return false;
        }
    }

    /// <summary>지정 슬롯을 삭제합니다.</summary>
    public bool DeleteSlot(int slotIndex)
    {
        if (!IsValidSlot(slotIndex)) return false;
        string path = SlotPath(slotIndex);
        if (!File.Exists(path)) return false;

        try
        {
            File.Delete(path);
            Debug.Log($"[SaveSystem] 슬롯 {slotIndex} 삭제됨");
            OnSlotDeleted?.Invoke(slotIndex);
            return true;
        }
        catch (Exception e)
        {
            OnError?.Invoke($"삭제 실패: {e.Message}");
            return false;
        }
    }

    public List<SaveSlotMeta> GetAllSlotMeta()
    {
        var list = new List<SaveSlotMeta>();
        for (int i = 0; i < slotCount; i++)
            list.Add(GetSlotMeta(i));
        return list;
    }

    public SaveSlotMeta GetSlotMeta(int slotIndex)
    {
        string path = SlotPath(slotIndex);
        if (!File.Exists(path))
            return new SaveSlotMeta { isEmpty = true, slotIndex = slotIndex };

        try
        {
            string json = File.ReadAllText(path);
            var data    = JsonUtility.FromJson<GameSaveData>(json);
            return new SaveSlotMeta
            {
                isEmpty              = false,
                slotIndex            = slotIndex,
                saveName             = data.saveName,
                saveDateTime         = data.saveDateTime,
                totalPlayTimeSeconds = data.totalPlayTimeSeconds,
                questSummary         = BuildQuestSummary(data.quests),
                levelSummary         = BuildLevelSummary(data.player)
            };
        }
        catch
        {
            return new SaveSlotMeta { isEmpty = true, slotIndex = slotIndex };
        }
    }

    public int   SlotCount      => slotCount;
    public float CurrentSession => _playTimeAccumulator;

    // ─── 내부: 데이터 빌드 & 적용 ─────────────────────────────────

    GameSaveData BuildSaveData(int slotIndex, string saveName, bool autoSave)
    {
        float prevPlayTime = 0f;
        string path = SlotPath(slotIndex);
        if (File.Exists(path))
        {
            try
            {
                var prev = JsonUtility.FromJson<GameSaveData>(File.ReadAllText(path));
                prevPlayTime = prev.totalPlayTimeSeconds;
            }
            catch { }
        }

        var data = new GameSaveData
        {
            slotIndex            = slotIndex,
            saveName             = string.IsNullOrEmpty(saveName)
                                   ? $"{(autoSave ? "[자동] " : "")}저장 {slotIndex + 1}"
                                   : saveName,
            saveDateTime         = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            totalPlayTimeSeconds = prevPlayTime + _playTimeAccumulator,
            player               = BuildPlayerData(),
            quests               = BuildQuestData(),
            gimmicks             = GimmickSaveHub.Instance != null
                                   ? GimmickSaveHub.Instance.CaptureAll()
                                   : new System.Collections.Generic.List<GimmickRecord>()
        };
        return data;
    }

    PlayerSaveData BuildPlayerData()
    {
        var pd = new PlayerSaveData();
        if (playerTransform != null)
        {
            pd.posX = playerTransform.position.x;
            pd.posY = playerTransform.position.y;
            pd.posZ = playerTransform.position.z;
        }

        if (GoldSystem.Instance != null)
            pd.gold = GoldSystem.Instance.CurrentGold;

        if (PlayerStats.Instance != null)
        {
            var statsData = PlayerStats.Instance.GetSaveData();
            pd.level      = statsData.level;
            pd.experience = statsData.currentXP;
        }

        // 현재 체력/기력 — 정본 PlayerVitals 에서 읽음(없으면 -1 유지 = 미저장)
        if (PlayerVitals.Instance != null)
        {
            pd.currentHP      = PlayerVitals.Instance.CurrentHP;
            pd.currentStamina = PlayerVitals.Instance.CurrentStamina;
        }

        // 인벤토리 내용(슬롯 위치 보존)
        if (Inventory.Instance != null)
            pd.inventory = Inventory.Instance.GetSaveData();

        return pd;
    }

    QuestSaveData BuildQuestData()
    {
        var qsd = new QuestSaveData();

        if (QuestManager.Instance == null) return qsd;

        foreach (var aq in QuestManager.Instance.GetActiveQuests())
        {
            var record = new ActiveQuestRecord
            {
                questID           = aq.questData.questID,
                currentStageIndex = aq.currentStageIndex
            };
            for (int si = 0; si < aq.stages.Count; si++)
            {
                foreach (var obj in aq.stages[si].objectives)
                {
                    record.objectives.Add(new ObjectiveRecord
                    {
                        stageIndex    = si,
                        objectiveID   = obj.objectiveID,
                        currentAmount = obj.currentAmount
                    });
                }
            }
            qsd.activeQuests.Add(record);
        }

        qsd.completedQuestIDs = QuestManager.Instance
            .GetCompletedQuests()
            .Select(q => q.questData.questID)
            .ToList();

        return qsd;
    }

    void ApplySaveData(GameSaveData data)
    {
        if (playerTransform != null)
            playerTransform.position = new Vector3(data.player.posX, data.player.posY, data.player.posZ);

        if (GoldSystem.Instance != null)
            GoldSystem.Instance.SetGold(data.player.gold);

        if (PlayerStats.Instance != null)
        {
            var statsData = PlayerStats.Instance.GetSaveData();
            statsData.level      = data.player.level;
            statsData.currentXP  = data.player.experience;
            PlayerStats.Instance.LoadSaveData(statsData);
        }

        // 현재 체력/기력 복원 — 스탯(최대치) 로드 이후에 설정해야 클램프가 올바름.
        // -1(구버전 세이브)이면 건너뜀 → PlayerVitals 가 시작 시 최대치로 채움.
        if (PlayerVitals.Instance != null)
        {
            if (data.player.currentHP >= 0)      PlayerVitals.Instance.SetHP(data.player.currentHP);
            if (data.player.currentStamina >= 0) PlayerVitals.Instance.SetStamina(data.player.currentStamina);
        }

        // 인벤토리 복원 (itemId → ItemData 조회에 itemDatabase 필요)
        if (Inventory.Instance != null)
            Inventory.Instance.LoadSaveData(data.player.inventory, itemDatabase);

        ApplyQuestData(data.quests);

        // 기믹 상태 복원(열린 문/감긴 태엽/주운 아이템). 구버전 세이브는 빈 리스트.
        if (GimmickSaveHub.Instance != null)
            GimmickSaveHub.Instance.RestoreAll(data.gimmicks);
    }

    void ApplyQuestData(QuestSaveData qsd)
    {
        if (QuestManager.Instance == null) return;
        if (questDatabase == null)
        {
            Debug.LogWarning("[SaveSystem] QuestDb가 연결되지 않아 퀘스트를 복원할 수 없습니다.");
            return;
        }

        QuestManager.Instance.ClearAllQuests();

        foreach (var record in qsd.activeQuests)
        {
            var questData = questDatabase.FindByID(record.questID);
            if (questData == null)
            {
                Debug.LogWarning($"[SaveSystem] questID '{record.questID}'를 QuestDb에서 찾을 수 없습니다.");
                continue;
            }
            QuestManager.Instance.RestoreActiveQuest(questData, record);
        }

        foreach (var questID in qsd.completedQuestIDs)
        {
            var questData = questDatabase.FindByID(questID);
            if (questData != null)
                QuestManager.Instance.RestoreCompletedQuest(questData);
        }
    }

    string SlotPath(int slotIndex)
        => Path.Combine(_saveDirectory, $"slot_{slotIndex}.json");

    bool IsValidSlot(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < slotCount) return true;
        Debug.LogWarning($"[SaveSystem] 유효하지 않은 슬롯 인덱스: {slotIndex}");
        return false;
    }

    static string BuildQuestSummary(QuestSaveData qsd)
        => $"진행 {qsd.activeQuests.Count}  완료 {qsd.completedQuestIDs.Count}";

    static string BuildLevelSummary(PlayerSaveData pd)
        => $"Lv.{pd.level}  골드 {pd.gold:N0}G";

    [ContextMenu("테스트: 슬롯 0에 저장")]
    void DEBUG_Save0()   => SaveToSlot(0);

    [ContextMenu("테스트: 슬롯 0 불러오기")]
    void DEBUG_Load0()   => LoadFromSlot(0);

    [ContextMenu("테스트: 슬롯 0 삭제")]
    void DEBUG_Delete0() => DeleteSlot(0);

    [ContextMenu("테스트: 저장 경로 열기")]
    void DEBUG_OpenPath()
    {
#if UNITY_EDITOR
        UnityEditor.EditorUtility.RevealInFinder(_saveDirectory);
#endif
    }
}
