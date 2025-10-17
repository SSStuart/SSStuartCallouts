using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using Rage;
using Rage.Native;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SSStuartCallouts.Callouts
{

    [CalloutInterfaceAPI.CalloutInterface("Train Derailment", CalloutProbability.VeryLow, "A train has derailed off the tracks", "Code 3")]
    public class TrainDerailment : Callout
    {
        public static string pluginName = Main.pluginName;
        public static string pluginVersion = Main.pluginVersion;

        private Ped Driver;
        private Ped Saboteur;
        public static Vehicle CrashedTrain, TrainCarriage1, TrainCarriage2, TrainCarriage3_Tanker;
        private List<Flatbed> Flatbeds = new List<Flatbed>();
        private uint FlatbedTimeout = 0;
        private List<bool> VehicleMarkedFlatbed = new List<bool>() { false, false, false, false };
        private Rage.Object Obstacle1, Obstacle2, Obstacle3, Obstacle4;
        private Blip CrashedTrainBlip;
        private Blip DriverBlip;
        private Blip investigationBlip;
        private Vector3 SpawnPoint;
        private Vector3 DriverSpawnPosition;
        private LHandle Pursuit;
        private bool EventCreated;
        private bool LogicExplosionDriver;
        private bool LogicOnScene;
        private bool LogicInspectedDriver;
        private bool LogicDriverDone;
        private bool LogicFireDone;
        private bool LogicInvestiSetup;
        private bool Sabotage;
        private bool PursuitCreated;
        private CalloutVariante CalloutVersion;

        private enum CalloutVariante
        {
            Windfarm,
            SanChiaski
        }

        public int RandomNumber(int minIncluded, int maxIncluded)
        {
            int random = new Random().Next(minIncluded, maxIncluded + 1);
            return random;
        }


        public override bool OnBeforeCalloutDisplayed()
        {
            switch (RandomNumber(0,1))
            {
                case 1:
                    Game.LogTrivial("RNG = 1");
                    CalloutVersion = CalloutVariante.SanChiaski;
                    break;
                case 0:
                default:
                    Game.LogTrivial("RNG = 0");
                    CalloutVersion = CalloutVariante.Windfarm;
                    break;
            }

            if (CalloutVersion == CalloutVariante.Windfarm)
                SpawnPoint = new Vector3(2067, 1567, 75.5f);
            else if (CalloutVersion == CalloutVariante.SanChiaski)
                SpawnPoint = new Vector3(2929.168f, 4604.511f, 49.23333f);

            Game.LogTrivial($"[{pluginName}] Callout version : " +CalloutVersion.ToString());

            ShowCalloutAreaBlipBeforeAccepting(SpawnPoint, 30f);
            AddMinimumDistanceCheck(300f, SpawnPoint);
            CalloutMessage = "Train Derailment";
            Functions.PlayScannerAudioUsingPosition("WE_HAVE VEHICLE_CATEGORY_TRAIN_01 CRIME_MOTOR_VEHICLE_ACCIDENT_01 IN_OR_ON_POSITION", SpawnPoint);

            return base.OnBeforeCalloutDisplayed();
        }

        public override bool OnCalloutAccepted()
        {
            switch (CalloutVersion)
            {
                case CalloutVariante.Windfarm:
                    CrashedTrain = new Vehicle("freight", SpawnPoint, 45f);
                    TrainCarriage1 = new Vehicle("freightcont1", new Vector3(2078.5f, 1554.4f, 77.3f), 38.3f);
                    TrainCarriage2 = new Vehicle("freightgrain", new Vector3(2083.9f, 1539.4f, 77.7f), 0f);
                    TrainCarriage3_Tanker = new Vehicle("tankercar", new Vector3(2091.4f, 1525.0f, 78.3f), 50f);
                    Obstacle1 = new Rage.Object("prop_rock_2_d", new Vector3(2059.2f, 1573.6f, 75f));
                    Obstacle2 = new Rage.Object("prop_rock_2_c", new Vector3(2080.5f, 1559.9f, 76.2f));
                    Obstacle3 = new Rage.Object("prop_rock_2_a", new Vector3(2068.4f, 1577.0f, 76.1f));
                    Obstacle4 = new Rage.Object("prop_rock_2_g", new Vector3(2072.9f, 1551.7f, 76.0f))
                    {
                        IsPositionFrozen = true
                    };
                    Driver = new Ped("s_m_m_gentransport", SpawnPoint, 0f);

                    NativeFunction.Natives.SWITCH_TRAIN_TRACK(0, false);
                    break;

                case CalloutVariante.SanChiaski:
                    CrashedTrain = new Vehicle("freight", SpawnPoint, 337f);
                    TrainCarriage1 = new Vehicle("freightgrain", new Vector3(2923.55f, 4592.15f, 48.48f), 281f);
                    TrainCarriage3_Tanker = new Vehicle("tankercar", new Vector3(2911.18f, 4580.73f, 48.34f), 338f);
                    TrainCarriage2 = new Vehicle("freightcont1", new Vector3(2900.51f, 4571.77f, 47.98f), 267f);

                    Obstacle1 = new Rage.Object("prop_rock_1_g", new Vector3(2930.86f, 4594.60f, 47.97f));
                    Obstacle2 = new Rage.Object("prop_rock_1_g", new Vector3(2907.07f, 4570.96f, 46.97f));
                    Obstacle3 = new Rage.Object("prop_rock_1_g", new Vector3(2914.35f, 4587.82f, 47.45f));
                    Obstacle4 = new Rage.Object("prop_metal_plates01", new Vector3(2937.93f, 4610.29f, 49.27f))
                    {
                        IsPositionFrozen = false,
                        Rotation = new Rotator(159.4008f, 44.9139f, 138.8085f)
                    };

                    Driver = new Ped("s_m_m_gentransport", SpawnPoint, 0f);
                    break;

                default:

                    break;
            }
            CrashedTrain.IsPersistent = true;
            TrainCarriage1.IsPersistent = true;
            TrainCarriage2.IsPersistent = true;
            TrainCarriage3_Tanker.IsPersistent = true;

            Obstacle1.IsPersistent = true;
            Obstacle1.IsCollisionEnabled = true;
            Obstacle1.IsPositionFrozen = true;
            Obstacle2.IsPersistent = true;
            Obstacle2.IsCollisionEnabled = true;
            Obstacle2.IsPositionFrozen = true;
            Obstacle3.IsPersistent = true;
            Obstacle3.IsCollisionEnabled = true;
            Obstacle3.IsPositionFrozen = true;
            Obstacle4.IsPersistent = true;
            Obstacle4.IsCollisionEnabled = true;

            Driver.IsPersistent = true;
            Driver.BlockPermanentEvents = true;
            Driver.WarpIntoVehicle(CrashedTrain, -1);
            Driver.Health = RandomNumber(50, 200);

            CrashedTrainBlip = new Blip(SpawnPoint)
            {
                Color = Main.calloutWaypointColor,
                IsRouteEnabled = true,
                Name = "Train derailment"
            };

            DriverSpawnPosition = SpawnPoint;

            EventCreated = false;
            LogicOnScene = false;
            LogicExplosionDriver = false;

            return base.OnCalloutAccepted();
        }

        public override void Process()
        {
            base.Process();

            if (!EventCreated && Game.LocalPlayer.Character.DistanceTo(SpawnPoint) < 300f)
            {
                CrashedTrain.EngineHealth = 0;
                CrashedTrain.IsDriveable = false;

                CrashedTrain.ApplyForce(new Vector3(0, 10f, 0), new Vector3(0, 0, 0), true, true);
                TrainCarriage1.ApplyForce(new Vector3(0, 10f, 0), new Vector3(0, 0, 0), true, true);
                TrainCarriage2.ApplyForce(new Vector3(0, 10f, 0), new Vector3(0, 0, 0), true, true);
                TrainCarriage3_Tanker.ApplyForce(new Vector3(0, 10f, 0), new Vector3(0, 0, 0), true, true);

                EventCreated = true;

                CalloutInterfaceAPI.Functions.SendMessage(this, "Caution: One of the carriages may contain flammable materials");
            }

            if (EventCreated && !LogicExplosionDriver && Game.LocalPlayer.Character.DistanceTo(SpawnPoint) < 150f)
            {
                if (RandomNumber(0, 3) == 1)
                    World.SpawnExplosion(TrainCarriage3_Tanker.Position, 5, RandomNumber(10,20), true, false, 1f);
                for (int fireBit = 0; fireBit < 20; fireBit++)
                    NativeFunction.Natives.StartScriptFire((float)(TrainCarriage3_Tanker.Position.X - 5 + (fireBit * 0.5)), (float)(TrainCarriage3_Tanker.Position.Y + RandomNumber(-1, 1)), World.GetGroundZ(TrainCarriage3_Tanker.Position, false, false), 5, true);

                if (Driver.IsAlive)
                {
                    switch (CalloutVersion)
                    {
                        case CalloutVariante.Windfarm:

                            break;
                        case CalloutVariante.SanChiaski:
                            DriverSpawnPosition = new Vector3(2933.23f, 4618.34f, 48.70f);
                            Driver.Position = DriverSpawnPosition;
                            Driver.Heading = 127;
                            break;
                        default:

                            break;
                    }
                    NativeFunction.Natives.TASK_START_SCENARIO_IN_PLACE(Driver, "WORLD_HUMAN_STUPOR_CLUBHOUSE", -1, false);
                }

                LogicExplosionDriver = true;
            }

            if (EventCreated && !LogicOnScene && Game.LocalPlayer.Character.DistanceTo(SpawnPoint) < 20f)
            {
                Game.DisplayHelp("Check the driver");

                CrashedTrainBlip.Delete();
                
                DriverBlip = Driver.AttachBlip();
                DriverBlip.Order = 2;
                DriverBlip.Scale = 0.8f;
                DriverBlip.Name = "Driver";
                DriverBlip.Color = System.Drawing.Color.LightSkyBlue;

                if(Driver.IsInAnyVehicle(false))
                    Driver.Tasks.LeaveVehicle(LeaveVehicleFlags.BailOut);
                GameFiber.Wait(5000);
                Game.DisplayHelp("Press ~b~End~w~ to end the callout.");

                LogicOnScene = true;
            }
            if (EventCreated && !LogicInspectedDriver && Game.LocalPlayer.Character.DistanceTo(Driver) < 5f)
            {
                if (Driver.IsAlive)
                {
                    Game.DisplayHelp("Take care of the driver, then press ~b~Enter~w~ to continue.");
                }
                DriverBlip.Delete();

                LogicInspectedDriver = true;
            }
            if (EventCreated && LogicInspectedDriver && !LogicInvestiSetup && (!Driver.Exists() || Driver.DistanceTo(DriverSpawnPosition) > 30f))
            {
                if (CalloutVersion == CalloutVariante.SanChiaski)
                    Game.DisplayHelp("Press ~b~Enter~w~ to start the investigation.");
                else if (CalloutVersion == CalloutVariante.Windfarm)
                    Game.DisplayHelp("Press ~b~End~w~ to end the callout.");

                LogicDriverDone = true;
            }
            // Flatbeds
            if (EventCreated && LogicInspectedDriver && CalloutVersion == CalloutVariante.Windfarm)
            {
                if (CrashedTrain != null && CrashedTrain.Exists() && Game.LocalPlayer.Character.DistanceTo(CrashedTrain) < 8f && !VehicleMarkedFlatbed[0])
                {
                    Game.DisplayHelp("Press ~b~E~w~ to call a flatbed for this carriage.");
                    if (Game.IsKeyDown(Keys.E))
                    {
                        Flatbed truck = new Flatbed(0);
                        Flatbeds.Add(truck);
                        VehicleMarkedFlatbed[0] = true;
                    }
                }
                if (TrainCarriage1 != null && TrainCarriage1.Exists() && Game.LocalPlayer.Character.DistanceTo(TrainCarriage1) < 8f && !VehicleMarkedFlatbed[1])
                {
                    Game.DisplayHelp("Press ~b~E~w~ to call a flatbed for this carriage.");
                    if (Game.IsKeyDown(Keys.E))
                    {
                        Flatbed truck = new Flatbed(1);
                        Flatbeds.Add(truck);
                        VehicleMarkedFlatbed[1] = true;
                    }
                }
                if (TrainCarriage2 != null && TrainCarriage2.Exists() && Game.LocalPlayer.Character.DistanceTo(TrainCarriage2) < 8f && !VehicleMarkedFlatbed[2])
                {
                    Game.DisplayHelp("Press ~b~E~w~ to call a flatbed for this carriage.");
                    if (Game.IsKeyDown(Keys.E))
                    {
                        Flatbed truck = new Flatbed(2);
                        Flatbeds.Add(truck);
                        VehicleMarkedFlatbed[2] = true;
                    }
                }
                if (TrainCarriage3_Tanker != null && TrainCarriage3_Tanker.Exists() && Game.LocalPlayer.Character.DistanceTo(TrainCarriage3_Tanker) < 8f && !VehicleMarkedFlatbed[3])
                {
                    Game.DisplayHelp("Press ~b~E~w~ to call a flatbed for this carriage.");
                    if (Game.IsKeyDown(Keys.E))
                    {
                        Flatbed truck = new Flatbed(3);
                        Flatbeds.Add(truck);
                        VehicleMarkedFlatbed[3] = true;
                    }
                }
            }
            if (EventCreated && LogicDriverDone && Game.IsKeyDown(Keys.Enter))
                LogicFireDone = true;
            if (EventCreated && LogicDriverDone && LogicFireDone && !LogicInvestiSetup && CalloutVersion == CalloutVariante.SanChiaski)
            {
                Sabotage = RandomNumber(0, 1) == 0;

                investigationBlip = new Blip(new Vector3(2997.234f, 4059.654f, 55.84571f), 50f);
                investigationBlip.Color = Main.calloutWaypointColor;
                investigationBlip.Alpha = 0.3f;
                investigationBlip.IsRouteEnabled = true;

                if (Sabotage)
                {
                    Saboteur = new Ped(new Vector3(2986.937f, 4070.471f, 56.2295f), 80f);
                    Saboteur.IsPersistent = true;
                    Saboteur.BlockPermanentEvents = true;
                }
                LogicInvestiSetup = true;
            }
            if (LogicInvestiSetup && Game.LocalPlayer.Character.DistanceTo(new Vector3(2997.234f, 4059.654f, 55.84571f)) < 40f)
            {
                if (Sabotage && !PursuitCreated)
                {
                    Saboteur.IsPersistent = false;
                    Saboteur.BlockPermanentEvents = false;
                    Pursuit = Functions.CreatePursuit();
                    Functions.AddPedToPursuit(Pursuit, Saboteur);
                    Functions.SetPursuitIsActiveForPlayer(Pursuit, true);
                    PursuitCreated = true;
                    CalloutInterfaceAPI.Functions.SendMessage(this, "A suspect was spotted near railway infrastructure and is fleeing. Pursuit has begun");
                }
            }
            if (PursuitCreated && !Functions.IsPursuitStillRunning(Pursuit) || (Saboteur.Exists() && !Saboteur.IsAlive))
            {
                End();
            }
            else if (EventCreated && ((Game.LocalPlayer.Character.DistanceTo(SpawnPoint) > 500f && !LogicInvestiSetup) || (Game.LocalPlayer.Character.DistanceTo(new Vector3(2997.234f, 4059.654f, 55.84571f)) > 1000f && LogicInvestiSetup)))
                End();

            if (Game.IsKeyDown(Keys.End))
                End();
        }

        public override void End()
        {
            base.End();

            if (TrainCarriage3_Tanker != null && TrainCarriage3_Tanker.Exists())
            {
                NativeFunction.Natives.StopFireInRange(TrainCarriage3_Tanker.Position.X, TrainCarriage3_Tanker.Position.Y, TrainCarriage3_Tanker.Position.Z, 30f);
            }

            int counter = 0;
            Game.LogTrivial($"[{pluginName}] 'Train derailment' callout ending in 30 sec.");
            while (Game.LocalPlayer.Character.DistanceTo(SpawnPoint) < 300f && counter <= 6)
            {
                GameFiber.Wait(5000);
                counter++;
            }

            if (CrashedTrainBlip.Exists()) CrashedTrainBlip.Delete();
            if (DriverBlip.Exists()) DriverBlip.Delete();
            if (Driver.Exists()) Driver.Delete();
            if (CrashedTrain.Exists()) CrashedTrain.Delete();
            if (TrainCarriage1.Exists()) TrainCarriage1.Delete();
            if (TrainCarriage2.Exists()) TrainCarriage2.Delete();
            if (TrainCarriage3_Tanker.Exists()) TrainCarriage3_Tanker.Delete();
            if (Obstacle1.Exists()) Obstacle1.Delete();
            if (Obstacle2.Exists()) Obstacle2.Delete();
            if (Obstacle3.Exists()) Obstacle3.Delete();
            if (Obstacle4.Exists()) Obstacle4.Delete();
            if (investigationBlip.Exists()) investigationBlip.Delete();
            if (Saboteur.Exists()) Saboteur.Dismiss();
            foreach (Flatbed flatbed in Flatbeds)
            {
                flatbed?.Remove();
            }

            NativeFunction.Natives.SWITCH_TRAIN_TRACK(0, true);

            Game.DisplayNotification("[CALLOUT 'TRAIN DERAILMENT' ENDED]");
            Game.LogTrivial($"[{pluginName}] 'Train derailment' callout has ended.");
        }
    }

    public class Flatbed
    {
        private Vehicle truck;
        private Vehicle trailer;
        private int targetIndex;
        private Vehicle trainTarget;
        private Ped driver;
        private Blip blip;
        private static List<List<Vector3>> spawnPos = new List<List<Vector3>>()
        {
            new List<Vector3>
            {
                new Vector3(2245.497f, 1217.362f, 76.6133f),    // Truck
                new Vector3(2255.9f, 1207.313f, 77.869f)        // Trailer
            },
            new List<Vector3>
            {
                new Vector3(2263.621f, 1194.953f, 76.433f),     // Truck
                new Vector3(2273.769f, 1184.249f, 76.545f)      // Trailer
            },
            new List<Vector3>
            {
                new Vector3(2282.73f, 1173.379f, 76.868f),      // Truck
                new Vector3(2292.343f, 1163.389f, 77.76163f)    // Trailer
            },
            new List<Vector3>
            {
                new Vector3(2303.992f, 1149.404f, 78.43932f),      // Truck
                new Vector3(2315.508f, 1136.22f, 79.44845f)    // Trailer
            }
        };
        private static List<List<Vector3>> drivePos = new List<List<Vector3>>()
        {
            new List<Vector3>
            {
                new Vector3(2052.04f, 1506.961f, 74.381f),      // Road, before turning
                new Vector3(2073.092f, 1534.605f, 75.271f),     // Close to the train, before the target
                new Vector3(2052.489f, 1565.629f, 74.79f),      // Side to side with the target
                new Vector3(2030.543f, 1562.684f, 74.081f)      // Back on the road
            },
            new List<Vector3>
            {
                new Vector3(2052.04f, 1506.961f, 74.381f),
                new Vector3(2077.263f, 1526.407f, 76.231f),
                new Vector3(2068.724f, 1550.246f, 75.821f),
                new Vector3(2030.776f, 1565.148f, 74.451f)
            },
            new List<Vector3>
            {
                new Vector3(2076.322f, 1456.997f, 75.62393f),
                new Vector3(2083.379f, 1514.438f, 78.28054f),
                new Vector3(2072.157f, 1540.501f, 76.73112f),
                new Vector3(2036.021f, 1552.089f, 75.47736f)
            },
            new List<Vector3>
            {
                new Vector3(2071.442f, 1470.465f, 75.59653f),
                new Vector3(2084.404f, 1506.111f, 78.31793f),
                new Vector3(2077.943f, 1528.775f, 77.19665f),
                new Vector3(2042.718f, 1541.006f, 75.47222f)
            }
        };

        public Flatbed(int target)
        {
            targetIndex = target;
            switch (targetIndex)
            {
                case 1:
                    trainTarget = TrainDerailment.TrainCarriage1;
                    break;
                case 2:
                    trainTarget = TrainDerailment.TrainCarriage2;
                    break;
                case 3:
                    trainTarget = TrainDerailment.TrainCarriage3_Tanker;
                    break;
                case 0:
                default:
                    trainTarget = TrainDerailment.CrashedTrain;
                    break;
            }

            truck = new Vehicle("packer", spawnPos[targetIndex][0], 42f)
            {
                IsPersistent = true,
                IsEngineOn = true,
            };
            trailer = new Vehicle("freighttrailer", spawnPos[targetIndex][1], 42f)
            {
                IsPersistent = true,
            };
            truck.Trailer = trailer;

            blip = new Blip(truck)
            {
                Sprite = BlipSprite.TowTruck,
                Name = "Flatbed"
            };

            driver = truck.CreateRandomDriver();
            driver.BlockPermanentEvents = true;

            Entity closestVehicleFront = World.GetClosestEntity(truck.GetOffsetPositionFront(10f), 8f, GetEntitiesFlags.ConsiderGroundVehicles | GetEntitiesFlags.ExcludePlayerVehicle);
            while (closestVehicleFront != null && closestVehicleFront.Model.Name == "freighttrailer")
            {
                GameFiber.Sleep(1000);
                closestVehicleFront = World.GetClosestEntity(truck.GetOffsetPositionFront(10f), 8f, GetEntitiesFlags.ConsiderGroundVehicles | GetEntitiesFlags.ExcludePlayerVehicle);
            }

            GameFiber.StartNew(delegate
            {
                Game.LogTrivial("Flatbed : Task : Drive near scene...");
                driver.Tasks.DriveToPosition(drivePos[targetIndex][0], 50, VehicleDrivingFlags.FollowTraffic, 15f).WaitForCompletion(60000);
                Game.LogTrivial("Flatbed : Task : Drive close to train...");
                driver.Tasks.DriveToPosition(drivePos[targetIndex][1], 10, VehicleDrivingFlags.DriveAroundPeds | VehicleDrivingFlags.DriveAroundVehicles | VehicleDrivingFlags.IgnorePathFinding | VehicleDrivingFlags.DriveBySight, 10f).WaitForCompletion(60000);
                Game.LogTrivial("Flatbed : Task : Drive & stop close target...");
                driver.Tasks.DriveToPosition(drivePos[targetIndex][2], 10, VehicleDrivingFlags.DriveAroundPeds | VehicleDrivingFlags.DriveAroundVehicles | VehicleDrivingFlags.IgnorePathFinding, 5f).WaitForCompletion(20000);
                driver.Tasks.PerformDrivingManeuver(VehicleManeuver.Wait).WaitForCompletion(5000);
                Game.LogTrivial("Flatbed : Waiting 10s...");
                GameFiber.Wait(10000);
                Game.LogTrivial("Flatbed : Attaching train, waiting 5s...");
                trainTarget.AttachTo(trailer, 0, new Vector3(0, -0.5f, (targetIndex == 0 ? 0 : -0.7f)), new Rotator(0, 0, 0));
                if (blip != null && blip.Exists())
                    blip.Delete();
                GameFiber.Wait(5000);
                Game.LogTrivial("Flatbed : Task : Drive back to road...");
                driver.Tasks.DriveToPosition(drivePos[targetIndex][3], 30, VehicleDrivingFlags.DriveAroundPeds | VehicleDrivingFlags.DriveAroundVehicles | VehicleDrivingFlags.IgnorePathFinding, 20f).WaitForCompletion(30000);
                driver.Tasks.CruiseWithVehicle(40f);
                GameFiber.Wait(60000);
                Remove();
            });
        }

        public void Remove()
        {
            if (trainTarget != null && trainTarget.Exists())
            {
                trainTarget.Delete();
            }
            if (trailer != null && trailer.Exists())
                trailer.Dismiss();
            if (driver != null && driver.Exists())
            {
                driver.Tasks.Clear();
                driver.Dismiss();
            }
            if (blip != null && blip.Exists())
                blip.Delete();
            if (truck != null && truck.Exists())
                truck.Dismiss();
        }
    }
}
