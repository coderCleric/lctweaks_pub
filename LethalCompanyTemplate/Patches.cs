using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LCTweaks
{
    [HarmonyPatch]
    public static class Patches
    {
        //Config things
        public static bool toggleSprint = false;
        public static float dropAllDelay = 0.2f;

        //Flags necessary for certain patches to run
        public static bool sHeld = false;
        public static bool sNew = false;
        public static bool autoWalk = false;

        //Needed for item discarding
        public static float dropAllTime = -1;

        /**
         * Override standard input reading for the toggle sprint
         */
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(PlayerControllerB), "Update")]
        public static IEnumerable<CodeInstruction> OverrideSprintInput(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            //If the option is disabled, just return the base code
            if (!toggleSprint)
                return instructions;

            //First, load the list of instructions
            List<CodeInstruction> code = new List<CodeInstruction>(instructions);

            //Next, find where we need to insert
            int insertIndex = -1;
            Label nextConLabel = il.DefineLabel();
            object elseLabel = null;
            for (int i = 0; i < code.Count - 1; i++)
            {
                //Count it as a good spot when we recognize the 2 lines
                if (code[i].opcode == OpCodes.Ldc_R4 && (float)code[i].operand == 0.3f)
                {
                    LCTweaks.DebugLog("Found expected structure for toggle sprint transpiler");

                    //Save the index to insert into
                    insertIndex = i;

                    //Store the label for the else statement
                    elseLabel = code[i + 1].operand;

                    //Store the label for the next condition
                    code[i + 2].labels.Add(nextConLabel);
                    break;
                }
            }

            //Going to declare all of the labels here
            Label BLabel = il.DefineLabel();
            Label DLabel = il.DefineLabel();
            Label ELabel = il.DefineLabel();
            Label GLabel = il.DefineLabel();

            //Need to determine the "held" and "new" flags
            //Can just hijack first bit due to similarity, need to determine if key is pressed enough
            code[insertIndex + 1].opcode = OpCodes.Bge_Un_S;
            code[insertIndex + 1].operand = BLabel;

            List<CodeInstruction> insertion = new List<CodeInstruction>();

            //If it's not, set flags to false and go straight to the later bit
            insertion.Add(new CodeInstruction(OpCodes.Ldc_I4_0));
            insertion.Add(new CodeInstruction(OpCodes.Stsfld, AccessTools.Field(typeof(Patches), nameof(Patches.sHeld))));
            insertion.Add(new CodeInstruction(OpCodes.Ldc_I4_0));
            insertion.Add(new CodeInstruction(OpCodes.Stsfld, AccessTools.Field(typeof(Patches), nameof(Patches.sNew))));
            insertion.Add(new CodeInstruction(OpCodes.Br_S, ELabel));

            //If it is, check the value of "held"
            CodeInstruction tmp = new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(Patches), nameof(Patches.sHeld)));
            tmp.labels.Add(BLabel);
            insertion.Add(tmp);
            insertion.Add(new CodeInstruction(OpCodes.Brfalse_S, DLabel));

            //If "held" is true, set "new" to false and go to the later bit
            insertion.Add(new CodeInstruction(OpCodes.Ldc_I4_0));
            insertion.Add(new CodeInstruction(OpCodes.Stsfld, AccessTools.Field(typeof(Patches), nameof(Patches.sNew))));
            insertion.Add(new CodeInstruction(OpCodes.Br_S, ELabel));

            //If "held" is false, set both flags to true
            tmp = new CodeInstruction(OpCodes.Ldc_I4_1);
            tmp.labels.Add(DLabel);
            insertion.Add(tmp);
            insertion.Add(new CodeInstruction(OpCodes.Stsfld, AccessTools.Field(typeof(Patches), nameof(Patches.sHeld))));
            insertion.Add(new CodeInstruction(OpCodes.Ldc_I4_1));
            insertion.Add(new CodeInstruction(OpCodes.Stsfld, AccessTools.Field(typeof(Patches), nameof(Patches.sNew))));

            //This is the later bit, now we need to route based on "isSprinting" and "new"
            //First, split based on if we're sprinting
            tmp = new CodeInstruction(OpCodes.Ldarg_0);
            tmp.labels.Add(ELabel);
            insertion.Add(tmp);
            insertion.Add(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(PlayerControllerB), nameof(PlayerControllerB.isSprinting))));
            insertion.Add(new CodeInstruction(OpCodes.Brtrue_S, GLabel));

            //If we are not sprinting and the key is newly pressed, check remaining conditions
            insertion.Add(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(Patches), nameof(Patches.sNew))));
            insertion.Add(new CodeInstruction(OpCodes.Brtrue_S, nextConLabel));
            //If the key is not newly pressed, go to the else
            insertion.Add(new CodeInstruction(OpCodes.Br_S, elseLabel));

            //If we are sprinting and the key is newly pressed, go to the else
            tmp = new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(Patches), nameof(Patches.sNew)));
            tmp.labels.Add(GLabel);
            insertion.Add(tmp);
            insertion.Add(new CodeInstruction(OpCodes.Brtrue_S, elseLabel));
            //If the key is not newly pressed, check remaining conditions
            insertion.Add(new CodeInstruction(OpCodes.Br_S, nextConLabel));

            //Insert the code
            if (insertIndex != -1)
            {
                code.InsertRange(insertIndex + 2, insertion);
            }

            //Return
            return code;
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
        [HarmonyPatch(typeof(PlayerControllerB), "OnEnable")]
        public static void BindInput(PlayerControllerB __instance)
        {
            InputActionAsset actions = IngamePlayerSettings.Instance.playerInput.actions;

            //Bind the custom input for auto-walk
            LCTCustomInputs.instance.AutoWalkButton.performed += LCTCustomInputs.ToggleAutoWalk;

            //Bind the input for dropping everything
            actions.FindAction("Discard").performed += LCTCustomInputs.DiscardPerformed;
            actions.FindAction("Discard").canceled += LCTCustomInputs.DiscardCanceled;
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerControllerB), "OnDisable")]
        public static void UnbindInput()
        {
            InputActionAsset actions = IngamePlayerSettings.Instance.playerInput.actions;

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
            //Early return if this isn't the client
            if (__instance != LCTCustomInputs.clientPlayer)
                return;

            //If drop all time exceeds current time, drop everything
            if(dropAllTime >= 0 && Time.time > dropAllTime)
            {
                __instance.DropAllHeldItemsAndSync();
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
