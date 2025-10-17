using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using Rage;
using System;
using System.Collections.Generic;

namespace SSStuartCallouts.Callouts
{

    [CalloutInterfaceAPI.CalloutInterface("Attacked Transport", CalloutProbability.Low, "A transport vehicle is under attack", "Code 3")]
    public class AttackedTransport : Callout
    {
        public static string pluginName = Main.pluginName;
        public static string pluginVersion = Main.pluginVersion;

        private Blip EventBlip;
        private Vector3 SpawnPoint;
        private int CalloutType;
        private Vehicle AttackersVehicle;
        private Ped AttackerOne, AttackerTwo;
        private Vehicle TransportVehicle;
        private Ped TransportDriver, TransportPassenger;
        private LHandle Pursuit;
        private bool EventCreated;
        private bool DriverOutOfTransport;
        private bool TransportVehicleUnlocked;
        private bool PursuitCreated;

        public int RandomNumber(int min, int max)
        {
            int random = new Random().Next(min, max);
            return random;
        }

        public override bool OnBeforeCalloutDisplayed()
        {
            CalloutType = RandomNumber(0, 2);
            SpawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around(800f));
            ShowCalloutAreaBlipBeforeAccepting(SpawnPoint, 30f);
            AddMinimumDistanceCheck(500f, SpawnPoint);
            if (CalloutType == 0)
            {
                CalloutMessage = "Attacked prisoner transport";
                Functions.PlayScannerAudioUsingPosition("WE_HAVE A CRIME_PRIONERS_ESCAPING_TRANSPORT IN_OR_ON_POSITION", SpawnPoint);
                CalloutInterfaceAPI.Functions.SendMessage(this, "Attacked vehicle is a prisoner transport");
            }
            else if (CalloutType == 1)
            {
                CalloutMessage = "Attacked cash transportation";
                Functions.PlayScannerAudioUsingPosition("WE_HAVE AN CRIME_ARMORED_TRUCK_ROBBERY IN_OR_ON_POSITION", SpawnPoint);
                CalloutInterfaceAPI.Functions.SendMessage(this, "Attacked vehicle is an armored transport");
            }

            return base.OnBeforeCalloutDisplayed();
        }

