using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HUD 상단 버튼 허브 — 모든 UI 패널 버튼 + 해금 시스템 관리.
/// </summary>
public class HUDTopBar : MonoBehaviour
{
    public enum SystemType
    {
        Inventory, Minimap, Save, Shop, StatusWindow, Map
    }

    [Serializable]
    public class SystemButton
    {
        public SystemType        type;
        public Button            button;
        public GameObject        lockIcon;
        public TextMeshProUGUI   labelText;
        [HideInInspector] public bool isUnlocked = false;
    }

    [Header("시스템 버튼 목록")]
    [SerializeField] private List<SystemButton> systemButtons = new();

    [Header("연결할 UI 패널들")]
    [SerializeField] private InventoryPanel    inventoryUI;
    [SerializeField] private StatusPanel statusWindowUI;
    [SerializeField] private SaveLoadPanel     saveLoadUI;

    [Header("해금 연출")]
    [SerializeField] private GameObject unlockVFXPrefab;
    [SerializeField] private AudioClip  unlockSFX;
    [SerializeField] private float      unlockFlashDuration = 0.6f;

    [Header("잠긴 버튼 색상")]
    [SerializeField] private Color lockedColor   = new Color(0.3f, 0.3f, 0.3f, 0.5f);
    [SerializeField] private Color unlockedColor = Color.white;

    public static event Action<SystemType> OnSystemUnlocked;

    private const string PREFS_PREFIX = "HUD_Unlocked_";

    // ─── 정적 접근(패널 게이팅용) ──────────────────────────────────────
    public static HUDTopBar Instance { get; private set; }

    /// <summary>
    /// 시스템이 해금됐는지 정적으로 조회. HUDTopBar 가 없으면 true(차단 안 함)로 처리해
    /// 소프트락을 방지한다. 패널이 "열기 전" 게이팅에 사용.
    /// </summary>
    public static bool IsSystemUnlocked(SystemType type)
    {
        if (Instance == null)
#if UNITY_2023_1_OR_NEWER
            Instance = FindAnyObjectByType<HUDTopBar>();
#else
            Instance = FindObjectOfType<HUDTopBar>();
#endif
        return Instance == null || Instance.IsUnlocked(type);
    }

    // ═════════════════════════════════════════════════════════════════

    void Start()
    {
        Instance = this;
        foreach (var sb in systemButtons) BindButton(sb);
        LoadUnlockStates();
        foreach (var sb in systemButtons) ApplyLockVisual(sb);
    }

    public void UnlockSystem(SystemType type)
    {
        var sb = FindButton(type);
        if (sb == null) { Debug.LogWarning($"[HUDTopBar] SystemButton '{type}' 없음"); return; }
        if (sb.isUnlocked) return;

        sb.isUnlocked = true;
        SaveUnlockState(type, true);
        StartCoroutine(UnlockAnimation(sb));
        OnSystemUnlocked?.Invoke(type);

        Debug.Log($"[HUDTopBar] 시스템 해금: {type}");
    }

    public void UnlockSystems(params SystemType[] types)
    {
        foreach (var t in types) UnlockSystem(t);
    }

    /// <summary>테스트용 임시 전체 해금. PlayerPrefs 에 저장하지 않아 껐다 켜면 원래 상태로 돌아온다.</summary>
    private static bool _debugUnlockAll = false;
    public static bool DebugUnlockAll => _debugUnlockAll;
    public static void SetDebugUnlockAll(bool on)
    {
        _debugUnlockAll = on;
        if (Instance != null)
            foreach (var sb in Instance.systemButtons) Instance.ApplyLockVisual(sb);
    }

    /// <summary>실제 해금 또는 디버그 강제 해금이면 true.</summary>
    bool EffectiveUnlocked(SystemButton sb) => _debugUnlockAll || (sb != null && sb.isUnlocked);

    public bool IsUnlocked(SystemType type)
    {
        if (_debugUnlockAll) return true;
        var sb = FindButton(type);
        return sb != null && sb.isUnlocked;
    }

