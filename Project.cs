using BepInEx;
using UnityEngine;
using System;
using System.Linq;
using BepInEx.Logging;
using System.Runtime.CompilerServices;
using System.Runtime;
using HUD;
using RWCustom;
using System.Collections.Generic;
using System.Globalization;
using static MonoMod.InlineRT.MonoModRule;
using JetBrains.Annotations;
using Unity.Mathematics;
using HarmonyLib;

using Menu;
using Menu.Remix.MixedUI;
using Expedition;
using JollyCoop.JollyMenu;
using MSCSceneID = MoreSlugcats.MoreSlugcatsEnums.MenuSceneID;
using IL.JollyCoop.JollyManual;

using SlugBase;
using On.MoreSlugcats;
using RainMeadow;
using DevConsole;
using DevConsole.Commands;
using Menu.Remix;
using Menu.Remix.MixedUI.ValueTypes;
using On.Watcher;
using SBCameraScroll;
using ImprovedInput;

using Logger = UnityEngine.Logger;

// ReSharper disable SimplifyLinqExpressionUseAll

// ReSharper disable UseMethodAny.0

// ReSharper disable once CheckNamespace
namespace JollyBeyond
{
    public static class GeneralCWT
    {
        static ConditionalWeakTable<Player, Data> table = new ConditionalWeakTable<Player, Data>();
        public static Data GetCustomData(this Player self) => table.GetOrCreateValue(self);

        public class Data
        {
            // variables
            public int facingFocus = 0;
            public int startBuffer = 0;
        }
    }

    [BepInPlugin(MOD_ID, "JollyBeyond", "1.0.0")]
    internal class Plugin : BaseUnityPlugin
    {
        public const string MOD_ID = "nassoc.jollybeyond";

        // thank you alphappy for logging help too
        internal static BepInEx.Logging.ManualLogSource logger;
        internal static void Log(LogLevel loglevel, object msg) => logger.Log(loglevel, msg);

        internal static Plugin instance;
        public Plugin()
        {
            logger = Logger;
            instance = this;
        }

        public static PlayerKeybind LeftRotation;
        public static PlayerKeybind RightRotation;
        public static PlayerKeybind DebugKeybind;
        private bool weInitializedYet = false;
        public void OnEnable()
        {
            try
            {
                Logger.LogDebug("JollyBeyond Plugin loading...");
                //On.RainWorld.OnModsInit += Extras.WrapInit(LoadResources);

                if (!weInitializedYet)
                {
                    On.RainWorld.OnModsInit += RainWorldOnModsInitHook;
                    //On.RainWorld.PostModsInit += PostModsInitt;
                    //On.RainWorld.Update += UpdateDatSin;
                    On.Player.Update += PlayerUpdatery;
                    On.PlayerGraphics.DefaultFaceSprite_float_int += DizzyFaceStuff;
                    On.PlayerGraphics.DrawSprites += DizzyFaceStuff2;
                }

                weInitializedYet = true;
                Logger.LogDebug("JollyBeyond Plugin successfully loaded!");
            }
            catch (Exception e)
            {
                Logger.LogDebug("JollyBeyond Plugin came back with an error! It says:\n"+e.Message);
                Debug.LogException(e);
            }
            
            try
            {
                LeftRotation = PlayerKeybind.Register("jollybeyond:leftrotation", "Jolly Co-op: Beyond", "Left Rotation", KeyCode.B, KeyCode.JoystickButton3);
                RightRotation = PlayerKeybind.Register("jollybeyond:rightrotation", "Jolly Co-op: Beyond", "Right Rotation", KeyCode.N, KeyCode.JoystickButton4);
                DebugKeybind = PlayerKeybind.Register("jollybeyond:debugkeybind", "Jolly Co-op: Beyond", "Debug Keybind", KeyCode.Y, KeyCode.JoystickButton5);
            }
            catch (Exception e)
            {
                Logger.LogDebug("JollyBeyond Plugin came back with an error when loading the keybinds! It says:\n"+e.Message);
            }
        }

        private void UpdateDatSin(On.RainWorld.orig_Update orig, RainWorld self)
        {
            orig(self);
        }

