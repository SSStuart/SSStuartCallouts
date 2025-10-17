using System;
using System.Collections.Generic;
using Rage;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;

namespace SSStuartCallouts.Callouts
{

    [CalloutInterfaceAPI.CalloutInterface("Abandoned Vehicle", CalloutProbability.Low, "Reports of an abandoned vehicle on the roadway", "Code 3")]
    public class AbandonedVehicle: Callout
    {
        public static string pluginName = Main.pluginName;
        public static string pluginVersion = Main.pluginVersion;

        private Ped Driver;
        private Vehicle AbandonedCar;
        private Blip AbandonedCarBlip;
        private Vector3 SpawnPoint;
        private LHandle Pursuit;
        private bool PursuitCreated;
        private bool EventCreated;
        private bool VehicleStolen;
        private Vehicle PlayerVehicle;
        private Vehicle BackupVehicle;
        private Blip BackupBlip;
        private Ped BackupPed;

        public int RandomNumber(int min, int max)
        {
            return new Random().Next(min, max);
        }


        public override bool OnBeforeCalloutDisplayed()
        {
            SpawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around(800f));
            ShowCalloutAreaBlipBeforeAccepting(SpawnPoint, 50f);
            AddMinimumDistanceCheck(200f, SpawnPoint);
            CalloutMessage = "Abandoned Vehicle";
            CalloutPosition = SpawnPoint;
            Functions.PlayScannerAudioUsingPosition("CITIZENS_REPORT_04 IN_OR_ON_POSITION", SpawnPoint);

            return base.OnBeforeCalloutDisplayed();
        }

        public override bool OnCalloutAccepted()
        {
            List<string> vehicleList = new List<string>
            {
                "speedo",
                "sanchez2",
                "faggio",
                "granger",
                "rebel",
                "rebel2",
                "dloader",
                "voodoo2"
            };

            AbandonedCar = new Vehicle(vehicleList[RandomNumber(0, vehicleList.Count)], SpawnPoint.Around(5f, 8f), RandomNumber(-180, 180));
            AbandonedCar.IsPersistent = true;

            Driver = new Ped(AbandonedCar.GetOffsetPositionRight(5f))
            {
                IsPersistent = true,
                BlockPermanentEvents = true,
                IsVisible = false,
                IsCollisionEnabled = false,
                IsPositionFrozen = true
            };

            AbandonedCarBlip = new Blip(AbandonedCar)
            {
                Color = Main.calloutWaypointColor,
                IsRouteEnabled = true,
                Name = "Abandoned Vehicle"
            };

            EventCreated = false;
            PursuitCreated = false;
            VehicleStolen = false;

            return base.OnCalloutAccepted();
        }

