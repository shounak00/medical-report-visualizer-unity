#if UNITY_EDITOR
using System;
using System.IO;
using System.Globalization;
using UnityEditor;
using UnityEngine;

// fo-dicom (Unity Asset Store package commonly uses these namespaces)
using Dicom;
using Dicom.Imaging;
using Dicom.IO.Buffer;

public static class SyntheticDicomSeriesGenerator_P1024
{
    [MenuItem("Tools/DICOM/Generate Synthetic CT Series (P-1024, 120 slices)")]
    public static void Generate()
    {
        const string patientId = "P-1024";
        const int sliceCount = 120;

        // Keep modest so generation is fast; viewer can upscale
        const int width = 256;
        const int height = 256;

        string outDir = Path.Combine(Application.dataPath, "StreamingAssets/DicomSeries/P-1024_SyntheticChestCT");
        Directory.CreateDirectory(outDir);

        // Shared UIDs for the study/series
        var studyUid = DicomUID.Generate();
        var seriesUid = DicomUID.Generate();
        var frameOfRefUid = DicomUID.Generate().UID;

        // Window/level preset (lung-ish)
        double windowCenter = -600;
        double windowWidth = 1500;

        // Helpers for DS (Decimal String) formatting
        string DS(double v) => v.ToString("0.####", CultureInfo.InvariantCulture);

        for (int i = 0; i < sliceCount; i++)
        {
            var ds = new DicomDataset(DicomTransferSyntax.ExplicitVRLittleEndian);

            // Required UIDs
            ds.Add(DicomTag.SOPClassUID, DicomUID.CTImageStorage);
            ds.Add(DicomTag.SOPInstanceUID, DicomUID.Generate());

            ds.Add(DicomTag.StudyInstanceUID, studyUid);
            ds.Add(DicomTag.SeriesInstanceUID, seriesUid);
            ds.Add(DicomTag.FrameOfReferenceUID, frameOfRefUid);

            // Patient (synthetic)
            ds.Add(DicomTag.PatientID, patientId);
            ds.Add(DicomTag.PatientName, "SYNTHETIC^PATIENT");
            ds.Add(DicomTag.PatientSex, "M");
            ds.Add(DicomTag.PatientAge, "058Y");

            // Study/Series descriptions
            ds.Add(DicomTag.Modality, "CT");
            ds.Add(DicomTag.StudyDescription, "Synthetic Chest CT (Demo)");
            ds.Add(DicomTag.SeriesDescription, "Axial Slices");
            ds.Add(DicomTag.InstanceNumber, i + 1);

            // Dates (optional)
            ds.Add(DicomTag.StudyDate, DateTime.Now.ToString("yyyyMMdd"));
            ds.Add(DicomTag.StudyTime, DateTime.Now.ToString("HHmmss"));

            // Geometry-ish metadata (use DS string values for this fo-dicom build)
            double sliceThickness = 1.0;
            double z = i * sliceThickness;

            ds.Add(DicomTag.SliceThickness, DS(sliceThickness));

            ds.Add(DicomTag.ImagePositionPatient, new[]
            {
                "0",
                "0",
                DS(z)
            });

            ds.Add(DicomTag.ImageOrientationPatient, new[]
            {
                "1","0","0",
                "0","1","0"
            });

            // Pixel format: 16-bit signed MONOCHROME2 (set on dataset; pixelData props are read-only)
            ds.Add(DicomTag.Rows, (ushort)height);
            ds.Add(DicomTag.Columns, (ushort)width);
            ds.Add(DicomTag.SamplesPerPixel, (ushort)1);
            ds.Add(DicomTag.PhotometricInterpretation, "MONOCHROME2");
            ds.Add(DicomTag.BitsAllocated, (ushort)16);
            ds.Add(DicomTag.BitsStored, (ushort)16);
            ds.Add(DicomTag.HighBit, (ushort)15);
            ds.Add(DicomTag.PixelRepresentation, (ushort)1); // signed

            // Window/level tags (use DS strings to avoid DS double issues)
            ds.Add(DicomTag.WindowCenter, DS(windowCenter));
            ds.Add(DicomTag.WindowWidth, DS(windowWidth));

            // Create pixel data (synthetic “CT-ish”)
            short[] pixels = GenerateSyntheticChestSlice(width, height, i, sliceCount);

            var pixelData = DicomPixelData.Create(ds, true);

            byte[] bytes = new byte[pixels.Length * 2];
            Buffer.BlockCopy(pixels, 0, bytes, 0, bytes.Length);
            pixelData.AddFrame(new MemoryByteBuffer(bytes));

            // Save
            string filename = $"slice_{(i + 1):D4}.dcm";
            string fullPath = Path.Combine(outDir, filename);

            new DicomFile(ds).Save(fullPath);
        }

        AssetDatabase.Refresh();
        Debug.Log($"Generated {sliceCount} synthetic CT DICOM slices at:\n{outDir}");
    }

    // Not medically accurate; “CT-ish” synthetic anatomy for demo
    private static short[] GenerateSyntheticChestSlice(int w, int h, int sliceIndex, int sliceCount)
    {
        var px = new short[w * h];
        float z = sliceCount <= 1 ? 0f : sliceIndex / (float)(sliceCount - 1);

        for (int y = 0; y < h; y++)
        {
            float v = (y / (float)(h - 1)) * 2f - 1f;
            for (int x = 0; x < w; x++)
            {
                float u = (x / (float)(w - 1)) * 2f - 1f;

                float r2 = u * u + v * v;
                float body = Mathf.Clamp01(1f - r2 * 0.9f);

                float shift = Mathf.Lerp(-0.12f, 0.12f, z);
                float lungL = 1f - Mathf.Clamp01(((u + 0.35f + shift) * (u + 0.35f + shift) / 0.22f) + (v * v / 0.40f));
                float lungR = 1f - Mathf.Clamp01(((u - 0.35f + shift) * (u - 0.35f + shift) / 0.22f) + (v * v / 0.40f));

                float bone = Mathf.Clamp01(1f - Mathf.Abs(r2 - 0.45f) * 7f);
                float n = Mathf.PerlinNoise((u + 1.3f) * 3.2f + z * 2f, (v + 1.1f) * 3.2f + z * 2f);

                float intensity =
                    body * 0.60f +
                    bone * 0.35f +
                    n * 0.10f -
                    (lungL * 0.28f + lungR * 0.28f);

                intensity = Mathf.Clamp01(intensity);

                // Map to rough HU-like range
                float hu = Mathf.Lerp(-1000f, 800f, intensity);
                px[y * w + x] = (short)Mathf.Clamp(hu, short.MinValue, short.MaxValue);
            }
        }

        return px;
    }
}
#endif
