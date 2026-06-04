// 화면에 골드 숫자 UI를 표시하는 코드임

using System.Collections;
using UnityEngine;
using TMPro;

public class GoldHUD : MonoBehaviour
{
    // [Header]는 Unity Inspector에 변수들의 제목을 표시함.
    [Header("골드 표시 텍스트")]
    // [SerializeField]는 변수를 Unity Inspector에서 수정 가능하게 함
    // 골드 숫자를 표시할 텍스트 UI 변수
    [SerializeField] private TMP_Text goldText;

    [Header("애니메이션 설정")]
    // useCountAnimation은 골드 변화 시 숫자가 천천히 변하는 효과의 사용을 결정할 변수
    [SerializeField] private bool useCountAnimation = true;
    // countDuration은 숫자가 변하는 데 걸리는 시간 변수
    [SerializeField] private float countDuration = 0.4f;
    // goldFormat은 1000단위 마다 ,를 넣는 숫자 표시 형식용 변수
    [SerializeField] private string goldFormat = "{0:N0}";

    // _displayedGold는 현재 화면에 표시되고 있는 골드 숫자 변수
    private int _displayedGold;
    // Coroutine은 시간에 따라 점점 진행되게 하는 자료형임
    // _countCoroutine은 현재 진행 중인 카운트 애니메이션 변수
    private Coroutine _countCoroutine;

    // OnEnable은 코드가 시작할 때 자동으로 호출되는 함수임
    // GoldSystem의 골드 변화 신호가 발생하면 HandleGoldChanged 함수가 호출됨
    void OnEnable()  => GoldSystem.OnGoldChanged += HandleGoldChanged;
    // OnDisable은 코드가 중단될 때 자동으로 호출되는 함수임, 함수가 더는 호출되지 않음
    void OnDisable() => GoldSystem.OnGoldChanged -= HandleGoldChanged;

    // Start는 게임 오브젝트가 활성화된 직후 한 번 호출되는 함수임
    void Start()
    {
        // GoldSystem이 존재하는지 확인함
        if (GoldSystem.Instance != null)
        {
            // 표시 중인 골드를 현재 실제 골드 값으로 바꿈
            _displayedGold = GoldSystem.Instance.CurrentGold;
            // 텍스트에 값을 표시함
            SetText(_displayedGold);
        }
    }

    void HandleGoldChanged(int newTotal, int delta, GoldChangeReason reason)
    {
        if (useCountAnimation)
        {
            // 진행 중인 애니메이션이 있으면 중지시킴
            if (_countCoroutine != null) StopCoroutine(_countCoroutine);
            // 새 목표 값으로 카운트 애니메이션을 시작함
            _countCoroutine = StartCoroutine(CountTo(newTotal));
        }
        else
        {
            // 표시 값을 갱신함
            _displayedGold = newTotal;
            SetText(_displayedGold);
        }
    }

    // SetText는 텍스트 UI에 골드 값을 표시하는 함수임
    void SetText(int value)
    {
        // goldText가 비어있지 않으면 형식대로 텍스트를 형성함
        if (goldText != null) goldText.text = string.Format(goldFormat, value);
    }

    // CountTo는 현재 표시 값에서 목표 값까지 변하는 코루틴임
    // IEnumerator는 시간에 따라 진행되는 함수의 자료형임
    IEnumerator CountTo(int target)
    {
        // 시작 값을 기록함
        int start = _displayedGold;
        // 걸린 시간을 0으로 초기화함
        float elapsed = 0f;

        while (elapsed < countDuration)
        {
            // Time.deltaTime라는 한 프레임에 흐른 시간을 더함
            elapsed += Time.deltaTime;
            // Mathf.SmoothStep은 0에서 1로 부드럽게 변화하는 값을 만듬
            float t = Mathf.SmoothStep(0f, 1f, elapsed / countDuration);
            // Mathf.Lerp(a, b, t)는 a와 b 사이를 t(0~1)사이에 값을 줌
            // Mathf.RoundToInt는 소수를 정수로 반올림함
            _displayedGold = Mathf.RoundToInt(Mathf.Lerp(start, target, t));
            // 변경된 값으로 텍스트를 업데이트함
            SetText(_displayedGold);
            // 한 프레임 쉬고 다시 진행함
            yield return null;
        }

        // 반복이 끝나면 정확히 목표 값으로 맞춤
        _displayedGold = target;
        SetText(_displayedGold);
    }
}
