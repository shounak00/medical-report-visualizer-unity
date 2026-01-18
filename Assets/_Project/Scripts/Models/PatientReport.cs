using System;
using System.Collections.Generic;

[Serializable]
public class PatientReport
{
    public string patientId;
    public int age;
    public string gender;

    public Vitals vitals;

    // ✅ USE LabResult (matches your UI)
    public List<LabResult> labs;

    public ImagingData imaging;
}