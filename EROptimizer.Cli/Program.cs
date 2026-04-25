using System.Text.Json;
using EROptimizer.Core;
using EROptimizer.Core.Backup;
using EROptimizer.Core.Hardware;
using EROptimizer.Core.Models;
using EROptimizer.Core.Services;

namespace EROptimizer.Cli;

internal static partial class Program
{
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
                        RunFullPackage(workspace, gameExe!, log);
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
                        if (!EnsureGameExe(ref gameExe, discovery))
                            break;
                        RunBootRestoreFromBackup(workspace, gameExe!, log);
                        break;
                    case "5":
                        gameExe = null;
                        MineConsoleUi.PrintBanner();
                        continue;
                    case "6":
                        ResolveManualExe(ref gameExe);
                        break;
                    case "Q":
                        log.Info("종료");
                        return exitCode;
                    default:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("1~6 또는 Q 를 입력하세요.");
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
            WaitExit();
        }

        return exitCode;
    }
}
