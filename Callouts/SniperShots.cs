using System;
using Rage;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using System.Collections.Generic;
using Rage.Native;

namespace SSStuartCallouts.Callouts
{

    [CalloutInterfaceAPI.CalloutInterface("Sniper Shots", CalloutProbability.Low, "Sniper shot at a civilian", "Code 3")]
    public class SniperShots: Callout
    {
        public static string pluginName = Main.pluginName;
        public static string pluginVersion = Main.pluginVersion;

        private int calloutVariation;
        private Ped Suspect;
        private Ped Victim;
        private Vehicle VictimVehicle;
        private List<string> vehicleList;
        private Vehicle BackupVehicle;
        private Vehicle Tanker;
        private Ped BackupPed;
        private Vector3 SpawnPoint;
        private Blip WaypointBlip;
        private bool SuspectShooting;
        private bool BackupOnsite;
        private uint speedzone;

        public int RandomNumber(int min, int max)
        {
            int random = new Random().Next(min, max);
            return random;
        }

        public override bool OnBeforeCalloutDisplayed()
        {
            List<Vector3> calloutsVariationsPos = new List<Vector3>()
            {
                new Vector3(-204f, 6216f, 32f),
                new Vector3(244.3871f, -381.6349f, 44.53736f),

            };
            calloutVariation = 0;

            if (Game.LocalPlayer.Character.DistanceTo(calloutsVariationsPos[0]) > Game.LocalPlayer.Character.DistanceTo(calloutsVariationsPos[1]))
                calloutVariation = 1;

            SpawnPoint = calloutsVariationsPos[calloutVariation];
            ShowCalloutAreaBlipBeforeAccepting(SpawnPoint, 30f);
            AddMinimumDistanceCheck(300f, SpawnPoint);
            CalloutMessage = "Shots fired";
            BackupOnsite = RandomNumber(0,2) == 0;

            Functions.PlayScannerAudioUsingPosition("WE_HAVE A CRIME_SNIPER CRIME_GUNFIRE IN_OR_ON_POSITION", SpawnPoint);

            return base.OnBeforeCalloutDisplayed();
        }

        public override bool OnCalloutAccepted()
        {
            if (calloutVariation == 0)
            {
                vehicleList = new List<string>
                {
                    "dilettante",
                    "jackal",
                    "buccaneer",
                    "picador",
                    "sabregt",
                    "rebel",
                    "huntley",
                    "cognoscenti"
                };

                Suspect = new Ped(new Vector3(-193.2576f, 6147.558f, 36.9f));
                Victim = new Ped(new Vector3(-206.4089f, 6225.474f, 31.49064f));
            } else if (calloutVariation == 1)
            {
                vehicleList = new List<string>
                {
                    "jackal",
                    "windsor",
                    "baller5",
                    "cognoscenti",
                    "cognoscenti2",
                    "superd",
                    "taxi"
                };

                Suspect = new Ped(new Vector3(241.7108f, -337.9136f, 60.00283f), 175.7724f);
                List<string> VictimModel = new List<string>
                {
                    "g_m_m_armboss_01",
                    "u_m_m_bankman",
                    "a_f_y_business_01",
                    "a_m_y_business_01",
                    "ig_davenorton",
                    "csb_reporter"
                };
                Victim = new Ped(VictimModel[RandomNumber(0,VictimModel.Count)], new Vector3(240.1729f, -399.2655f, 47.92437f), 0f);
            }

            Suspect.IsPersistent = true;
            Suspect.BlockPermanentEvents = true;
            Suspect.Inventory.GiveNewWeapon(WeaponHash.SniperRifle, 500, true);
            Suspect.RelationshipGroup = "ATTACKERS";
            Suspect.Accuracy = 10;

            Victim.IsPersistent = true;
            Victim.BlockPermanentEvents = true;
            if (calloutVariation == 1)
            {
                Victim.Tasks.TakeCoverAt(new Vector3(240.1729f, -399.2655f, 47.92437f), new Vector3(242.3397f, -336.8919f, 60.00277f), 10000, false);
                VictimVehicle = new Vehicle(vehicleList[RandomNumber(0,vehicleList.Count)], new Vector3(257.9648f, -377.5579f, 43.88403f), 248.254f);
                VictimVehicle.IsPersistent = true;
            }

            if(BackupOnsite)
            {
                if (calloutVariation == 0)
                {
                    BackupVehicle = new Vehicle("sheriff", new Vector3(-137.583f, 6240.756f, 31.18568f), 196);
                    BackupPed = new Ped("s_m_y_sheriff_01", new Vector3(-139.9209f, 6242.334f, 31.16767f), 135);
                    BackupPed.Tasks.PlayAnimation("amb@code_human_police_investigate@idle_b", "idle_d", 1f, AnimationFlags.Loop);

                    Tanker = new Vehicle("tanker", new Vector3(-205.2529f, 6198.685f, 31.23893f), 314.3732f);
                    Tanker.IsPersistent = true;

                    //speedzone = World.AddSpeedZone(new Vector3(-201.0948f, 6181.535f, 31.17964f), 100f, 30f);
                    NativeFunction.Natives.SET_ROADS_IN_AREA(-230, 6167, 0, -94, 6316, 200, false, true);
                }
                else if (calloutVariation == 1)
                {
                    BackupVehicle = new Vehicle("police2", new Vector3(226.8215f, -363.4158f, 43.97262f), 212.704f);
                    BackupPed = new Ped("s_m_y_cop_01", new Vector3(224.7354f, -364.8016f, 44.11553f), 0f);

                    speedzone = World.AddSpeedZone(new Vector3(244.5784f, -364.001f, 44.47871f), 50f, 0f);
                }
                BackupVehicle.IsPersistent = true;
                BackupVehicle.IsSirenOn = true;

                BackupPed.IsPersistent = true;
                BackupPed.BlockPermanentEvents = true;
            }

            WaypointBlip = new Blip(SpawnPoint, 50f);
            WaypointBlip.Color = Main.calloutWaypointColor;
            WaypointBlip.Alpha = 0.3f;
            WaypointBlip.IsRouteEnabled = true;

            SuspectShooting = false;

            return base.OnCalloutAccepted();
        }

