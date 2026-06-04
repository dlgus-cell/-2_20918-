// 골드 관련 기능을 담당함

using System;
using UnityEngine;

public class GoldSystem : MonoBehaviour
{
    public static GoldSystem Instance { get; private set; }

    // 골드가 변할 때 이유를 전달하는 신호
    public static event Action<int, int, GoldChangeReason> OnGoldChanged;
    // 골드 부족으로 사용 실패 시 발생하는 신호
    public static event Action OnGoldInsufficient;

    [Header("초기 설정")]
    // 게임 시작 시 보유한 골드 양
    [SerializeField] private int startingGold = 0;

    [Header("골드 상한선 (0 = 무제한)")]
    // 골드 최대 보유량
    [SerializeField] private int goldCap = 0;

    // 현재 보유 골드를 저장하는 변수
    private int _currentGold;
    // 다른 코드에서 현재 골드 값을 읽기 위한 변수
    public int CurrentGold => _currentGold;

    // Awake는 코드 생성 시 가장 먼저 한 번 호출되는 함수임
    void Awake()
    {
        // 다른 Instance가 있으면 파괴됨
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        // DontDestroyOnLoad는 씬이 바뀌어도 코드가 있는 오브젝트를 유지시킴
        DontDestroyOnLoad(gameObject);
        _currentGold = startingGold;
    }

    // 골드를 추가하는 함수
    public void AddGold(int amount, GoldChangeReason reason = GoldChangeReason.Misc)
    {
        // 양이 0 이하면 경고 출력 후 종료
        if (amount <= 0) { Debug.LogWarning($"[GoldSystem] AddGold: amount는 양수여야 합니다. (amount={amount})"); return; }

        // 변동 전 골드를 기록함
        int previous = _currentGold;
        _currentGold += amount;
        // 상한이 설정되어 있고 상한을 넘으면 상한 값으로 제한함
        if (goldCap > 0 && _currentGold > goldCap) _currentGold = goldCap;

        // 실제 변동량을 계산함
        int actualChange = _currentGold - previous;
        Debug.Log($"[GoldSystem] +{actualChange} Gold ({reason}) → 총 {_currentGold}G");
        // ?. 는 null이 아니면 호출함, null이면 무시함
        OnGoldChanged?.Invoke(_currentGold, actualChange, reason);
    }

    // 골드를 사용하는 함수
    public bool SpendGold(int amount, GoldChangeReason reason = GoldChangeReason.Purchase)
    {
        if (amount <= 0) { Debug.LogWarning($"[GoldSystem] SpendGold: amount는 양수여야 합니다. (amount={amount})"); return false; }
        if (_currentGold < amount)
        {
            Debug.Log($"[GoldSystem] 골드 부족! 보유:{_currentGold}G / 필요:{amount}G");
            // 골드 부족 신호를 발송함
            OnGoldInsufficient?.Invoke();
            return false;
        }

        _currentGold -= amount;
        Debug.Log($"[GoldSystem] -{amount} Gold ({reason}) → 총 {_currentGold}G");
        OnGoldChanged?.Invoke(_currentGold, -amount, reason);
        return true;
    }

    // 현재 골드를 특정 값으로 직접 설정함
    public void SetGold(int amount)
    {
        // Mathf.Max는 두 값 중 큰 값을 돌려줌
        _currentGold = Mathf.Max(0, amount);
        // Mathf.Min은 두 값 중 작은 값을 돌려줌
        if (goldCap > 0) _currentGold = Mathf.Min(_currentGold, goldCap);
        OnGoldChanged?.Invoke(_currentGold, 0, GoldChangeReason.Load);
    }

    // 특정 금액을 낼 수 있는지 확인하는 함수
    public bool CanAfford(int amount) => _currentGold >= amount;
}
