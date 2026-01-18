using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class UIGraphSeries
{
    public string name;
    public List<float> values = new();
    public bool visible = true;

    public Color lineColor = Color.white;
    public Color pointColor = Color.white;
}
