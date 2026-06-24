using Rage;
using System;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace SSStuartCallouts
{
    public static class UpdateChecker
    {
        private static readonly string pluginKey = "SSStuartCallouts";
        private static readonly InitializationFile updateSettings = new InitializationFile("Plugins/SSStuart_UpdateChecker.ini");

        private static readonly string url = "https://ssstuart.net/api/GTAModVersion/SSStuart%20Callouts";
        private static readonly HttpClient httpClient = new HttpClient();
        private static Version lastVersion = null;
        private static readonly Version currentVersion = new Version(Main.pluginVersion);
        private static string updateAvailable = "";

        public static void CheckForUpdates()
        {
            if (!CheckUpdateSettings())
                return;

            System.Threading.Tasks.Task.Run(async () =>
            {
                updateAvailable = await CheckUpdate();
            });

            GameFiber.StartNew(updateNotification);

            void updateNotification()
            {
                do
                {
                    GameFiber.Yield();

                    if (updateAvailable == "yes")
                    {
                        DisplayUpdateNotification();
                    }
                } while (updateAvailable == "");
            }
        }

        private static bool CheckUpdateSettings()
        {
            // Dynamically create settings (+ hack for comments)
            if (!updateSettings.DoesKeyExist("General", "# Number of days between update checks. 0"))
                updateSettings.Write("General", "# Number of days between update checks. 0", " disable update checks globally.");
            if (!updateSettings.DoesKeyExist("General", "UpdateChecksFrequency"))
                updateSettings.Write("General", "UpdateChecksFrequency", 1);

            if (!updateSettings.DoesKeyExist("IndividualPlugins", "# Whether to check for updates for each plugins. True"))
                updateSettings.Write("IndividualPlugins", "# Whether to check for updates for each plugins. True", " enable update checks according to the configured frequency.");
            if (!updateSettings.DoesKeyExist("IndividualPlugins", pluginKey))
                updateSettings.Write("IndividualPlugins", pluginKey, true);

            if (!updateSettings.DoesKeyExist("Datas", "# The following values should not be modified ("))
                updateSettings.Write("Datas", "# The following values should not be modified (", "");
            if (!updateSettings.DoesKeyExist("Datas", $"{pluginKey}_lastUpdate"))
                updateSettings.Write("Datas", $"{pluginKey}_lastUpdate", DateTime.Now.AddDays(-1).ToString("O"));


            bool shouldCheckForUpdate = updateSettings.ReadBoolean("IndividualPlugins", pluginKey);
            int checkFrequency = updateSettings.ReadInt16("General", "UpdateChecksFrequency");

            // Check if updates are disabled (globally or for this plugin)
            if (!shouldCheckForUpdate || checkFrequency == 0)
            {
                Game.LogTrivial($"[{Main.pluginName}] Update check disabled");
                return false;
            }

            // Check if the last update check was made more than one "interval" ago
            DateTime lastCheck = DateTime.Parse(updateSettings.ReadString("Datas", $"{pluginKey}_lastUpdate"));
            if (lastCheck.AddDays(checkFrequency) < DateTime.Now)
            {
                updateSettings.Write("Datas", $"{pluginKey}_lastUpdate", DateTime.Now.ToString("O"));
                return true;
            }
            else
            {
                Game.LogTrivial($"[{Main.pluginName}] Updates already checked recently");
                return false;
            }
        }

        private static async System.Threading.Tasks.Task<string> CheckUpdate()
        {
            try
            {
                System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

                HttpResponseMessage response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                string responseMessage = await response.Content.ReadAsStringAsync();
                Match m = new Regex("last_version\":\"([\\d.]+)\"").Match(responseMessage);

                if (m.Success)
                {
                    System.Text.RegularExpressions.Group g = m.Groups[1];
                    CaptureCollection cc = g.Captures;
                    Capture c = cc[0];
                    lastVersion = new Version(c.ToString());

                    Game.LogTrivial($"[{Main.pluginName}] Current version: {currentVersion}, Latest version: {lastVersion}");
                    if (currentVersion < lastVersion)
                    {
                        Game.LogTrivial($"[{Main.pluginName}] Update available ! Current version: {currentVersion}, Latest version: {lastVersion}");
                        return "yes";
                    }
                    else if (currentVersion >= lastVersion)
                    {
                        Game.LogTrivial($"[{Main.pluginName}] You are using the latest version ({currentVersion}).");
                        return "no";
                    }

                }
                else
                {
                    Game.LogTrivial($"[{Main.pluginName}] Update check failed: Could not parse version from response : {responseMessage}");
                    return "error";
                }
            }
            catch (Exception ex)
            {
                Game.LogTrivial($"[{Main.pluginName}] Update check failed: {ex.InnerException}");
                return "error";
            }

            return "error";
        }

        private static void DisplayUpdateNotification()
        {
            do
            {
                GameFiber.Yield();
                GameFiber.Sleep(5000);
            } while (Game.IsLoading);
            Game.DisplayNotification("mpturf", "swap", Main.pluginName, $"V {lastVersion}", $"~y~Update available !");
        }
    }
}
