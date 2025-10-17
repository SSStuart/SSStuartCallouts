using System;
using Rage;
using LSPD_First_Response.Mod.API;
using System.Reflection;
using System.Drawing;

namespace SSStuartCallouts
{
    public class Main : Plugin
    {
        public static string pluginName = "SSStuart Callouts";
        public static string pluginVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();

        public static Color calloutWaypointColor = Color.Orange;

        //Initialization of the plugin.
        public override void Initialize()
        {
            Functions.OnOnDutyStateChanged += OnOnDutyStateChangedHandler;

            Game.LogTrivial($"{pluginName} Plugin v{pluginVersion} has been initialised.");
            Game.LogTrivial($"Go on duty to fully load {pluginName}.");

            UpdateChecker.CheckForUpdates();

            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(LSPDFRResolveEventHandler);
        }
        
        public override void Finally()
        {
            Game.LogTrivial($"{pluginName} has been cleaned up.");
        }
        
        private static void OnOnDutyStateChangedHandler(bool OnDuty)
        {
            if (OnDuty)
            {
                RegisterCallouts();
                Game.DisplayNotification("commonmenu", "mp_hostcrown", pluginName, $"V {pluginVersion}", "~g~Loaded successfully !");
            }
        }
        
        private static void RegisterCallouts()
        {
            // Functions.RegisterCallout(typeof(Callouts.TestCallout));
            Functions.RegisterCallout(typeof(Callouts.CarCrash));
            Functions.RegisterCallout(typeof(Callouts.TrainDerailment));
            Functions.RegisterCallout(typeof(Callouts.AbandonedVehicle));
        }

        public static Assembly LSPDFRResolveEventHandler(object sender, ResolveEventArgs args)
        {
            foreach (Assembly assembly in Functions.GetAllUserPlugins())
            {
                if (args.Name.ToLower().Contains(assembly.GetName().Name.ToLower()))
                {
                    return assembly;
                }
            }
            return null;
        }

        public static bool IsLSPDFRPluginRunning(string Plugin, Version minversion = null)
        {
            foreach (Assembly assembly in Functions.GetAllUserPlugins())
            {
                AssemblyName assemblyName = assembly.GetName();
                if (assemblyName.Name.ToLower() == Plugin.ToLower())
                {
                    if (minversion == null || assemblyName.Version.CompareTo(minversion) >= 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}