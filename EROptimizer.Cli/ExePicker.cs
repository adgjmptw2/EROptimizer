using System.Windows.Forms;
using System.Threading;

namespace EROptimizer.Cli;

internal static class ExePicker
{
    public static string? PickExeInteractive()
    {
        string? path = null;
        void Show()
        {
            using var dlg = new OpenFileDialog
            {
                Filter = "실행 파일 (*.exe)|*.exe|모든 파일|*.*",
                Title = "Eternal Return 실행 파일 선택",
                CheckFileExists = true
            };
            if (dlg.ShowDialog() == DialogResult.OK)
                path = dlg.FileName;
        }

        var t = new Thread(Show) { IsBackground = false };
        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        t.Join();
        return path;
    }
}
