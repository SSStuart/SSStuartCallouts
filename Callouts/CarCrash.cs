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

        public override bool OnBeforeCalloutDisplayed()
        {
            SpawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around2D(500f));
            ShowCalloutAreaBlipBeforeAccepting(SpawnPoint, 30f);
            AddMinimumDistanceCheck(200f, SpawnPoint);
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

            CrashedVehicle = new Vehicle(vehicleList[MathHelper.GetRandomInteger(vehicleList.Count)], SpawnPoint.Around2D(4f, 8f), MathHelper.GetRandomInteger(-180, 180))
            {
                IsPersistent = true
            };

            if (CrashedVehicle.Model.IsBus)
                Driver = new Ped("s_m_m_gentransport", SpawnPoint, 0f);
            else if (CrashedVehicle.Model.IsBigVehicle)
                Driver = new Ped("s_m_y_dockwork_01", SpawnPoint, 0f);
            else
                Driver = new Ped(CrashedVehicle.GetOffsetPositionRight(5f));
            Driver.IsPersistent = true;
            Driver.BlockPermanentEvents = true;
            Driver.WarpIntoVehicle(CrashedVehicle, -1);
            Driver.Health = MathHelper.GetRandomInteger(100, 200);

            EventBlip = new Blip(SpawnPoint)
            {
                Color = Main.calloutWaypointColor,
                IsRouteEnabled = true,
                Name = "Car Crash"
            };

            EventCreated = false;
            DriverMarked = false;

            return base.OnCalloutAccepted();
        }

        public override void Process()
        {
            base.Process();

            if (!EventCreated && Game.LocalPlayer.Character.DistanceTo(CrashedVehicle) < 300f)
            {
                string vehicleName = NativeFunction.Natives.GET_FILENAME_FOR_AUDIO_CONVERSATION<string>(CrashedVehicle.Model.Name);
                Game.DisplayNotification($"The involved vehicle is a ~o~{vehicleName}");
                CalloutInterfaceAPI.Functions.SendMessage(this, $"The involved vehicle is a {vehicleName}");

                CrashedVehicle.EngineHealth = MathHelper.GetRandomInteger(100);
                CrashedVehicle.IsDriveable = false;
                CrashedVehicle.IndicatorLightsStatus = VehicleIndicatorLightsStatus.Both;
                if (MathHelper.GetRandomInteger(2) == 1)
                    CrashedVehicle.Wheels[MathHelper.GetRandomInteger(2)].BurstTire();

                if (!CrashedVehicle.IsOnScreen)
                    CrashedVehicle.Velocity = new Vector3(20, 30, 0);

                CrashedVehicle.SetRotationRoll(MathHelper.GetRandomInteger(-180, 180));

                if (MathHelper.GetRandomInteger(3) == 1)
                    CrashedVehicle.Doors[0].BreakOff();
                if (MathHelper.GetRandomInteger(3) == 1)
                    CrashedVehicle.PunctureFuelTank();

                if (Driver.IsAlive)
                {
                    Game.LogTrivial($"[{pluginName}] Driver is alive");
                    Driver.Tasks.LeaveVehicle(CrashedVehicle, LeaveVehicleFlags.LeaveDoorOpen).WaitForCompletion(5000);
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
                        if (MathHelper.GetRandomInteger(2) == 1)
                        {
                            Game.LogTrivial($"[{pluginName}] Driver set on fire");
                            Driver.IsOnFire = true;
                        }
                        else
                        {
                            Game.LogTrivial($"[{pluginName}] Driver fleeing");
                            Driver.Tasks.Flee(CrashedVehicle.Position, 20f, 10000).WaitForCompletion(10000);
                        }
                        }
                    } else
                    {
                        if (MathHelper.GetRandomInteger(2) == 1)
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
                Game.DisplayHelp("Inspect the ~o~driver~w~");

                DriverBlip = Driver.AttachBlip();
                DriverBlip.Order = 2;
                DriverBlip.Scale = 0.75f;
                DriverBlip.Name = "Driver";
                DriverBlip.Color = System.Drawing.Color.Orange;
                DriverBlip.IsRouteEnabled = true;

                DriverMarked = true;
            }

            if (Game.LocalPlayer.Character.DistanceTo(Driver) < 3f)
            {
                if (DriverBlip.Exists())
                    DriverBlip.Delete();

                Game.DisplayHelp("Press ~b~End~w~ to end the callout.");
            }

            if (DriverMarked && (Game.LocalPlayer.Character.DistanceTo(Driver) > 300f || !Driver.Exists() || Game.LocalPlayer.Character.DistanceTo(CrashedVehicle) > 300f))
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
