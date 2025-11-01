using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using UnityEngine;

namespace TLD_Skill_Adjustment
{
    [HarmonyPatch(typeof(IntestinalParasites), "Update")]
    internal static class ParasiteAntibioticReminder
    {
        private static bool _previousHasTakenDoseToday = false;

        private static bool _hudActive = false;

        private static readonly bool _debugEnabled = System.IO.File.Exists(
            System.IO.Path.Combine(MelonLoader.Utils.MelonEnvironment.UserDataDirectory, "AlternativeParasitesAtMaxLvl.debug")
        );

        static void Postfix(IntestinalParasites __instance)
        {
            try
            {
                if (!TLD_Settings.Instance.EnableAntibioticReminder)
                    return;

                if (__instance == null)
                    return;

                if (GameManager.IsMainMenuActive())
                    return;

                bool hasTakenDoseToday = __instance.HasTakenDoseToday();

                if (!_hudActive && hasTakenDoseToday)
                {
                    _hudActive = true;
                    _previousHasTakenDoseToday = hasTakenDoseToday;
                    if (_debugEnabled)
                        MelonLogger.Msg("[AlternativeParasitesAtMaxLvl] Antibiotic reminder activated after first dose.");
                    return;
                }

                if (!_hudActive)
                    return;

                if (_previousHasTakenDoseToday && !hasTakenDoseToday)
                {
                    MelonLogger.Msg("[AlternativeParasitesAtMaxLvl] 24 hours elapsed since last dose. Showing HUD message.");

                    Panel_HUD hud = InterfaceManager.GetPanel<Panel_HUD>();
                    if (hud != null)
                    {
                        HUDMessage.AddMessage("Next antibiotic dose for parasites is timely.");
                        if (_debugEnabled)
                            MelonLogger.Msg("[AlternativeParasitesAtMaxLvl] HUD message displayed successfully.");
                    }
                    else
                    {
                        MelonLogger.Msg("[AlternativeParasitesAtMaxLvl] Could not find Panel_HUD instance!");
                    }
                }

                _previousHasTakenDoseToday = hasTakenDoseToday;
            }
            catch (System.Exception ex)
            {
                if (_debugEnabled)
                    MelonLogger.Error("[AlternativeParasitesAtMaxLvl] Reminder exception: " + ex);
            }
        }
    }
}