using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using MelonLoader;
using HarmonyLib;
using ABI_RC.Core.Savior;

using CustomTriggerThreshold;

[assembly: MelonInfo(typeof(CustomTriggerThresholdMod), "CustomTriggerThreshold", "1.0.0", "DragonPlayer", "https://github.com/DragonPlayerX/CustomTriggerThreshold")]
[assembly: MelonGame("Alpha Blend Interactive", "ChilloutVR")]

namespace CustomTriggerThreshold
{
    public class CustomTriggerThresholdMod : MelonMod
    {
        public static readonly string Version = "1.0.0";

        public static CustomTriggerThresholdMod Instance;
        public static MelonLogger.Instance Logger => Instance.LoggerInstance;

        public static MelonPreferences_Category Category;
        public static MelonPreferences_Entry<float> SnapTurnThreshold;
        public static MelonPreferences_Entry<float> TriggerThreshold;
        public static MelonPreferences_Entry<float> GripThreshold;
        public static bool PreferencesSaved;

        private static FieldInfo targetField;
        private static FieldInfo triggerThresholdField;
        private static FieldInfo gripThresholdField;
        private static float triggerThreshold;
        private static float gripThreshold;
        private static int fieldReferences = 0;

        public override void OnApplicationStart()
        {
            Instance = this;
            Logger.Msg("Initializing CustomTriggerThreshold " + Version + "...");

            Category = MelonPreferences.CreateCategory("CustomTriggerThreshold", "Custom Trigger Threshold");
            SnapTurnThreshold = Category.CreateEntry("SnapTurnThreshold", CVRInputManager.SnapTurnTrigger, "Snap Turn Threshold");
            TriggerThreshold = Category.CreateEntry("TriggerThreshold", CVRInputManager.vrTriggerDownThreshold, "Trigger Threshold");
            GripThreshold = Category.CreateEntry("GripThreshold", CVRInputManager.vrTriggerDownThreshold, "Grip Threshold");

            targetField = typeof(CVRInputManager).GetField(nameof(CVRInputManager.vrTriggerDownThreshold));
            triggerThresholdField = typeof(CustomTriggerThresholdMod).GetField(nameof(triggerThreshold), BindingFlags.NonPublic | BindingFlags.Static);
            gripThresholdField = typeof(CustomTriggerThresholdMod).GetField(nameof(gripThreshold), BindingFlags.NonPublic | BindingFlags.Static);

            SetupField(typeof(CVRInputManager).GetField(nameof(CVRInputManager.SnapTurnTrigger)), SnapTurnThreshold);
            SetupField(triggerThresholdField, TriggerThreshold);
            SetupField(gripThresholdField, GripThreshold);

            HarmonyInstance.Patch(typeof(InputModuleSteamVR).GetMethod(nameof(InputModuleSteamVR.UpdateInput), BindingFlags.Instance | BindingFlags.Public),
                transpiler: new HarmonyMethod(typeof(CustomTriggerThresholdMod).GetMethod(nameof(SteamVRInputTranspiler), BindingFlags.Static | BindingFlags.NonPublic)));
            Logger.Msg("Executed transpiler for SteamVRInputModule.");

            HarmonyInstance.Patch(typeof(InputModuleGamepad).GetMethod(nameof(InputModuleGamepad.UpdateInput), BindingFlags.Instance | BindingFlags.Public),
                transpiler: new HarmonyMethod(typeof(CustomTriggerThresholdMod).GetMethod(nameof(GamepadInputTranspiler), BindingFlags.Static | BindingFlags.NonPublic)));
            Logger.Msg("Executed transpiler for GamepadInputModule.");

            Logger.Msg("Running version " + Version + " of CustomTriggerThreshold.");
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (buildIndex != -1 || PreferencesSaved)
                return;

            PreferencesSaved = true;
            MelonCoroutines.Start(SavePreferencesLater());
        }

        private static IEnumerable<CodeInstruction> SteamVRInputTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.LoadsField(targetField))
                {
                    fieldReferences++;

                    if (fieldReferences > 0 && fieldReferences <= 8)
                        yield return new CodeInstruction(OpCodes.Ldsfld, triggerThresholdField);
                    else if (fieldReferences > 8 && fieldReferences <= 16)
                        yield return new CodeInstruction(OpCodes.Ldsfld, gripThresholdField);
                    else
                        yield return instruction;
                }
                else
                {
                    yield return instruction;
                }
            }

            if (fieldReferences != 16)
                Logger.Error("Error in SteamVRInput transpiler. " + fieldReferences + " field references were found.");

            fieldReferences = 0;
        }

        private static IEnumerable<CodeInstruction> GamepadInputTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.LoadsField(targetField))
                {
                    fieldReferences++;

                    if (fieldReferences > 0 && fieldReferences <= 4)
                        yield return new CodeInstruction(OpCodes.Ldsfld, triggerThresholdField);
                    else if (fieldReferences > 4 && fieldReferences <= 8)
                        yield return new CodeInstruction(OpCodes.Ldsfld, gripThresholdField);
                    else
                        yield return instruction;
                }
                else
                {
                    yield return instruction;
                }
            }

            if (fieldReferences != 8)
                Logger.Error("Error in GamepadInput transpiler. " + fieldReferences + " field references were found.");

            fieldReferences = 0;
        }

        private static void SetupField(FieldInfo field, MelonPreferences_Entry<float> entry)
        {
            field.SetValue(null, entry.Value);

            Logger.Msg("Set [" + entry.DisplayName + "] to " + entry.Value.ToString("0.00"));

            entry.OnValueChanged += (oldValue, newValue) =>
            {
                field.SetValue(null, newValue);
                Logger.Msg("Changed [" + entry.DisplayName + "] from " + oldValue.ToString("0.00") + " to " + newValue.ToString("0.00"));
            };
        }

        private static IEnumerator SavePreferencesLater()
        {
            yield return null;
            Category.SaveToFile(false);
            Logger.Msg("Saved preferences.");
        }
    }
}