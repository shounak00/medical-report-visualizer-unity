#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

using Dicom;
using Dicom.Imaging;
using Dicom.IO.Buffer;

public static class DicomToPngExporter
{
    [MenuItem("Tools/DICOM/Export Series To PNG (for WebGL)")]
    public static void Export()
    {
        string dicomDir = Path.Combine(Application.dataPath, "StreamingAssets/DicomSeries/P-1024_SyntheticChestCT");
        string pngDir   = Path.Combine(Application.dataPath, "StreamingAssets/Imaging/P-1024_SlicesPNG");

        if (!Directory.Exists(dicomDir))
        {
            Debug.LogError("DICOM folder not found: " + dicomDir);
            return;
        }

        Directory.CreateDirectory(pngDir);

        var paths = Directory.GetFiles(dicomDir, "*.dcm")
            .Select(p => new { p, inst = ReadInstance(p) })
            .OrderBy(x => x.inst)
            .Select(x => x.p)
            .ToArray();

        // Lung-ish window/level for export
        float wc = -600f;
        float ww = 1500f;
        float low = wc - ww * 0.5f;
        float invW = 1f / ww;

        for (int i = 0; i < paths.Length; i++)
        {
            var file = DicomFile.Open(paths[i]);
            var ds = file.Dataset;

            int rows = ds.Get<int>(DicomTag.Rows, 0);
            int cols = ds.Get<int>(DicomTag.Columns, 0);

            var pixelData = DicomPixelData.Create(ds);
            IByteBuffer frame = pixelData.GetFrame(0);
            byte[] bytes = frame.Data;

            short[] src = new short[bytes.Length / 2];
            System.Buffer.BlockCopy(bytes, 0, src, 0, bytes.Length);

            var tex = new Texture2D(cols, rows, TextureFormat.RGBA32, false);
            var outPx = new UnityEngine.Color32[cols * rows];

            for (int p = 0; p < outPx.Length; p++)
            {
                float n = (src[p] - low) * invW;
                byte g = (byte)(Mathf.Clamp01(n) * 255f);
                outPx[p] = new UnityEngine.Color32(g, g, g, 255);
            }

            tex.SetPixels32(outPx);
            tex.Apply(false, false);

            var png = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);

            File.WriteAllBytes(Path.Combine(pngDir, $"slice_{i+1:D4}.png"), png);

            if (i % 20 == 0) Debug.Log($"Exported {i+1}/{paths.Length}");
        }

        AssetDatabase.Refresh();
        Debug.Log("PNG export done: " + pngDir);
    }

    private static int ReadInstance(string path)
    {
        try
        {
            var f = DicomFile.Open(path);
            if (f.Dataset.Contains(DicomTag.InstanceNumber))
                return f.Dataset.Get<int>(DicomTag.InstanceNumber, 0);
        }
        catch {}
        return 0;
    }
}
#endif
