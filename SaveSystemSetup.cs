#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using System.IO;

/// <summary>
/// 에디터 유틸리티 — 씬에 저장 시스템 UI 전체를 자동으로 구성합니다.
///
/// [사용법]
///   Unity 메뉴 → Tools → Quest RPG → Setup Save System in Scene
///   실행하면 씬의 Canvas에 SaveButton, SaveLoadPanel이 자동 생성됩니다.
///   SaveSystem, SaveLoadPanel 컴포넌트도 자동으로 추가됩니다.
/// </summary>
public static class SaveSystemSetup
{
    [MenuItem("Tools/Quest RPG/Setup Save System in Scene")]
    public static void SetupInScene()
    {
        // ── 1. 기존 Canvas 찾기 or 생성 ──────────────────────────────
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            var cGO = new GameObject("Canvas");
            canvas  = cGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            cGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cGO.AddComponent<GraphicRaycaster>();
            var scaler = cGO.GetComponent<CanvasScaler>();
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight  = 0.5f;
        }

        var canvasT = canvas.transform;

        // ── 2. SaveSystem ────────────────────────────────────────────
        if (Object.FindObjectOfType<SaveSystem>() == null)
        {
            var smGO = new GameObject("SaveSystem");
            smGO.AddComponent<SaveSystem>();
            Debug.Log("[SaveSystemSetup] SaveSystem 생성됨");
        }

        // ── 3. 우측 상단 SaveButton ────────────────────────────────────
        var saveBtn = CreateSaveButton(canvasT);

        // ── 4. SaveLoadPanel ──────────────────────────────────────────
        var panel = CreateSaveLoadPanel(canvasT);

        // ── 5. SaveLoadPanel 컴포넌트 연결 ───────────────────────────────
        var uiHost = new GameObject("SaveLoadPanel");
        uiHost.transform.SetParent(canvasT, false);
        var ui = uiHost.AddComponent<SaveLoadPanel>();

        // 리플렉션으로 private SerializeField 연결
        SetField(ui, "saveButton",      saveBtn.GetComponent<Button>());
        SetField(ui, "saveLoadPanel",   panel);
        SetField(ui, "closePanelButton",FindDeep<Button>(panel.transform, "CloseButton"));
        SetField(ui, "panelTitleText",  FindDeep<TextMeshProUGUI>(panel.transform, "TitleText"));
        SetField(ui, "saveTabButton",   FindDeep<Button>(panel.transform, "SaveTabButton"));
        SetField(ui, "loadTabButton",   FindDeep<Button>(panel.transform, "LoadTabButton"));
        SetField(ui, "slotContainer",   FindDeepTransform(panel.transform, "SlotContainer"));
        SetField(ui, "statusText",      FindDeep<TextMeshProUGUI>(panel.transform, "StatusText"));
        SetField(ui, "confirmDialog",   FindDeepGO(panel.transform, "ConfirmDialog"));
        SetField(ui, "confirmMessage",  FindDeep<TextMeshProUGUI>(panel.transform, "ConfirmMessage"));
        SetField(ui, "confirmYesButton",FindDeep<Button>(panel.transform, "ConfirmYesButton"));
        SetField(ui, "confirmNoButton", FindDeep<Button>(panel.transform, "ConfirmNoButton"));

