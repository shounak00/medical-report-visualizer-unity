using System;

[Serializable]
public class LabResult
{
    public string name;
    public float value;

    // normal range
    public float normalMin;
    public float normalMax;

    public bool IsLow => value < normalMin;
    public bool IsHigh => value > normalMax;
    public bool IsAbnormal => IsLow || IsHigh;
}