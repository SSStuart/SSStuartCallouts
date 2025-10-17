using System;
using System.Collections.Generic;
using Rage;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;

namespace SSStuartCallouts.Callouts
{

    [CalloutInterfaceAPI.CalloutInterface("Car Crash", CalloutProbability.Medium, "Vehicle accident reported", "Code 3")]
    public class CarCrash: Callout
    {
        public static string pluginName = Main.pluginName;
        public static string pluginVersion = Main.pluginVersion;

        private Ped Driver;
        private Vehicle CrashedVehicle;
        private Blip EventBlip;
        private Blip CrashedVehicleBlip;
        private Blip DriverBlip;
        private Vector3 SpawnPoint;
        private bool DriverMarked;
        private bool EventCreated;
        private bool EndCalloutDisplayed;

        public int RandomNumber(int min, int max)
        {
            int random = new Random().Next(min, max);
            return random;
        }


        public override bool OnBeforeCalloutDisplayed()
        {
            SpawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around(800f));
            ShowCalloutAreaBlipBeforeAccepting(SpawnPoint, 30f);
            AddMinimumDistanceCheck(300f, SpawnPoint);
            CalloutMessage = "Car Crash";
            CalloutPosition = SpawnPoint;
            Functions.PlayScannerAudioUsingPosition("WE_HAVE CRIME_MOTOR_VEHICLE_ACCIDENT_01 IN_OR_ON_POSITION", SpawnPoint);

            return base.OnBeforeCalloutDisplayed();
        }

        public override bool OnCalloutAccepted()
        {
            List<string> vehicleList = new List<string>
            {
                "speedo",
                "scrap",
                "felon",
                "jackal",
                "sentinel",
                "granger",
                "bullet",
                "feltzer2",
                "coach",
                "stratum",
                "premier",
                "rebel",
                "phoenix",
                "issi2",
                "benson"
            };

            CrashedVehicle = new Vehicle(vehicleList[RandomNumber(0, vehicleList.Count)], SpawnPoint.Around(4f, 8f), RandomNumber(-180, 180));
            CrashedVehicle.IsPersistent = true;

            if (CrashedVehicle.Model.IsBus)
                Driver = new Ped("s_m_m_gentransport", SpawnPoint, 0f);
            else if (CrashedVehicle.Model.IsBigVehicle)
                Driver = new Ped("s_m_y_dockwork_01", SpawnPoint, 0f);
            else
                Driver = new Ped(CrashedVehicle.GetOffsetPositionRight(5f));
            Driver.IsPersistent = true;
            Driver.BlockPermanentEvents = true;
            Driver.WarpIntoVehicle(CrashedVehicle, -1);
            Driver.Health = RandomNumber(100, 200);

            EventBlip = new Blip(SpawnPoint);
            EventBlip.Color = Main.calloutWaypointColor;
            EventBlip.IsRouteEnabled = true;
            EventBlip.Name = "Car Crash";

            EventCreated = false;
            DriverMarked = false;
            EndCalloutDisplayed = false;

            return base.OnCalloutAccepted();
        }