        EditorUtility.SetDirty(uiHost);
        Debug.Log("[SaveSystemSetup] 저장 시스템 UI 구성 완료!");
        EditorGUIUtility.PingObject(uiHost);
    }

    // ═════════════════════════════════════════════════════════════════
    //  UI 빌더
    // ═════════════════════════════════════════════════════════════════

    static GameObject CreateSaveButton(Transform parent)
    {
        var go   = new GameObject("SaveButton", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(1, 1);
        rt.anchorMax        = new Vector2(1, 1);
        rt.pivot            = new Vector2(1, 1);
        rt.anchoredPosition = new Vector2(-20, -20);
        rt.sizeDelta        = new Vector2(100, 40);

        var img = go.GetComponent<Image>();
        img.color = new Color(0.15f, 0.18f, 0.25f, 0.92f);

        // 텍스트
        var txtGO = new GameObject("Text", typeof(RectTransform));
        txtGO.transform.SetParent(go.transform, false);
        StretchFill(txtGO.GetComponent<RectTransform>());
        var tmp    = txtGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = "💾 저장";
        tmp.fontSize  = 14;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;

        return go;
    }

    static GameObject CreateSaveLoadPanel(Transform parent)
    {
        // ── 어두운 오버레이 배경 ──────────────────────────────────────
        var overlay = new GameObject("SaveLoadPanel", typeof(RectTransform), typeof(Image));
        overlay.transform.SetParent(parent, false);
        StretchFill(overlay.GetComponent<RectTransform>());
        overlay.GetComponent<Image>().color = new Color(0, 0, 0, 0.65f);
        overlay.AddComponent<Button>(); // 바깥 클릭 시 닫기용 (선택)
        overlay.SetActive(false);

        // ── 창 본체 ───────────────────────────────────────────────────
        var window = MakeRect(overlay.transform, "Window");
        var winRT  = window.GetComponent<RectTransform>();
        winRT.anchorMin        = new Vector2(0.5f, 0.5f);
        winRT.anchorMax        = new Vector2(0.5f, 0.5f);
        winRT.pivot            = new Vector2(0.5f, 0.5f);
        winRT.anchoredPosition = Vector2.zero;
        winRT.sizeDelta        = new Vector2(680, 480);
        var winImg = window.AddComponent<Image>();
        winImg.color = new Color(0.1f, 0.12f, 0.16f, 0.98f);
        var winVLG = window.AddComponent<VerticalLayoutGroup>();
        winVLG.padding   = new RectOffset(16, 16, 14, 14);
        winVLG.spacing   = 10;
        winVLG.childForceExpandWidth  = true;
        winVLG.childForceExpandHeight = false;

        // ── 헤더 ─────────────────────────────────────────────────────
        var header  = MakeHBlock(window.transform, "Header", 40);
        var titleTMP = MakeTMP(header.transform, "TitleText", "게임 저장", 20, true);
        StretchFill(titleTMP.rectTransform);
        titleTMP.alignment = TextAlignmentOptions.Left;
        titleTMP.rectTransform.anchoredPosition = new Vector2(6, 0);

        var closeBtn = MakeButton(header.transform, "CloseButton", "✕", new Color(0.7f, 0.2f, 0.2f), 36, 36);
        var closeBtnRT = closeBtn.GetComponent<RectTransform>();
        closeBtnRT.anchorMin        = new Vector2(1, 0.5f);
        closeBtnRT.anchorMax        = new Vector2(1, 0.5f);
        closeBtnRT.pivot            = new Vector2(1, 0.5f);
        closeBtnRT.anchoredPosition = new Vector2(0, 0);
        closeBtnRT.sizeDelta        = new Vector2(36, 36);

        // ── 탭 ───────────────────────────────────────────────────────
        var tabRow = MakeHBlock(window.transform, "TabRow", 36);
        tabRow.GetComponent<Image>().color = new Color(0.07f, 0.09f, 0.12f, 1f);
        var tabHLG = tabRow.AddComponent<HorizontalLayoutGroup>();
        tabHLG.childForceExpandWidth  = true;
        tabHLG.childForceExpandHeight = true;
        tabHLG.spacing = 2;
        MakeButton(tabRow.transform, "SaveTabButton",   "저장",    new Color(0.2f, 0.45f, 0.8f));
        MakeButton(tabRow.transform, "LoadTabButton",   "불러오기", new Color(0.3f, 0.3f, 0.3f));

        // ── 슬롯 컨테이너 (Scroll) ────────────────────────────────────
        var scrollGO = new GameObject("SlotScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollGO.transform.SetParent(window.transform, false);
        scrollGO.GetComponent<Image>().color = Color.clear;
        var scrollLE = scrollGO.AddComponent<LayoutElement>();
        scrollLE.flexibleHeight = 1;
        var scroll = scrollGO.GetComponent<ScrollRect>();
        scroll.horizontal = false;

        var viewport = MakeRect(scrollGO.transform, "Viewport");
        viewport.AddComponent<Image>().color = Color.clear;
        viewport.AddComponent<Mask>().showMaskGraphic = false;
        var viewportRT = viewport.GetComponent<RectTransform>();
        StretchFill(viewportRT);

        var content = MakeRect(viewport.transform, "SlotContainer");
        var contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot     = new Vector2(0.5f, 1);
        contentRT.offsetMin = Vector2.zero;
        contentRT.offsetMax = Vector2.zero;
        var csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 6;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;

        scroll.viewport  = viewportRT;
        scroll.content   = contentRT;

        // ── 상태 텍스트 ───────────────────────────────────────────────
        var statusGO = new GameObject("StatusText", typeof(RectTransform));
        statusGO.transform.SetParent(window.transform, false);
        var statusLE = statusGO.AddComponent<LayoutElement>();
        statusLE.minHeight = 24;
        var statusTMP = statusGO.AddComponent<TextMeshProUGUI>();
        statusTMP.text      = "";
        statusTMP.fontSize  = 13;
        statusTMP.color     = new Color(0.4f, 1f, 0.6f);
        statusTMP.alignment = TextAlignmentOptions.Center;

        // ── 확인 다이얼로그 ───────────────────────────────────────────
        CreateConfirmDialog(overlay.transform);

        return overlay;
    }

    static void CreateConfirmDialog(Transform parent)
    {
        var dialog = new GameObject("ConfirmDialog", typeof(RectTransform), typeof(Image));
        dialog.transform.SetParent(parent, false);
        StretchFill(dialog.GetComponent<RectTransform>());
        dialog.GetComponent<Image>().color = new Color(0, 0, 0, 0.55f);
        dialog.SetActive(false);

        var box = MakeRect(dialog.transform, "Box");
        box.AddComponent<Image>().color = new Color(0.14f, 0.16f, 0.22f, 1f);
        var boxRT = box.GetComponent<RectTransform>();
        boxRT.anchorMin = boxRT.anchorMax = boxRT.pivot = new Vector2(0.5f, 0.5f);
        boxRT.sizeDelta = new Vector2(420, 170);
        var boxVLG = box.AddComponent<VerticalLayoutGroup>();
        boxVLG.padding = new RectOffset(24, 24, 20, 20);
        boxVLG.spacing = 14;
        boxVLG.childForceExpandWidth  = true;
        boxVLG.childForceExpandHeight = false;

        var msgGO = new GameObject("ConfirmMessage", typeof(RectTransform));
        msgGO.transform.SetParent(box.transform, false);
        var le = msgGO.AddComponent<LayoutElement>(); le.minHeight = 60;
        var msg = msgGO.AddComponent<TextMeshProUGUI>();
        msg.text      = "정말 하시겠습니까?";
        msg.fontSize  = 15;
        msg.alignment = TextAlignmentOptions.Center;
        msg.color     = Color.white;

        var btnRow = MakeRect(box.transform, "BtnRow");
        var btnLE  = btnRow.AddComponent<LayoutElement>(); btnLE.minHeight = 38;
        var hlg    = btnRow.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 12; hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = true;

        MakeButton(btnRow.transform, "ConfirmYesButton", "확인", new Color(0.2f, 0.55f, 1f));
        MakeButton(btnRow.transform, "ConfirmNoButton",  "취소", new Color(0.45f, 0.45f, 0.45f));
    }

    // ═════════════════════════════════════════════════════════════════
    //  팩토리
    // ═════════════════════════════════════════════════════════════════

    static GameObject MakeRect(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static GameObject MakeHBlock(Transform parent, string name, float height)
    {
        var go  = MakeRect(parent, name);
        go.AddComponent<Image>().color = Color.clear;
        var le  = go.AddComponent<LayoutElement>(); le.minHeight = height;
        return go;
    }

    static TextMeshProUGUI MakeTMP(Transform parent, string name, string text, int size, bool bold)
    {
        var go  = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        tmp.color     = Color.white;
        return tmp;
    }

    static GameObject MakeButton(Transform parent, string name, string label, Color bg,
                                  float w = 0, float h = 0)
    {
        var go  = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = bg;
        if (w > 0 || h > 0)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(w, h);
        }
        var txtGO = new GameObject("Text", typeof(RectTransform));
        txtGO.transform.SetParent(go.transform, false);
        StretchFill(txtGO.GetComponent<RectTransform>());
        var tmp   = txtGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 13;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;
        return go;
    }

    static void StretchFill(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    // ─────────────────────────────────────────────────────────────────
    //  찾기 유틸
    // ─────────────────────────────────────────────────────────────────

    static T FindDeep<T>(Transform root, string name) where T : Component
    {
        var t = FindDeepTransform(root, name);
        return t ? t.GetComponent<T>() : null;
    }

    static Transform FindDeepTransform(Transform root, string name)
    {
        foreach (Transform child in root)
        {
            if (child.name == name) return child;
            var found = FindDeepTransform(child, name);
            if (found) return found;
        }
        return null;
    }

    static GameObject FindDeepGO(Transform root, string name)
    {
        var t = FindDeepTransform(root, name);
        return t ? t.gameObject : null;
    }

    static void SetField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Public    |
            System.Reflection.BindingFlags.Instance);
        field?.SetValue(target, value);
    }
}
#endif
