using System;
using System.IO;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class PatientReportLoader : MonoBehaviour
{
    [Header("Option A (Recommended for dev): Drag JSON file as TextAsset")]
    [SerializeField] private TextAsset reportJsonAsset;

    [Header("Option B (WebGL-safe streaming): put file in StreamingAssets")]
    [SerializeField] private bool useStreamingAssets;
    [SerializeField] private string streamingFileName = "patient_report_sample.json";

    public IEnumerator Load(Action<PatientReport> onLoaded, Action<string> onError)
    {
        if (!useStreamingAssets)
        {
            if (reportJsonAsset == null)
            {
                onError?.Invoke("PatientReportLoader: reportJsonAsset is not assigned.");
                yield break;
            }

            var report = JsonUtility.FromJson<PatientReport>(reportJsonAsset.text);
            if (report == null) onError?.Invoke("Failed to parse JSON (TextAsset).");
            else onLoaded?.Invoke(report);

            yield break;
        }

        // StreamingAssets path (WebGL uses URL-style access)
        string path = Path.Combine(Application.streamingAssetsPath, streamingFileName);

        using (var req = UnityWebRequest.Get(path))
        {
            yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            if (req.result != UnityWebRequest.Result.Success)
#else
            if (req.isNetworkError || req.isHttpError)
#endif
            {
                onError?.Invoke($"StreamingAssets load failed: {req.error} | {path}");
                yield break;
            }

            var json = req.downloadHandler.text;
            var report = JsonUtility.FromJson<PatientReport>(json);

            if (report == null) onError?.Invoke("Failed to parse JSON (StreamingAssets).");
            else onLoaded?.Invoke(report);
        }
    }
}