using System;

[Serializable]
public class ImagingData
{
    public int ctSlices;
    public string dicomSeriesPath;
    public string[] findings;
}