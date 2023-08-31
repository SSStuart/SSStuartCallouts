using LSPD_First_Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rage;
using Rage.Native;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using LSPD_First_Response.Engine.Scripting.Entities;
using SSStuartCallouts;
using SSStuartCallouts.Callouts;
using System.Reflection;

namespace SSStuartCallouts
{

    public class Main : Plugin
    {
        readonly string pluginName = "SSStuart Callouts";

        //Initialization of the plugin.
        public override void Initialize()
        {
            Functions.OnOnDutyStateChanged += OnOnDutyStateChangedHandler;
 
            Game.LogTrivial(pluginName + " Plugin " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() + " has been initialised.");
            Game.LogTrivial("Go on duty to fully load " + pluginName + ".");

            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(LSPDFRResolveEventHandler);
        }
        
        public override void Finally()
        {
            Game.LogTrivial(pluginName + " has been cleaned up.");
        }
        
        private static void OnOnDutyStateChangedHandler(bool OnDuty)
        {
            if (OnDuty)
            {
                RegisterCallouts();
                //Game.DisplayNotification("~y~SSStuart Callouts~w~ has loaded successfully !");
                Game.DisplayNotification("commonmenu", "mp_hostcrown", "SSStuart Callout", "V 0.1", "~g~Loaded successfully !");
            }
        }
        
        private static void RegisterCallouts()
        {
            // Functions.RegisterCallout(typeof(Callouts.TestCallout));
            Functions.RegisterCallout(typeof(Callouts.CarCrash));
            Functions.RegisterCallout(typeof(Callouts.TrainDerailment));
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