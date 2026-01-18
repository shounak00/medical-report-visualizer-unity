using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;

// fo-dicom (Asset Store) namespaces
using Dicom;
using Dicom.Imaging;
using Dicom.IO.Buffer;

public class DicomSliceViewerController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private RawImage sliceImage;
    [SerializeField] private Slider sliceSlider;
    [SerializeField] private TMP_Text sliceIndexText;

    [Header("Meta + Findings UI")]
    [SerializeField] private TMP_Text metaText;
    [SerializeField] private TMP_Text findingsListText;

    [Header("Window/Level UI (DICOM only)")]
    [SerializeField] private Slider wcSlider;
    [SerializeField] private TMP_Text wcValueText;
    [SerializeField] private Slider wwSlider;
    [SerializeField] private TMP_Text wwValueText;

    [Header("Preset Buttons (DICOM only)")]
    [SerializeField] private Button lungPresetButton;
    [SerializeField] private Button softPresetButton;
    [SerializeField] private Button bonePresetButton;

    [Header("Series Folder (relative to StreamingAssets)")]
    [Tooltip("DICOM folder relative path. Example: DicomSeries/P-1024_SyntheticChestCT")]
    [SerializeField] private string seriesFolder = "DicomSeries/P-1024_SyntheticChestCT";

    [Header("PNG fallback folder (relative to StreamingAssets)")]
    [Tooltip("PNG fallback folder relative path. Example: Imaging/P-1024_SlicesPNG")]
    [SerializeField] private string pngFolder = "Imaging/P-1024_SlicesPNG";

    [Header("Window/Level (DICOM)")]
    [SerializeField] private float windowWidth = 1500f;
    [SerializeField] private float windowCenter = -600f;

    private enum ImagingMode { Dicom, Png }
    private ImagingMode _mode;

    // DICOM mode
    private DicomFile[] _files = Array.Empty<DicomFile>();

    // PNG mode
    private string[] _pngUrls = Array.Empty<string>();
    private Coroutine _pngLoadRoutine;

    // Shared
    private Texture2D _tex;
    private int _count;
    private int _currentIndex;

    private string _patientId = "P-1024";
    private string[] _findings;

    private void Start()
    {
#if UNITY_WEBGL
        _mode = ImagingMode.Png;
#else
        _mode = ImagingMode.Dicom;
#endif

        if (titleText != null)
            titleText.text = _mode == ImagingMode.Dicom
                ? "CT Viewer (DICOM Series)"
                : "CT Viewer (PNG Slice Stack)";

        // DICOM-only UI
        if (_mode == ImagingMode.Dicom)
        {
            SetupWindowLevelUI();
            SetupPresetButtons();
        }
        else
        {
            HideDicomOnlyUI();
        }

        LoadSeriesOrStack();
        SetupSlider();

        if (_count > 0)
            ShowSlice(0);

        UpdateMetaText();
    }

    // --- Public API (called from DashboardController) ---

    /// <summary>
    /// Set where to load imaging from. Use the DICOM folder for native builds.
    /// For WebGL, the component automatically switches to PNG and uses pngFolder instead.
    /// </summary>
    public void SetSeriesFolder(string seriesFolderRelativeToStreamingAssets, int expectedSlices = -1, string pngFolderRelativeToStreamingAssets = null)
    {
        if (!string.IsNullOrEmpty(seriesFolderRelativeToStreamingAssets))
            seriesFolder = seriesFolderRelativeToStreamingAssets;

        if (!string.IsNullOrEmpty(pngFolderRelativeToStreamingAssets))
            pngFolder = pngFolderRelativeToStreamingAssets;

        // If WebGL and expectedSlices is provided, we can use it to build fixed URLs even if directory listing isn't available
        if (expectedSlices > 0) _count = expectedSlices;

        LoadSeriesOrStack();
        SetupSlider();

        if (_count > 0)
            ShowSlice(0);

        UpdateMetaText();
    }

    public void BindPatientContext(string patientId, string[] findings)
    {
        _patientId = string.IsNullOrEmpty(patientId) ? "P-????" : patientId;
        _findings = findings;

        if (findingsListText != null)
        {
            if (_findings == null || _findings.Length == 0)
                findingsListText.text = "• (no findings provided)";
            else
                findingsListText.text = "• " + string.Join("\n• ", _findings);
        }

        UpdateMetaText();
    }

    // --- Mode selection + UI ---

    private void HideDicomOnlyUI()
    {
        if (wcSlider != null) wcSlider.gameObject.SetActive(false);
        if (wwSlider != null) wwSlider.gameObject.SetActive(false);
        if (wcValueText != null) wcValueText.gameObject.SetActive(false);
        if (wwValueText != null) wwValueText.gameObject.SetActive(false);

        if (lungPresetButton != null) lungPresetButton.gameObject.SetActive(false);
        if (softPresetButton != null) softPresetButton.gameObject.SetActive(false);
        if (bonePresetButton != null) bonePresetButton.gameObject.SetActive(false);
    }

    private void LoadSeriesOrStack()
    {
        if (_mode == ImagingMode.Dicom)
            LoadDicomSeries();
        else
            LoadPngStack();
    }

    // --- Window/Level + Presets (DICOM only) ---

    private void SetupWindowLevelUI()
    {
        if (wcSlider != null)
        {
            wcSlider.onValueChanged.RemoveAllListeners();
            wcSlider.minValue = -1200f;
            wcSlider.maxValue = 1200f;
            wcSlider.value = windowCenter;
            wcSlider.onValueChanged.AddListener(v =>
            {
                windowCenter = v;
                if (wcValueText != null) wcValueText.text = Mathf.RoundToInt(v).ToString();
                RedrawCurrent();
            });

            if (wcValueText != null) wcValueText.text = Mathf.RoundToInt(wcSlider.value).ToString();
        }

        if (wwSlider != null)
        {
            wwSlider.onValueChanged.RemoveAllListeners();
            wwSlider.minValue = 1f;
            wwSlider.maxValue = 3000f;
            wwSlider.value = windowWidth;
            wwSlider.onValueChanged.AddListener(v =>
            {
                windowWidth = v;
                if (wwValueText != null) wwValueText.text = Mathf.RoundToInt(v).ToString();
                RedrawCurrent();
            });

            if (wwValueText != null) wwValueText.text = Mathf.RoundToInt(wwSlider.value).ToString();
        }
    }

    private void SetupPresetButtons()
    {
        void Apply(float wc, float ww)
        {
            windowCenter = wc;
            windowWidth = Mathf.Max(1f, ww);

            if (wcSlider != null) wcSlider.SetValueWithoutNotify(windowCenter);
            if (wwSlider != null) wwSlider.SetValueWithoutNotify(windowWidth);

            if (wcValueText != null) wcValueText.text = Mathf.RoundToInt(windowCenter).ToString();
            if (wwValueText != null) wwValueText.text = Mathf.RoundToInt(windowWidth).ToString();

            RedrawCurrent();
        }

        if (lungPresetButton != null)
        {
            lungPresetButton.onClick.RemoveAllListeners();
            lungPresetButton.onClick.AddListener(() => Apply(-600f, 1500f)); // lung
        }

        if (softPresetButton != null)
        {
            softPresetButton.onClick.RemoveAllListeners();
            softPresetButton.onClick.AddListener(() => Apply(40f, 400f)); // soft tissue
        }

        if (bonePresetButton != null)
        {
            bonePresetButton.onClick.RemoveAllListeners();
            bonePresetButton.onClick.AddListener(() => Apply(300f, 1500f)); // bone-ish
        }
    }

    private void RedrawCurrent()
    {
        if (_count <= 0) return;
        ShowSlice(_currentIndex);
    }

    // --- Loading: DICOM ---

    private void LoadDicomSeries()
    {
        string folderPath = Path.Combine(Application.streamingAssetsPath, seriesFolder);

        if (!Directory.Exists(folderPath))
        {
            Debug.LogError("DICOM series folder not found: " + folderPath);
            _files = Array.Empty<DicomFile>();
            _count = 0;
            return;
        }

        var paths = Directory.GetFiles(folderPath, "*.dcm", SearchOption.TopDirectoryOnly);
        if (paths.Length == 0)
        {
            Debug.LogError("No .dcm files found in: " + folderPath);
            _files = Array.Empty<DicomFile>();
            _count = 0;
            return;
        }

        _files = paths
            .Select(p => DicomFile.Open(p))
            .OrderBy(f => GetInstanceNumber(f.Dataset))
            .ToArray();

        _count = _files.Length;

        Debug.Log($"Loaded {_count} DICOM slices from: {folderPath}");
    }

    private int GetInstanceNumber(DicomDataset ds)
    {
        try
        {
            if (ds.Contains(DicomTag.InstanceNumber))
                return ds.Get<int>(DicomTag.InstanceNumber, 0);
        }
        catch { }
        return 0;
    }

    // --- Loading: PNG (WebGL-friendly) ---

    private void LoadPngStack()
    {
        // Build URL paths; in WebGL we don't rely on Directory.GetFiles.
        string basePath = Path.Combine(Application.streamingAssetsPath, pngFolder);

#if UNITY_WEBGL
        int assumed = _count > 0 ? _count : 120; // if JSON passed ctSlices, use it; else default 120
        _pngUrls = Enumerable.Range(1, assumed)
            .Select(i => Path.Combine(basePath, $"slice_{i:D4}.png"))
            .ToArray();
        _count = _pngUrls.Length;
#else
        if (!Directory.Exists(basePath))
        {
            Debug.LogError("PNG stack folder not found: " + basePath);
            _pngUrls = Array.Empty<string>();
            _count = 0;
            return;
        }

        _pngUrls = Directory.GetFiles(basePath, "*.png", SearchOption.TopDirectoryOnly)
            .OrderBy(p => p)
            .Select(p => p)
            .ToArray();

        _count = _pngUrls.Length;

        Debug.Log($"Loaded {_count} PNG slices from: {basePath}");
#endif
    }

    // --- Slider wiring ---

    private void SetupSlider()
    {
        if (sliceSlider == null) return;

        sliceSlider.onValueChanged.RemoveAllListeners();

        if (_count <= 0)
        {
            sliceSlider.minValue = 0;
            sliceSlider.maxValue = 0;
            sliceSlider.value = 0;
            sliceSlider.interactable = false;

            if (sliceIndexText != null) sliceIndexText.text = "Slice: - / -";
            return;
        }

        sliceSlider.wholeNumbers = true;
        sliceSlider.minValue = 0;
        sliceSlider.maxValue = _count - 1;
        sliceSlider.value = Mathf.Clamp(sliceSlider.value, 0, _count - 1);
        sliceSlider.interactable = true;

        sliceSlider.onValueChanged.AddListener(v => ShowSlice((int)v));
    }

    // --- Rendering ---

    private void ShowSlice(int index)
    {
        _currentIndex = Mathf.Clamp(index, 0, Mathf.Max(0, _count - 1));

        if (_mode == ImagingMode.Dicom)
        {
            ShowDicomSlice(_currentIndex);
        }
        else
        {
            if (_pngLoadRoutine != null) StopCoroutine(_pngLoadRoutine);
            _pngLoadRoutine = StartCoroutine(ShowPngSlice(_currentIndex));
        }
    }

    private void ShowDicomSlice(int index)
    {
        if (_files == null || _files.Length == 0) return;

        index = Mathf.Clamp(index, 0, _files.Length - 1);
        var ds = _files[index].Dataset;

        int rows = ds.Get<int>(DicomTag.Rows, 0);
        int cols = ds.Get<int>(DicomTag.Columns, 0);

        if (rows <= 0 || cols <= 0)
        {
            Debug.LogError($"Invalid DICOM image size at slice {index}. Rows={rows} Cols={cols}");
            return;
        }

        var pixelData = DicomPixelData.Create(ds);
        IByteBuffer frame = pixelData.GetFrame(0);
        byte[] bytes = frame.Data;

        short[] src = new short[bytes.Length / 2];
        Buffer.BlockCopy(bytes, 0, src, 0, bytes.Length);

        EnsureTexture(cols, rows);

        float ww = Mathf.Max(1f, windowWidth);
        float wc = windowCenter;
        float low = wc - ww * 0.5f;
        float invW = 1f / ww;

        var outPx = new UnityEngine.Color32[cols * rows];
        for (int i = 0; i < outPx.Length; i++)
        {
            float n = (src[i] - low) * invW;
            byte g = (byte)(Mathf.Clamp01(n) * 255f);
            outPx[i] = new UnityEngine.Color32(g, g, g, 255);
        }

        _tex.SetPixels32(outPx);
        _tex.Apply(false, false);

        if (sliceImage != null) sliceImage.texture = _tex;
        if (sliceIndexText != null) sliceIndexText.text = $"Slice: {index + 1} / {_count}";

        UpdateMetaText();
    }

    private IEnumerator ShowPngSlice(int index)
    {
        if (_pngUrls == null || _pngUrls.Length == 0) yield break;

        index = Mathf.Clamp(index, 0, _pngUrls.Length - 1);
        string url = _pngUrls[index];

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

            UpdateMetaText();
        }
    }

    private void UpdateMetaText()
    {
        if (metaText == null) return;

        string mode = _mode == ImagingMode.Dicom ? "DICOM" : "PNG";
        string wl = _mode == ImagingMode.Dicom
            ? $" | WL: {Mathf.RoundToInt(windowCenter)} / {Mathf.RoundToInt(windowWidth)}"
            : "";

        metaText.text = $"Patient: {_patientId} | Slices: {_count} | Mode: {mode}{wl}";
    }

    private void EnsureTexture(int w, int h)
    {
        if (_tex != null && _tex.width == w && _tex.height == h) return;

        if (_tex != null) Destroy(_tex);

        _tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        _tex.wrapMode = TextureWrapMode.Clamp;
        _tex.filterMode = FilterMode.Bilinear;
    }

    private void OnDestroy()
    {
        if (_tex != null) Destroy(_tex);
    }
}
