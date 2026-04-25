namespace EROptimizer.Core;

public static class ErConstants
{
    public const string SteamAppId = "1049590";
    public const string GameExePrimary = "EternalReturn.exe";
    public const string GameDataFolder = "EternalReturn_Data";
    public const string BootConfigFileName = "boot.config";

    public const string HighPerformancePowerSchemeGuid = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c";

    public const string GpuPreferenceValue = "2;";

    public const string BootConfigPackageBlock = """
gfx-enable-gfx-jobs=1
gfx-enable-native-gfx-jobs=1
max-chunks-per-shader=8
wait-for-native-debugger=0
vr-enabled=0
hdr-display-enabled=1
gc-max-time-slice=10
androidStartInFullscreen=1
androidRenderOutsideSafeArea=1
adaptive-performance-samsung-boost-launch=1
compute-skinning=1
memorysetup-bucket-allocator-granularity=16
memorysetup-bucket-allocator-bucket-count=8
memorysetup-bucket-allocator-block-size=4194304
memorysetup-bucket-allocator-block-count=1
memorysetup-main-allocator-block-size=25165824
memorysetup-thread-allocator-block-size=25165824
memorysetup-gfx-main-allocator-block-size=25165824
memorysetup-gfx-thread-allocator-block-size=25165824
memorysetup-cache-allocator-block-size=4194304
memorysetup-typetree-allocator-block-size=2097152
memorysetup-profiler-bucket-allocator-granularity=16
memorysetup-profiler-bucket-allocator-bucket-count=8
memorysetup-profiler-bucket-allocator-block-size=4194304
memorysetup-profiler-bucket-allocator-block-count=1
memorysetup-profiler-allocator-block-size=16777216
memorysetup-profiler-editor-allocator-block-size=1048576
memorysetup-job-temp-allocator-block-size=50331648
memorysetup-job-temp-allocator-block-size-background=1048576
memorysetup-job-temp-allocator-reduction-small-platforms=262144
memorysetup-allocator-temp-initial-block-size-main=262144
memorysetup-allocator-temp-initial-block-size-worker=262144
memorysetup-temp-allocator-size-main=12582912
memorysetup-temp-allocator-size-preload-manager=524288
memorysetup-temp-allocator-size-background-worker=32768
memorysetup-temp-allocator-size-job-worker=262144
memorysetup-temp-allocator-size-nav-mesh-worker=65536
memorysetup-temp-allocator-size-audio-worker=65536
memorysetup-temp-allocator-size-cloud-worker=32768
memorysetup-temp-allocator-size-gfx=393216
gfx-enable-async-upload=1
gfx-forward-plus-enable=1
gfx-nvx-quality=1
gfx-nvx-gpu-branch-culling=1
""";
}
