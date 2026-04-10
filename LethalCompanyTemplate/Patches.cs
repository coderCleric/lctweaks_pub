using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LCTweaks
{
    [HarmonyPatch]
    public static class Patches
    {
        //Flags necessary for certain patches to run
        public static bool sHeld = false;
        public static bool sNew = false;
        public static bool autoWalk = false;

        //Misc
        public static float dropAllTime = -1;
        private static Terminal term = null;
        private static bool bindingsBound = false;

        /**
         * Do tweaks on scan nodes
         */
        [HarmonyPrefix]
        [HarmonyPatch(typeof(HUDManager), "MeetsScanNodeRequirements")]
        public static void ScanThroughWallsInShip(ScanNodeProperties node)
        {
            //Error checks
            if (!LCTweaks.config.ScanInShip.Value || node.transform.parent == null ||
                node.transform.parent.gameObject.GetComponent<PhysicsProp>() == null)
                return;

            //Make items in the ship scannable through walls
            GrabbableObject item = node.transform.parent.gameObject.GetComponent<PhysicsProp>();
            if (item != null && item.isInShipRoom)
                node.requiresLineOfSight = false;
            else if (LCTweaks.config.ScanInShip.Value && item != null)
                node.requiresLineOfSight = true;
        }

        /**
         * Override how many objects can be scanned
         */
        [HarmonyPostfix]
        [HarmonyPatch(typeof(HUDManager), "Awake")]
        public static void OverridePingNumber(HUDManager __instance)
        {
            //Change how many things are hit by the spherecast
            __instance.scanNodesHit = new RaycastHit[LCTweaks.config.MaxScannables.Value];

            //Change the number of UI elements allocated for scanning
            int dif = LCTweaks.config.MaxScannables.Value - 13;
            if (dif > 0)
            {
                //First, change the size of the array
                RectTransform[] uiThings = new RectTransform[LCTweaks.config.MaxScannables.Value];

                //Next, copy existing elements
                for (int i = 0; i < __instance.scanElements.Length; i++)
                {
                    uiThings[i] = __instance.scanElements[i];
                }

                //Finally, copy actual objects and add them in
                Transform parent = uiThings[0].parent;
                for (int i = 13; i < LCTweaks.config.MaxScannables.Value; i++)
                {
                    uiThings[i] = GameObject.Instantiate(uiThings[0], parent);
                }

                //Copy it back
                __instance.scanElements = uiThings;
            }
        }

        /**
         * Make disabling mines blow them up
         */
        [HarmonyPrefix]
        [HarmonyPatch(typeof(TerminalAccessibleObject), nameof(TerminalAccessibleObject.CallFunctionFromTerminal))]
        public static bool ExplodeFromTerminal(TerminalAccessibleObject __instance)
        {
            //If it's not a landmine (or the option is disabled), return
            Landmine mine = __instance.gameObject.GetComponent<Landmine>();
            if (mine == null || !LCTweaks.config.TerminalBoom.Value)
                return true;

            //Otherwise, explode
            var method = typeof(Landmine).GetMethod("TriggerMineOnLocalClientByExiting", BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(mine, new object[] { });
            return false;
        }

        /**
         * Makes mines disappear from the terminal when they explode
         */
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Landmine), nameof(Landmine.Detonate))]
        public static void ClearMineFromTerm(Landmine __instance)
        {
            TerminalAccessibleObject termThing = __instance.gameObject.GetComponent<TerminalAccessibleObject>();
            Image boxImg = termThing.mapRadarBox;
            if (boxImg != null)
                boxImg.transform.parent.gameObject.SetActive(false);
        }

        /**
         * Register the terminal on awake so we can track it
         */
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Terminal), "Awake")]
        public static void GrabTerminal(Terminal __instance)
        {
            term = __instance;
        }

        /**
         * Make dogs not hear noises near the terminal
         * 
         * @param noisePosition The position that the noise came from
         */
        [HarmonyPrefix]
        [HarmonyPatch(typeof(MouthDogAI), nameof(MouthDogAI.DetectNoise))]
        public static bool MuteNearTerm(Vector3 noisePosition)
        {
            //Return true if we see no terminal
            if (term == null || !LCTweaks.config.MuteNearTerm.Value)
                return true;

            //If the position is within 5 meters of the terminal, cancel the detection
            bool ret = Vector3.Distance(noisePosition, term.transform.position) > 5f;
            if (!ret)
                LCTweaks.DebugLog("Dog had a noise detection event cancelled!");
            return ret;
        }

        /**
         * Attach the recoloring object to living players
         */
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerControllerB), "Awake")]
        public static void CreateRecolorComp(PlayerControllerB __instance)
        {
            //Attach the health color thing to the player
            if (LCTweaks.config.HealthOnTerm.Value)
            {
                DotColorController dot = __instance.transform.Find("Misc/MapDot").gameObject.AddComponent<DotColorController>();
                dot.player = __instance;
            }
        }

        /**
         * Attach the recoloring object to dead bodies
         */
        [HarmonyPostfix]
        [HarmonyPatch(typeof(DeadBodyInfo), "Start")]
        public static void CreateRecolorCompDead(DeadBodyInfo __instance)
        {
            if (LCTweaks.config.HealthOnTerm.Value)
            {
                DotColorController dot = __instance.transform.Find("MapDot").gameObject.AddComponent<DotColorController>();
            }
        }

        /**
         * Attach the recoloring object to masked
         */
        [HarmonyPostfix]
        [HarmonyPatch(typeof(MaskedPlayerEnemy), "Awake")]
        public static void CreateRecolorCompMasked(DeadBodyInfo __instance)
        {
            if (LCTweaks.config.HealthOnTerm.Value)
            {
                DotColorController dot = __instance.transform.Find("Misc/MapDot").gameObject.AddComponent<DotColorController>();
            }
        }

        /**
         * Override standard input reading for the toggle run
         * 
         * @param __instance The calling input action
         * @param __result The resulting value
         */
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(PlayerControllerB), "Update")]
        public static IEnumerable<CodeInstruction> OverrideWalkInput(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            //First, load the list of instructions
            List<CodeInstruction> code = new List<CodeInstruction>(instructions);

            //Find where we want to be inserting
            int insertIndex = -1;
            Label afterLabel = il.DefineLabel();
            for (int i = 0; i < code.Count; i++)
            {
                //Look for the signature of IL_043b
                if (code[i].opcode == OpCodes.Stfld && (FieldInfo)code[i].operand == AccessTools.Field(typeof(PlayerControllerB), nameof(PlayerControllerB.moveInputVector)))
                {
                    LCTweaks.DebugLog("Found expected structure for walk transpiler");
                    insertIndex = i + 1;
                    code[i + 1].labels.Add(afterLabel);
                    break;
                }
            }

            //Make the new code to inject
            List<CodeInstruction> insertion = new List<CodeInstruction>();
            insertion.Add(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(Patches), nameof(Patches.autoWalk))));
            insertion.Add(new CodeInstruction(OpCodes.Brfalse_S, afterLabel));

            insertion.Add(new CodeInstruction(OpCodes.Ldarg_0));
            insertion.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Vector2), "get_up")));
            insertion.Add(new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(PlayerControllerB), nameof(PlayerControllerB.moveInputVector))));

            //Insert the code
            if (insertIndex != -1)
            {
                code.InsertRange(insertIndex, insertion);
            }

            return code;
        }

        /**
         * When the player is enabled or disabled, handle the binding of input events
         */
        [HarmonyPostfix]
        //[HarmonyPatch(typeof(StartMatchLever), "StartGame")]
        [HarmonyPatch(typeof(StartOfRound), "Start")]
        public static void BindInput()
        {
            if (bindingsBound)
                return;
            bindingsBound = true;

            InputActionAsset actions = IngamePlayerSettings.Instance.playerInput.actions;
            LCTweaks.DebugLog("Binding controls");
            //Bind the custom input for auto-walk
            LCTCustomInputs.instance.AutoWalkButton.performed += LCTCustomInputs.ToggleAutoWalk;

            //Bind the input for dropping everything
            actions.FindAction("Discard").performed += LCTCustomInputs.DiscardPerformed;
            actions.FindAction("Discard").canceled += LCTCustomInputs.DiscardCanceled;
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameNetworkManager), "Disconnect")]
        public static void UnbindInput()
        {
            if (!bindingsBound)
                return;
            bindingsBound = false;

            InputActionAsset actions = IngamePlayerSettings.Instance.playerInput.actions;
            LCTweaks.DebugLog("Unbinding controls");

            //Unbind the custom input for auto-walk
            LCTCustomInputs.instance.AutoWalkButton.performed -= LCTCustomInputs.ToggleAutoWalk;

            //Unbind the input for dropping everything
            actions.FindAction("Discard").performed -= LCTCustomInputs.DiscardPerformed;
            actions.FindAction("Discard").canceled -= LCTCustomInputs.DiscardCanceled;
        }

        /**
         * When assigned, tell the custom input what object is the local player
         */
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerControllerB), "ConnectClientToPlayerObject")]
        public static void DetectClientPlayer(PlayerControllerB __instance)
        {
            LCTCustomInputs.clientPlayer = __instance;
            LCTweaks.DebugLog("Found the client player!");
        }

        /**
         * In player update, manage discarding of items
         */
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerControllerB), "Update")]
        public static void DiscardAllItems(PlayerControllerB __instance)
        {
            //The coroutine that will run (I don't entirely know why this is necessary, but I'm hoping it helps stuff)
            IEnumerator DiscardAtEndOfFrame()
            {
                yield return new WaitForEndOfFrame();
                __instance.DropAllHeldItemsAndSync(__instance.transform.position, __instance.localItemHolder.position, 
                    __instance.localItemHolder.eulerAngles, __instance.playerEye.transform.position, __instance.playerEye.transform.eulerAngles);
            }

            //Early return if this isn't the client
            if (__instance != LCTCustomInputs.clientPlayer)
                return;

            //If drop all time exceeds current time, drop everything
            if(dropAllTime >= 0 && Time.time > dropAllTime)
            {
                __instance.StartCoroutine(DiscardAtEndOfFrame());
                dropAllTime = -1;
            }
        }

        /**
         * No swapping items while holding to drop
         */
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerControllerB), "ScrollMouse_performed")]
        public static bool CancelItemSwitch()
        {
            return dropAllTime < 0;
        }
    }
}