        public override bool OnCalloutAccepted()
        {
            List<string> attackersVehicleList = new List<string>
            {
                "speedo",
                "scrap",
                "felon",
                "jackal",
                "sentinel",
                "granger",
                "bullet",
                "feltzer2",
                "stratum",
                "premier",
                "rebel",
                "phoenix",
                "issi2",
                "benson"
            };

            List<string> attackersPedList = new List<string>
            {
                "ig_car3guy1",
                "g_m_m_chicold_01",
                "u_m_m_edtoh",
                "g_m_y_korean_02",
                "s_m_y_robber_01",
                "g_m_y_strpunk_02"
            };

            AttackersVehicle = new Vehicle(attackersVehicleList[RandomNumber(0, attackersVehicleList.Count)], SpawnPoint.Around(10f), RandomNumber(-180, 180));
            AttackersVehicle.IsPersistent = true;

            AttackerOne = new Ped(attackersPedList[RandomNumber(0, attackersPedList.Count)], AttackersVehicle.GetOffsetPositionRight(3f), 0);
            AttackerOne.IsPersistent = true;
            AttackerOne.BlockPermanentEvents = true;
            AttackerOne.WarpIntoVehicle(AttackersVehicle, -1);
            AttackerOne.Inventory.GiveNewWeapon("weapon_assaultrifle", 500, true);
            AttackerOne.RelationshipGroup = "ATTACKERS";

            AttackerTwo = new Ped(attackersPedList[RandomNumber(0, attackersPedList.Count)], AttackersVehicle.GetOffsetPositionRight(3f), 0);
            AttackerTwo.IsPersistent = true;
            AttackerTwo.BlockPermanentEvents = true;
            AttackerTwo.WarpIntoVehicle(AttackersVehicle, 0);
            AttackerTwo.Inventory.GiveNewWeapon("weapon_heavyshotgun", 500, true);
            AttackerTwo.RelationshipGroup = "ATTACKERS";

            if (CalloutType == 0)
            {
                if (RandomNumber(0, 2) == 0)
                    TransportVehicle = new Vehicle("policet", SpawnPoint, RandomNumber(-180, 180));
                else
                    TransportVehicle = new Vehicle("pbus", SpawnPoint, RandomNumber(-180, 180));
            } else {
                if (RandomNumber(0, 2) == 0)
                    TransportVehicle = new Vehicle("stockade", SpawnPoint, RandomNumber(-180, 180));
                else
                    TransportVehicle = new Vehicle("boxville2", SpawnPoint, RandomNumber(-180, 180));
            }
            TransportVehicle.IsPersistent = true;
            TransportVehicle.IndicatorLightsStatus = VehicleIndicatorLightsStatus.Both;
            TransportVehicle.IsEngineOn = true;
            TransportVehicle.LockStatus = VehicleLockStatus.Locked;
            TransportVehicle.IsInvincible = true;

            if (CalloutType == 0)
            {
                TransportDriver = new Ped("s_m_m_prisguard_01", SpawnPoint, 0f);
                TransportPassenger = new Ped("u_m_y_prisoner_01", SpawnPoint, 0f);
            } else
            {
                TransportDriver = new Ped("s_m_m_armoured_01", SpawnPoint, 0f);
                TransportPassenger = new Ped("s_m_m_armoured_02", SpawnPoint, 0f);
            }
            TransportDriver.IsPersistent = true;
            TransportDriver.BlockPermanentEvents = true;
            TransportDriver.WarpIntoVehicle(TransportVehicle, -1);
            TransportPassenger.IsPersistent = true;
            TransportPassenger.BlockPermanentEvents = true;
            TransportPassenger.WarpIntoVehicle(TransportVehicle, 0);

            EventBlip = new Blip(TransportVehicle);
            EventBlip.Color = Main.calloutWaypointColor;
            EventBlip.Sprite = BlipSprite.ArmoredVan;
            EventBlip.IsRouteEnabled = true;
            EventBlip.Name = "Attacked transport";

            EventCreated = false;
            DriverOutOfTransport = false;
            TransportVehicleUnlocked = false;
            PursuitCreated = false;


            return base.OnCalloutAccepted();
        }

