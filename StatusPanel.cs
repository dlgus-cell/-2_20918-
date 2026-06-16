using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// 상태창 패널 컨트롤러. K키 또는 HUD 버튼으로 토글.
/// (구버전 StatusWindowUI)
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class StatusPanel : MonoBehaviour
{
    [Header("패널 루트")]
    [SerializeField] private GameObject panelRoot;

    [Header("플레이어 정보")]
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text levelText;

    [Header("경험치 바")]
    [SerializeField] private Slider   xpSlider;
    [SerializeField] private TMP_Text xpText;

    [Header("스탯 포인트")]
    [SerializeField] private GameObject statPointsPanel;
    [SerializeField] private TMP_Text   statPointsText;
    [SerializeField] private TMP_Text   statPointsLabel;

    [Header("스탯 행 목록")]
    [SerializeField] private StatRow   statRowPrefab;
    [SerializeField] private Transform statListParent;

    [Header("표시할 스탯 순서")]
    [SerializeField] private StatType[] statOrder = {
        StatType.Strength,
        StatType.Vitality,
        StatType.Stamina,
        StatType.Agility,
        StatType.Intelligence,
        StatType.Defense
    };

    [Header("토글 키")]
    [SerializeField] private KeyCode toggleKey = KeyCode.K;

    [Header("해금 게이팅")]
    [Tooltip("켜면 시스템이 해금되기 전엔 상태창이 열리지 않는다.")]
    [SerializeField] private bool requireUnlock = true;
    [SerializeField] private HUDTopBar.SystemType systemType = HUDTopBar.SystemType.StatusWindow;

    [Header("스탯 포인트 강조 색상")]
    [SerializeField] private Color pointsAvailableColor = new Color(1f, 0.95f, 0.3f, 1f);
    [SerializeField] private Color pointsEmptyColor     = new Color(0.6f, 0.6f, 0.6f, 1f);

    private CanvasGroup     _cg;
    private bool            _isOpen = false;
    private List<StatRow>   _rows   = new();
    private Coroutine       _xpBarRoutine;
    private Coroutine       _pointFlashRoutine;

    void Awake()
    {
        _cg = GetComponent<CanvasGroup>();
        SetVisible(false);
    }

    void OnEnable()
    {
        PlayerStats.OnStatsChanged      += RefreshAll;
        PlayerStats.OnStatPointsChanged += RefreshStatPoints;
        PlayerStats.OnLevelUp           += OnLevelUp;
        PlayerStats.OnXPChanged         += RefreshXP;
    }

    void OnDisable()
    {
        PlayerStats.OnStatsChanged      -= RefreshAll;
        PlayerStats.OnStatPointsChanged -= RefreshStatPoints;
        PlayerStats.OnLevelUp           -= OnLevelUp;
        PlayerStats.OnXPChanged         -= RefreshXP;
    }

    void Start()
    {
        BuildStatRows();
        RefreshAll();
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey)) Toggle();
    }

    public void Toggle()
    {
        // 해금 전에는 열기 차단(닫기는 허용).
        if (!_isOpen && requireUnlock && !HUDTopBar.IsSystemUnlocked(systemType))
        {
            Debug.Log($"[StatusPanel] '{systemType}' 미해금 — 열기 차단");
            return;
        }
        _isOpen = !_isOpen;
        SetVisible(_isOpen);
        if (_isOpen) RefreshAll();
    }

    public void Open()
    {
        if (requireUnlock && !HUDTopBar.IsSystemUnlocked(systemType)) return;
        _isOpen = true;  SetVisible(true);  RefreshAll();
    }
    public void Close() { _isOpen = false; SetVisible(false); }

    void SetVisible(bool v)
    {
        if (panelRoot) panelRoot.SetActive(v);
        _cg.alpha          = v ? 1f : 0f;
        _cg.blocksRaycasts = v;
        _cg.interactable   = v;
    }

    void BuildStatRows()
    {
        if (statRowPrefab == null || statListParent == null) return;

        foreach (var type in statOrder)
        {
            var row = Instantiate(statRowPrefab, statListParent);
            row.Init(type);
            _rows.Add(row);
        }
    }

    void RefreshAll()
    {
        if (PlayerStats.Instance == null) return;
        var ps = PlayerStats.Instance;

        if (playerNameText) playerNameText.text = ps.PlayerName;
        if (levelText)      levelText.text      = $"Lv. {ps.Level}";

        RefreshXP(ps.CurrentXP, ps.GetXPRequired(ps.Level));
        RefreshStatPoints(ps.StatPoints);
    }

    void RefreshXP(int current, int required)
    {
        if (xpText) xpText.text = $"{current:N0} / {required:N0} XP";

        if (xpSlider)
        {
            float target = required > 0 ? (float)current / required : 0f;
            if (_xpBarRoutine != null) StopCoroutine(_xpBarRoutine);
            _xpBarRoutine = StartCoroutine(LerpSlider(xpSlider, target, 0.5f));
        }
    }

    void RefreshStatPoints(int points)
    {
        if (statPointsText)
        {
            statPointsText.text  = points.ToString();
            statPointsText.color = points > 0 ? pointsAvailableColor : pointsEmptyColor;
        }

        if (statPointsLabel)
            statPointsLabel.color = points > 0 ? pointsAvailableColor : pointsEmptyColor;

        if (points > 0 && statPointsText != null)
        {
            if (_pointFlashRoutine != null) StopCoroutine(_pointFlashRoutine);
            _pointFlashRoutine = StartCoroutine(PulseText(statPointsText, pointsAvailableColor));
        }
    }

    void OnLevelUp(int prev, int next)
    {
        if (levelText) levelText.text = $"Lv. {next}";

        if (levelText)
        {
            StopAllCoroutines();
            StartCoroutine(FlashText(levelText, new Color(1f, 0.9f, 0.3f), Color.white));
        }
    }

    static IEnumerator LerpSlider(Slider s, float target, float dur)
    {
        float start   = s.value;
        float elapsed = 0f;

        while (elapsed < dur)
        {
            elapsed  += Time.unscaledDeltaTime;
            s.value   = Mathf.Lerp(start, target, Mathf.SmoothStep(0, 1, elapsed / dur));
            yield return null;
        }
        s.value = target;
    }

    static IEnumerator FlashText(TMP_Text txt, Color flash, Color normal)
    {
        txt.color = flash;
        yield return new WaitForSecondsRealtime(0.3f);
        float t = 0f;
        while (t < 0.5f)
        {
            t += Time.unscaledDeltaTime;
            txt.color = Color.Lerp(flash, normal, t / 0.5f);
            yield return null;
        }
        txt.color = normal;
    }

    static IEnumerator PulseText(TMP_Text txt, Color baseColor)
    {
        for (int i = 0; i < 3; i++)
        {
            float t = 0f;
            while (t < 0.25f)
            {
                t += Time.unscaledDeltaTime;
                float s = Mathf.Lerp(1f, 1.3f, Mathf.PingPong(t / 0.25f, 1f));
                txt.transform.localScale = Vector3.one * s;
                yield return null;
            }
        }
        txt.transform.localScale = Vector3.one;
    }
}
