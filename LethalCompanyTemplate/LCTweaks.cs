using BepInEx;
using HarmonyLib;
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
            Patches.toggleSprint = Config.Bind<bool>("Input",
                "ToggleSprint",
                false,
                "If true, pressing the sprint key will keep the player sprinting until they stop moving.").Value;
            Patches.dropAllDelay = Config.Bind<float>("Input",
                "DropAllDelay",
                1f,
                "How many seconds drop must be held to drop all items.\n" +
                "Must be greater than or equal to the drop scrap delay").Value;

            //Bound the config
            Patches.dropAllDelay = Mathf.Max(Patches.dropAllDelay, 0.1f);

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