        public override void Process()
        {
            base.Process();

            if (!EventCreated && Game.LocalPlayer.Character.DistanceTo(CrashedVehicle) < 300f)
            {
                Game.DisplayNotification("The involved vehicle is a ~o~" + CrashedVehicle.Model.Name);
                CalloutInterfaceAPI.Functions.SendMessage(this, "The involved vehicle is a " + CrashedVehicle.Model.Name);

                CrashedVehicle.EngineHealth = RandomNumber(0, 100);
                CrashedVehicle.IsDriveable = false;
                CrashedVehicle.IndicatorLightsStatus = VehicleIndicatorLightsStatus.Both;
                if(RandomNumber(0,2) == 1)
                    CrashedVehicle.Wheels[RandomNumber(0,2)].BurstTire();

                if (!CrashedVehicle.IsOnScreen)
                    CrashedVehicle.Velocity = new Vector3(20, 30, 0);

                if (RandomNumber(0,3) == 1)
                    CrashedVehicle.Doors[0].BreakOff();
                if (RandomNumber(0, 3) == 1)
                    CrashedVehicle.PunctureFuelTank();

                if (Driver.IsAlive)
                {
                    Game.LogTrivial($"[{pluginName}] Driver is alive");
                    Driver.Tasks.LeaveVehicle(CrashedVehicle, LeaveVehicleFlags.LeaveDoorOpen).WaitForCompletion();
                    EventBlip.Delete();
                    CrashedVehicleBlip = CrashedVehicle.AttachBlip();
                    CrashedVehicleBlip.RouteColor = Main.calloutWaypointColor;
                    CrashedVehicleBlip.Sprite = BlipSprite.VehicleDeathmatch;
                    CrashedVehicleBlip.Name = "Crashed Vehicle";
                    CrashedVehicleBlip.IsRouteEnabled = true;
                    CrashedVehicleBlip.Order = 1;
                    if (CrashedVehicle.IsOnFire)
                    {
                        Game.LogTrivial($"[{pluginName}] Vehicle on fire");
                        if (RandomNumber(0, 2) == 1)
                        {
                            Game.LogTrivial($"[{pluginName}] Driver set on fire");
                            Driver.IsOnFire = true;
                        }
                        else
                        {
                            Game.LogTrivial($"[{pluginName}] Driver fleeing");
                            Driver.Tasks.ReactAndFlee(Game.LocalPlayer.Character).WaitForCompletion(10000);
                        }
                    } else
                    {
                        if (RandomNumber(0, 2) == 1)
                        {
                            Game.LogTrivial($"[{pluginName}] Driver ragdolling");
                            Driver.IsRagdoll = true;
                        }
                        else
                        {
                            Game.LogTrivial($"[{pluginName}] Driver walking away");
                            Driver.Tasks.Wander().WaitForCompletion(5000);
                        }
                    }
                    Driver.Tasks.Clear();
                }

                EventCreated = true;
            }

            if (EventCreated && !DriverMarked && (Game.LocalPlayer.Character.DistanceTo(CrashedVehicle) < 20f || Game.LocalPlayer.Character.DistanceTo(Driver) < 20f))
            {
                Game.DisplayHelp("Inspect the driver");

                DriverBlip = Driver.AttachBlip();
                DriverBlip.Order = 2;
                DriverBlip.Scale = 0.8f;
                DriverBlip.Name = "Driver";
                DriverBlip.Color = System.Drawing.Color.LightSkyBlue;
                DriverBlip.IsRouteEnabled = true;

                DriverMarked = true;
            }

            if (Game.LocalPlayer.Character.DistanceTo(Driver) < 3f)
            {
                if (DriverBlip.Exists())
                    DriverBlip.Delete();
            }
            if (EventCreated && DriverMarked && Driver.IsRagdoll && Game.LocalPlayer.Character.DistanceTo(Driver) < 3f)
            {
                Game.DisplaySubtitle("The driver seems to be ~y~unconscious");
            }


            if (EventCreated && !EndCalloutDisplayed && Driver.IsDead)
            {
                Game.DisplayNotification("The driver has died.");
                Game.DisplayHelp("Press ~b~End~w~ to end the callout.");
                EndCalloutDisplayed = true;
            }
            else if (EventCreated && (Game.LocalPlayer.Character.DistanceTo(Driver) > 300f || !Driver.Exists() || Game.LocalPlayer.Character.DistanceTo(CrashedVehicle) > 300f))
            {
                End();
            }
            if (Game.IsKeyDown(System.Windows.Forms.Keys.End))
                End();
        }

        public override void End()
        {
            base.End();

            if (EventBlip.Exists()) EventBlip.Delete();
            if (DriverBlip.Exists()) DriverBlip.Delete();
            if (Driver.Exists())
            {
                if (Driver.Tasks != null) Driver.Tasks.Clear();
                Driver.Dismiss();
            }
            if (CrashedVehicleBlip.Exists()) CrashedVehicleBlip.Delete();
            if (CrashedVehicle.Exists()) CrashedVehicle.Dismiss();

            Game.DisplayNotification("[CALLOUT 'CAR CRASH' ENDED]");
            Game.LogTrivial($"[{pluginName}] 'Car crash' callout has ended.");
        }
    }
}
