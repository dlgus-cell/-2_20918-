using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 화면 하단 핫바 패널.
/// 칸을 미리 maxSlots 개 풀로 만들어 두고, 실제 아이템 수(Count)만큼만 보이게 한다.
/// (HorizontalLayoutGroup이 보이는 칸만 붙여서 "한 칸씩" 늘어난다)
/// </summary>
public class HotbarPanel : MonoBehaviour
{
    [Header("핫바 슬롯 프리팹")]
    [SerializeField] private HotbarCell slotPrefab;

    [Header("슬롯 배치 부모 (HorizontalLayoutGroup 권장)")]
    [SerializeField] private Transform slotContainer;

    private readonly List<HotbarCell> _cells = new();

    void OnEnable()  => HotbarManager.OnHotbarChanged += Refresh;
    void OnDisable() => HotbarManager.OnHotbarChanged -= Refresh;

    void Start()
    {
        BuildPool();
        Refresh();
    }

    void BuildPool()
    {
        if (slotPrefab == null || slotContainer == null) return;
        int max = HotbarManager.Instance != null ? HotbarManager.Instance.MaxSlots : 9;

        for (int i = 0; i < max; i++)
        {
            var c = Instantiate(slotPrefab, slotContainer);
            c.gameObject.SetActive(false);
            _cells.Add(c);
        }
    }

    void Refresh()
    {
        int count = HotbarManager.Instance != null ? HotbarManager.Instance.Count : 0;
        for (int i = 0; i < _cells.Count; i++)
        {
            bool on = i < count;
            _cells[i].gameObject.SetActive(on);
            if (on) _cells[i].Init(i);
        }
    }
}
