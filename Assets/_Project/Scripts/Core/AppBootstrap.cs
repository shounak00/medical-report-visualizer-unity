using System.Collections.Generic;
using UnityEngine;

public class AppBootstrap : MonoBehaviour
{
    [SerializeField] private UIGraphRenderer heartRateGraph;

    private void Start()
    {
        // Quick demo data for Batch 1
        var hr = new List<float> { 72, 75, 78, 80, 76, 74, 77 };
        heartRateGraph.Render(hr, Color.white, Color.white);
    }
}