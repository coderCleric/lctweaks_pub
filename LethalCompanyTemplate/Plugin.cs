using BepInEx;

namespace LethalCompanyTemplate
{
    [BepInPlugin(MOD_GUID, MOD_NAME, MOD_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        //Plugin info
        public const string MOD_GUID = "coderCleric.LCTweaks";
        public const string MOD_NAME = "LCTweaks";
        public const string MOD_VERSION = "1.0.0";


        private void Awake()
        {
            //Say a message once we finish
            Logger.LogInfo($"Plugin {MOD_GUID} is loaded!");
        }
    }
}