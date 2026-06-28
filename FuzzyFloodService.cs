namespace DaerahRawanBanjir;

public static class FuzzyFloodService
{
    public static FuzzyFloodResult Process(double curahHujan, double elevasi)
    {
        RainfallCategory category =
            RainfallCategoryHelper.GetCategory(curahHujan);

        return ProcessMonitoring(
            curahHujan,
            category,
            elevasi
        );
    }

    public static FuzzyFloodResult ProcessMonitoring(
        double rainfallMmPerDay,
        RainfallCategory rainfallCategory,
        double elevationMeter)
    {
        double rainfallRisk =
            Math.Clamp(rainfallMmPerDay / 120.0 * 100.0, 0, 100);

        double categoryRisk =
            RainfallCategoryHelper.GetCategoryRiskValue(rainfallCategory);

        double elevationRisk =
            Math.Clamp((40.0 - elevationMeter) / 40.0 * 100.0, 0, 100);

        double finalRisk =
            (rainfallRisk * 0.45) +
            (categoryRisk * 0.35) +
            (elevationRisk * 0.20);

        string statusCode;
        string statusText;

        if (finalRisk >= 70)
        {
            statusCode = "BAHAYA";
            statusText = "BAHAYA / Sangat Rawan Banjir";
        }
        else if (finalRisk >= 40)
        {
            statusCode = "WASPADA";
            statusText = "WASPADA / Siaga Potensi Banjir";
        }
        else
        {
            statusCode = "AMAN";
            statusText = "AMAN / Kondusif";
        }

        return new FuzzyFloodResult
        {
            RiskScore = finalRisk,
            StatusCode = statusCode,
            StatusText = statusText,
            RainHigh = Math.Clamp(rainfallMmPerDay / 120.0, 0, 1),
            ElevationLow = Math.Clamp((40.0 - elevationMeter) / 40.0, 0, 1),
            RainfallCategoryText = RainfallCategoryHelper.GetCategoryText(rainfallCategory),
            Recommendation = GetRecommendation(statusCode)
        };
    }

    private static string GetRecommendation(string statusCode)
    {
        if (statusCode == "BAHAYA")
        {
            return "Segera tingkatkan kewaspadaan. Pantau saluran air, siapkan jalur evakuasi, dan hindari daerah rendah atau aliran sungai.";
        }

        if (statusCode == "WASPADA")
        {
            return "Lakukan pemantauan berkala. Bersihkan drainase, amankan dokumen penting, dan perhatikan informasi cuaca terbaru.";
        }

        return "Kondisi masih relatif aman. Tetap pantau perubahan curah hujan dan kondisi lingkungan sekitar.";
    }
}

public class FuzzyFloodResult
{
    public double RiskScore { get; set; }

    public string StatusCode { get; set; } = string.Empty;

    public string StatusText { get; set; } = string.Empty;

    public double RainHigh { get; set; }

    public double ElevationLow { get; set; }

    public string RainfallCategoryText { get; set; } = string.Empty;

    public string Recommendation { get; set; } = string.Empty;
}