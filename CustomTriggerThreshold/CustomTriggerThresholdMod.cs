using System.Collections;
using System.Reflection;
using MelonLoader;
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
        public static bool PreferencesSaved;

        public override void OnApplicationStart()
        {
            Instance = this;
            Logger.Msg("Initializing CustomTriggerThreshold " + Version + "...");

            Category = MelonPreferences.CreateCategory("CustomTriggerThreshold", "Custom Trigger Threshold");
            SnapTurnThreshold = Category.CreateEntry("SnapTurnThreshold", CVRInputManager.SnapTurnTrigger, "Snap Turn Threshold");
            TriggerThreshold = Category.CreateEntry("TriggerThreshold", CVRInputManager.vrTriggerDownThreshold, "Trigger Threshold");

            SetupField(typeof(CVRInputManager).GetField(nameof(CVRInputManager.SnapTurnTrigger)), SnapTurnThreshold);
            SetupField(typeof(CVRInputManager).GetField(nameof(CVRInputManager.vrTriggerDownThreshold)), TriggerThreshold);

            Logger.Msg("Running version " + Version + " of CustomTriggerThreshold.");
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (buildIndex != -1 || PreferencesSaved)
                return;

            PreferencesSaved = true;
            MelonCoroutines.Start(SavePreferencesLater());
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
