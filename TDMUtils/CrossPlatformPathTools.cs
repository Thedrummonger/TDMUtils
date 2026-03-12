using System.Runtime.InteropServices;

namespace TDMUtils
{
    public static class CrossPlatformPathTools
    {
        public static string? FindExistingAppRoot(string appFolderName) => FindAllExistingAppRoots(appFolderName).FirstOrDefault();

        public static string[] FindAllExistingAppRoots(string appFolderName)
        {
            if (string.IsNullOrWhiteSpace(appFolderName))
                throw new ArgumentException("App folder name cannot be null or whitespace.", nameof(appFolderName));

            var results = new List<string>();

            foreach (var basePath in GetAllValidPaths())
            {
                var full = Path.Combine(basePath, appFolderName);
                if (Directory.Exists(full))
                    results.Add(full);
            }

            return [.. results];
        }
        public static IEnumerable<string> GetAllValidPaths()
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(AppDataPathInfo.WineLinuxConfigFromUserPath) && seen.Add(AppDataPathInfo.WineLinuxConfigFromUserPath!))
                yield return AppDataPathInfo.WineLinuxConfigFromUserPath!;

            if (!string.IsNullOrWhiteSpace(AppDataPathInfo.WineLinuxConfigFromHomePath) && seen.Add(AppDataPathInfo.WineLinuxConfigFromHomePath!))
                yield return AppDataPathInfo.WineLinuxConfigFromHomePath!;

            if (!string.IsNullOrWhiteSpace(AppDataPathInfo.NativeAppDataPath) && seen.Add(AppDataPathInfo.NativeAppDataPath!))
                yield return AppDataPathInfo.NativeAppDataPath!;
        }
    }

    public static class AppDataPathInfo
    {
        public static bool IsWindows { get; } = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        public static bool IsLinux { get; } = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        public static bool IsMacOS { get; } = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        public static string EnvironmentUserName => Environment.UserName ?? string.Empty;
        public static bool IsRunningUnderProton => IsWindows && string.Equals(EnvironmentUserName, "steamuser", StringComparison.OrdinalIgnoreCase);
        public static string? HomeEnvironmentVariable => Environment.GetEnvironmentVariable("HOME");

        public static string? NativeAppDataPath => GetNativeAppDataPath();

        public static string? WineLinuxConfigFromUserPath => GetWineLinuxConfigFromUserPath();
        public static string? WineLinuxConfigFromHomePath => GetWineLinuxConfigFromHomePath();

        public static string? RecommendedBasePath => IsWindows && WineLinuxConfigPath != null ? WineLinuxConfigPath : NativeAppDataPath;

        public static string? WineLinuxConfigPath => WineLinuxConfigFromUserPath ?? WineLinuxConfigFromHomePath;

        private static string? GetNativeAppDataPath()
        {
            var AppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Directory.Exists(AppDataPath) ? AppDataPath : null;
        }

        private static string? GetWineLinuxConfigFromUserPath()
        {
            if (!IsWindows || string.IsNullOrWhiteSpace(EnvironmentUserName)) return null;
            string candidate = Path.Combine(@"Z:\home", EnvironmentUserName, ".config");
            if (!Directory.Exists(candidate)) return null;
            return candidate;
        }

        private static string? GetWineLinuxConfigFromHomePath()
        {
            if (!IsWindows || string.IsNullOrWhiteSpace(HomeEnvironmentVariable)) return null;
            string normalizedHome = HomeEnvironmentVariable!.Replace('/', '\\').TrimStart('\\');
            string candidate = Path.Combine(@"Z:\", normalizedHome, ".config");
            if (!Directory.Exists(candidate)) return null;
            return candidate;
        }
    }
}
