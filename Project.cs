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
            public float facingFocus = 0;
            public int startBuffer = 0;
            public int firstPressed = 0;
            public int canTailFlop = 0;
            public float lastFacingFocus = 0;
            
            // meadow compat
            public int myAssignedNum = -1;
            public int beenHoldingFlopFor = 0;
            
            // pup values
            public int pupHowLong = 0;
            public bool pupLeft = false;
            public bool pupDoesBoth = false;
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
                LeftRotation = PlayerKeybind.Register("jollybeyond:leftrotation", "Jolly Co-op: Beyond", "Left Rotation", KeyCode.G, KeyCode.JoystickButton3);
                RightRotation = PlayerKeybind.Register("jollybeyond:rightrotation", "Jolly Co-op: Beyond", "Right Rotation", KeyCode.H, KeyCode.JoystickButton4);
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
            if (Math.Abs(self.player.GetCustomData().facingFocus)>1.5f)//self.player.GetCustomData().startBuffer > 4 && self.player.GetCustomData().startBuffer < 9
            {
                if (self.player.GetCustomData().lastFacingFocus < 0f)//self.player.Input()[LeftRotation] && self.player.GetCustomData().firstPressed != 1
                {
                    bool june = self.player.GetCustomData().lastFacingFocus > -15f;
                    int wlue = AvgIntsIf(2, imgIndex, june);
                    origOrig = orig(self, -1, (eyeScale > 0 ? (imgIndex > 4 ? 0 : 1) : wlue));
                    //Logger.LogInfo("bbb " + origOrig + " b "+eyeScale);
                }
                if (self.player.GetCustomData().lastFacingFocus > 0f)//self.player.Input()[RightRotation] && self.player.GetCustomData().firstPressed != -1
                {
                    bool june = self.player.GetCustomData().lastFacingFocus < 15f;
                    int wlue = AvgIntsIf(2, imgIndex, june);
                    origOrig = orig(self, 1, (eyeScale < 0 ? (imgIndex > 4 ? 0 : 1) : wlue));
                    //Logger.LogInfo("aaa " + origOrig + " b "+eyeScale);
                }
            }
            return origOrig;
        }

        private float NumToPow(float num, float target)
        {
            return (float)Math.Pow(2, Math.Abs(num - target) / 20f);
        }

        private int cycleMadness = 0;
        private void DizzyFaceStuff2(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            // meadow compat
            if ((ModManager.ActiveMods.Any(x => x.id == "henpemaz_rainmeadow") && OnlineManager.lobby != null && !self.player.isNPC))// 
            {
                cycleMadness = (cycleMadness > 5 ? 0 : cycleMadness + 1);
                if (cycleMadness == 0 || true)
                {
                    /*OnlinePhysicalObject onlinePhysicalObject;
                    OnlinePhysicalObject.map.TryGetValue(self.player.abstractPhysicalObject, out onlinePhysicalObject);
                    foreach (OnlinePlayer onlinePlayer in OnlineManager.players)
                    {
                        if (!onlinePlayer.isMe)
                        {
                            onlinePlayer.InvokeOnceRPC(new OnlineEmote.OnlEmote(OnlineEmote.Emote), new object[]
                            {
                                onlinePhysicalObject,
                                (self.player.GetCustomData().startBuffer == 9 ? 0 : (self.player.Input()[LeftRotation] ? (self.player.Input()[RightRotation] ? 3 : 1) : (self.player.Input()[RightRotation] ? 2 : 0))),
                                false,
                            });
                        }
                    }*/
                    foreach (KeyValuePair<OnlinePlayer, OnlineEntity.EntityId> keyValuePair in OnlineManager.lobby.playerAvatars)
                    {
                        if (keyValuePair.Key == OnlineManager.mePlayer && ((keyValuePair.Value.FindEntity(false) as OnlinePhysicalObject).apo.realizedObject as Player).playerState.playerNumber == self.player.playerState.playerNumber)
                        {
                            foreach (OnlinePlayer onlinePlayer2 in OnlineManager.players)
                            {
                                if (!onlinePlayer2.isMe)
                                {
                                    onlinePlayer2.InvokeOnceRPC(new OnlineEmote.OnlEmote(OnlineEmote.Emote), new object[]
                                    {
                                        keyValuePair.Value.FindEntity(false) as OnlinePhysicalObject,
                                        (self.player.Input()[LeftRotation] ? (self.player.Input()[RightRotation] ? (self.player.GetCustomData().startBuffer != 9 || self.player.GetCustomData().beenHoldingFlopFor > 5 ? 0 : 3) : 1) : (self.player.Input()[RightRotation] ? 2 : 0)),
                                        false,
                                    });
                                }
                            }
                        }
                    }
                }
                Logger.Log(LogLevel.Info,"glue eat "+(self.player.GetCustomData().myAssignedNum));
                if (self.player.GetCustomData().myAssignedNum != -1)
                {
                    self.player.Input()[LeftRotation] = (self.player.GetCustomData().myAssignedNum % 2 == 1);
                    self.player.Input()[RightRotation] = (self.player.GetCustomData().myAssignedNum > 1);
                }
            }
            if (self.player.isNPC && (!ModManager.ActiveMods.Any(x => x.id == "henpemaz_rainmeadow") || OnlineManager.lobby == null))
            {
                if (self.player.abstractCreature.personality.energy / 50f + UnityEngine.Random.value > 0.99f)
                {
                    self.player.GetCustomData().pupHowLong = (int)Math.Round(UnityEngine.Random.value * 20f + 10f);
                    self.player.GetCustomData().pupLeft = (UnityEngine.Random.value < 0.5f);
                    self.player.GetCustomData().pupDoesBoth = (UnityEngine.Random.value < 0.1f+(self.player.abstractCreature.personality.bravery/10f));
                }
                if (self.player.GetCustomData().pupHowLong > 0)
                {
                    self.player.Input()[(self.player.GetCustomData().pupLeft ? LeftRotation : RightRotation)] = true;
                    if (self.player.GetCustomData().pupDoesBoth)
                    {
                        self.player.Input()[(self.player.GetCustomData().pupLeft ? RightRotation : LeftRotation)] = true;
                    }
                    self.player.GetCustomData().pupHowLong--;
                }
            }
            orig(self, sLeaser, rCam, timeStacker, camPos);
            //Logger.Log(LogLevel.Info,"flue eat "+self.player.isNPC+" "+(self.player.GetCustomData().myAssignedNum));
            if (self.player.GetCustomData().facingFocus != 0)
            {
                self.player.GetCustomData().lastFacingFocus = self.player.GetCustomData().facingFocus + 0;
            }

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
            bool stopThatTrain = false;
            int wasOld = 0;
            if (self.player.Input()[LeftRotation] && self.player.GetCustomData().firstPressed != 1)
            {
                eitherButtonPressed = true;
                if (self.player.GetCustomData().startBuffer > 4 && self.player.GetCustomData().startBuffer < 9)
                {
                    stopThatTrain = true;
                    if (!self.player.Input()[RightRotation])
                    {
                        self.player.GetCustomData().facingFocus = Math.Max(self.player.GetCustomData().facingFocus-NumToPow(self.player.GetCustomData().facingFocus,-20), -20);
                        self.player.GetCustomData().firstPressed = -1;
                    }
                    else
                    {
                        self.player.GetCustomData().facingFocus = Math.Max(self.player.GetCustomData().facingFocus-NumToPow(self.player.GetCustomData().facingFocus,-30), -30);
                    }
                    if (sLeaser.sprites[9].scaleX > 0)
                    {
                        wasOld = -1;
                        //sLeaser.sprites[9].scaleX *= -1;
                    }
                    //sLeaser.sprites[9].x += 1;
                }
                else if (self.player.Input()[RightRotation])
                {
                    self.player.GetCustomData().startBuffer = 9;
                    self.player.GetCustomData().beenHoldingFlopFor++;
                    if (self.player.GetCustomData().canTailFlop == 0)
                    {
                        self.player.GetCustomData().canTailFlop = 20;
                        for (int i=0;i<self.tail.Length;i++)
                        {
                            if (self.player.canJump == 0 || self.player.bodyMode == Player.BodyModeIndex.ClimbingOnBeam)
                            {
                                self.tail[i].vel.x = (self.player.mainBodyChunk.pos.x > self.tail[self.tail.Length-1].pos.x ? 6f : -6f);
                            }
                            else
                            {
                                self.tail[i].vel.y = 3f;//(self.player.canJump > 0 ? 3f : -3f);
                            }
                        }
                    }
                    //self.player.bodyChunks[0].vel.y = 0.1f;
                    //self.player.bodyChunks[1].vel.y = 0.1f;
                }
            }
            if (self.player.Input()[RightRotation] && self.player.GetCustomData().firstPressed != -1)
            {
                eitherButtonPressed = true;
                if (self.player.GetCustomData().startBuffer > 4 && self.player.GetCustomData().startBuffer < 9)
                {
                    stopThatTrain = true;
                    if (!self.player.Input()[LeftRotation])
                    {
                        self.player.GetCustomData().facingFocus = Math.Min(self.player.GetCustomData().facingFocus+NumToPow(self.player.GetCustomData().facingFocus,20), 20);
                        self.player.GetCustomData().firstPressed = 1;
                    }
                    else
                    {
                        self.player.GetCustomData().facingFocus = Math.Min(self.player.GetCustomData().facingFocus+NumToPow(self.player.GetCustomData().facingFocus,20), 30);
                    }
                    if (sLeaser.sprites[9].scaleX < 0) // && Custom.IntClamp((int)Mathf.Lerp((float)num4, 1f, self.player.sleepCurlUp), 0, 8) < 2
                    {
                        wasOld = 1;
                        //sLeaser.sprites[9].scaleX *= -1;
                    }
                    //sLeaser.sprites[9].x -= 1;
                }
            }
            if (!eitherButtonPressed)
            {
                self.player.GetCustomData().firstPressed = 0;
                self.player.GetCustomData().startBuffer = 0;
                self.player.GetCustomData().facingFocus = (self.player.GetCustomData().facingFocus > 0 ? Math.Max(self.player.GetCustomData().facingFocus - 1,0) : (self.player.GetCustomData().facingFocus < 0 ? Math.Min(self.player.GetCustomData().facingFocus + 1,0) : 0));
                self.player.GetCustomData().canTailFlop = Math.Max(0,self.player.GetCustomData().canTailFlop-1);
                self.player.GetCustomData().beenHoldingFlopFor = 0;
            }
            else if (self.player.GetCustomData().startBuffer < 5)
            {
                self.player.GetCustomData().startBuffer++;
            }
            else if (self.player.GetCustomData().startBuffer == 9 && false)
            {
                //Logger.Log(LogLevel.Info,"fuck dude "+(self.hands[0].absoluteHuntPos.x-self.player.mainBodyChunk.pos.x));
                //int whichHande = (self.hands[0].reachingForObject ? 1 : 0);
                //Vector2 vector22 = self.player.PointDir();
                //Vector2 vector22 = (self.hands[self.player.handPointing].absoluteHuntPos - self.player.mainBodyChunk.pos).normalized + self.player.mainBodyChunk.pos;
                //if (self.player.handPointing != -1) self.player.handPointing = 1;
                if (self.player.handPointing == 1)
                {
                    self.hands[1-self.player.handPointing].reachingForObject = true;
                    self.hands[1-self.player.handPointing].reachedSnapPosition = false;
                    //self.hands[1-self.player.handPointing].mode = Limb.Mode.HuntAbsolutePosition;
                    self.hands[1-self.player.handPointing].absoluteHuntPos = self.hands[self.player.handPointing].absoluteHuntPos+new Vector2(0,0);
                    self.hands[1-self.player.handPointing].absoluteHuntPos.x = self.player.mainBodyChunk.pos.x-(self.hands[1-self.player.handPointing].absoluteHuntPos.x-self.player.mainBodyChunk.pos.x)*0.9f;
                    //self.hands[1-self.player.handPointing].absoluteHuntPos = new Vector2(self.player.mainBodyChunk.pos.x - vector22.x * 100f, self.player.mainBodyChunk.pos.y + vector22.y * 100f);
                    //self.player.handPointing = 0;
                    //self.hands[1-self.player.handPointing].absoluteHuntPos =
                }
                else if (self.player.handPointing == 0)
                {
                    Vector2 vector22 = (self.hands[self.player.handPointing].absoluteHuntPos - self.player.mainBodyChunk.pos).normalized;

                    self.hands[1].reachingForObject = true;
                    //self.hands[1].reachedSnapPosition = false;
                    //self.hands[1].mode = Limb.Mode.HuntAbsolutePosition;
                    //Vector2 oldVector = self.hands[0].absoluteHuntPos+new Vector2(0,0);
                    //Vector2 newVector = self.hands[0].absoluteHuntPos+new Vector2(0,0);
                    //newVector.x = self.player.mainBodyChunk.pos.x-(newVector.x-self.player.mainBodyChunk.pos.x)*0.9f;
                    Vector2 newVector = new Vector2(self.player.mainBodyChunk.pos.x - vector22.x * 100f, self.player.mainBodyChunk.pos.y + vector22.y * 100f);
                    //self.hands[0].absoluteHuntPos = newVector;
                    //self.hands[0].absoluteHuntPos*=0.5f;
                    self.hands[1].absoluteHuntPos = newVector;
                }
            }
            if (stopThatTrain)
            {
                sLeaser.sprites[9].x = vector3.x - camPos.x;
                sLeaser.sprites[9].y = vector3.y - 2f - camPos.y;
            }
            sLeaser.sprites[3].rotation += 45*((self.player.GetCustomData().facingFocus)/20f);//*(self.player.GetCustomData().facingFocus<0 ? 0.5f : 1f)
            sLeaser.sprites[3].x += (Math.Abs(wasOld) < 2f ? 2 : 3)*(self.player.GetCustomData().facingFocus/20f);
            //if (wasOld != 0 && Math.Abs(wasOld) < 2f)
            //sLeaser.sprites[9].x -= wasOld;
            //sLeaser.sprites[9].scaleX *= -1;
            if (self.player.GetCustomData().facingFocus != 0)
            {
                if (self.player.GetCustomData().lastFacingFocus < 0 && sLeaser.sprites[9].scaleX > 0)
                {
                    sLeaser.sprites[9].x += 1;
                    sLeaser.sprites[9].scaleX = -Math.Abs(sLeaser.sprites[9].scaleX);
                }
                else if (self.player.GetCustomData().lastFacingFocus > 0 && sLeaser.sprites[9].scaleX < 0)
                {
                    //sLeaser.sprites[9].x -= 1;
                    sLeaser.sprites[9].scaleX = Math.Abs(sLeaser.sprites[9].scaleX);
                }
            }
        }

        // MARKER: Utils
        private void Log(object text)
        {
            Logger.LogDebug("[JollyBeyond] " + text);
        }

        private int AvgIntsIf(int a, int b, bool c)
        {
            return (c ? (a + b)/2 : a);
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
    
    public static class OnlineEmote
    {
        // Token: 0x06000004 RID: 4 RVA: 0x00002344 File Offset: 0x00000544
        [SoftRPCMethod]
        public static void Emote(OnlinePhysicalObject player, int whichButton, bool isLocal)
        {
            try
            {
                (player.apo.realizedObject as Player).GetCustomData().myAssignedNum = (isLocal ? -1 : whichButton);
            }
            catch (Exception)
            {
                
            }
        }

        // Token: 0x02000004 RID: 4
        // (Invoke) Token: 0x06000007 RID: 7
        public delegate void OnlEmote(OnlinePhysicalObject player, int whichButton, bool isLocal);
    }
}