using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rage;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;


namespace SSStuart_Callouts.Callouts
{

    [CalloutInfo("CarCrash", CalloutProbability.High)]

    public class CarCrash: Callout
    {
        private Ped Driver;
        private Vehicle CrashedVehicle;
        private Blip CrashedVehicleBlip;
        private Blip DriverBlip;
        private LHandle EventHandle;
        private Vector3 SpawnPoint;
        private bool DriverMarked;
        private bool EventCreated;

        public override bool OnBeforeCalloutDisplayed()
        {
            SpawnPoint = World.GetRandomPositionOnStreet();
            ShowCalloutAreaBlipBeforeAccepting(SpawnPoint, 30f);
            AddMinimumDistanceCheck(5f, SpawnPoint);
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
                "feltzer",
                "dashound",
                "stratum",
                "premier",
                "rebel",
                "phoenix",
                "issi",
                "benson"
            };

            CrashedVehicle = new Vehicle(vehicleList[new Random().Next(vehicleList.Count)], SpawnPoint);
            CrashedVehicle.IsPersistent = true;

            Driver = new Ped(CrashedVehicle.GetOffsetPositionRight(5f));
            Driver.IsPersistent = true;
            Driver.BlockPermanentEvents = true;
            Driver.WarpIntoVehicle(CrashedVehicle, -1);
            Driver.Health = new Random().Next(50, 200);

            CrashedVehicleBlip = CrashedVehicle.AttachBlip();
            CrashedVehicleBlip.Color = System.Drawing.Color.Orange;
            CrashedVehicleBlip.Scale = 5f;
            CrashedVehicleBlip.Alpha = 0.5f;
            CrashedVehicleBlip.Name = "Crashed Vehicle";
            CrashedVehicleBlip.IsRouteEnabled = true;

            EventCreated = false;
            DriverMarked = false;

            return base.OnCalloutAccepted();
        }

        public override void Process()
        {
            base.Process();

            if (!EventCreated && Game.LocalPlayer.Character.DistanceTo(CrashedVehicle) < 200f)
            {
                Game.DisplayNotification("[200]");

                CrashedVehicle.EngineHealth = 10f;
                CrashedVehicle.IsDriveable = false;
                CrashedVehicle.IndicatorLightsStatus = VehicleIndicatorLightsStatus.RightOnly;

                EventCreated = true;
            }

            if (EventCreated && !DriverMarked && Game.LocalPlayer.Character.DistanceTo(CrashedVehicle) < 20f)
            {
                Game.DisplayNotification("[20]");

                Game.LogTrivial("Velocity applied to vehicle.");
                CrashedVehicle.Velocity = new Vector3(10, 20, 0);
                CrashedVehicleBlip.Order = 1;
                CrashedVehicleBlip.IsRouteEnabled = false;

                GameFiber.Wait(5000);
                if (Driver.IsAlive)
                {
                    Driver.Tasks.LeaveVehicle(CrashedVehicle, LeaveVehicleFlags.LeaveDoorOpen).WaitForCompletion();
                    Driver.Tasks.GoStraightToPosition(CrashedVehicle.GetOffsetPositionFront(-10), 2f, 0, 0, 10000).WaitForCompletion();
                }

                DriverBlip = Driver.AttachBlip();
                DriverBlip.Order = 2;
                DriverBlip.Scale = 0.8f;
                DriverBlip.Color = System.Drawing.Color.LightSkyBlue;
                DriverBlip.IsRouteEnabled = true;

                DriverMarked = true;
            }


            if (EventCreated && Driver.IsDead)
            {
                Game.DisplayNotification("The driver is dead.");

                End();
            }
            else if (EventCreated && (Game.LocalPlayer.Character.DistanceTo(Driver) > 600f || !Driver.Exists()))
            {
                End();
            }
        }

        public override void End()
        {
            base.End();

            if (DriverBlip.Exists()) DriverBlip.Delete();
            if (Driver.Exists()) Driver.Dismiss();
            if (CrashedVehicleBlip.Exists()) CrashedVehicleBlip.Delete();
            if (CrashedVehicle.Exists()) CrashedVehicle.Dismiss();

            Game.DisplayNotification("[CALLOUT 'CAR CRASH' ENDED]");
            Game.LogTrivial("[SSStuart Callout] 'Car crash' callout has ended.");
        }
    }
}
