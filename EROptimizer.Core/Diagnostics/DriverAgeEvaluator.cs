namespace EROptimizer.Core.Diagnostics;

public static class DriverAgeEvaluator
{
    public static DateTime? TryParseDriverDateYyyyMmDd(string? yyyyMmDd)
    {
        if (yyyyMmDd is null || string.IsNullOrWhiteSpace(yyyyMmDd))
            return null;
        var t = yyyyMmDd.Trim();
        if (DateTime.TryParse(t, out var d))
            return d.Date;
        return null;
    }

    public static string EvaluateReferenceLabel(DateTime? driverDate)
    {
        if (driverDate is null)
            return "판단 불가";

        var today = DateTime.Today;
        if (driverDate.Value.Date > today.AddDays(7))
            return "정상(미래 날짜·참고)";

        var ageMonths = ((today.Year - driverDate.Value.Year) * 12) + today.Month - driverDate.Value.Month;
        if (ageMonths <= 6)
            return "정상";
        if (ageMonths <= 12)
            return "보통";
        if (ageMonths <= 24)
            return "업데이트 확인 권장";

        return "오래된 드라이버일 수 있음";
    }
}
