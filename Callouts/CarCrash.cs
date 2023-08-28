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
        private Blip DriverBlip;
        private LHandle EventHandle;
        private Vector3 SpawnPoint;
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

            Driver = new Ped(CrashedVehicle.GetOffsetPositionFront(new Random().Next(2,6)));
            Driver.IsPersistent = true;
            Driver.BlockPermanentEvents = true;
            if (new Random().Next(0, 2) == 1)
            {
                Driver.WarpIntoVehicle(CrashedVehicle, -1);
            }

            DriverBlip = Driver.AttachBlip();
            DriverBlip.Color = System.Drawing.Color.Orange;
            DriverBlip.IsRouteEnabled = true;

            EventCreated = false;

            return base.OnCalloutAccepted();
        }

        public override void Process()
        {
            base.Process();

            if (!EventCreated && Game.LocalPlayer.Character.DistanceTo(CrashedVehicle) < 200f)
            {
                CrashedVehicle.Position = new Vector3(CrashedVehicle.Position.X, CrashedVehicle.Position.Y, CrashedVehicle.Position.Z + 20f);
                CrashedVehicle.SetRotationRoll(180f);
                CrashedVehicle.EngineHealth = 0f;
                CrashedVehicle.IsEngineOn = false;
                CrashedVehicle.IsDriveable = false;
                CrashedVehicle.IndicatorLightsStatus = VehicleIndicatorLightsStatus.Both;

                Driver.Health = new Random().Next(100, 200);

                EventCreated = true;
            }

            if (EventCreated && (Game.LocalPlayer.Character.DistanceTo(Driver) > 800f || Game.LocalPlayer.Character.DistanceTo(CrashedVehicle) > 800f))
            {
                End();
            }
        }

        public override void End()
        {
            base.End();

            if (Driver.Exists()) Driver.Dismiss();
            if (DriverBlip.Exists()) DriverBlip.Delete();
            if (CrashedVehicle.Exists()) CrashedVehicle.Dismiss();

            Game.LogTrivial("[SSStuart Callout] 'Car crash' callout has ended.");
        }
    }
}