    public void ResetAllUnlocks()
    {
        foreach (var sb in systemButtons)
        {
            sb.isUnlocked = false;
            PlayerPrefs.DeleteKey(PREFS_PREFIX + sb.type);
            ApplyLockVisual(sb);
        }
        PlayerPrefs.Save();
    }

    // ═════════════════════════════════════════════════════════════════

    void BindButton(SystemButton sb)
    {
        if (sb.button == null) return;
        sb.button.onClick.RemoveAllListeners();
        sb.button.onClick.AddListener(() => OnButtonClicked(sb));
    }

    void OnButtonClicked(SystemButton sb)
    {
        if (!EffectiveUnlocked(sb)) return;   // 잠긴 시스템은 어떤 식으로도 동작하지 않음

        switch (sb.type)
        {
            case SystemType.Inventory:
            case SystemType.Shop:
                inventoryUI?.Toggle();
                break;

            case SystemType.StatusWindow:
                statusWindowUI?.Toggle();
                break;

            case SystemType.Save:
                saveLoadUI?.gameObject.SendMessage("TogglePanel",
                    SendMessageOptions.DontRequireReceiver);
                break;
        }
    }

    // ═════════════════════════════════════════════════════════════════

    IEnumerator UnlockAnimation(SystemButton sb)
    {
        if (sb.button != null) sb.button.gameObject.SetActive(true);  // 숨겨져 있던 버튼 표시

        if (unlockVFXPrefab && sb.button != null)
            Instantiate(unlockVFXPrefab, sb.button.transform.position, Quaternion.identity);

        if (unlockSFX != null)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.PlayOneShot(unlockSFX);
            Destroy(src, unlockSFX.length + 0.1f);
        }

        if (sb.lockIcon != null)
        {
            var lockImg = sb.lockIcon.GetComponent<Image>();
            if (lockImg != null)
            {
                float t = 0f;
                while (t < unlockFlashDuration)
                {
                    t += Time.unscaledDeltaTime;
                    lockImg.color = new Color(1, 1, 1, 1f - t / unlockFlashDuration);
                    yield return null;
                }
            }
            sb.lockIcon.SetActive(false);
        }

        ApplyLockVisual(sb);
    }

    void ApplyLockVisual(SystemButton sb)
    {
        if (sb.button == null) return;

        bool unlocked = EffectiveUnlocked(sb);

        // 잠긴 시스템의 버튼(관련 UI)은 화면에서 아예 숨긴다.
        sb.button.gameObject.SetActive(unlocked);

        if (unlocked)
        {
            sb.button.interactable = true;
            var img = sb.button.GetComponent<Image>();
            if (img) img.color = unlockedColor;
            if (sb.lockIcon) sb.lockIcon.SetActive(false);
        }
    }

    IEnumerator ShakeButton(Button btn)
    {
        if (btn == null) yield break;
        var rt     = btn.GetComponent<RectTransform>();
        var origin = rt.anchoredPosition;
        float t    = 0f;

        while (t < 0.3f)
        {
            t += Time.unscaledDeltaTime;
            rt.anchoredPosition = origin + new Vector2(
                Mathf.Sin(t * 60f) * 6f * (1f - t / 0.3f), 0);
            yield return null;
        }
        rt.anchoredPosition = origin;
    }

    // ═════════════════════════════════════════════════════════════════

    void SaveUnlockState(SystemType type, bool value)
    {
        PlayerPrefs.SetInt(PREFS_PREFIX + type, value ? 1 : 0);
        PlayerPrefs.Save();
    }

    void LoadUnlockStates()
    {
        foreach (var sb in systemButtons)
            sb.isUnlocked = PlayerPrefs.GetInt(PREFS_PREFIX + sb.type, 0) == 1;
    }

    SystemButton FindButton(SystemType type)
        => systemButtons.Find(sb => sb.type == type);

    [ContextMenu("테스트: 모든 시스템 해금")]
    void DEBUG_UnlockAll()
    {
        foreach (SystemType t in Enum.GetValues(typeof(SystemType)))
            UnlockSystem(t);
    }

    [ContextMenu("테스트: 모든 시스템 잠금 초기화")]
    void DEBUG_ResetAll() => ResetAllUnlocks();
}
