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

        private Walker MachineWalker { get; set; }

        MyIni _ini = new MyIni();

        public Program()
        {
            MyIniParseResult result;
            if (!_ini.TryParse(Me.CustomData, out result))
                throw new Exception(result.ToString());

            var walkerConfig = new WalkerConfig
            {
                HorizontalPistonTag = _ini.Get("walker_config", "HorizontalPistonTag").ToString(),
                LcdOutputTag = _ini.Get("walker_config", "LcdOutputTag").ToString(),
                VerticalPistonTag = _ini.Get("walker_config", "VerticalPistonTag").ToString(),
                FrontTag = _ini.Get("walker_config", "FrontTag").ToString(),
                RearTag = _ini.Get("walker_config", "RearTag").ToString(),
                LeftTag = _ini.Get("walker_config", "LeftTag").ToString(),
                RightTag = _ini.Get("walker_config", "RightTag").ToString(),
                LandingGearTag = _ini.Get("walker_config", "LandingGearTag").ToString()
            };

            var blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType(blocks, b => b.IsSameConstructAs(Me));

            try
            {
                MachineWalker = new Walker(blocks, Me, walkerConfig, this);
            }
            catch (Exception e)
            {
                Echo(e.Message);
            }


            Runtime.UpdateFrequency = UpdateFrequency.Update10;
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
            MachineWalker.Walk(this);
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

        public class Walker
        {
            public Walker(List<IMyTerminalBlock> blocks, IMyProgrammableBlock me, WalkerConfig config, Program program)
            {
                Blocks = blocks;
                //Me = me;
                WalkerConfig = config;

                var error = Initialize(program);

                if (!string.IsNullOrEmpty(error))
                {
                    throw new Exception(error);
                }

            }

            private string Initialize(Program program)
            {
                var error = string.Empty;
                program.Echo("Initializing");
                program.Echo("Getting output lcd");
                if (this.OutputLcd == null)
                {
                    OutputLcd = Blocks.FirstOrDefault(x => x.CustomName.Contains(WalkerConfig.LcdOutputTag)) as IMyTextSurface;
                    WriteTextToSurface(OutputLcd, new[] { "Initializing..." });
                }

                program.Echo("Assigning leg array objects");

                if (LegArray.Legs[0, 0] == null)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        for (int ii = 0; ii < 2; ii++)
                        {
                            LegArray.Legs[i, ii] = new Leg();
                        }
                    }
                }

                if (LegArray != null)
                {
                    program.Echo("Assigning landing gear params");

                    var frontLeftLgId = new[] { WalkerConfig.FrontTag, WalkerConfig.LandingGearTag, WalkerConfig.LeftTag };
                    var frontRightLgId = new[] { WalkerConfig.FrontTag, WalkerConfig.LandingGearTag, WalkerConfig.RightTag };
                    var rearLeftLgId = new[] { WalkerConfig.RearTag, WalkerConfig.LandingGearTag, WalkerConfig.LeftTag };
                    var rearRightLgId = new[] { WalkerConfig.RearTag, WalkerConfig.LandingGearTag, WalkerConfig.RightTag };

                    program.Echo("Assigning vertical piston params");

                    var flVertPis = new[] { WalkerConfig.VerticalPistonTag, WalkerConfig.FrontTag, WalkerConfig.LeftTag };
                    var frVertPis = new[] { WalkerConfig.VerticalPistonTag, WalkerConfig.FrontTag, WalkerConfig.RightTag };
                    var rlVertPis = new[] { WalkerConfig.VerticalPistonTag, WalkerConfig.RearTag, WalkerConfig.LeftTag };
                    var rrVertPis = new[] { WalkerConfig.VerticalPistonTag, WalkerConfig.RearTag, WalkerConfig.RightTag };

                    program.Echo("Assigning horizontal piston params");

                    var flHorzPis = new[] { WalkerConfig.HorizontalPistonTag, WalkerConfig.FrontTag, WalkerConfig.LeftTag };
                    var frHorzPis = new[] { WalkerConfig.HorizontalPistonTag, WalkerConfig.FrontTag, WalkerConfig.RightTag };
                    var rlHorzPis = new[] { WalkerConfig.HorizontalPistonTag, WalkerConfig.RearTag, WalkerConfig.LeftTag };
                    var rrHorzPis = new[] { WalkerConfig.HorizontalPistonTag, WalkerConfig.RearTag, WalkerConfig.RightTag };

                    string name;

                    program.Echo("Assigning blocks");

                    foreach (var block in Blocks)
                    {
                        name = block.CustomName;
                        program.Echo($"Checking {name}");

                        if (LegArray != null)
                        {
                            if (frontLeftLgId.All(x => name.Contains(x)))
                            {
                                LegArray.Legs[0, 0].LandingGears.Add(block as IMyLandingGear);
                                continue;
                            }

                            if (frontRightLgId.All(x => name.Contains(x)))
                            {
                                LegArray.Legs[0, 1].LandingGears.Add(block as IMyLandingGear);
                                continue;
                            }

                            if (rearLeftLgId.All(x => name.Contains(x)))
                            {
                                LegArray.Legs[1, 0].LandingGears.Add(block as IMyLandingGear);
                                continue;
                            }

                            if (rearRightLgId.All(x => name.Contains(x)))
                            {
                                LegArray.Legs[1, 0].LandingGears.Add(block as IMyLandingGear);
                                continue;
                            }

                            if (flVertPis.All(x => name.Contains(x)))
                            {
                                LegArray.Legs[0, 0].VerticalPistons.Add(block as IMyPistonBase);
                                continue;
                            }

                            if (frVertPis.All(x => name.Contains(x)))
                            {
                                LegArray.Legs[0, 1].VerticalPistons.Add(block as IMyPistonBase);
                                continue;
                            }

                            if (rlVertPis.All(x => name.Contains(x)))
                            {
                                LegArray.Legs[1, 0].VerticalPistons.Add(block as IMyPistonBase);
                                continue;
                            }

                            if (rrVertPis.All(x => name.Contains(x)))
                            {
                                LegArray.Legs[1, 1].VerticalPistons.Add(block as IMyPistonBase);
                                continue;
                            }

                            if (flHorzPis.All(x => name.Contains(x)))
                            {
                                LegArray.Legs[0, 0].HorizontalPiston = block as IMyPistonBase;
                                continue;
                            }

                            if (frHorzPis.All(x => name.Contains(x)))
                            {
                                LegArray.Legs[0, 1].HorizontalPiston = block as IMyPistonBase;
                                continue;
                            }

                            if (rlHorzPis.All(x => name.Contains(x)))
                            {
                                LegArray.Legs[1, 0].HorizontalPiston = block as IMyPistonBase;
                                continue;
                            }

                            if (rrHorzPis.All(x => name.Contains(x)))
                            {
                                LegArray.Legs[1, 1].HorizontalPiston = block as IMyPistonBase;
                                continue;
                            }
                        }
                    }


                    var totalLandingGears = 0;
                    var totalLegs = 0;

                    foreach (var leg in LegArray.Legs)
                    {
                        if (leg != null)
                        {
                            totalLegs += 1;
                            totalLandingGears += leg.LandingGears.Count;
                        }

                    }

                    WriteTextToSurface(OutputLcd, new[] { $"Total Landing Gears: {totalLandingGears}", $"Total Legs: {totalLegs}" }, 1f);
                }

                return error;
            }
            
            public int CommandStage = -3;

            public void Walk(Program program)
            {
                program.Echo(CommandStage.ToString());
                var legs = new Dictionary<int, Leg>
                {
                    { 0, LegArray.Legs[0, 0]},
                    { 1,  LegArray.Legs[0, 1]},
                    { 2,  LegArray.Legs[1, 0]},
                    { 3,  LegArray.Legs[1, 1]}
                };

                Leg legA = LegArray.Legs[0, 0];
                Leg legB = LegArray.Legs[1, 1];
                Leg legC = LegArray.Legs[0, 1];
                Leg legD = LegArray.Legs[1, 0];

                Move(legA, legB, legC, legD, legs.Values.ToList());

            }

            public void Move(Leg legA, Leg legB, Leg legC, Leg legD, List<Leg> legs)
            {
                
                switch (CommandStage)
                {
                    case -3:
                        legA.HorizontalExtend();
                        legC.HorizontalExtend();

                        legB.HorizontalRetract();
                        legD.HorizontalRetract();

                        if (legs.All(x => x.CommandComplete = true))
                        {
                            CommandStage = -2;
                            legs.ForEach(x => x.CommandComplete = false);
                        }
                        break;
                    case -2:
                        legs.ForEach(x => x.VerticalExtend());

                        if (legs.All(x => x.CommandComplete = true))
                        {
                            legs.ForEach(x => x.CommandComplete = false);
                            CommandStage = -1;
                        }
                        break;
                    case -1:
                        legs.ForEach(x => x.LockLandingGears());

                        if (legs.All(x => x.CommandComplete = true))
                        {
                            legs.ForEach(x => x.CommandComplete = false);
                            CommandStage = 0;
                        }
                        break;
                    case 0:
                        legB.UnlockLandingGears();
                        legD.UnlockLandingGears();

                        if (legB.CommandComplete && legD.CommandComplete)
                        {
                            legs.ForEach(x => x.CommandComplete = false);
                            CommandStage = 1;
                        }
                        break;
                    case 1:
                        legB.VerticalRetract();
                        legD.VerticalRetract();
                        if (legB.CommandComplete && legD.CommandComplete)
                        {
                            legs.ForEach(x => x.CommandComplete = false);
                            CommandStage = 2;
                        }
                        break;
                    case 2:
                        legB.HorizontalRetract();
                        legD.HorizontalRetract();
                        legA.HorizontalRetract();
                        legC.HorizontalRetract();

                        if (legs.All(x => x.CommandComplete))
                        {
                            legs.ForEach(x => x.CommandComplete = false);
                            CommandStage = 3;
                        }

                        break;
                    case 3:
                        legB.VerticalExtend();
                        legD.VerticalExtend();

                        if (legB.CommandComplete && legD.CommandComplete)
                        {
                            legs.ForEach(x => x.CommandComplete = false);
                            CommandStage = 4;
                        }
                        break;
                    case 4:
                        legB.LockLandingGears();
                        legD.LockLandingGears();
                        if (legB.CommandComplete && legD.CommandComplete)
                        {
                            legs.ForEach(x => x.CommandComplete = false);
                            CommandStage = 5;
                        }
                        break;
                    case 5:
                        legA.UnlockLandingGears();
                        legC.UnlockLandingGears();

                        if (legA.CommandComplete && legC.CommandComplete)
                        {
                            legs.ForEach(x => x.CommandComplete = false);
                            CommandStage = 6;
                        }

                        break;
                    case 6:
                        legA.VerticalRetract();
                        legC.VerticalRetract();

                        if (legA.CommandComplete && legC.CommandComplete)
                        {
                            legs.ForEach(x => x.CommandComplete = false);
                            CommandStage = 7;
                        }

                        break;
                    case 7:
                        legs.ForEach(x => x.HorizontalExtend());

                        if (legs.All(x => x.CommandComplete))
                        {
                            legs.ForEach(x => x.CommandComplete = false);
                            CommandStage = 0;
                        }

                        break;
                }
            }


            public IMyTextSurface OutputLcd { get; set; }
            public WalkerConfig WalkerConfig { get; set; }
            public List<IMyTerminalBlock> Blocks { get; set; }
            public IMyProgrammableBlock Me { get; set; }
            public LegArray LegArray { get; set; } = new LegArray();

            private void WriteTextToSurface(IMyTextSurface screen, string[] messages, float fontSize = 3f, string font = "Debug", Color? bgColor = null, bool append = false)
            {
                screen.ContentType = ContentType.TEXT_AND_IMAGE;
                screen.Script = string.Empty;
                screen.BackgroundColor = bgColor ?? Color.Black;
                screen.Font = font;
                screen.FontSize = fontSize;

                int count = 0;

                foreach (var m in messages)
                {
                    if (count > 0)
                    {
                        append = true;
                    }
                    screen.WriteText(m, append);
                    count += 1;
                }
            }
        }

        public class WalkerConfig
        {
            public string LcdOutputTag { get; set; }
            public string HorizontalPistonTag { get; set; }
            public string VerticalPistonTag { get; set; }
            public string FrontTag { get; set; }
            public string RearTag { get; set; }
            public string LeftTag { get; set; }
            public string RightTag { get; set; }
            public string LandingGearTag { get; set; }
        }

        public class RuntimeVariables
        {
            public int MaxHeight { get; set; }
        }

        public class LegArray
        {
            public Leg[,] Legs { get; set; } = new Leg[2, 2];
        }

        public class Leg
        {
            public IMyPistonBase HorizontalPiston { get; set; }
            public List<IMyPistonBase> VerticalPistons { get; set; } = new List<IMyPistonBase>();
            public List<IMyLandingGear> LandingGears { get; set; } = new List<IMyLandingGear>();

            public DateTime WaitUntil { get; set; }

            private int _stuckCount;
            private float _lastVertPos = 0;
            public LegCommand CurrentCommand;
            public bool CommandComplete;

            private readonly float _tolerance = 0.0001f;

            public void HorizontalExtend()
            {
                HorizontalPiston.Extend();
                CurrentCommand = LegCommand.HorizontalExtend;

                if (Normalise(Math.Abs(HorizontalPiston.MaxLimit - HorizontalPiston.CurrentPosition)) < _tolerance)
                {
                    CurrentCommand = LegCommand.Done;
                    CommandComplete = true;
                }
            }

            public void VerticalExtend()
            {
                CurrentCommand = LegCommand.VerticalExtend;
                VerticalPistons.ForEach(x => x.Extend());

                if (VerticalPistons.Any(x => Normalise(Math.Abs(x.CurrentPosition - _lastVertPos)) < _tolerance) )
                {
                    _stuckCount += 1;

                    if (_stuckCount > 100)
                    {
                        CurrentCommand = LegCommand.Done;
                        CommandComplete = true;
                        _stuckCount = 0;
                        _lastVertPos = 0;
                    }
                }
                else
                {
                    _stuckCount = 0;
                }

                _lastVertPos = VerticalPistons.FirstOrDefault()?.CurrentPosition ?? 0;
            }

            public void HorizontalRetract()
            {
                HorizontalPiston.Retract();
                CurrentCommand = LegCommand.HorizontalRetract;

                if (Normalise(Math.Abs(HorizontalPiston.CurrentPosition - HorizontalPiston.MinLimit)) < _tolerance)
                {
                    CurrentCommand = LegCommand.Done;
                    CommandComplete = true;
                }
            }

            public static float Normalise(float f)
            {
                if (f < 0)
                {
                    f = 0 - f;
                }

                return f;
            }

            public void VerticalRetract()
            {
                CurrentCommand = LegCommand.VerticalRetract;
                VerticalPistons.ForEach(x => x.Retract());

                if (VerticalPistons.Any(x => Normalise(Math.Abs(x.CurrentPosition - _lastVertPos)) < _tolerance))
                {
                    _stuckCount += 1;

                    if (_stuckCount > 100)
                    {
                        CurrentCommand = LegCommand.Done;
                        CommandComplete = true;
                        _stuckCount = 0;
                        _lastVertPos = 0;
                    }
                }
                else
                {
                    _stuckCount = 0;
                }

                _lastVertPos = VerticalPistons.FirstOrDefault()?.CurrentPosition ?? 0;
            }

            public void LockLandingGears()
            {
                LandingGears.ForEach(x => x.Lock());

                if (LandingGears.All(x => x.IsLocked))
                {
                    CommandComplete = true;
                }
                
                if (LandingGears.All(x => x.IsLocked == false))
                {
                    LandingGears.ForEach(x => x.ToggleLock());
                }
            }

            public void UnlockLandingGears()
            {
                LandingGears.ForEach(x => x.Unlock());

                if (LandingGears.All(x => x.IsLocked == false))
                {
                    CommandComplete = true;
                }
                
                if (LandingGears.All(x => x.IsLocked == true))
                {
                    LandingGears.ForEach(x => x.ToggleLock());
                }
            }

        }

        public enum LegCommand
        {
            None = 0,
            HorizontalExtend = 1,
            VerticalExtend = 2,
            HorizontalRetract = 3,
            VerticalRetract = 4,
            Done = 5
        }
    }


}
