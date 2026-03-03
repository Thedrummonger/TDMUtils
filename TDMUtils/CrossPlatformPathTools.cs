using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TDMUtils
{
    internal static class CrossPlatformPathTools
    {
        public static string GetAppDataRoot(string appFolderName) => Path.Combine(GetAppDataBase(), appFolderName);
        public static bool IsRunningUnderProton() => string.Equals(Environment.UserName, "steamuser", StringComparison.OrdinalIgnoreCase);

        public static string GetAppDataBase()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var winePath = TryGetWineConfigPath();
                if (winePath != null)
                    return winePath;
            }

            return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        }

        private static string? TryGetWineConfigPath()
        {
            var user = Environment.UserName;
            if (!string.IsNullOrEmpty(user))
            {
                var zUserConfig = Path.Combine(@"Z:\home", user, ".config");
                if (Directory.Exists(zUserConfig))
                    return zUserConfig;
            }

            var home = Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrEmpty(home))
            {
                var zHomeConfig = Path.Combine(@"Z:" + home.Replace("/", "\\"), ".config");
                if (Directory.Exists(zHomeConfig))
                    return zHomeConfig;
            }

            return null;
        }
    }
}
