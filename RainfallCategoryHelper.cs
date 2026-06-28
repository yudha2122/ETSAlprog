namespace DaerahRawanBanjir;

public enum RainfallCategory
{
    SangatRingan,
    Ringan,
    Sedang,
    Lebat,
    SangatLebat
}

public static class RainfallCategoryHelper
{
    public static RainfallCategory GetCategory(double rainfallMmPerDay)
    {
        if (rainfallMmPerDay < 5)
        {
            return RainfallCategory.SangatRingan;
        }

        if (rainfallMmPerDay <= 20)
        {
            return RainfallCategory.Ringan;
        }

        if (rainfallMmPerDay <= 50)
        {
            return RainfallCategory.Sedang;
        }

        if (rainfallMmPerDay <= 100)
        {
            return RainfallCategory.Lebat;
        }

        return RainfallCategory.SangatLebat;
    }

    public static string GetCategoryText(RainfallCategory category)
    {
        return category switch
        {
            RainfallCategory.SangatRingan => "Hujan Sangat Ringan",
            RainfallCategory.Ringan => "Hujan Ringan",
            RainfallCategory.Sedang => "Hujan Sedang",
            RainfallCategory.Lebat => "Hujan Lebat",
            RainfallCategory.SangatLebat => "Hujan Sangat Lebat",
            _ => "Tidak Diketahui"
        };
    }

    public static double GetCategoryRiskValue(RainfallCategory category)
    {
        return category switch
        {
            RainfallCategory.SangatRingan => 10,
            RainfallCategory.Ringan => 25,
            RainfallCategory.Sedang => 50,
            RainfallCategory.Lebat => 75,
            RainfallCategory.SangatLebat => 95,
            _ => 0
        };
    }
}