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
        public const string MOD_VERSION = "1.0.2";
        public int c = 0;

        //Other fields
        private static LCTweaks instance = null;
        public static LCTConfig config;


        private void Awake()
        {
            instance = this;

            //Make the config
            config = new LCTConfig(Config);

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