        public override void Process()
        {
            base.Process();

            if (Game.LocalPlayer.Character.DistanceTo2D(SpawnPoint) < 100 && !SuspectShooting)
            {
                if (calloutVariation == 0)
                    Suspect.Tasks.FireWeaponAt(Game.LocalPlayer.Character, -1, FiringPattern.BurstFireInCover);
                else if (calloutVariation == 1)
                    Suspect.Tasks.FireWeaponAt(Victim, -1, FiringPattern.BurstFire);
                SuspectShooting = true;

                Victim.Tasks.TakeCoverFrom(Suspect, -1);

                if (BackupOnsite)
                    CalloutInterfaceAPI.Functions.SendMessage(this, "Backup on site");
            }

            if (Game.IsKeyDown(System.Windows.Forms.Keys.End) || !Suspect.Exists() || (Suspect.Exists() && (Suspect.IsDead || Functions.IsPedArrested(Suspect))))
            {
                Game.LogTrivial($"Suspect Exist ? {Suspect.Exists()} | Is dead ? {Suspect.IsDead} | Is arrested ? {Functions.IsPedArrested(Suspect)}");
                End();
            }
        }

        public override void End()
        {
            base.End();

            World.RemoveSpeedZone(speedzone);
            // Native to reenable nodes

            if (Suspect.Exists())
            {
                if (Suspect.Tasks != null) Suspect.Tasks.Clear();
                Suspect.Dismiss();
            }
            if (Victim.Exists())
            {
                //if (Victim.Tasks != null) Victim.Tasks.Clear();
                if (VictimVehicle.Exists() && VictimVehicle.EngineHealth > 400)
                {
                    Game.LogTrivial($"[{pluginName}] victim entering is vehicle");
                    Victim.Tasks.EnterVehicle(VictimVehicle, -1, 10f).WaitForCompletion(15000);
                }
                Victim.Dismiss();
            }
            if (VictimVehicle.Exists()) VictimVehicle.Dismiss();

            if (BackupPed.Exists())
            {
                if (BackupPed.Tasks != null) BackupPed.Tasks.Clear();
                if (BackupPed.IsAlive) BackupPed.Tasks.EnterVehicle(BackupVehicle, -1).WaitForCompletion(10000);
                BackupPed.Dismiss();
            }
            if (BackupVehicle.Exists()) BackupVehicle.Dismiss();
            if (Tanker.Exists()) Tanker.Dismiss();
            if (WaypointBlip.Exists()) WaypointBlip.Delete();

            Game.DisplayNotification("[CALLOUT 'SHOTS FIRED' ENDED]");
            Game.LogTrivial($"[{pluginName}] 'Shots Fired' callout has ended.");
        }
    }
}
