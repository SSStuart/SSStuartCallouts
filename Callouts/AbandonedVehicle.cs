using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using Rage;
using Rage.Native;
using System.Collections.Generic;

namespace SSStuartCallouts.Callouts
{

    [CalloutInterfaceAPI.CalloutInterface("Abandoned Vehicle", CalloutProbability.Low, "Reports of an abandoned vehicle on the roadway", "Code 3")]
    public class AbandonedVehicle : Callout
    {
        public static string pluginName = Main.pluginName;

        private Vector3 SpawnPoint;
        private LHandle Pursuit;
        private Blip AbandonedCarBlip;
        private Blip CarDeliveryBlip;
        private GameFiber CarDeliveryFiber;
        private Ped Thief;
        private Ped CarDeliveryPed;
        private Vehicle AbandonedCar;
        private Vehicle PlayerVehicle;
        private Vehicle ReplacementVehicle;
        private bool EventCreated;
        private bool PlayerVehicleSet;
        private bool ThiefInAction;
        private bool PursuitCreated;

        public override bool OnBeforeCalloutDisplayed()
        {
            SpawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around2D(400f));
            ShowCalloutAreaBlipBeforeAccepting(SpawnPoint, 50f);
            AddMinimumDistanceCheck(150f, SpawnPoint);
            CalloutMessage = "Abandoned Vehicle";
            CalloutPosition = SpawnPoint;
            Functions.PlayScannerAudioUsingPosition("CITIZENS_REPORT_04 CRIME_STOLEN_VEHICLE_SPOTTED IN_OR_ON_POSITION", SpawnPoint);

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

            AbandonedCar = new Vehicle(vehicleList[MathHelper.GetRandomInteger(vehicleList.Count)], SpawnPoint.Around2D(3f, 8f), MathHelper.GetRandomInteger(-180, 180))
            {
                IsPersistent = true
            };

            AbandonedCarBlip = new Blip(AbandonedCar)
            {
                Color = Main.calloutWaypointColor,
                IsRouteEnabled = true,
                Name = "Abandoned Vehicle"
            };

            EventCreated = false;
            PlayerVehicleSet = false;
            ThiefInAction = false;
            PursuitCreated = false;

            return base.OnCalloutAccepted();
        }