        public override void Process()
        {
            base.Process();

            if (!EventCreated && Game.LocalPlayer.Character.DistanceTo(TransportVehicle) < 300f)
            {
                Game.LogTrivial($"[{pluginName}] Arriving at the scene");
                TransportDriver.Tasks.CruiseWithVehicle(100f, VehicleDrivingFlags.DriveAroundVehicles | VehicleDrivingFlags.Emergency | VehicleDrivingFlags.DriveAroundPeds);
                AttackerOne.Tasks.ChaseWithGroundVehicle(TransportDriver);

                EventCreated = true;
            }
            if (EventCreated && !TransportVehicleUnlocked && TransportDriver.Exists())
                if (TransportDriver.IsDead)
                {
                    Game.LogTrivial($"[{pluginName}] Driver Dead -> Unlocking the vehicle");
                    TransportVehicle.LockStatus = VehicleLockStatus.Unlocked;
                    TransportVehicleUnlocked = true;
                    TransportPassenger.Tasks.LeaveVehicle(LeaveVehicleFlags.LeaveDoorOpen).WaitForCompletion(5000);
                    if (CalloutType == 0)
                    {
                        if (AttackersVehicle.DistanceTo(TransportVehicle) < 50f)
                            AttackerOne.Tasks.PerformDrivingManeuver(VehicleManeuver.GoForwardStraightBraking);
                        else
                            AttackerOne.Tasks.DriveToPosition(TransportPassenger.Position, 100f, VehicleDrivingFlags.Emergency).WaitForCompletion(30000);
                        TransportPassenger.Tasks.EnterVehicle(AttackersVehicle, 0);
                    }
                    else
                        TransportPassenger.Tasks.ReactAndFlee(AttackerOne);
                }

            if (EventCreated && !PursuitCreated 
                && ((TransportDriver.CurrentVehicle == null && !DriverOutOfTransport) 
                || (TransportVehicleUnlocked && !DriverOutOfTransport)))
            {
                EventBlip.IsRouteEnabled = false;
                TransportVehicle.IsInvincible = false;
                GameFiber.Wait(2000);
                Game.LogTrivial($"[{pluginName}] Driver out of transport -> AttackerOne entering the transport");
                AttackerOne.Tasks.EnterVehicle(TransportVehicle, -1).WaitForCompletion(10000);
                Game.LogTrivial($"[{pluginName}] Cruising with the transport");
                AttackerOne.Tasks.CruiseWithVehicle(TransportVehicle, 150f, VehicleDrivingFlags.AllowWrongWay);
                CalloutInterfaceAPI.Functions.SendMessage(this, "Vehicle hijacked");
                Game.LogTrivial($"[{pluginName}] AttackerTwo entering is vehicle");
                AttackerTwo.Tasks.EnterVehicle(AttackersVehicle, -1, 5f).WaitForCompletion(10000);
                Game.LogTrivial($"[{pluginName}] Chasing the transport");
                AttackerTwo.Tasks.ChaseWithGroundVehicle(AttackerOne);
                while (!AttackerTwo.IsInAnyVehicle(false))
                    GameFiber.Yield();

                GameFiber.Wait(2000);

                Pursuit = Functions.CreatePursuit();
                Functions.AddPedToPursuit(Pursuit, AttackerOne);
                Functions.AddPedToPursuit(Pursuit, AttackerTwo);
                if (CalloutType == 0)
                    Functions.AddPedToPursuit(Pursuit, TransportPassenger);
                Functions.SetPursuitIsActiveForPlayer(Pursuit, true);
                Functions.RequestBackup(AttackerOne.Position, LSPD_First_Response.EBackupResponseType.Pursuit, LSPD_First_Response.EBackupUnitType.AirUnit);
                EventBlip.Delete();
                
                DriverOutOfTransport = true;
                PursuitCreated = true;
            }

            bool AttackersDeadOrArrested = (AttackerOne.Exists() && (Functions.IsPedArrested(AttackerOne) || AttackerOne.IsDead))
                                            && (AttackerTwo.Exists() && (Functions.IsPedArrested(AttackerTwo) || AttackerTwo.IsDead));
            bool TransportPrisonerArrestedOrDead = true;
            if (CalloutType == 0)
            {
                TransportPrisonerArrestedOrDead = TransportPassenger.Exists() && (Functions.IsPedArrested(TransportPassenger) || TransportPassenger.IsDead);
            }

            if (Game.IsKeyDown(System.Windows.Forms.Keys.End) 
                || PursuitCreated && !Functions.IsPursuitStillRunning(Pursuit) 
                || (AttackersDeadOrArrested && TransportPrisonerArrestedOrDead))
                End();
        }

        public override void End()
        {
            base.End();

            if (EventBlip.Exists()) EventBlip.Delete();
            if (AttackersVehicle.Exists()) AttackersVehicle.Dismiss();
            if (AttackerOne.Exists())
            {
                if (AttackerOne.Tasks != null) AttackerOne.Tasks.Clear();
                AttackerOne.Dismiss();
            }
            if (AttackerTwo.Exists())
            {
                if (AttackerTwo.Tasks != null) AttackerTwo.Tasks.Clear();
                AttackerTwo.Dismiss();
            }

            if (TransportDriver.Exists())
            {
                TransportDriver.Dismiss();
            }
            if (TransportPassenger.Exists())
            {
                TransportPassenger.Dismiss();
            }
            if (TransportVehicle.Exists()) TransportVehicle.Dismiss();

            Game.DisplayNotification("[CALLOUT 'ATTACKED TRANSPORT' ENDED]");
            Game.LogTrivial($"[{pluginName}] 'Attacked Transport' (varia° {CalloutType}) callout has ended.");
        }
    }
}
