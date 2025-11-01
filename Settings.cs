using ModSettings;

namespace TLD_Skill_Adjustment
{
    internal class TLD_Settings : JsonModSettings
    {
        public static TLD_Settings Instance { get; private set; }

        [Section("Parasite Risk Options")]
        [Name("Fixed Risk Mode")]
        [Description("If enabled, eating any amount of predator meat at Cooking 5 always causes 1% parasite risk. If disabled, risk increases +1% per piece (capped).")]
        public bool FixedRiskMode = true;

        [Name("Enable Antibiotic Reminder")]
        [Description("If enabled, shows a HUD message when a new antibiotic dose can be taken to cure Intestinal parasites (after 24h).")]
        public bool EnableAntibioticReminder = true;

        protected override void OnConfirm()
        {
            base.OnConfirm();
            Save();
            MelonLoader.MelonLogger.Msg("[Settings] Saved mod settings.");
        }

        public static void OnLoad()
        {
            Instance = new TLD_Settings();
            Instance.AddToModSettings("Alternative Parasites at Max Level");
        }
    }
}