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
        //config - you can edit this
        private const string MainTextPanelTag = "[CM]";
        private const string SmallTextPanel1Tag = "[CMSM01]";
        private const string SmallTextPanel2Tag = "[CMSM02]";
        private const string SmallTextPanel3Tag = "[CMSM03]";
        private const string SmallTextPanel4Tag = "[CMSM04]";
        private const string LockLightTag = "LockLight";
        private const string InputContainerTag = "[CMIN01]";
        private const string DepositLightTag = "DepositLight";
        private const string StorageContainerTag = "[CMST01]";
        private const string GameLightTag = "GameLight";
        private const int LowestBid = 10;

        public long CurrentTick = 0;
        public float HalfRotate = 3.15f;
        private readonly MyItemType CreditItemType = new MyItemType("MyObjectBuilder_PhysicalObject", "SpaceCredit");

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
            InitializeTextPanel(blocks.Screen.TextPanel);

            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }

        public class Blocks
        {
            public List<IMyTerminalBlock> AllBlocks { get; set; } = new List<IMyTerminalBlock>();
            public PlayingScreen Screen { get; set; } = new PlayingScreen();
            public IMyInteriorLight[] LockLights { get; set; } = new IMyInteriorLight[3];
            public IMyTextSurface[] SmallTextPanels { get; set; } = new IMyTextSurface[4];
            public IMyInventory InputContainer { get; set; }
            public IMyInteriorLight DepositLight { get; set; }
            public IMyInventory StorageContainer { get; set; }
            public IMyInteriorLight GameLight { get; set; }
        }

        public class ColumnDetail
        {
            public float PositionX { get; set; }
            public float PositionY { get; set; }
            public Vector2 Size { get; set; }
            public Color Color { get; set; }
        }

        public class Game {
            public bool HasStarted { get; set; }
            public long PlayerCredits { get; set; }
            public bool InputHasCredits { get; set; }
            public int InputCurrentCredits { get; set; }
            public bool DepositEnabled { get; set; }
        }

        public class PlayingScreen
        {
            public IMyTextSurface TextPanel { get; set; }
            public float X_Min { get; } = 50;
            public float Y_Min { get; } = 0;
            public int Columns { get; } = 3;
            public int Rows { get; } = 4;
            public int ChanceDivider { get; } = 2;
            public List<int> ColumnLock { get; set; }

            /// <summary>
            /// x
            /// y
            /// sizex 
            /// </summary>
            public List<ColumnDetail> ColumnDetails { get; set; } = new List<ColumnDetail>();
            public ScreenSprite[,] SpriteGrid = new ScreenSprite[3, 4];
            public Game Game { get; set; } = new Game();

            private float _xmax;
            private float _ymax;

            public float X_Max
            {
                get
                {
                    return _xmax;
                }
                private set
                {
                    _xmax = TextPanel?.SurfaceSize.X ?? 0;
                }
            }

            public float Y_Max
            {
                get
                {
                    return _ymax;
                }
                private set
                {
                    _ymax = TextPanel?.SurfaceSize.Y ?? 0;
                }
            }
        }

        public class ScreenSprite : ColumnDetail
        {
            public MySprite Sprite { get; set; }
            public string SpriteName { get; set; }
        }

        private readonly Blocks blocks;

        private void GetBlocks()
        {
            GridTerminalSystem.GetBlocksOfType(blocks.AllBlocks, b => b.IsSameConstructAs(Me));
            Echo($"Found {blocks.AllBlocks.Count} blocks");
            var textSurfaceBlock = blocks.AllBlocks.Find(x => x.CustomName.Contains(MainTextPanelTag));
            if (textSurfaceBlock != null)
            {
                var panel = textSurfaceBlock as IMyTextSurface;
                if (panel != null)
                {
                    blocks.Screen.TextPanel = panel;
                }
            }
            else
            {
                Echo("Panel not found");
            }

            var lockLights = blocks.AllBlocks.Where(x => x.CustomName.Contains(LockLightTag)).ToList();

            if (lockLights.Count > 0)
            {
                foreach (var i in lockLights)
                {
                    var num = i.CustomName.Last().ToString();
                    blocks.LockLights[Int32.Parse(num) - 1] = i as IMyInteriorLight;
                }
            }
            
            blocks.SmallTextPanels[0] = GetTextSurfaceByName(SmallTextPanel1Tag);
            blocks.SmallTextPanels[1] = GetTextSurfaceByName(SmallTextPanel2Tag);
            blocks.SmallTextPanels[2] = GetTextSurfaceByName(SmallTextPanel3Tag);
            blocks.SmallTextPanels[3] = GetTextSurfaceByName(SmallTextPanel4Tag);

            var inputContainer = blocks.AllBlocks.FirstOrDefault(x => x.CustomName.Contains(InputContainerTag));
            if (inputContainer != null)
            {                
                blocks.InputContainer = inputContainer.GetInventory(); ;
            }
            else
            {
                Echo("Input container not found!!");
            }

            var depositLight = blocks.AllBlocks.FirstOrDefault(x => x.CustomName.Contains(DepositLightTag));
            if (depositLight != null)
            {
                blocks.DepositLight = depositLight as IMyInteriorLight;
            }

            var storageContainer = blocks.AllBlocks.FirstOrDefault(x => x.CustomName.Contains(StorageContainerTag));
            if (storageContainer != null)
            {
                blocks.StorageContainer = storageContainer.GetInventory();
            }

            var gameLight = blocks.AllBlocks.FirstOrDefault(x => x.CustomName.Contains(GameLightTag));
            if (gameLight != null)
            {
                blocks.GameLight = gameLight as IMyInteriorLight;
            }
        }

        private IMyTextSurface GetTextSurfaceByName(string nameContains)
        {
            var b = blocks.AllBlocks.First(x => x.CustomName.Contains(nameContains));
            if (b != null)
            {
                var n = b as IMyTextSurface;
                if (n != null)
                {
                    return n;
                }
                else
                {
                    throw new Exception($"Conversion failure: {nameContains}");
                }
            }
            else
            {
                throw new Exception($"Block containing {nameContains} not found");
            }
        }

        private readonly Random rand = new Random();
        public List<string> AvailableSprites { get; set; }

        public void ToggleLockLights()
        {
            foreach (var light in blocks.LockLights)
            {
                light.Enabled = !light.Enabled;
            }
        }

        public void LockLightsOff()
        {
            foreach (var light in blocks.LockLights)
            {
                light.Enabled = false;
            }
        }

        public void LockLightsOn()
        {
            foreach (var light in blocks.LockLights)
            {
                light.Enabled = true;
            }
        }

        public void DepositLightSwitch(bool On, bool toggle = false)
        {
            if (toggle)
            {
                blocks.DepositLight.Enabled = !blocks.DepositLight.Enabled;
            }
            else
            {
                if (On)
                {
                    blocks.DepositLight.Enabled = true;
                }
                else
                {
                    blocks.DepositLight.Enabled = false;
                }
            }
        }

        public bool CheckDepositlight()
        {
            if (blocks.DepositLight != null)
            {
                blocks.Screen.Game.DepositEnabled =  blocks.DepositLight.Enabled;
            }
            else
            {
                Echo("Deposit Light is null");
            }

            return false;
        }

        private void DrawBlankColumns(IMyTextSurface panel, MySpriteDrawFrame frame)
        {
            panel.ContentType = ContentType.SCRIPT;
            panel.Script = string.Empty;
            panel.BackgroundColor = Color.Black;

            if (blocks.Screen.ColumnDetails.Count > 0)
            {
                foreach (var i in blocks.Screen.ColumnDetails)
                {
                    Echo("New column bg");
                    var spr = MySprite.CreateSprite(AvailableSprites[(int)Sprites.Block1], new Vector2(i.PositionX, i.PositionY), i.Size);
                    spr.Color = i.Color;
                    frame.Add(spr);
                }

            }
        }

        private bool BuyCredits()
        {
            bool success = false;
            var input = blocks.InputContainer;
            var storage = blocks.StorageContainer;

            if (input.CanTransferItemTo(storage, CreditItemType))
            {
                var items = new List<MyInventoryItem>();

                var item = input.FindItem(CreditItemType);
                var count = input.GetItemAmount(CreditItemType);

                if (item.HasValue) {
                    success = input.TransferItemTo(storage, item.Value, count);                   
                }
            }

            return success;   
        }

        private void ResetScreen(IMyTextSurface screen)
        {
            screen.ContentType = ContentType.SCRIPT;
            screen.Script = string.Empty;
            screen.BackgroundColor = Color.Black;
            screen.ScriptBackgroundColor = Color.Black;
        }

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

        //private bool WriteSpriteToSurface(IMyTextSurface screen, MySprite[] sprites,)
        //{
            
        //}

        private void DrawSmallTextPanels()
        {
            var screen1 = blocks.SmallTextPanels[0];
            var screen2 = blocks.SmallTextPanels[1];
            var screen3 = blocks.SmallTextPanels[2];
            var screen4 = blocks.SmallTextPanels[3];

            var colors = new Color[] { Color.Red, Color.Yellow, Color.Green };

            if (blocks.Screen.Game.HasStarted)
            {
                
            }
            else
            {
                if (blocks.Screen.Game.PlayerCredits > 0)
                {

                    if (screen3 != null)
                    {
                        ResetScreen(screen3);

                        using (var frame0 = screen3.DrawFrame())
                        {
                            var color = colors[rand.Next(0, colors.Length)];

                            var spr0 = MySprite.CreateText("CREDITS", "Debug", color, 4, TextAlignment.CENTER);
                            var spr2 = MySprite.CreateText(blocks.Screen.Game.PlayerCredits.ToString(), "Debug", color, 4, TextAlignment.CENTER);
                            spr0.Position = new Vector2(screen3.SurfaceSize.X / 2, (screen1.SurfaceSize.Y / 2) - 100);
                            spr2.Position = new Vector2(screen3.SurfaceSize.X / 2, (screen1.SurfaceSize.Y / 2) - 0);
                            var spr3 = MySprite.CreateText("GAME", "Debug", color, 4, TextAlignment.CENTER);
                            spr3.Position = new Vector2(screen3.SurfaceSize.X / 2, (screen1.SurfaceSize.Y / 2) - 200);
                            frame0.Add(spr3);
                            frame0.Add(spr0);
                            frame0.Add(spr2);
                        }
                    }

                    if (screen4 != null)
                    {
                        screen4.ContentType = ContentType.TEXT_AND_IMAGE;
                        screen4.Script = string.Empty;
                        screen4.BackgroundColor = Color.Black;
                        screen4.Font = "Debug";
                        screen4.FontSize = 3f;

                        if (blocks.Screen.Game.PlayerCredits > LowestBid)
                        {
                            screen4.WriteText("Press button\nbelow to\nstart\ngame");
                        }
                        else
                        {
                            screen4.WriteText($"Insert \n{LowestBid - blocks.Screen.Game.PlayerCredits} more\ncredits to\nstart\ngame");
                        }
                    }
                }
                else
                {
                    if (screen3 != null)
                    {
                        ResetScreen(screen3);

                        using (var frame0 = screen3.DrawFrame())
                        {
                            var color = colors[rand.Next(0, colors.Length)];

                            var spr0 = MySprite.CreateText("CREDITS", "Debug", color, 4, TextAlignment.CENTER);
                            var spr2 = MySprite.CreateText(blocks.Screen.Game.PlayerCredits.ToString(), "Debug", color, 4, TextAlignment.CENTER);
                            spr0.Position = new Vector2(screen3.SurfaceSize.X / 2, (screen1.SurfaceSize.Y / 2) - 100);
                            spr2.Position = new Vector2(screen3.SurfaceSize.X / 2, (screen1.SurfaceSize.Y / 2) - 0);
                            var spr3 = MySprite.CreateText("GAME", "Debug", color, 4, TextAlignment.CENTER);
                            spr3.Position = new Vector2(screen3.SurfaceSize.X / 2, (screen1.SurfaceSize.Y / 2) - 200);
                            frame0.Add(spr3);
                            frame0.Add(spr0);
                            frame0.Add(spr2);
                        }
                    }
                }


                if (blocks.Screen.Game.InputHasCredits)
                {
                    if (blocks.Screen.Game.DepositEnabled && blocks.Screen.Game.InputCurrentCredits > LowestBid)
                    {
                        BuyCredits();
                    }

                    if (screen1 != null)
                    {
                        ResetScreen(screen1);
                        

                        using (var frame0 = screen1.DrawFrame())
                        {
                            var color = colors[rand.Next(0, colors.Length)];

                            var spr0 = MySprite.CreateText("CREDITS", "Debug", color, 4, TextAlignment.CENTER);
                            var spr2 = MySprite.CreateText(blocks.Screen.Game.InputCurrentCredits.ToString(), "Debug", color, 4, TextAlignment.CENTER);
                            spr0.Position = new Vector2(screen1.SurfaceSize.X / 2, (screen1.SurfaceSize.Y / 2) - 100);
                            spr2.Position = new Vector2(screen1.SurfaceSize.X / 2, (screen1.SurfaceSize.Y / 2));
                            

                            frame0.Add(spr0);
                            frame0.Add(spr2);
                        }

                        if (screen2 != null)
                        {
                            screen2.ContentType = ContentType.TEXT_AND_IMAGE;
                            screen2.Script = string.Empty;
                            screen2.BackgroundColor = Color.Black;
                            screen2.Font = "Debug";
                            screen2.FontSize = 3f;

                            screen2.WriteText("Press button\nbelow to\npurchase\ngame credit");
                        }
                    }
                }
                else
                {
                    DepositLightSwitch(false);

                    if (screen1 != null)
                    {
                        ResetScreen(screen1);

                        using (var frame0 = screen1.DrawFrame())
                        {
                            var color = colors[rand.Next(0, colors.Length)];

                            var spr0 = MySprite.CreateText("INSERT", "Debug", color, 4, TextAlignment.CENTER);
                            var spr2 = MySprite.CreateText("CREDITS", "Debug", color, 4, TextAlignment.CENTER);
                            spr2.Position = new Vector2(screen1.SurfaceSize.X / 2, (screen1.SurfaceSize.Y / 2) + 100);
                            frame0.Add(spr0);
                            frame0.Add(spr2);
                        }
                    }

                    if (screen2 != null)
                    {
                        ResetScreen(screen2);

                        using (var frame1 = screen2.DrawFrame())
                        {
                            var color = colors[rand.Next(0, colors.Length)];
                            var spr1 = MySprite.CreateSprite(AvailableSprites[(int)Sprites.ArrowUp],
                                                                new Vector2(screen2.SurfaceSize.X / 2, screen2.SurfaceSize.Y / 2),
                                                                new Vector2(screen2.SurfaceSize.X, screen2.SurfaceSize.Y));
                            spr1.RotationOrScale = HalfRotate;
                            spr1.Color = color;
                            frame1.Add(spr1);
                        }
                    }
                }
                
            }
        }

        private void CheckInputForCredits()
        {
            var input = blocks.InputContainer;
            if (input != null)
            {
                List<MyInventoryItem> items = new List<MyInventoryItem>();
                var amount = input.GetItemAmount(new MyItemType("MyObjectBuilder_PhysicalObject", "SpaceCredit")).ToIntSafe();

                blocks.Screen.Game.InputCurrentCredits = amount;

                if (blocks.Screen.Game.HasStarted == false)
                {
                    if (amount > 0)
                    {
                        blocks.Screen.Game.InputHasCredits = true;
                        LockLightsOff();
                    }
                    else
                    {
                        blocks.Screen.Game.InputHasCredits = false;
                        LockLightsOn();
                    }
                }
            }
            else
            {
                blocks.Screen.Game.InputHasCredits = false;
                Echo("Input container is null");
            }
        }

        private void CheckGameCredits()
        {
            blocks.Screen.Game.PlayerCredits = blocks.StorageContainer.GetItemAmount(CreditItemType).ToIntSafe();
        }

        private void MoveTextPanelNext(IMyTextSurface panel)
        {
            //GetColumnLock();
            
            var grid = blocks.Screen.SpriteGrid;
            var newGrid = new ScreenSprite[blocks.Screen.Columns, blocks.Screen.Rows];

            using (var frame = panel.DrawFrame())
            {
                DrawBlankColumns(panel, frame);

                for (int i = 0; i < blocks.Screen.Columns; i++)
                {
                    for (int ii = 0; ii < blocks.Screen.Rows; ii++)
                    {
                        var now = grid[i, ii];
                        MySprite newSprite = new MySprite();
                        string chosenSprite = string.Empty;

                        if (blocks.Screen.ColumnLock.Contains(i)) // don't move next
                        {
                            Echo($"i:{i} | ii: {ii} is locked");
                            if (now?.Sprite != null)
                            {
                                chosenSprite = now.SpriteName;
                                newSprite = MySprite.CreateSprite(chosenSprite, new Vector2(now.PositionX, now.PositionY), now.Size);
                            }
                            else
                            {
                                Echo($"Now is null. i:{i} | ii: {ii}");
                            }
                        }
                        else //move next
                        {
                            if (ii > 0)
                            {
                                ScreenSprite prev;
                                try
                                {
                                    prev = blocks.Screen.SpriteGrid[i, ii - 1];

                                    newSprite = prev.Sprite;
                                    newSprite.Position = new Vector2(now.PositionX, now.PositionY);
                                    chosenSprite = prev.SpriteName;
                                    newSprite = MySprite.CreateSprite(chosenSprite, new Vector2(now.PositionX, now.PositionY), now.Size);
                                }
                                catch (Exception ex)
                                {
                                    Echo(ex.Message);
                                }
                            }
                            else
                            {
                                chosenSprite = AvailableSprites[rand.Next(0, AvailableSprites.Count / blocks.Screen.ChanceDivider)];
                                newSprite = MySprite.CreateSprite(chosenSprite, new Vector2(now.PositionX, now.PositionY), now.Size);
                            }
                        }

                        newGrid[i, ii] = new ScreenSprite()
                        {
                            PositionX = newSprite.Position.Value.X,
                            PositionY = newSprite.Position.Value.Y,
                            Size = newSprite.Size.Value,
                            Sprite = newSprite,
                            SpriteName = chosenSprite
                        };

                        frame.Add(newSprite);
                    }
                }
            }

            blocks.Screen.SpriteGrid = newGrid;
        }

        private void CheckColumnLock()
        {
            if (blocks.Screen.ColumnLock == null)
            {
                blocks.Screen.ColumnLock = new List<int>();
            }

            var index = 0;

            foreach (var lockLight in blocks.LockLights)
            {
                if (lockLight.Enabled)
                {
                    if (blocks.Screen.ColumnLock.Contains(index) == false)
                    {
                        blocks.Screen.ColumnLock.Add(index);
                    }
                }
                else
                {
                    if (blocks.Screen.ColumnLock.Contains(index))
                    {
                        blocks.Screen.ColumnLock.Remove(index);
                    }
                }

                index++;
            }            
        }

        private void CheckGameActive()
        {
            var game = blocks.Screen.Game;

            if (game.HasStarted)
            {
                if (blocks.GameLight.Enabled)
                {
                    game.HasStarted = true;
                }
                else
                {
                    game.HasStarted = false;
                }
            }
            else
            {
                if (game.PlayerCredits > LowestBid)
                {
                    if (blocks.GameLight.Enabled)
                    {
                        game.HasStarted = true;
                    }
                }
                else
                {
                    blocks.GameLight.Enabled = false;
                }
            }
        }

        private void InitializeTextPanel(IMyTextSurface panel)
        {
            panel.ContentType = ContentType.SCRIPT;
            panel.Script = string.Empty;
            panel.BackgroundColor = Color.Black;
            //panel.ClearImagesFromSelection();

            AvailableSprites = new List<string>();
            panel.GetSprites(AvailableSprites);

            //build columns
            var columnWidth = panel.SurfaceSize.X / blocks.Screen.Columns;
            var columnHeight = panel.SurfaceSize.Y;
            Color[] colors = { Color.Red, Color.Yellow, Color.Green };

            using (var frame = panel.DrawFrame())
            {
                for (int i = 0; i < blocks.Screen.Columns; i++)
                {
                    var detail = new ColumnDetail()
                    {
                        Color = colors[i],
                        PositionX = columnWidth * i + 85,
                        PositionY = 255,
                        Size = new Vector2(columnWidth, columnHeight)
                    };

                    var spr = MySprite.CreateSprite(AvailableSprites[(int)Sprites.Block1], new Vector2(detail.PositionX, detail.PositionY), detail.Size);
                    spr.Color = detail.Color;

                    blocks.Screen.ColumnDetails.Add(detail);
                    frame.Add(spr);

                    for (int ii = 0; ii < blocks.Screen.Rows; ii++)
                    {
                        var y = ((detail.Size.Y / blocks.Screen.Rows) * ii) + 60;
                        var x = detail.PositionX;
                        var size = new Vector2(detail.Size.X, detail.Size.Y / blocks.Screen.Rows);
                        var spriteName = AvailableSprites[rand.Next(0, AvailableSprites.Count)];
                        var sprite = new ScreenSprite()
                        {
                            PositionY = y,
                            PositionX = x,
                            Size = size,
                            Sprite = MySprite.CreateSprite(spriteName, new Vector2(x, y), size),
                            SpriteName = spriteName
                        };

                        blocks.Screen.SpriteGrid[i, ii] = sprite;
                        //var n = MySprite.CreateSprite(availableSprites[(int)sprite.Sprite], new Vector2(sprite.PositionX, sprite.PositionY), sprite.Size);
                        var n = MySprite.CreateSprite(AvailableSprites[rand.Next(0, AvailableSprites.Count)], new Vector2(sprite.PositionX, sprite.PositionY), sprite.Size);
                        frame.Add(n);
                    }
                }
            }
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
            try
            {
                if (CurrentTick % 2 == 0)
                {
                    CheckGameCredits();
                    CheckInputForCredits();
                    CheckGameActive();
                    CheckDepositlight();
                    CheckColumnLock();
                    MoveTextPanelNext(blocks.Screen.TextPanel);
                }
                else if (CurrentTick % 3 == 0)
                {
                    DrawSmallTextPanels();
                }                 
                
            }
            catch (Exception ex)
            {
                Echo("CAUGHT EXCEPTION");
                Echo(ex.Message);
            }

            // The main entry point of the script, invoked every time
            // one of the programmable block's Run actions are invoked,
            // or the script updates itself. The updateSource argument
            // describes where the update came from. Be aware that the
            // updateSource is a  bitfield  and might contain more than 
            // one update type.
            // 
            // The method itself is required, but the arguments above
            // can be removed if not needed.
            CurrentTick++;
        }

        public enum Sprites
        {
            Blank1 = 0,
            Blank2 = 1,
            ArrowUp = 2,
            Cross = 3,
            Warning = 4,
            Stop = 5,
            Maintenance = 6,
            Block1 = 7,
            Blank3 = 8,
            HUDLeft = 9,
            HUDRight = 10,
            Block2 = 11,
            Block3 = 12,
            Power = 13,
            H2O = 14,
            O2 = 15,
            PinkBlock1 = 16
        }
    }
}
