using BepInEx;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LCTweaks
{
    [BepInPlugin(MOD_GUID, MOD_NAME, MOD_VERSION)]
    [BepInDependency("com.rune580.LethalCompanyInputUtils")]
    public class LCTweaks : BaseUnityPlugin
    {
        //Plugin info
        public const string MOD_GUID = "coderCleric.LCTweaks";
        public const string MOD_NAME = "LCTweaks";
        public const string MOD_VERSION = "1.0.0";
        public int c = 0;

        //Other fields
        private static LCTweaks instance = null;


        private void Awake()
        {
            instance = this;

            //Do the config
            //Input
            Patches.toggleSprint = Config.Bind<bool>("Input",
                "ToggleSprint",
                false,
                "If true, pressing the sprint key will keep the player sprinting until they stop moving.").Value;
            Patches.dropAllDelay = Config.Bind<float>("Input",
                "DropAllDelay",
                1f,
                "How many seconds drop must be held to drop all items.\n" +
                "Must be greater than or equal to the drop scrap delay").Value;

            //Scanner
            Patches.maxScannables = Config.Bind<int>("Scanner",
                "MaxScannableObjects",
                13,
                "Sets how many items the scanner can pick up at once.\n" +
                "Must be at least 13.").Value;
            Patches.scanInShip = Config.Bind<bool>("Scanner",
                "ScanThroughWallsInShip",
                false,
                "If true, scrap in the ship can be scanned through walls.").Value;

            //Terminal
            Patches.terminalBoom = Config.Bind<bool>("Terminal",
                "TerminalMineExplosion",
                false,
                "If true, disabling mines from the terminal will trigger them to explode.").Value;
            Patches.muteNearTerm = Config.Bind<bool>("Terminal",
                "MuteNearTerminal",
                false,
                "Makes it so that eyeless dogs cannot hear noises that come from within 5 meters of the terminal.").Value;
            Patches.showHealthOnTerm = Config.Bind<bool>("Terminal",
                "ShowHealthOnRadar",
                false,
                "If true, player indicators on radar will change with health.").Value;

            //Bound the configs
            Patches.dropAllDelay = Mathf.Max(Patches.dropAllDelay, 0.1f);
            Patches.maxScannables = Math.Max(Patches.maxScannables, 13);

            //Make the input instance
            new LCTCustomInputs();

            //Make all of the patches
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());

            //Say a message once we finish
            Logger.LogInfo($"Plugin {MOD_GUID} is loaded!");
        }

        /**
         * Simple debug logging
         */
        public static void DebugLog(string msg)
        {
            instance.Logger.LogInfo(msg);
        }
    }
}