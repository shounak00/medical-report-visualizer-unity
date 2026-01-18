using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

public class PngSliceViewerController : MonoBehaviour
{
    [SerializeField] private RawImage sliceImage;
    [SerializeField] private Slider sliceSlider;
    [SerializeField] private TMP_Text sliceIndexText;

    [SerializeField] private string pngFolder = "Imaging/P-1024_SlicesPNG";

    private string[] _urls = new string[0];
    private int _count;

    private void Start()
    {
        StartCoroutine(LoadIndex());
    }

    private IEnumerator LoadIndex()
    {
        string dir = Path.Combine(Application.streamingAssetsPath, pngFolder);

#if UNITY_WEBGL
        // In WebGL we can’t Directory.GetFiles reliably; so we assume fixed naming and ctSlices count externally.
        // We'll just try first 120 (matches your patient JSON).
        int assumed = 120;
        _urls = Enumerable.Range(1, assumed)
            .Select(i => Path.Combine(dir, $"slice_{i:D4}.png"))
            .ToArray();
#else
        if (Directory.Exists(dir))
        {
            var files = Directory.GetFiles(dir, "*.png");
            _urls = files.OrderBy(f => f).Select(f => f).ToArray();
        }
#endif

        _count = _urls.Length;

        if (sliceSlider != null)
        {
            sliceSlider.wholeNumbers = true;
            sliceSlider.minValue = 0;
            sliceSlider.maxValue = Mathf.Max(0, _count - 1);
            sliceSlider.value = 0;
            sliceSlider.onValueChanged.AddListener(v => StartCoroutine(Show((int)v)));
        }

        yield return Show(0);
    }

    private IEnumerator Show(int index)
    {
        if (_count == 0) yield break;

        index = Mathf.Clamp(index, 0, _count - 1);
        string url = _urls[index];

        using (var req = UnityWebRequestTexture.GetTexture(url))
        {
            yield return req.SendWebRequest();
#if UNITY_2020_2_OR_NEWER
            if (req.result != UnityWebRequest.Result.Success)
#else
            if (req.isNetworkError || req.isHttpError)
#endif
            {
                Debug.LogError("PNG load failed: " + req.error + " | " + url);
                yield break;
            }

            var tex = DownloadHandlerTexture.GetContent(req);
            if (sliceImage != null) sliceImage.texture = tex;
            if (sliceIndexText != null) sliceIndexText.text = $"Slice: {index + 1} / {_count}";
        }
    }
}