        public override void Process()
        {
            base.Process();

            if (!EventCreated && Game.LocalPlayer.Character.DistanceTo(AbandonedCar) < 300f)
            {
                Game.DisplayNotification($"The abandoned vehicle is a ~o~{AbandonedCar.Model.Name}");
                CalloutInterfaceAPI.Functions.SendMessage(this, $"The abandoned vehicle is a {AbandonedCar.Model.Name}");

                AbandonedCar.IsEngineOn = true;
                AbandonedCar.IndicatorLightsStatus = VehicleIndicatorLightsStatus.Both;
                if(RandomNumber(0,2) == 1)
                    AbandonedCar.Doors[0].IsFullyOpen = true;

                EventCreated = true;
            }

            if (EventCreated && !VehicleStolen && Game.LocalPlayer.Character.IsInAnyVehicle(false))
                PlayerVehicle = Game.LocalPlayer.Character.LastVehicle;

            if (EventCreated && !PursuitCreated && !VehicleStolen && (Game.LocalPlayer.Character.DistanceTo(PlayerVehicle) > 10f) && !Game.LocalPlayer.Character.IsInAnyVehicle(false))
            {
                AbandonedCarBlip.DisableRoute();
                Game.DisplayNotification("Inspect the vehicle");
                if (!PlayerVehicle.IsOnScreen && !Game.LocalPlayer.Character.IsInAnyVehicle(false))
                {
                    int counterInvisible = 0;
                    while (counterInvisible < 10)
                    {
                        GameFiber.Wait(100);
                        if (PlayerVehicle.IsOnScreen)
                            break;
                        counterInvisible++;
                    }
                    if (counterInvisible == 10)
                        VehicleStolen = true;

                    if (VehicleStolen)
                    {
                        Driver.IsPositionFrozen = false;
                        Driver.IsCollisionEnabled = true;
                        Driver.Position = PlayerVehicle.GetOffsetPositionRight(-2f);
                        Driver.IsVisible = true;
                        Driver.BlockPermanentEvents = true;

                        Game.LogTrivial("Warping thief into vehicle");
                        Driver.WarpIntoVehicle(PlayerVehicle, -1);
                        Game.LogTrivial("Assinging task to drive away");
                        Driver.Tasks.CruiseWithVehicle(100f);

                        if (AbandonedCarBlip != null && AbandonedCarBlip.Exists())
                            AbandonedCarBlip.Delete();

                        Pursuit = Functions.CreatePursuit();
                        Functions.AddPedToPursuit(Pursuit, Driver);
                        Functions.RequestBackup(World.GetNextPositionOnStreet(Driver.Position.Around(100f)), LSPD_First_Response.EBackupResponseType.Pursuit, LSPD_First_Response.EBackupUnitType.AirUnit);
                        Functions.SetPursuitIsActiveForPlayer(Pursuit, true);

                        GameFiber.Wait(1500);
                        CalloutInterfaceAPI.Functions.SendMessage(this, "The unit's vehicle has been stolen, pursuit initiated.");
                        Game.DisplayNotification("dia_police", "dia_police", "Dispatch", "", "Calling for backup to pick you up");
                        Functions.PlayScannerAudioUsingPosition("WE_HAVE CRIME_GRAND_THEFT_AUTO OUTRO_03 ASSISTANCE_REQUIRED IN_OR_ON_POSITION", Driver.Position);

                        BackupVehicle = new Vehicle("police", World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around(100f)), Game.LocalPlayer.Character.Heading)
                        {
                            IsSirenOn = true,
                            IsPersistent = true
                        };

                        BackupPed = new Ped("s_m_m_chemsec_01", BackupVehicle.GetOffsetPositionRight(2f), BackupVehicle.Heading)
                        {
                            BlockPermanentEvents = true,
                            IsPersistent = true
                        };

                        BackupBlip = new Blip(BackupPed)
                        {
                            Sprite = BlipSprite.PolicePatrol,
                            Color = System.Drawing.Color.LightSkyBlue
                        };

                        BackupPed.WarpIntoVehicle(BackupVehicle, -1);
                        Game.LogTrivial("Task : Drive to player");
                        BackupPed.Tasks.DriveToPosition(Game.LocalPlayer.Character.Position, 100f, VehicleDrivingFlags.Emergency, 20f);
                        while (BackupVehicle.DistanceTo(Game.LocalPlayer.Character) > 20f)
                        {
                            Game.LogTrivial("Distance to player > 20m");
                            Game.LogTrivial("Task : Drive to player");
                            BackupPed.Tasks.DriveToPosition(Game.LocalPlayer.Character.Position, 80f, VehicleDrivingFlags.Emergency, 20f);
                            GameFiber.Wait(2000);
                        }

                        while (BackupVehicle.Speed > 1f)
                            GameFiber.Wait(500);
                        Game.LogTrivial("Speed < 1m/s   Task : Clear");
                        BackupPed.Tasks.Clear();
                        if (!Game.LocalPlayer.Character.IsInAnyVehicle(false))
                        {
                            Game.DisplaySubtitle("Hey, get in the car!", 5000);
                            BackupPed.Tasks.ShuffleToAdjacentSeat();
                            BackupPed.IsPositionFrozen = true;
                            BackupPed.StaysInVehiclesWhenJacked = true;
                            BackupVehicle.IsSirenOn = false;

                            while (!Game.LocalPlayer.Character.IsInAnyVehicle(false))
                                GameFiber.Wait(500);
                        }
                        
                        BackupBlip.Delete();
                        BackupPed.Delete();
                        uint backupTransformationTimeout = Game.GameTime;
                        while (BackupVehicle.Speed < 5f && backupTransformationTimeout + 20000 > Game.GameTime)
                        {
                            GameFiber.Sleep(2000);
                        }
                        BackupPed = new Ped("s_m_y_cop_01", BackupVehicle.GetOffsetPositionFront(-20f), 0);
                        BackupPed.WarpIntoVehicle(BackupVehicle, 0);
                        BackupPed.BlockPermanentEvents = false;
                        Functions.AddCopToPursuit(Pursuit, BackupPed);

                        PursuitCreated = true;
                    }
                }
            }

            else if (PursuitCreated && !Functions.IsPursuitStillRunning(Pursuit) || !Driver.IsAlive)
            {
                End();
            }
        }

        public override void End()
        {
            base.End();

            if (AbandonedCarBlip.Exists()) AbandonedCarBlip.Delete();
            if (AbandonedCar.Exists()) AbandonedCar.Dismiss();
            if (Driver.Exists()) Driver.Dismiss();
            if (Driver.Tasks != null) Driver.Tasks.Clear();
            if (BackupVehicle != null && BackupVehicle.Exists() && Game.LocalPlayer.Character.IsInVehicle(BackupVehicle, false))
            {
                BackupPed.Tasks.PerformDrivingManeuver(VehicleManeuver.Wait).WaitForCompletion();
                GameFiber.Wait(1000);
                Game.LocalPlayer.Character.Tasks.LeaveVehicle(LeaveVehicleFlags.LeaveDoorOpen).WaitForCompletion();
                GameFiber.Wait(1000);
            } else if (BackupVehicle != null && BackupVehicle.Exists() && BackupPed != null && BackupPed.Exists() && !BackupPed.IsInVehicle(BackupVehicle, false))
            {
                BackupPed.Tasks.EnterVehicle(BackupVehicle, -1).WaitForCompletion(10000);
            } else if (BackupPed != null && BackupPed.Exists() && (BackupVehicle == null || !BackupVehicle.Exists()))
                if (BackupPed.Tasks != null) BackupPed.Tasks.Clear();
            if (BackupBlip.Exists()) BackupBlip.Delete();
            if (BackupPed.Exists()) BackupPed.Dismiss();
            if (BackupVehicle.Exists()) BackupVehicle.Dismiss();
            if (PlayerVehicle.Exists()) PlayerVehicle.Dismiss();
            if (Game.LocalPlayer.Character.Tasks != null) Game.LocalPlayer.Character.Tasks.Clear();

            Game.DisplayNotification("[CALLOUT 'ABANDONED VEHICLE' ENDED]");
            Game.LogTrivial($"[{pluginName}] 'Abandoned Vehicle' callout has ended.");
        }
    }
}
