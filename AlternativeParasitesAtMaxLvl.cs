using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using MelonLoader.Utils;
using System;
using System.IO;
using UnityEngine;

namespace TLD_Skill_Adjustment
{
    public class CookingParasiteRiskMod : MelonMod
    {
        public override void OnInitializeMelon()
        {
            bool debug = File.Exists(Path.Combine(MelonEnvironment.UserDataDirectory, "AlternativeParasitesAtMaxLvl.debug"));
            MelonLogger.Msg("=== Initializing Alternative Parasites at Max Level Mod ===");

            TLD_Settings.OnLoad();

            HarmonyLib.Harmony harmony = new HarmonyLib.Harmony("com.baltazar.cookingparasiterisk");
            harmony.PatchAll();

            if (debug)
                MelonLogger.Msg("[DEBUG] Debug mode active.");

            MelonLogger.Msg("=== Alternative Parasites at Max Level Mod initialized successfully ===");
        }
    }

    // -----------------------------
    // PATCH: PlayerManager.OnEatingComplete
    // -----------------------------
    [HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.OnEatingComplete), new Type[] { typeof(bool), typeof(bool), typeof(float) })]
    internal static class CookingParasiteRiskPatch
    {
        private static float _lastTriggerTime = -1f;
        private static string _lastFoodName = "";
        private static readonly bool _debugEnabled = File.Exists(Path.Combine(MelonEnvironment.UserDataDirectory, "AlternativeParasitesAtMaxLvl.debug"));

        static void Postfix(bool success, bool playerCancel, float progress)
        {
            try
            {
                if (!success && !playerCancel)
                    return;

                PlayerManager player = GameManager.GetPlayerManagerComponent();
                if (player == null) return;

                GearItem eatenGear = player.m_FoodItemEaten ?? player.m_FoodItemOpened;
                if (eatenGear == null || eatenGear.m_FoodItem == null) return;

                FoodItem food = eatenGear.m_FoodItem;
                string foodName = food.name ?? "<null>";

                // Avoid duplicate trigger (animation replay)
                if (Time.time - _lastTriggerTime < 0.5f && _lastFoodName == foodName)
                    return;

                _lastTriggerTime = Time.time;
                _lastFoodName = foodName;

                DebugLog($"[AlternativeParasitesAtMaxLvl] >>> Postfix triggered. Eaten: {foodName}, playerCancel={playerCancel}");

                if (!IsPredatorMeat(food))
                {
                    DebugLog("[AlternativeParasitesAtMaxLvl] Not predator meat. Skipping.");
                    return;
                }

                var cookingSkill = GameManager.GetSkillCooking();
                int cookingLevel = cookingSkill.GetCurrentTierNumber();
                DebugLog($"[AlternativeParasitesAtMaxLvl] Cooking level: {cookingLevel}");

                // Below level 5 → vanilla handles risk
                if (cookingLevel < 4)
                {
                    DebugLog("[AlternativeParasitesAtMaxLvl] Cooking level < 5, skipping (vanilla handles risk).");
                    return;
                }

                IntestinalParasites parasites = GameManager.GetIntestinalParasitesComponent();
                if (parasites == null) return;

                // Already have active parasites? Skip new risk logic
                bool hasAffliction = Traverse.Create(parasites).Field("m_HasParasites").GetValue<bool>();
                if (hasAffliction)
                {
                    DebugLog("[AlternativeParasitesAtMaxLvl] Player already has parasites — skipping new risk.");
                    return;
                }

                bool hasRisk = parasites.HasIntestinalParasitesRisk();
                float current = parasites.GetCurrentRisk();
                DebugLog($"[AlternativeParasitesAtMaxLvl] Current risk active? {hasRisk}, current value = {current}%");

                // --------------------------
                // FIXED RISK MODE
                // --------------------------
                if (TLD_Settings.Instance.FixedRiskMode)
                {
                    if (!hasRisk)
                    {
                        parasites.Start();
                        parasites.AddRiskPercent(new float[] { 1f }, false);
                        DebugLog("[AlternativeParasitesAtMaxLvl] Fixed mode: started new parasite risk (24h timer).");
                    }
                    else
                    {
                        DebugLog("[AlternativeParasitesAtMaxLvl] Fixed mode: risk already active, no change.");
                    }
                }
                else
                {
                    // --------------------------
                    // INCREMENTAL RISK MODE
                    // --------------------------
                    if (!hasRisk)
                    {
                        parasites.Start();
                        parasites.AddRiskPercent(new float[] { 1f }, false);
                        DebugLog("[AlternativeParasitesAtMaxLvl] Incremental mode: started new parasite risk at 1% (24h timer).");
                    }
                    else
                    {
                        parasites.AddRiskPercent(new float[] { 1f }, false);
                        float newRisk = parasites.GetCurrentRisk();
                        DebugLog($"[AlternativeParasitesAtMaxLvl] Incremental mode: risk increased by +1%, now {newRisk}%");
                    }
                }
            }
            catch (Exception ex)
            {
                if (_debugEnabled)
                    MelonLogger.Error("[AlternativeParasitesAtMaxLvl] Exception: " + ex);
            }
        }

        private static bool IsPredatorMeat(FoodItem food)
        {
            string name = food.name != null ? food.name.ToLowerInvariant() : "";
            return name.Contains("wolf") || name.Contains("bear") || name.Contains("cougar");
        }

        private static void DebugLog(string msg)
        {
            if (_debugEnabled)
                MelonLogger.Msg(msg);
        }
    }
}