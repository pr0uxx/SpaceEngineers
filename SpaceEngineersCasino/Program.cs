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
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        public Program()
        {
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

            blocks = new Blocks();
            GetBlocks();
        }

        public class Blocks
        {
            public List<IMyTerminalBlock> AllBlocks { get; set; } = new List<IMyTerminalBlock>();
            public IMyTextSurface TextPanel { get; set; }
        }

        private readonly Blocks blocks;

        private void GetBlocks()
        {

            //GridTerminalSystem.GetBlocks(blocks.AllBlocks);
            GridTerminalSystem.GetBlocksOfType(blocks.AllBlocks, b => b.IsSameConstructAs(Me));
            Echo($"Found {blocks.AllBlocks.Count} blocks");
            var block = blocks.AllBlocks.Find(x => x.CustomName.Contains("[CM]"));
            if (block != null)
            {
                var panel = block as IMyTextPanel;

                if (panel != null)
                {
                    blocks.TextPanel = panel;

                    Echo($"Panel is {blocks.TextPanel.CustomName}");

                    InitializeTextPanel(blocks.TextPanel);
                }
            }
            else
            {
                Echo("Panel not found");
            }

        }

        private void InitializeTextPanel(IMyTextPanel panel)
        {
            panel.Font = "Monospace";
            Echo(panel.Font);
            panel.WritePublicText(".................");

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
    }
}
