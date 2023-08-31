using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rage;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;

namespace SSStuartCallouts.Callouts
{

    [CalloutInfo("TrainDerailment", CalloutProbability.High)]

    public class TrainDerailment : Callout
    {
        private Ped Driver;
        private Vehicle CrashedTrain;
        private Vehicle TrainCarriage1;
        private Vehicle TrainCarriage2;
        private Vehicle TrainCarriage3_Tanker;
        private Blip CrashedTrainBlip;
        private Blip DriverBlip;
        private Vector3 SpawnPoint;
        private bool EventCreated;
        private bool Explosion;
        private bool DriverOut;

        public int RandomNumber(int min, int max)
        {
            int random = new Random().Next(min, max);
            return random;
        }


        public override bool OnBeforeCalloutDisplayed()
        {
            SpawnPoint = new Vector3(2067, 1567, 75.5f);
            ShowCalloutAreaBlipBeforeAccepting(SpawnPoint, 30f);
            AddMinimumDistanceCheck(5f, SpawnPoint);
            CalloutMessage = "Train derailment";
            CalloutPosition = SpawnPoint;
            Functions.PlayScannerAudioUsingPosition("WE_HAVE VEHICLE_CATEGORY_TRAIN_01 CRIME_MOTOR_VEHICLE_ACCIDENT_01 IN_OR_ON_POSITION", SpawnPoint);

            return base.OnBeforeCalloutDisplayed();
        }

        public override bool OnCalloutAccepted()
        {

            CrashedTrain = new Vehicle("freight", SpawnPoint, 45f);
            CrashedTrain.IsPersistent = true;

            TrainCarriage1 = new Vehicle("freightcont1", new Vector3(2078.5f, 1554.4f, 77.3f), 38.3f);
            TrainCarriage1.IsPersistent = true;
            TrainCarriage2 = new Vehicle("freightgrain", new Vector3(2083.9f, 1539.4f, 77.7f), 0f);
            TrainCarriage2.IsPersistent = true;
            TrainCarriage3_Tanker = new Vehicle("tankercar", new Vector3(2091.4f, 1525.0f, 78.3f), 50f);

            Driver = new Ped("s_m_m_gentransport", SpawnPoint, 0f);
            Driver.IsPersistent = true;
            Driver.BlockPermanentEvents = true;
            Driver.WarpIntoVehicle(CrashedTrain, -1);
            Driver.Health = RandomNumber(50, 200);

            CrashedTrainBlip = new Blip(SpawnPoint);
            CrashedTrainBlip.Color = System.Drawing.Color.Orange;
            CrashedTrainBlip.IsRouteEnabled = true;
            CrashedTrainBlip.Name = "Train derailment";

            EventCreated = false;
            DriverOut = false;
            Explosion = false;

            return base.OnCalloutAccepted();
        }

        public override void Process()
        {
            base.Process();

            if (!EventCreated && Game.LocalPlayer.Character.DistanceTo(CrashedTrain) < 300f)
            {
                CrashedTrain.EngineHealth = 0;
                CrashedTrain.IsDriveable = false;

                CrashedTrain.ApplyForce(new Vector3(0, 10f, 0), new Vector3(0, 0, 0), true, true);
                TrainCarriage1.ApplyForce(new Vector3(0, 10f, 0), new Vector3(0, 0, 0), true, true);
                TrainCarriage2.ApplyForce(new Vector3(0, 10f, 0), new Vector3(0, 0, 0), true, true);
                TrainCarriage3_Tanker.ApplyForce(new Vector3(0, 10f, 0), new Vector3(0, 0, 0), true, true);

                EventCreated = true;
            }

            if (EventCreated && !Explosion && Game.LocalPlayer.Character.DistanceTo(CrashedTrain) < 150f)
            {
                if (RandomNumber(0, 3) == 1)
                    World.SpawnExplosion(TrainCarriage3_Tanker.Position, 5, RandomNumber(10,20), true, false, 1f);

                Explosion = true;
            }

            if (EventCreated && !DriverOut && Game.LocalPlayer.Character.DistanceTo(CrashedTrain) < 20f)
            {
                Game.DisplayNotification("Check the driver");

                CrashedTrainBlip.Delete();
                
                DriverBlip = Driver.AttachBlip();
                DriverBlip.Order = 2;
                DriverBlip.Scale = 0.8f;
                DriverBlip.Name = "Driver";
                DriverBlip.Color = System.Drawing.Color.LightSkyBlue;

                Driver.Tasks.LeaveVehicle(LeaveVehicleFlags.BailOut);
                GameFiber.Wait(5000);
                Game.DisplayHelp("Press ~b~End~w~ to end the callout.");

                DriverOut = true;
            }
            else if (EventCreated && Game.LocalPlayer.Character.DistanceTo(CrashedTrain) > 500f)
                End();

            if (Game.IsKeyDown(System.Windows.Forms.Keys.End))
                End();
        }

        public override void End()
        {
            base.End();

            int counter = 0;
            Game.LogTrivial("[SSStuart Callout] 'Train derailment' callout ending in 30 sec.");
            while (Game.LocalPlayer.Character.DistanceTo(CrashedTrain) < 300f && counter < 6)
            {
                GameFiber.Wait(5000);
                counter++;
            }

            if (CrashedTrainBlip.Exists()) CrashedTrainBlip.Delete();
            if (Driver.Exists()) Driver.Delete();
            if (CrashedTrain.Exists()) CrashedTrain.Delete();
            if (TrainCarriage1.Exists()) TrainCarriage1.Delete();
            if (TrainCarriage2.Exists()) TrainCarriage2.Delete();
            if (TrainCarriage3_Tanker.Exists()) TrainCarriage3_Tanker.Delete();

            Game.DisplayNotification("[CALLOUT 'TRAIN DERAILMENT' ENDED]");
            Game.LogTrivial("[SSStuart Callout] 'Train derailment' callout has ended.");
        }
    }
}
