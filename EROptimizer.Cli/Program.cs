using EROptimizer.Core;
using EROptimizer.Core.Backup;
using EROptimizer.Core.Hardware;
using EROptimizer.Core.Models;
using EROptimizer.Core.Services;

namespace EROptimizer.Cli;

internal static partial class Program
{
    private static bool s_suppressFinalWaitExit;

    [STAThread]
    private static int Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.Title = "이터널리턴 최적화 | EROptimizer";

        var workspace = AppContext.BaseDirectory;
        var exitCode = 0;
        string? gameExe = null;
        var appSession = DateTime.Now.ToString("yyyyMMdd_HHmmss");

        using var log = ErLog.Create(workspace, appSession, echoInfoToConsole: false);
        log.Info($"작업 루트: {workspace}");

        try
        {
            MineConsoleUi.PrintBanner();

            while (true)
            {
                var discovery = SteamGameLocator.DiscoverDetailed();
                MineConsoleUi.PrintDiscoveryFlow(discovery);
                if (discovery.IsComplete)
                    gameExe = discovery.GameExePath;

                MineConsoleUi.PrintMenuTitle();
                MineConsoleUi.PrintMainMenu();
                MineConsoleUi.PrintThinBar();
                var sel = MineConsoleUi.PromptLine("번호를 선택하세요 : ")?.Trim().ToUpperInvariant();

                switch (sel)
                {
                    case "1":
                        if (!EnsureGameExe(ref gameExe, discovery))
                            break;
                        if (RunFullPackage(workspace, gameExe!, log))
                        {
                            s_suppressFinalWaitExit = true;
                            MineConsoleUi.ShowOptimizationCompleteExitPrompt();
                            return exitCode;
                        }

                        break;
                    case "2":
                        if (!EnsureGameExe(ref gameExe, discovery))
                            break;
                        RunBootConfigOnly(workspace, gameExe!, log);
                        break;
                    case "3":
                        if (!EnsureGameExe(ref gameExe, discovery))
                            break;
                        RunFullRestoreFromBackup(workspace, gameExe!, log);
                        break;
                    case "4":
                        RunMonitorRefreshMenu();
                        break;
                    case "5":
                        RunOverlayProcessMenu();
                        break;
                    case "6":
                        if (!EnsureGameExe(ref gameExe, discovery))
                            break;
                        RunBootRestoreFromBackup(workspace, gameExe!, log);
                        break;
                    case "7":
                        gameExe = null;
                        MineConsoleUi.PrintBanner();
                        continue;
                    case "8":
                        ResolveManualExe(ref gameExe);
                        break;
                    case "Q":
                        log.Info("종료");
                        return exitCode;
                    default:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("1~8 또는 Q 를 입력하세요.");
                        Console.ResetColor();
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            exitCode = 1;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(ex);
            Console.ResetColor();
            log.Error(ex.ToString());
        }
        finally
        {
            if (!s_suppressFinalWaitExit)
                WaitExit();
        }

        return exitCode;
    }
}
