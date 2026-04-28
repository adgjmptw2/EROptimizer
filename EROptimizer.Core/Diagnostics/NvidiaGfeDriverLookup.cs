using System.Net;
using System.Text;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EROptimizer.Core.Diagnostics;

public sealed class NvidiaGfeLatestInfo
{
    public string? Version { get; init; }
    public string? ReleaseDate { get; init; }
}

public static class NvidiaGfeDriverLookup
{
    private const string GfeUserAgent = "NvBackend/36.0.0.0";
    private static readonly string GfeBase =
        "https://gfwsl.geforce.com/nvidia_web_services/" +
        "controller.gfeclientcontent.NG.php/" +
        "com.nvidia.services.GFEClientContent_NG.getDispDrvrByDevid";

    private static readonly string[] FallbackDesktopSample = ["1B80_10DE_119E_10DE"];
    private static readonly string[] FallbackNotebookSample = ["1BE0_10DE"];

    /// <param name="nvidiaAdapter">첫 NVIDIA 어댑터(PNPDeviceID로 실제 PCI 문자열 구성). null이면 샘플 ID만 사용.</param>
    public static NvidiaGfeLatestInfo? TryGetLatestGeForceDriverInfo(DisplayAdapterInfo? nvidiaAdapter)
    {
        try
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        }
        catch
        {
            /* */
        }

        var notebook = ChassisHelper.IsNotebookChassis();
        var ids = ResolveDeviceIds(nvidiaAdapter, notebook);
        return TryGetInternal(ids, notebook, false, 1042) ??
               TryGetInternal(ids, notebook, true, 1042) ??
               TryGetInternal(ids, !notebook, false, 1042) ??
               TryGetInternal(ids, !notebook, true, 1042);
    }

    private static string[] ResolveDeviceIds(DisplayAdapterInfo? adapter, bool notebookChassis)
    {
        if (adapter != null &&
            NvidiaPnpDeviceId.TryBuildGfeDeviceId(adapter.PnpDeviceId, out var id))
            return new[] { id };
        return notebookChassis ? FallbackNotebookSample : FallbackDesktopSample;
    }

    private static NvidiaGfeLatestInfo? TryGetInternal(string[] dIDa, bool notebook, bool dch, int language, bool x64 = true)
    {
        var build = GetWindowsBuild();
        if (string.IsNullOrEmpty(build)) build = "19045";
        var queryObj = new
        {
            dIDa,
            osC = "10.0",
            osB = build,
            is6 = x64 ? "1" : "0",
            lg = language.ToString(),
            iLp = notebook ? "1" : "0",
            prvMd = "0",
            gcV = "3.18.0.94",
            gIsB = "0",
            dch = dch ? "1" : "0",
            upCRD = "0",
            isCRD = "0"
        };
        var json = JsonConvert.SerializeObject(queryObj, new JsonSerializerSettings
        {
            StringEscapeHandling = StringEscapeHandling.Default,
            Formatting = Formatting.None
        });
        var url = GfeBase + "/" + Uri.EscapeDataString(json);
        return HttpGetDriverInfo(url);
    }

    private static string? GetWindowsBuild()
    {
        try
        {
            using var k = Registry.LocalMachine
                .OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", false);
            return k?.GetValue("CurrentBuildNumber")?.ToString();
        }
        catch
        {
            return null;
        }
    }

    private static NvidiaGfeLatestInfo? HttpGetDriverInfo(string url, int timeoutMs = 10_000)
    {
        try
        {
            var req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "GET";
            req.UserAgent = GfeUserAgent;
            req.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            req.Timeout = timeoutMs;
            using var resp = (HttpWebResponse)req.GetResponse();
            using var sr = new StreamReader(resp.GetResponseStream()!, Encoding.UTF8);
            var body = sr.ReadToEnd();
            if (string.IsNullOrWhiteSpace(body)) return null;
            var jo = JObject.Parse(body);
            var da = jo["DriverAttributes"] as JObject;
            if (da == null) return null;
            var ver = da["Version"]?.ToString();
            if (string.IsNullOrEmpty(ver))
                return null;
            var rd = da["ReleaseDate"]?.ToString()
                     ?? da["ReleaseDateTime"]?.ToString()
                     ?? da["DriverReleaseDate"]?.ToString();
            return new NvidiaGfeLatestInfo { Version = ver, ReleaseDate = NormalizeGfeDate(rd) };
        }
        catch
        {
            return null;
        }
    }

    private static string? NormalizeGfeDate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var s = raw!.Trim();
        if (s.Length >= 8 && char.IsDigit(s[0]))
            return s.Length >= 10 ? s[..10].Replace('/', '-') : s;
        return s;
    }
}
