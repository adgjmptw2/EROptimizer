namespace EROptimizer.Core.Diagnostics;

public static class DriverAgePresentation
{
    public static string GuidanceText(DateTime? driverDate, string evaluateReferenceLabel)
    {
        if (evaluateReferenceLabel == "판단 불가" || driverDate is null)
            return "설치 날짜를 읽지 못했습니다. 장치 관리자에서 확인할 수 있습니다.";

        var y = driverDate.Value.Year;
        var m = driverDate.Value.Month;
        var semi = m >= 7 ? "하반기" : "상반기";

        return evaluateReferenceLabel switch
        {
            "정상" =>
                $"{y}년 {semi} 드라이버입니다. 비교적 최근 빌드로 보입니다.",
            "정상(미래 날짜·참고)" =>
                "날짜가 현재보다 늦게 표시됩니다. OS·제조사 표기 오류일 수 있으니 참고만 하세요.",
            "보통" =>
                $"{y}년 {semi} 드라이버입니다. 특별한 문제가 없다면 유지해도 됩니다.",
            "업데이트 확인 권장" =>
                $"{y}년 {semi} 드라이버입니다. 여유가 될 때 제조사 사이트에서 패치를 확인해 보세요.",
            "오래된 드라이버일 수 있음" =>
                "배포일이 오래되었을 수 있습니다. 게임 문제가 있으면 제조사 드라이버를 확인해 보세요.",
            _ =>
                "로컬 날짜만 참고했습니다. 불안하면 장치 관리자나 제조사 페이지를 확인하세요."
        };
    }
}
