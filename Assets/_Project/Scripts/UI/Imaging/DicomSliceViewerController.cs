using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

using Dicom;
using Dicom.Imaging;
using Dicom.IO.Buffer;

public class DicomSliceViewerController : MonoBehaviour
{
    [Header("Meta + Window/Level UI")]
    [SerializeField] private TMP_Text metaText;

    [SerializeField] private Slider wcSlider;
    [SerializeField] private TMP_Text wcValueText;

    [SerializeField] private Slider wwSlider;
    [SerializeField] private TMP_Text wwValueText;

    [Header("Findings UI")]
    [SerializeField] private TMP_Text findingsListText;

    
    [Header("UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private RawImage sliceImage;
    [SerializeField] private Slider sliceSlider;
    [SerializeField] private TMP_Text sliceIndexText;

    [Header("Series Folder (relative to StreamingAssets)")]
    [SerializeField] private string seriesFolder = "DicomSeries/P-1024_SyntheticChestCT";

    [Header("Window/Level")]
    [SerializeField] private float windowWidth = 1500f;
    [SerializeField] private float windowCenter = -600f;

    private DicomFile[] _files = Array.Empty<DicomFile>();
    private Texture2D _tex;
    private int _count;
    
    private string _patientId = "P-1024";
    private string[] _findings = null;
    private int _currentIndex = 0;
    
    [Header("Preset Buttons")]
    [SerializeField] private Button lungPresetButton;
    [SerializeField] private Button softPresetButton;
    [SerializeField] private Button bonePresetButton;


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
            lungPresetButton.onClick.AddListener(() => Apply(-600f, 1500f));
        }

        if (softPresetButton != null)
        {
            softPresetButton.onClick.RemoveAllListeners();
            softPresetButton.onClick.AddListener(() => Apply(40f, 400f));
        }

        if (bonePresetButton != null)
        {
            bonePresetButton.onClick.RemoveAllListeners();
            bonePresetButton.onClick.AddListener(() => Apply(300f, 1500f));
        }
    }


    private void Start()
    {
        SetupWindowLevelUI();
        UpdateMetaText();

        if (titleText != null) titleText.text = "CT Viewer (Synthetic DICOM Series)";

        LoadSeries();
        SetupSlider();

        if (_count > 0)
            ShowSlice(0);
        
        SetupPresetButtons();

    }
    
    private void Update()
    {
        if (_count <= 0 || sliceSlider == null) return;

        // Arrow key step
        //if (Input.GetKeyDown(KeyCode.LeftArrow)) StepSlice(-1);
        //if (Input.GetKeyDown(KeyCode.RightArrow)) StepSlice(1);

        // Mouse wheel scrub (optional)
        float wheel = Input.mouseScrollDelta.y;
        if (Mathf.Abs(wheel) > 0.01f) StepSlice(wheel > 0 ? 1 : -1);
    }

    private void StepSlice(int delta)
    {
        int next = Mathf.Clamp(_currentIndex + delta, 0, _count - 1);
        sliceSlider.SetValueWithoutNotify(next);
        ShowSlice(next);
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


    private void RedrawCurrent()
    {
        if (_count <= 0) return;
        ShowSlice(_currentIndex);
    }

    private void UpdateMetaText()
    {
        if (metaText == null) return;
        metaText.text = $"Patient: {_patientId}   |   Slices: {_count}   |   WL: {Mathf.RoundToInt(windowCenter)} / {Mathf.RoundToInt(windowWidth)}";
    }


    private void LoadSeries()
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

        // Load + sort by InstanceNumber (works for your generated series)
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
            // Old fo-dicom style
            // If tag missing, return 0
            if (ds.Contains(DicomTag.InstanceNumber))
                return ds.Get<int>(DicomTag.InstanceNumber, 0);
        }
        catch { /* ignore */ }

        return 0;
    }

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
        sliceSlider.value = 0;
        sliceSlider.interactable = true;

        sliceSlider.onValueChanged.AddListener(v => ShowSlice((int)v));
    }
    
    public void SetSeriesFolder(string seriesFolderRelativeToStreamingAssets, int expectedSlices = -1)
    {
        seriesFolder = seriesFolderRelativeToStreamingAssets;

        LoadSeries();
        SetupSlider();

        if (_count > 0)
            ShowSlice(0);

        if (expectedSlices > 0 && expectedSlices != _count)
            Debug.LogWarning($"Expected {expectedSlices} slices but found {_count} in {seriesFolder}");
    }


    private void ShowSlice(int index)
    {
        _currentIndex = index;

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

        // Extract 16-bit signed pixel data from the dataset
        var pixelData = DicomPixelData.Create(ds);
        IByteBuffer frame = pixelData.GetFrame(0);
        byte[] bytes = frame.Data;

        short[] src = new short[bytes.Length / 2];
        Buffer.BlockCopy(bytes, 0, src, 0, bytes.Length);

        EnsureTexture(cols, rows);

        // Window/Level -> 8-bit grayscale
        float ww = Mathf.Max(1f, windowWidth);
        float wc = windowCenter;
        float low = wc - ww * 0.5f;
        float invW = 1f / ww;

        UnityEngine.Color32[] outPx = new UnityEngine.Color32[cols * rows];

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
