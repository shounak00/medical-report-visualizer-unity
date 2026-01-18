using System.Collections.Generic;
using UnityEngine;
using Dicom;

public class DashboardController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PatientReportLoader loader;

    [Header("Graphs")]
    [SerializeField] private UIGraphRenderer heartRateGraph;
    [SerializeField] private UIGraphRenderer bpSystolicGraph;
    [SerializeField] private UIGraphRenderer bpDiastolicGraph;

    [Header("Headers (Labels)")]
    [SerializeField] private GraphHeaderView heartRateHeader;
    [SerializeField] private GraphHeaderView bpSystolicHeader;
    [SerializeField] private GraphHeaderView bpDiastolicHeader;

    [Header("Colors")]
    [SerializeField] private Color heartRateColor = new Color(0.20f, 0.80f, 0.20f, 1f); // green
    [SerializeField] private Color systolicColor  = new Color(0.90f, 0.30f, 0.20f, 1f); // red-ish
    [SerializeField] private Color diastolicColor = new Color(0.20f, 0.55f, 0.95f, 1f); // blue-ish
    
    [Header("Labs")]
    [SerializeField] private LabTableRenderer labTable;
    
    [Header("Imaging")]
    //[SerializeField] private SliceViewerController sliceViewer;
    [SerializeField] private DicomSliceViewerController sliceViewer;

    //public DicomSliceViewerController dicomViewer;
    //public PngSliceViewerController pngViewer;





    private void Start()
    {
        


        
        if (loader == null)
        {
            Debug.LogError("DashboardController: loader not assigned.");
            return;
        }
        
        

        StartCoroutine(loader.Load(OnLoaded, OnError));
        

    }

    private void OnLoaded(PatientReport report)
    {
        
        if (report?.vitals == null)
        {
            Debug.LogError("Report loaded but vitals is null.");
            return;
        }

        // ---- Titles ----
        if (heartRateHeader != null) heartRateHeader.SetTitle("Heart Rate (bpm)");
        if (bpSystolicHeader != null) bpSystolicHeader.SetTitle("Blood Pressure — Systolic (mmHg)");
        if (bpDiastolicHeader != null) bpDiastolicHeader.SetTitle("Blood Pressure — Diastolic (mmHg)");

        // ---- Heart Rate ----
        var hr = report.vitals.heartRate ?? new List<float>();
        heartRateGraph.Render(hr, heartRateColor, heartRateColor);
        SetRangeIfPossible(heartRateHeader, hr, " bpm");

        // ---- Blood Pressure split ----
        var sys = new List<float>();
        var dia = new List<float>();

        if (report.vitals.bloodPressure != null)
        {
            foreach (var bp in report.vitals.bloodPressure)
            {
                sys.Add(bp.systolic);
                dia.Add(bp.diastolic);
            }
        }

        bpSystolicGraph.Render(sys, systolicColor, systolicColor);
        bpDiastolicGraph.Render(dia, diastolicColor, diastolicColor);

        SetRangeIfPossible(bpSystolicHeader, sys, " mmHg");
        SetRangeIfPossible(bpDiastolicHeader, dia, " mmHg");
        
        if (labTable != null)
            labTable.Render(report.labs);
        
        // Imaging: DICOM series + findings + patient id
        if (sliceViewer != null && report.imaging != null)
        {
            // Set series path (fallback if not present)
            var path = string.IsNullOrEmpty(report.imaging.dicomSeriesPath)
                ? "DicomSeries/P-1024_SyntheticChestCT"
                : report.imaging.dicomSeriesPath;

            sliceViewer.SetSeriesFolder(path, report.imaging.ctSlices);
            sliceViewer.BindPatientContext(report.patientId, report.imaging.findings);
        }



        
        //if (sliceViewer != null && report.imaging != null)
            //sliceViewer.Init(report.imaging.ctSlices);
            
        
        


    }

    private void SetRangeIfPossible(GraphHeaderView header, List<float> values, string unit)
    {
        if (header == null || values == null || values.Count == 0) return;

        float min = values[0], max = values[0];
        for (int i = 1; i < values.Count; i++)
        {
            if (values[i] < min) min = values[i];
            if (values[i] > max) max = values[i];
        }

        header.SetRange(min, max, unit);
    }

    private void OnError(string err)
    {
        Debug.LogError(err);
    }
}
