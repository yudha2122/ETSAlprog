namespace DaerahRawanBanjir;

public class BmkgDummyRealtimeService
{
    private readonly Random _random = new();

    private double _currentRainfall = 12;
    private double _currentElevation = 14;

    public RealtimeFloodData GenerateNextData()
    {
        double rainfallChange = (_random.NextDouble() * 14) - 7;

        if (_random.NextDouble() < 0.10)
        {
            rainfallChange += _random.NextDouble() * 35;
        }

        if (_random.NextDouble() < 0.06)
        {
            rainfallChange -= _random.NextDouble() * 20;
        }

        _currentRainfall = Math.Clamp(
            _currentRainfall + rainfallChange,
            0,
            160
        );

        double elevationChange = (_random.NextDouble() * 1.2) - 0.6;

        _currentElevation = Math.Clamp(
            _currentElevation + elevationChange,
            2,
            40
        );

        RainfallCategory category =
            RainfallCategoryHelper.GetCategory(_currentRainfall);

        return new RealtimeFloodData
        {
            RainfallMmPerDay = _currentRainfall,
            RainfallCategory = category,
            RainfallCategoryText = RainfallCategoryHelper.GetCategoryText(category),
            ElevationMeter = _currentElevation,
            UpdatedAt = DateTime.Now
        };
    }
}