using Rage;
using System;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace SSStuartCallouts
{
    public static class UpdateChecker
    {
        private static readonly string url = "https://ssstuart.net/api/GTAModVersion/SSStuartCallouts";
        private static readonly HttpClient httpClient = new HttpClient();
        private static Version lastVersion = null;
        private static readonly Version currentVersion = new Version(Main.pluginVersion);
        private static string updateAvailable = "";

        public static void CheckForUpdates()
        {
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
                    } else if (currentVersion >= lastVersion)
                    {
                        Game.LogTrivial($"[{Main.pluginName}] You are using the latest version ({currentVersion}).");
                        return "no";
                    }
                    
                } else
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
