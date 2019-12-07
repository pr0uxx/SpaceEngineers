using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        // This file contains your actual script.
        //
        // You can either keep all your code here, or you can create separate
        // code files to make your program easier to navigate while coding.
        //
        // In order to add a new utility class, right-click on your project, 
        // select 'New' then 'Add Item...'. Now find the 'Space Engineers'
        // category under 'Visual C# Items' on the left hand side, and select
        // 'Utility Class' in the main area. Name it in the box below, and
        // press OK. This utility class will be merged in with your code when
        // deploying your final script.
        //
        // You can also simply create a new utility class manually, you don't
        // have to use the template if you don't want to. Just do so the first
        // time to see what a utility class looks like.
        // 
        // Go to:
        // https://github.com/malware-dev/MDK-SE/wiki/Quick-Introduction-to-Space-Engineers-Ingame-Scripts
        //
        // to learn more about ingame scripts.

        private readonly Drone drone;

        public Program()
        {
            var antennaBlocks = new List<IMyRadioAntenna>();
            GridTerminalSystem.GetBlocksOfType(antennaBlocks);
            var gyros = new List<IMyGyro>();
            GridTerminalSystem.GetBlocksOfType(gyros);
            var remoteControls = new List<IMyRemoteControl>();
            GridTerminalSystem.GetBlocksOfType(remoteControls);
            var thrusters = new List<IMyThrust>();
            GridTerminalSystem.GetBlocksOfType(thrusters);

            drone = new Drone
            {
                Antenna = antennaBlocks.First(),
                Gyros = gyros,
                ProgrammableBlock = Me,
                RemoteControl = remoteControls.First(),
                Thruster = thrusters.First(),
                Configuration = new DroneConfiguration(Me),
                Program = this
            };

            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }

        public void Save()
        {
            // Called when the program needs to save its state. Use
            // this method to save your state to the Storage field
            // or some other means. 
            // 
            // This method is optional and can be removed if not
            // needed.
        }

        public void Main(string argument, UpdateType updateSource)
        {
            drone.Hover();
        }


        public class Drone
        {
            public Program Program { get; set; }
            public IMyRadioAntenna Antenna { get; set; }
            public IList<IMyGyro> Gyros { get; set; } = new List<IMyGyro>();
            public IMyRemoteControl RemoteControl { get; set; }
            public IMyProgrammableBlock ProgrammableBlock { get; set; }
            public IMyThrust Thruster { get; set; }
            public DroneConfiguration Configuration { get; set; }

            double CTRL_COEFF = 0.8; //Set lower if overshooting, set higher to respond quicker
            int LIMIT_GYROS = 999; //Set to the max number of gyros to use
                                   //(Using less gyros than you have allows you to still steer while
                                   // leveler is operating.)

            int stage = 0;
            int counter = 0;

            public void Hover()
            {
                switch (stage)
                {
                    case 0:
                        RotateToGravity();
                        break;
                    case 1:
                        RotateToGravity(10);
                        break;
                    case 2:
                        RotateToGravity(-10);
                        break;
                }

                //MaintainHeight();
                //MaintainSpeed();

                switch (counter)
                {
                    case 30:
                        stage = 1;
                        break;
                    case 60:
                        stage = 0;
                        break;
                    case 90:
                        stage = 2;
                        break;
                    case 120:
                        stage = 0;
                        counter = -1;
                        break;
                }
                counter++;
            }

            public void MaintainHeight()
            {
                double elevation;

                if (RemoteControl.TryGetPlanetElevation(MyPlanetElevation.Surface, out elevation))
                {
                    if (elevation < Configuration.MinimumHeight)
                    {
                        Thruster.ThrustOverridePercentage = 0.5f;
                    }
                    else if (elevation > Configuration.MaximumHeight)
                    {
                        Thruster.ThrustOverridePercentage = 0.01f;
                    }
                    else
                    {
                        Thruster.ThrustOverridePercentage = 0;
                        RemoteControl.DampenersOverride = true;
                    }
                }
            }

            public void MaintainSpeed()
            {
                var currentSpeed = RemoteControl.GetShipSpeed();

                if (currentSpeed > Configuration.MaxSpeed)
                {
                    RotateToGravity();
                    Thruster.ThrustOverridePercentage = 0;
                    RemoteControl.DampenersOverride = true;
                }
            }

            public void RotateToGravity(float angleOverride = 0)
            {
                Matrix orientation;
                RemoteControl.Orientation.GetMatrix(out orientation);

                Vector3 down = orientation.Down;

                Vector3D grav = RemoteControl.GetNaturalGravity();
                grav.Normalize();

                foreach (var gyro in Gyros)
                {
                    gyro.Orientation.GetMatrix(out orientation);
                    var localDown = Vector3D.Transform(down, MatrixD.Transpose(orientation));

                    var localGrav = Vector3D.Transform(grav, MatrixD.Transpose(gyro.WorldMatrix.GetOrientation()));

                    var rotation = Vector3D.Cross(localDown, localGrav);
                    var angle = rotation.Length();
                    angle = Math.Atan2(angle, Math.Sqrt(Math.Max(0.0, 1.0 - angle * angle)));

                    var overrideFactor = ((angle / 100) * 90) / 100;

                    if (angle < 0.01 || (overrideFactor > 0 && (angle > overrideFactor - 0.01 && angle < overrideFactor + 0.01)))
                    {   //Close enough
                        //Echo("Level");
                        gyro.SetValueBool("Override", false);
                        continue;
                    }

                    //Echo("Off level: "+(ang*180.0/3.14).ToString()+"deg");

                    //Control speed to be proportional to distance (angle) we have left
                    double ctrl_vel = gyro.GetMaximum<float>("Yaw") * (angle / Math.PI) * CTRL_COEFF;
                    ctrl_vel = Math.Min(gyro.GetMaximum<float>("Yaw"), ctrl_vel);
                    ctrl_vel = Math.Max(0.01, ctrl_vel); //Gyros don't work well at very low speeds
                    rotation.Normalize();
                    rotation *= ctrl_vel;
                    gyro.SetValueFloat("Pitch", (float)rotation.GetDim(0));
                    gyro.SetValueFloat("Yaw", -(float)rotation.GetDim(1));
                    gyro.SetValueFloat("Roll", -(float)rotation.GetDim(2));
                    gyro.SetValueFloat("Power", 1.0f);
                    gyro.SetValueBool("Override", true);
                }
            }
        }

        public class DroneConfiguration
        {
            public DroneConfiguration(IMyProgrammableBlock program)
            {
                MyIniParseResult r;
                if (!_ini.TryParse(program.CustomData, out r))
                    throw new Exception(r.ToString());

                var maxHeight = _ini.Get("drone_config", "MaximumHeight");
                var minHeight = _ini.Get("drone_config", "MaximumHeight");
                var fpsco = _ini.Get("drone_config", "FuelPercentSafetyCutOff");
                var maxSpeed = _ini.Get("drone_config", "MaximumHeight");

                MaximumHeight = maxHeight.IsEmpty ? 50 : maxHeight.ToInt32();
                MinimumHeight = minHeight.IsEmpty ? 50 : minHeight.ToInt32();
                FuelPercentSafetyCutOff = fpsco.IsEmpty ? 50 : fpsco.ToInt32();
                MaxSpeed = maxSpeed.IsEmpty ? 50 : maxSpeed.ToInt32();
            }

            public int MaximumHeight { get; set; }
            public int MinimumHeight { get; set; }
            public int FuelPercentSafetyCutOff { get; set; }
            public int MaxSpeed { get; set; }

            protected MyIni _ini = new MyIni();
        }

        public class CommandController
        {
            List<Action>[] ActionQueue = new List<Action>[3];
        }

        public class ActionQueue
        {
            public ActionQueue()
            {
                Queue[0] = new List<Action>();
                Queue[1] = new List<Action>();
                Queue[2] = new List<Action>();
            }

            List<Action>[] Queue = new List<Action>[3];
            int TotalCount;

            /// <summary>
            /// 1 - Low
            /// 2 - Medium
            /// 3 - High
            /// </summary>
            /// <param name="priority"></param>
            public void Add(int priority, Action action)
            {
                priority = Math.Min(3, priority);
                priority = Math.Max(priority, 1) - 1;

                Queue[priority].Add(action);
            }

            public void Execute(int concurrentActions)
            {
                concurrentActions = Math.Min(concurrentActions, TotalCount);
                if (concurrentActions > 0)
                {
                    int rem = 0;
                    var hpCount = Math.Floor(concurrentActions / 2.0);
                    var diff = (int)hpCount - Queue[2].Count;
                    if (diff > 0)
                    {
                        rem += diff;
                        hpCount = Queue[2].Count;
                    }

                    var mpCount = Math.Floor(concurrentActions / 4.0);
                    if (mpCount + rem < Queue[1].Count)
                    {
                        mpCount = mpCount + rem;
                        rem = 0;
                    }
                    else
                    {
                        diff = (int)mpCount - Queue[1].Count;
                        rem += diff;
                        mpCount = Queue[1].Count;
                    }
                    
                    var lpCount = concurrentActions - hpCount - mpCount;

                    if (lpCount + rem < Queue[0].Count)
                    {
                        lpCount = lpCount + rem;
                        rem = 0;
                    }
                    else
                    {
                        diff = (int)lpCount - Queue[0].Count;
                        rem += diff;
                        lpCount = Queue[0].Count;
                    }

                    var actions = new List<Action>();
                    actions.AddRange(Queue[2].Take((int)hpCount).ToArray());
                    actions.AddRange(Queue[1].Take((int)mpCount).ToArray());
                    actions.AddRange(Queue[0].Take((int)lpCount).ToArray());
                    var counterMax = Math.Max(Math.Max(hpCount, mpCount), lpCount);


                    if (actions.Count > 0)
                    {
                        for (int i = 0; i < counterMax; i++)
                        {
                            if (i < lpCount && Queue[0].Count > 0)
                            {
                                Queue[0]?.RemoveAt(0);
                            }
                                TotalCount -= 1;
                            if (i < mpCount && Queue[1].Count > 0)
                            {
                                Queue[1]?.RemoveAt(0);
                                TotalCount -= 1;
                            }
                            if (i < hpCount && Queue[2].Count > 0)
                            {
                                Queue[2]?.RemoveAt(0);
                                TotalCount -= 1;
                            }
                            
                        }

                        foreach (var a in actions)
                        {
                            a.Invoke();
                        }
                    }

                    
                }               
            }
        }
    }
}
