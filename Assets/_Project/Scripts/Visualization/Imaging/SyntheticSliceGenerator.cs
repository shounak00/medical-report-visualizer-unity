using UnityEngine;

public static class SyntheticSliceGenerator
{
    // Generates a grayscale "CT-ish" slice texture
    public static Texture2D GenerateSlice(int width, int height, int sliceIndex, int sliceCount)
    {
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        float z = sliceCount <= 1 ? 0f : sliceIndex / (float)(sliceCount - 1);

        // Fake anatomy blobs using simple signed distance fields + noise
        for (int y = 0; y < height; y++)
        {
            float v = (y / (float)(height - 1)) * 2f - 1f;
            for (int x = 0; x < width; x++)
            {
                float u = (x / (float)(width - 1)) * 2f - 1f;

                // Base soft tissue field
                float body = 1f - Mathf.Clamp01((u * u + v * v) * 0.9f);

                // Two "lung" cavities that change with z
                float shift = Mathf.Lerp(-0.15f, 0.15f, z);
                float lungL = 1f - Mathf.Clamp01(((u + 0.35f + shift) * (u + 0.35f + shift) / 0.25f) + (v * v / 0.40f));
                float lungR = 1f - Mathf.Clamp01(((u - 0.35f + shift) * (u - 0.35f + shift) / 0.25f) + (v * v / 0.40f));

                // "Bone" ring-ish bright area
                float bone = Mathf.Clamp01(1f - Mathf.Abs((u * u + v * v) - 0.45f) * 6f);

                // Simple procedural noise (cheap)
                float n = Mathf.PerlinNoise((u + 1.2f) * 3.0f + z * 2f, (v + 1.2f) * 3.0f + z * 2f);

                // Compose (lungs darker, bone brighter)
                float intensity =
                    body * 0.55f +
                    bone * 0.35f +
                    n * 0.12f -
                    (lungL * 0.25f + lungR * 0.25f);

                intensity = Mathf.Clamp01(intensity);

                var c = new Color(intensity, intensity, intensity, 1f);
                tex.SetPixel(x, y, c);
            }
        }

        tex.Apply(false, false);
        return tex;
    }
}
