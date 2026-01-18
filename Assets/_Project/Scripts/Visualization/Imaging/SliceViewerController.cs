using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SliceViewerController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private RawImage sliceImage;
    [SerializeField] private Slider sliceSlider;
    [SerializeField] private TMP_Text sliceIndexText;

    [Header("Config")]
    [SerializeField] private int textureSize = 256;

    private int _sliceCount = 1;
    private Texture2D _currentTex;

    private void Awake()
    {
        if (titleText != null)
            titleText.text = "CT Slices (Synthetic)";
    }

    public void Init(int sliceCount)
    {
        _sliceCount = Mathf.Max(1, sliceCount);

        if (sliceSlider != null)
        {
            sliceSlider.wholeNumbers = true;
            sliceSlider.minValue = 0;
            sliceSlider.maxValue = _sliceCount - 1;
            sliceSlider.onValueChanged.RemoveAllListeners();
            sliceSlider.onValueChanged.AddListener(OnSliderChanged);
            sliceSlider.value = 0;
        }

        RenderSlice(0);
    }

    private void OnSliderChanged(float v)
    {
        RenderSlice((int)v);
    }

    private void RenderSlice(int sliceIndex)
    {
        if (_currentTex != null)
            Destroy(_currentTex);

        _currentTex = SyntheticSliceGenerator.GenerateSlice(textureSize, textureSize, sliceIndex, _sliceCount);

        if (sliceImage != null)
            sliceImage.texture = _currentTex;

        if (sliceIndexText != null)
            sliceIndexText.text = $"Slice: {sliceIndex + 1} / {_sliceCount}";
    }

    private void OnDestroy()
    {
        if (_currentTex != null)
            Destroy(_currentTex);
    }
}