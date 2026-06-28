namespace DaerahRawanBanjir;

public class RealtimeFloodData
{
    public double RainfallMmPerDay { get; set; }

    public RainfallCategory RainfallCategory { get; set; }

    public string RainfallCategoryText { get; set; } = string.Empty;

    public double ElevationMeter { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}