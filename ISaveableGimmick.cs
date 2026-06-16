/// <summary>
/// 저장 가능한 기믹이 구현하는 인터페이스. (폴더3 3단계 설계)
///
/// 구현 대상: LockedDoor(열림/닫힘), WindupGimmick(감김/풀림),
///            ItemPickup(주움/안주움) 등.
///
/// ── 동작 ─────────────────────────────────────────────────────────
///   SaveId        : 씬 안에서 이 기믹을 식별하는 고유 문자열(Inspector 지정).
///                   같은 ID 가 둘 이상이면 마지막 것만 저장되므로 유일해야 함.
///   CaptureState(): 현재 상태를 문자열로 직렬화. 빈 문자열이면 "기본값"으로 보고
///                   저장에서 생략될 수 있다.
///   RestoreState(): 저장됐던 상태 문자열로 복원. 시각 연출용 UnityEvent 를
///                   여기서 다시 발동시켜 화면 상태를 맞춘다.
/// </summary>
public interface ISaveableGimmick
{
    string SaveId { get; }
    string CaptureState();
    void   RestoreState(string state);
}