        private void RainWorldOnModsInitHook(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);
            try
            {
                MachineConnector.SetRegisteredOI(Plugin.MOD_ID, JollyBeyondConfig.Instance);
            }
            catch (Exception e)
            {
                Logger.LogDebug("JollyBeyond Plugin came back with an error! It says:\n"+e.Message);
            }
        }

        private void PostModsInitt(On.RainWorld.orig_PostModsInit orig, RainWorld self)
        {
            orig(self);
        }

        private int debugThingy = 0;
        private bool debugThingyPressed = false;
        private void PlayerUpdatery(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);
            if (self.Input()[DebugKeybind])
            {
                //(self.graphicsModule as PlayerGraphics).head
                //self.bodyChunks[0].Rotation[0] += (self.Input()[RightRotation] ? 0.1f : 0.01f);
                if (!debugThingyPressed)
                {
                    debugThingy++;
                    debugThingy=debugThingy%9;
                    debugThingyPressed = true;
                }
            }
            else
            {
                debugThingyPressed = false;
            }
        }

        private int[][] headMapping = [
            [2,3,4,5,6,7,8,0,1],
            [2,2,2,1,2,3,4,5,6],
        ];
        private Vector2[][] faceMoveMapping = [
            [new Vector2(0f,0f)]
        ];
        private string DizzyFaceStuff(On.PlayerGraphics.orig_DefaultFaceSprite_float_int orig, PlayerGraphics self, float eyeScale, int imgIndex)
        {
            string origOrig = orig(self, eyeScale, imgIndex);
            if (self.player.GetCustomData().startBuffer > 4 && self.player.GetCustomData().startBuffer < 9)
            {
                if (self.player.Input()[RightRotation])
                {
                    //origOrig = orig(self, eyeScale, headMapping[(eyeScale >= 0f ? 0 : 1)][imgIndex]);//self.player.flipDirection < 0
                    //origOrig = orig(self, eyeScale, (imgIndex + (self.player.flipDirection >= 0 || self.player.Input()[RightRotation] ? 2 : 7)) % 9);
                    //origOrig = orig(self, eyeScale, headMapping[(eyeScale >= 0f ? 0 : 1)][(imgIndex+debugThingy)%9]);
                    //eyescale -1 and imgindex <7: eyescale becomes -1, imgindex+=2
                    //eyescale -1 and imgindex >6: eyescale becomes 1, imgindex=15-(imgindex)
                    //eyescale 1 and imgindex >1: eyescale becomes 1, imgindex-=2
                    //eyescale 1 and imgindex <2: eyescale becomes -1, imgindex=1-(imgindex)
                    /*if (eyeScale < 0)
                    {
                        origOrig = orig(self, (imgIndex<7 ? 1 : -1)*eyeScale, (imgIndex < 7 ? imgIndex+2 : 15-imgIndex));
                    }
                    else
                    {
                        origOrig = orig(self, (imgIndex>1 ? 1 : -1)*eyeScale, (imgIndex > 1 ? imgIndex-2 : 1-imgIndex));
                    }*/
                    //eyescale -1 and imgindex >1: eyescale becomes -1, imgindex-=2
                    //eyescale -1 and imgindex <2: eyescale becomes 1, imgindex=1-(imgindex)
                    //eyescale 1 and imgindex >4: eyescale becomes 1, imgindex-=2
                    //eyescale 1 and imgindex <5: eyescale becomes 1, imgindex=2
                    if (eyeScale < 0)
                    {
                        //origOrig = orig(self, (imgIndex>1 ? 1f : -1f)*eyeScale, (imgIndex >1 ? imgIndex-2 : 2-imgIndex));
                    }
                    else
                    {
                        //origOrig = orig(self, eyeScale, (imgIndex > 4 ? imgIndex-2 : 2));
                    }
                    origOrig = orig(self, 1, (eyeScale < 0 ? (imgIndex > 4 ? 0 : 1) : 2));
                    Logger.LogInfo("aaa " + origOrig + " b "+eyeScale);
                }
                if (self.player.Input()[RightRotation])
                {
                    //origOrig = orig(self, eyeScale, headMapping[(self.player.flipDirection < 0 ? 0 : 1)][imgIndex]);
                }
            }
            return origOrig;
        }

        private void DizzyFaceStuff2(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);
            Vector2 vector = Vector2.Lerp(self.drawPositions[0, 1], self.drawPositions[0, 0], 0.5f);
            Vector2 vector2 = Vector2.Lerp(self.drawPositions[1, 1], self.drawPositions[1, 0], 0.5f);
            Vector2 vector3 = Vector2.Lerp(self.head.lastPos, self.head.pos, 0.5f);
            float num3 = Custom.AimFromOneVectorToAnother(Vector2.Lerp(vector2, vector, 0.5f), vector3);
            int num4 = Mathf.RoundToInt(Mathf.Abs(num3 / 360f * 34f));
            if (self.player.sleepCurlUp > 0f)
            {
                num4 = 7;
                num4 = Custom.IntClamp((int)Mathf.Lerp((float)num4, 4f, self.player.sleepCurlUp), 0, 8);
            }
            bool eitherButtonPressed = false;
            if (self.player.Input()[LeftRotation])
            {
                eitherButtonPressed = true;
                if (self.player.GetCustomData().startBuffer > 4 && self.player.GetCustomData().startBuffer < 9)
                {
                    if (sLeaser.sprites[9].scaleX > 0)
                    {
                        sLeaser.sprites[9].scaleX *= -1;
                    }

                    sLeaser.sprites[3].rotation -= 45;
                    sLeaser.sprites[3].x -= 2;
                }
                else if (self.player.Input()[RightRotation])
                {
                    self.player.GetCustomData().startBuffer = 9;
                    self.player.mainBodyChunk.vel.y = -0.1f;
                }
            }
            if (self.player.Input()[RightRotation])
            {
                eitherButtonPressed = true;
                if (self.player.GetCustomData().startBuffer > 4 && self.player.GetCustomData().startBuffer < 9)
                {
                    if (sLeaser.sprites[9].scaleX < 0) // && Custom.IntClamp((int)Mathf.Lerp((float)num4, 1f, self.player.sleepCurlUp), 0, 8) < 2
                    {
                        sLeaser.sprites[9].scaleX *= -1;
                    }

                    sLeaser.sprites[3].rotation += 45;
                    sLeaser.sprites[3].x += 2;
                }
            }
            if (!eitherButtonPressed)
            {
                self.player.GetCustomData().startBuffer = 0;
            }
            else if (self.player.GetCustomData().startBuffer < 5)
            {
                self.player.GetCustomData().startBuffer++;
            }
        }

        // MARKER: Utils
        private void Log(object text)
        {
            Logger.LogDebug("[JollyBeyond] " + text);
        }
    }

    public class JollyBeyondConfig : OptionInterface
    {
        public static JollyBeyondConfig Instance { get; } = new JollyBeyondConfig();

        public static void RegisterOI()
        {
            if (MachineConnector.GetRegisteredOI(Plugin.MOD_ID) != Instance)
                MachineConnector.SetRegisteredOI(Plugin.MOD_ID, Instance);
        }

        public static Configurable<KeyCode> swapKeyCode = Instance.config.Bind("swapKeyCode", KeyCode.KeypadEnter,
            new ConfigurableInfo("The key to press in order to toggle the sin/cos swap. Default KeypadEnter.")
        );

        public override void Initialize()
        {
            Plugin.Log(LogLevel.Info, "[JollyBeyond] Attempting to initialize JollyBeyond's config...");//!
            base.Initialize();
            Tabs = [
                new OpTab(this, "Main Page"),
            ];

            Tabs[0].AddItems([
                new OpLabel(30f, 560f, "Ruined Math Config - Main Page", true),
                new OpLabel(10f, 500f, "Button To Swap Using"),
                new OpKeyBinder(swapKeyCode, new Vector2(200f, 500f), new Vector2(150f,30f)),
            ]);
            Plugin.Log(LogLevel.Info, "[JollyBeyond] RuinedMath's config successfully initialized!");
        }
    }
}