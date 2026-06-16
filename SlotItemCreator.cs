#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using System.IO;

/// <summary>
/// 에디터 유틸리티 — SaveLoadPanel에서 사용하는 SlotItem 프리팹을 자동으로 생성합니다.
///
/// ★ 통폐합 변경 (5단계):
///   - LevelSummaryText 항목 추가 ("Lv.12  골드 3,400G" 표시용)
///
/// [사용법]
///   Unity 메뉴 → Tools → Quest RPG → Create Save Slot Prefab
///   Assets/Prefabs/UI/SlotItem.prefab 이 생성됩니다.
///   SaveLoadPanel Inspector의 slotItemPrefab 슬롯에 드래그하세요.
/// </summary>
public static class SlotItemCreator
{
    private const string PrefabPath = "Assets/Prefabs/UI/SlotItem.prefab";

    [MenuItem("Tools/Quest RPG/Create Save Slot Prefab")]
    public static void CreateSlotItemPrefab()
    {
        Directory.CreateDirectory(Application.dataPath + "/Prefabs/UI");

        // ── 루트 ────────────────────────────────────────────────────
        var root     = new GameObject("SlotItem");
        var rootRect = root.AddComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(0, 140);   // ★ 높이 약간 늘림 (LevelSummary 추가)

        root.AddComponent<Image>().color = new Color(0.12f, 0.14f, 0.18f, 0.95f);

        var layout = root.AddComponent<HorizontalLayoutGroup>();
        layout.padding              = new RectOffset(12, 12, 10, 10);
        layout.spacing              = 10;
        layout.childAlignment       = TextAnchor.MiddleLeft;
        layout.childForceExpandWidth  = false;
        layout.childForceExpandHeight = true;

        // ── 왼쪽: 슬롯 번호 ──────────────────────────────────────────
        var numBlock = CreateBlock(root.transform, "NumberBlock", 50);
        numBlock.GetComponent<Image>().color = new Color(0.2f, 0.45f, 0.8f);
        var numTMP = CreateTMP(numBlock.transform, "SlotNumberText", "1", 22, true);
        numTMP.alignment = TextAlignmentOptions.Center;
        StretchFill(numTMP.rectTransform);

        // ── 중앙: 정보 ───────────────────────────────────────────────
        var infoBlock = CreateBlock(root.transform, "InfoBlock", 0);
        infoBlock.GetComponent<Image>().color = Color.clear;
        infoBlock.AddComponent<LayoutElement>().flexibleWidth = 1;

        var vlg = infoBlock.AddComponent<VerticalLayoutGroup>();
        vlg.padding              = new RectOffset(4, 4, 4, 4);
        vlg.spacing              = 2;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;

        CreateTMP(infoBlock.transform, "SaveNameText",     "빈 슬롯",  16, true,  Color.white);
        CreateTMP(infoBlock.transform, "DateTimeText",     "",          12, false, new Color(0.7f, 0.7f, 0.7f));
        CreateTMP(infoBlock.transform, "PlayTimeText",     "",          12, false, new Color(0.7f, 0.8f, 1f));
        // ★ LevelSummaryText 추가
        CreateTMP(infoBlock.transform, "LevelSummaryText", "",          12, false, new Color(1f,   0.85f, 0.4f));
        CreateTMP(infoBlock.transform, "QuestSummaryText", "",          11, false, new Color(0.6f, 1f,   0.6f));

        // ── 오른쪽: 버튼 ─────────────────────────────────────────────
        var btnBlock = CreateBlock(root.transform, "ButtonBlock", 90);
        btnBlock.GetComponent<Image>().color = Color.clear;

        var btnVLG = btnBlock.AddComponent<VerticalLayoutGroup>();
        btnVLG.padding              = new RectOffset(0, 0, 4, 4);
        btnVLG.spacing              = 5;
        btnVLG.childForceExpandWidth  = true;
        btnVLG.childForceExpandHeight = true;

        CreateButton(btnBlock.transform, "SaveButton",   "저장",     new Color(0.2f,  0.55f, 1f));
        CreateButton(btnBlock.transform, "LoadButton",   "불러오기", new Color(0.25f, 0.7f,  0.4f));
        CreateButton(btnBlock.transform, "DeleteButton", "삭제",     new Color(0.75f, 0.25f, 0.25f));

        // ── 구분선 ────────────────────────────────────────────────────
        var div     = new GameObject("Divider", typeof(RectTransform), typeof(Image));
        div.transform.SetParent(root.transform, false);
        var divRT   = div.GetComponent<RectTransform>();
        divRT.anchorMin        = new Vector2(0, 0);
        divRT.anchorMax        = new Vector2(1, 0);
        divRT.sizeDelta        = new Vector2(0, 1);
        divRT.anchoredPosition = Vector2.zero;
        div.GetComponent<Image>().color = new Color(1, 1, 1, 0.08f);

        // ── 저장 ──────────────────────────────────────────────────────
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.Refresh();

        Debug.Log($"[SlotItemCreator] 프리팹 생성 완료: {PrefabPath}");
        EditorGUIUtility.PingObject(prefab);
    }

    // ── 헬퍼 ─────────────────────────────────────────────────────────

    static GameObject CreateBlock(Transform parent, string name, float preferredWidth)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        if (preferredWidth > 0)
        {
            var le         = go.AddComponent<LayoutElement>();
            le.preferredWidth = preferredWidth;
            le.minWidth       = preferredWidth;
        }
        return go;
    }

    static TextMeshProUGUI CreateTMP(Transform parent, string name, string text,
                                     int size, bool bold, Color? color = null)
    {
        var go  = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.AddComponent<LayoutElement>().minHeight = size + 4;

        var tmp         = go.AddComponent<TextMeshProUGUI>();
        tmp.text        = text;
        tmp.fontSize    = size;
        tmp.fontStyle   = bold ? FontStyles.Bold : FontStyles.Normal;
        tmp.color       = color ?? Color.white;
        tmp.alignment   = TextAlignmentOptions.Left;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        return tmp;
    }

    static void CreateButton(Transform parent, string name, string label, Color bg)
    {
        var go  = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = bg;

        var txtGO = new GameObject("Text", typeof(RectTransform));
        txtGO.transform.SetParent(go.transform, false);
        StretchFill(txtGO.GetComponent<RectTransform>());

        var tmp       = txtGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 12;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;
    }

    static void StretchFill(RectTransform rt)
    {
        rt.anchorMin  = Vector2.zero;
        rt.anchorMax  = Vector2.one;
        rt.offsetMin  = Vector2.zero;
        rt.offsetMax  = Vector2.zero;
    }
}
#endif
