using GameNetcodeStuff;
using LethalCompanyInputUtils.Api;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LCTweaks
{
    public class LCTCustomInputs : LcInputActions
    {
        public static LCTCustomInputs instance { get; private set; }
        public static PlayerControllerB clientPlayer = null;

        //When any instance is constructed, set it to be the instance
        public LCTCustomInputs() : base()
        {
            instance = this;
        }

        //The actual custom inputs
        [InputAction("<Keyboard>/c", Name = "Auto-Walk")]
        public InputAction AutoWalkButton { get; private set; }

        /**
         * When the player presses the auto-walk key, toggle the auto-walk bool
         */
        public static void ToggleAutoWalk(InputAction.CallbackContext context)
        {
            if(clientPlayer != null && clientPlayer.isPlayerControlled && !clientPlayer.isTypingChat && !clientPlayer.inTerminalMenu && !clientPlayer.quickMenuManager.isMenuOpen)
                Patches.autoWalk = !Patches.autoWalk;
        }

        /**
         * Set the time to discard things
         */
        public static void DiscardPerformed(InputAction.CallbackContext context)
        {
            Patches.dropAllTime = Time.time + LCTweaks.config.DropAllDelay.Value;
        }

        /**
         * Revert the time to discard things
         */
        public static void DiscardCanceled(InputAction.CallbackContext context)
        {
            LCTweaks.DebugLog("Drop canceled");
            Patches.dropAllTime = -1;
        }
    }
}
