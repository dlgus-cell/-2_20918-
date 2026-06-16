using System;

/// <summary>기믹 하나의 저장 상태. saveId(고유) → state(문자열) 매핑.</summary>
[Serializable]
public class GimmickRecord
{
    public string saveId;
    public string state;
}