        public override void Process()
        {
            base.Process();

            if (!EventCreated && Game.LocalPlayer.Character.DistanceTo(AbandonedCar) < 300f)
            {
                string vehicleName = NativeFunction.Natives.GET_FILENAME_FOR_AUDIO_CONVERSATION<string>(AbandonedCar.Model.Name);
                Game.DisplayNotification($"The abandoned vehicle is a ~o~{vehicleName}");
                CalloutInterfaceAPI.Functions.SendMessage(this, $"The abandoned vehicle (possibly stolen) is a {vehicleName}");

                AbandonedCar.IsEngineOn = true;
                AbandonedCar.IndicatorLightsStatus = VehicleIndicatorLightsStatus.Both;
                if (MathHelper.GetRandomInteger(2) == 1)
                    AbandonedCar.Doors[0].IsFullyOpen = true;

                EventCreated = true;
            }

            if (EventCreated && !PlayerVehicleSet && Game.LocalPlayer.Character.IsInAnyVehicle(false))
            {
                PlayerVehicle = Game.LocalPlayer.Character.LastVehicle;
                PlayerVehicleSet = true;
            }

            if (PlayerVehicleSet && !ThiefInAction && (Game.LocalPlayer.Character.DistanceTo(PlayerVehicle) > 6f) && !Game.LocalPlayer.Character.IsInVehicle(PlayerVehicle, false))
            {
                AbandonedCarBlip.DisableRoute();
                Game.DisplayNotification("Inspect the vehicle");

                Entity closePed = World.GetClosestEntity(World.GetEntities(GetEntitiesFlags.ConsiderHumanPeds | GetEntitiesFlags.ExcludePlayerPed | GetEntitiesFlags.ExcludePoliceOfficers), PlayerVehicle.Position);

                if (closePed != null)
                {
                    Game.LogTrivial($"[{pluginName}] Using existing ped");
                    Thief = (Ped)closePed;
                    Thief.IsPersistent = true;
                }
                else
                {
                    Game.LogTrivial($"[{pluginName}] Using spawned ped");
                    Thief = new Ped(AbandonedCar.Position.Around(50f))
                    {
                        IsPersistent = true,
                        BlockPermanentEvents = true,
                    };
                }

                CarDeliveryFiber = GameFiber.StartNew(delegate
                {
                    if (Thief.IsInAnyVehicle(false))
                    {
                        if (Thief.CurrentVehicle.Driver == Thief)
                        {
                            Game.LogTrivial($"[{pluginName}] Making thief approach by car");
                            Thief.Tasks.DriveToPosition(PlayerVehicle.GetOffsetPositionFront(-10f), 50f, VehicleDrivingFlags.Normal).WaitForCompletion(1200000);
                        }
                        else
                            Thief.Tasks.LeaveVehicle(LeaveVehicleFlags.None).WaitForCompletion(5000);
                    }

                    Game.LogTrivial($"[{pluginName}] Thief approching player car on foot");
                    Thief.Tasks.FollowNavigationMeshToPosition(PlayerVehicle.GetOffsetPositionRight(-2f), PlayerVehicle.Heading, 1f).WaitForCompletion(60000);
                    Thief.Tasks.EnterVehicle(PlayerVehicle, -1, 3f).WaitForCompletion(5000);
                    Game.LogTrivial($"[{pluginName}] Assinging task to drive away");
                    Thief.Tasks.CruiseWithVehicle(PlayerVehicle, 100f, VehicleDrivingFlags.Emergency);

                    GameFiber.Sleep(5000);

                    if (!Thief.IsInVehicle(PlayerVehicle, true))
                    {
                        End();
                        return;
                    }

                    if (AbandonedCarBlip.Exists())
                        AbandonedCarBlip.Delete();

                    Pursuit = Functions.CreatePursuit();
                    Functions.AddPedToPursuit(Pursuit, Thief);
                    Functions.RequestBackup(Thief.Position, LSPD_First_Response.EBackupResponseType.Pursuit, LSPD_First_Response.EBackupUnitType.AirUnit);
                    Functions.SetPursuitIsActiveForPlayer(Pursuit, true);

                    GameFiber.Wait(1500);
                    CalloutInterfaceAPI.Functions.SendMessage(this, "The unit's vehicle has been stolen, pursuit initiated.");
                    Game.DisplayNotification("dia_police", "dia_police", "Dispatch", "", "Sending a new replacement vehicle");
                    Functions.PlayScannerAudioUsingPosition("WE_HAVE CRIME_GRAND_THEFT_AUTO OUTRO_03 ASSISTANCE_REQUIRED IN_OR_ON_POSITION", Thief.Position);

                    ReplacementVehicle = new Vehicle("police", World.GetNextPositionOnStreet(Game.LocalPlayer.Character.GetOffsetPositionFront(-100)), Game.LocalPlayer.Character.Heading)
                    {
                        IsSirenOn = true,
                        IsPersistent = true
                    };

                    CarDeliveryPed = new Ped("csb_agent", ReplacementVehicle.GetOffsetPositionRight(2f), ReplacementVehicle.Heading)
                    {
                        BlockPermanentEvents = true,
                        IsPersistent = true
                    };
                    CarDeliveryPed.ResetVariation();

                    CarDeliveryBlip = new Blip(ReplacementVehicle)
                    {
                        Sprite = BlipSprite.PolicePatrol,
                        Color = System.Drawing.Color.LightSkyBlue
                    };

                    CarDeliveryPed.WarpIntoVehicle(ReplacementVehicle, -1);
                    Game.LogTrivial($"[{pluginName}] Task : Drive to player");
                    CarDeliveryPed.Tasks.DriveToPosition(Game.LocalPlayer.Character.Position, 100f, VehicleDrivingFlags.Emergency, 20f);
                    while (ReplacementVehicle.DistanceTo(Game.LocalPlayer.Character) > 20f)
                    {
                        Game.LogTrivial($"[{pluginName}] Distance to player > 20m");
                        Game.LogTrivial($"[{pluginName}] Task : Drive to player");
                        CarDeliveryPed.Tasks.DriveToPosition(Game.LocalPlayer.Character.Position, 80f, VehicleDrivingFlags.Emergency, 20f);
                        GameFiber.Wait(2000);
                    }

                    while (ReplacementVehicle.Speed > 1f)
                        GameFiber.Wait(500);
                    Game.LogTrivial($"[{pluginName}] Speed < 1m/s   Task : Clear");
                    CarDeliveryPed.Tasks.Clear();
                    if (!Game.LocalPlayer.Character.IsInAnyVehicle(false))
                    {
                        Game.DisplaySubtitle("Hey, here's a replacement car!", 5000);
                        ReplacementVehicle.IsSirenOn = false;
                        CarDeliveryPed.Tasks.LeaveVehicle(LeaveVehicleFlags.None).WaitForCompletion(2000);
                        CarDeliveryPed.Tasks.Wander();
                    }

                    CarDeliveryBlip.Delete();

                    PursuitCreated = true;
                });

                ThiefInAction = true;
            }

            else if (PursuitCreated && !Functions.IsPursuitStillRunning(Pursuit) || (ThiefInAction && !Thief.IsAlive))
            {
                End();
            }
        }

        public override void End()
        {
            base.End();

            if (Pursuit != null && Functions.IsPursuitStillRunning(Pursuit))
                Functions.ForceEndPursuit(Pursuit);

            if (CarDeliveryFiber != null && CarDeliveryFiber.IsAlive)
                CarDeliveryFiber.Abort();

            if (AbandonedCarBlip.Exists()) AbandonedCarBlip.Delete();
            if (AbandonedCar.Exists()) AbandonedCar.Dismiss();
            if (Thief.Exists()) Thief.Dismiss();
            if (CarDeliveryBlip.Exists()) CarDeliveryBlip.Delete();
            if (CarDeliveryPed.Exists()) CarDeliveryPed.Dismiss();
            if (ReplacementVehicle.Exists()) ReplacementVehicle.Dismiss();
            if (PlayerVehicle.Exists()) PlayerVehicle.Dismiss();

            Game.DisplayNotification("[CALLOUT 'ABANDONED VEHICLE' ENDED]");
            Game.LogTrivial($"[{pluginName}] 'Abandoned Vehicle' callout has ended.");
        }
    }
}
