using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 해제 보상 장치 — 함정이 해제되는 순간(코어 충전 / 가시 피격 / 베기 / 등불꽃 등)
/// 호출되어, 설정된 버프를 플레이어에게 주고 연결된 함정을 일시 비활성한다.
///
/// [연결]
///   - 각 함정의 해제 이벤트나 GimmickCore.onCharged 의 UnityEvent 에 Trigger() 연결.
///   - 같이 끌 함정은 onSuppress 에 함정의 Suppress()/SetActive(false) 를 연결.
///
/// [버프 종류]
///   MoveSpeed/AttackPower  → PlayerStatusEffects 에 값만 넣음(팀원 이동/전투가 읽음)
///   RestoreStamina/HP, HazardImmunity → 내 영역(PlayerVitals/StatusEffects)에서 완결
///   StunMonsters           → MonsterStunField 를 통해 처리(몬스터 코드 연결 필요)
/// </summary>
public class HazardWard : MonoBehaviour
{
    public enum BuffKind { None, MoveSpeed, AttackPower, RestoreStamina, RestoreHP, HazardImmunity, StunMonsters }

    [Header("버프 종류")]
    [SerializeField] private BuffKind buff = BuffKind.HazardImmunity;

    [Tooltip("배율 버프(이속·공격력)는 배율로, 회복류는 회복량으로 사용.")]
    [SerializeField] private float magnitude = 1.3f;
    [SerializeField] private float duration  = 5f;

    [Header("몬스터 기절용(StunMonsters)")]
    [SerializeField] private MonsterStunField stunField;

    [Header("해제 시 함께 끌 함정 / 추가 동작")]
    public UnityEvent onSuppress;   // 예: ShockTile.SetActive(false), FireZone.Suppress
    public UnityEvent onTriggered;

    /// <summary>함정 해제 이벤트에서 호출.</summary>
    public void Trigger()
    {
        ApplyBuff();
        onSuppress?.Invoke();
        onTriggered?.Invoke();
    }

    void ApplyBuff()
    {
        var fx = PlayerStatusEffects.Instance;
        switch (buff)
        {
            case BuffKind.MoveSpeed:      fx?.ApplyMoveSpeedBuff(magnitude, duration);             break;
            case BuffKind.AttackPower:    fx?.ApplyAttackBuff(magnitude, duration);                break;
            case BuffKind.HazardImmunity: fx?.ApplyHazardImmunity(duration);                       break;
            case BuffKind.RestoreStamina: PlayerVitals.Instance?.RestoreStamina(Mathf.RoundToInt(magnitude)); break;
            case BuffKind.RestoreHP:      PlayerVitals.Instance?.Heal(Mathf.RoundToInt(magnitude));           break;
            case BuffKind.StunMonsters:   if (stunField != null) stunField.Trigger();             break;
            case BuffKind.None:
            default:                                                                              break;
        }
    }
}
