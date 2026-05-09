using EROptimizer.Core.Diagnostics;

namespace EROptimizer.Cli;

internal static partial class Program
{
    private static void RunMonitorRefreshMenu()
    {
        MineConsoleUi.WriteDiagnosticProgress("모니터 주사율 확인 중...", 50);
        var d = DisplayDiagnosticsService.GetPrimaryDisplayRefresh();
        Console.WriteLine();
        MineConsoleUi.ClearDiagnosticProgressLine();
        MineConsoleUi.PrintMonitorRefreshCheck(d);
    }

    private static void RunOverlayProcessMenu()
    {
        MineConsoleUi.WriteDiagnosticProgress("오버레이 프로세스 확인 중...", 10);
        var rows = OverlayProcessScanner.Scan(1000);
        Console.WriteLine();
        MineConsoleUi.ClearDiagnosticProgressLine();
        MineConsoleUi.PrintOverlayInspectionReport(rows);
    }
}
