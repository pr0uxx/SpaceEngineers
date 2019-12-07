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
        MyIni _ini = new MyIni();
        private readonly Airlock airlock;

        public Program()
        {
            MyIniParseResult result;
            if (!_ini.TryParse(Me.CustomData, out result))
                throw new Exception(result.ToString());

            airlock = new Airlock(this, _ini);
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
            // The constructor, called only once every session and
            // always before any other method is called. Use it to
            // initialize your script. 
            //     
            // The constructor is optional and can be removed if not
            // needed.
            // 
            // It's recommended to set Runtime.UpdateFrequency 
            // here, which will allow your script to run itself without a 
            // timer block.
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
            Echo($"Method: {airlock.ProgramStage.Method} | Stage: {airlock.ProgramStage.MethodStage}");
            airlock.Run(this);
            // The main entry point of the script, invoked every time
            // one of the programmable block's Run actions are invoked,
            // or the script updates itself. The updateSource argument
            // describes where the update came from. Be aware that the
            // updateSource is a  bitfield  and might contain more than 
            // one update type.
            // 
            // The method itself is required, but the arguments above
            // can be removed if not needed.
        }

        public class Airlock
        {
            public Airlock(Program program, MyIni ini)
            {
                Config = new AirlockConfig(ini);
                Blocks = new AirlockBlocks(program, Config);
                ProgramStage = new ProgramStage
                {
                    Method = -1,
                    MethodStage = -1
                };
            }

            public AirlockConfig Config { get; set; }

            public AirlockBlocks Blocks { get; set; }
            public ProgramStage ProgramStage { get; set; }

            public void Run(Program program)
            {
                switch (ProgramStage.Method)
                {
                    //Init
                    case -1:
                        switch (ProgramStage.MethodStage)
                        {
                            case -1:
                                Blocks.OutsideDoor.Enabled = true;
                                Blocks.InsideDoor.Enabled = true;
                                Blocks.OutsideDoor.CloseDoor();
                                Blocks.InsideDoor.CloseDoor();
                                ProgramStage.MethodStage = 0;
                                break;
                            case 0:
                                if (Blocks.OutsideDoor.OpenRatio + Blocks.InsideDoor.OpenRatio == 0)
                                {
                                    ProgramStage.MethodStage = 1;
                                }
                                break;
                            case 1:
                                Blocks.InsideDoor.Enabled = false;
                                Blocks.OutsideDoor.Enabled = false;
                                Blocks.Vents.ForEach(x => x.Depressurize = true);
                                ProgramStage.Method = 0;
                                ProgramStage.MethodStage = 0;
                                break;
                        }
                        break;
                    //Detect
                    case 0:
                        switch (ProgramStage.MethodStage)
                        {
                            case 0:
                                if (Blocks.OutsideSensor.Detect().Value == true)
                                {
                                    ProgramStage.Method = 1;
                                    ProgramStage.MethodStage = 0;
                                }

                                if (Blocks.InsideSensor.Detect().Value == true)
                                {
                                    ProgramStage.Method = 2;
                                    ProgramStage.MethodStage = 0;
                                }
                                break;
                        }
                        break;
                    //Outside Detected
                    case 1:
                        switch (ProgramStage.MethodStage)
                        {
                            case 0:
                                Blocks.OutsideDoor.Enabled = true;
                                Blocks.OutsideDoor.OpenDoor();
                                ProgramStage.MethodStage = 1;
                                break;
                            case 1:
                                if (Blocks.OutsideDoor.OpenRatio == 1)
                                {
                                    ProgramStage.MethodStage = 2;
                                }
                                break;
                            case 2:
                                if (Blocks.InteriorSensor.Detect().Value == true)
                                {
                                    ProgramStage.MethodStage = 3;
                                }
                                break;
                            case 3:
                                Blocks.OutsideDoor.CloseDoor();
                                ProgramStage.MethodStage = 4;
                                break;
                            case 4:
                                if (Blocks.OutsideDoor.OpenRatio == 0)
                                {
                                    Blocks.OutsideDoor.Enabled = false;
                                    ProgramStage.MethodStage = 5;
                                }
                                break;
                            case 5:
                                Blocks.Vents.ForEach(x => x.Depressurize = false);
                                ProgramStage.MethodStage = 6;
                                break;
                            case 6:
                                if (Blocks.Vents.All(x => x.GetOxygenLevel() > .9f))
                                {
                                    ProgramStage.MethodStage = 7;
                                }
                                else
                                {
                                    Blocks.Vents.ForEach(x => program.Echo($"Oxygen Level: {x.GetOxygenLevel()}"));
                                }
                                break;
                            case 7:
                                Blocks.InsideDoor.Enabled = true;
                                Blocks.InsideDoor.OpenDoor();
                                ProgramStage.MethodStage = 8;
                                break;
                            case 8:
                                if (Blocks.InsideSensor.Detect().Value == false
                                    && Blocks.OutsideSensor.Detect().Value == false
                                    && Blocks.InteriorSensor.Detect().Value == false)
                                {
                                    ProgramStage.Method = -1;
                                    ProgramStage.MethodStage = -1;
                                }
                                break;
                        }
                        break;
                    //Inside Detected
                    case 2:
                        switch (ProgramStage.MethodStage)
                        {
                            case 0:
                                Blocks.Vents.ForEach(x => x.Depressurize = false);
                                ProgramStage.MethodStage = 1;
                                break;
                            case 1:
                                if (Blocks.Vents.All(x => x.GetOxygenLevel() > .9f))
                                {
                                    ProgramStage.MethodStage = 2;
                                }
                                break;
                            case 2:
                                Blocks.InsideDoor.Enabled = true;
                                Blocks.InsideDoor.OpenDoor();
                                ProgramStage.MethodStage = 3;
                                break;
                            case 3:
                                if (Blocks.InteriorSensor.Detect().Value == true)
                                {
                                    ProgramStage.MethodStage = 4;
                                }
                                break;
                            case 4:
                                Blocks.InsideDoor.CloseDoor();
                                ProgramStage.MethodStage = 5;
                                break;
                            case 5:
                                if (Blocks.InsideDoor.OpenRatio == 0)
                                {
                                    ProgramStage.MethodStage = 6;
                                }
                                break;
                            case 6:
                                Blocks.InsideDoor.Enabled = false;
                                Blocks.Vents.ForEach(x => x.Depressurize = true);
                                Blocks.OutsideVent.Depressurize = false;
                                ProgramStage.MethodStage = 7;
                                break;
                            case 7:
                                if (Blocks.Vents.All(x => x.GetOxygenLevel() < .1f))
                                {
                                    ProgramStage.MethodStage = 8;
                                }
                                break;
                            case 8:
                                Blocks.OutsideVent.Depressurize = true;
                                Blocks.OutsideDoor.Enabled = true;
                                Blocks.OutsideDoor.OpenDoor();
                                ProgramStage.MethodStage = 9;
                                break;
                            case 9:
                                if (Blocks.InsideSensor.Detect().Value == false
                                    && Blocks.OutsideSensor.Detect().Value == false
                                    && Blocks.InteriorSensor.Detect().Value == false)
                                {
                                    ProgramStage.Method = -1;
                                    ProgramStage.MethodStage = -1;
                                }
                                break;
                        }
                        break;
                }
            }
        }

        public class AirlockBlocks
        {
            public AirlockBlocks(Program program, AirlockConfig config)
            {
                ProgrammableBlock = program.Me;

                var allBlocks = new List<IMyTerminalBlock>();
                program.GridTerminalSystem.GetBlocks(allBlocks);

                allBlocks.ForEach(x =>
                {
                    var customName = x.CustomName;

                    if (customName.Contains(config.AirlockTag))
                    {
                        if (customName.Contains(config.DoorTag))
                        {
                            if (customName.Contains(config.OutsideTag))
                            {
                                OutsideDoor = x as IMyDoor;
                                return;
                            }

                            if (customName.Contains(config.InsideTag))
                            {
                                InsideDoor = x as IMyDoor;
                                return;
                            }
                        }

                        if (customName.Contains(config.VentTag))
                        {
                            if (customName.Contains(config.OutsideTag))
                            {
                                OutsideVent = x as IMyAirVent;
                                return;
                            }

                            Vents.Add(x as IMyAirVent);
                            return;
                        }

                        if (customName.Contains(config.SensorTag))
                        {
                            if (customName.Contains(config.InsideTag))
                            {
                                InsideSensor = new InsideSensor(x as IMySensorBlock);
                                return;
                            }

                            if (customName.Contains(config.OutsideTag))
                            {
                                OutsideSensor = new OutsideSensor(x as IMySensorBlock);
                                return;
                            }

                            if (customName.Contains(config.InterorTag))
                            {
                                InteriorSensor = new InteriorSensor(x as IMySensorBlock);
                                return;
                            }
                        }
                    }
                });
            }


            public IMyProgrammableBlock ProgrammableBlock { get; set; }
            public List<IMyAirVent> Vents { get; set; } = new List<IMyAirVent>();
            public IMyAirVent OutsideVent { get; set; }
            public IMyDoor InsideDoor { get; set; }
            public IMyDoor OutsideDoor { get; set; }
            public InsideSensor InsideSensor { get; set; }
            public OutsideSensor OutsideSensor { get; set; }
            public InteriorSensor InteriorSensor { get; set; }


        }

        public class Sensor
        {
            public Sensor(IMySensorBlock sensorBlock)
            {
                SensorBlock = sensorBlock;
            }

            IMySensorBlock SensorBlock { get; set; }

            public bool? Detect()
            {
                return SensorBlock != null ? SensorBlock.IsActive : false;
            }

            public virtual bool Trigger()
            {
                throw new Exception("Method not implemented");
            }
        }

        public class ProgramStage
        {
            public int Method { get; set; }
            public int MethodStage { get; set; }
        }

        public class InteriorSensor : Sensor
        {
            public InteriorSensor(IMySensorBlock sensorBlock) : base(sensorBlock)
            {
            }
        }

        public class InsideSensor : Sensor
        {
            public InsideSensor(IMySensorBlock sensorBlock) : base(sensorBlock)
            {
            }
        }

        public class OutsideSensor : Sensor
        {
            public OutsideSensor(IMySensorBlock sensorBlock) : base(sensorBlock)
            {
            }

            //public override bool Trigger(AirlockBlocks blocks)
            //{

            //}
        }

        public class AirlockConfig
        {
            public AirlockConfig(MyIni ini)
            {
                var at = ini.Get(Strings.AirlockConfig, "AirlockTag").ToString();
                var dt = ini.Get(Strings.AirlockConfig, "DoorTag").ToString();
                var vt = ini.Get(Strings.AirlockConfig, "VentTag").ToString();
                var st = ini.Get(Strings.AirlockConfig, "SensorTag").ToString();
                var ot = ini.Get(Strings.AirlockConfig, "OutsideTag").ToString();
                var it = ini.Get(Strings.AirlockConfig, "InsideTag").ToString();
                var itt = ini.Get(Strings.AirlockConfig, "InsideTag").ToString();

                AirlockTag = !string.IsNullOrEmpty(at) ? at : AirlockTag;
                DoorTag = !string.IsNullOrEmpty(dt) ? dt : DoorTag;
                VentTag = !string.IsNullOrEmpty(vt) ? vt : VentTag;
                SensorTag = !string.IsNullOrEmpty(st) ? st : SensorTag;
                OutsideTag = !string.IsNullOrEmpty(ot) ? ot : OutsideTag;
                InsideTag = !string.IsNullOrEmpty(it) ? it : InsideTag;
                InterorTag = !string.IsNullOrEmpty(itt) ? itt : InterorTag;

                if (new string[] { AirlockTag,
                    DoorTag, VentTag, SensorTag,
                    OutsideTag, InsideTag, InterorTag }.Any(x => string.IsNullOrEmpty(x)))
                {
                    throw new NullReferenceException("Config is incorrect");
                }
            }

            public string AirlockTag { get; set; } = "[AL]";
            public string DoorTag { get; set; } = "[DOOR]";
            public string VentTag { get; set; } = "[VENT]";
            public string SensorTag { get; set; } = "[SENS]";
            public string OutsideTag { get; set; } = "[OUT]";
            public string InsideTag { get; set; } = "[IN]";
            public string InterorTag { get; set; } = "[INT]";
        }

        internal static class Strings
        {
            public const string AirlockConfig = "airlock_config";
        }
    }
}
