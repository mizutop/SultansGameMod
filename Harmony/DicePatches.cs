using HarmonyLib;
using UnityEngine;

namespace SultansGameMod.Harmony
{
    [HarmonyPatch]
    internal static class DicePatches
    {
        public static bool AlwaysMaxDice = false;
        public static bool FixedDiceEnabled = false;
        public static int FixedDiceResult = 6;

        private static bool _loggedOnce = false;

        [HarmonyPatch(typeof(Il2Cpp.SudanDiceController), nameof(Il2Cpp.SudanDiceController.Roll))]
        [HarmonyPrefix]
        private static void Prefix_SudanDiceRoll(ref int finalPoint)
        {
            if (AlwaysMaxDice)
            {
                finalPoint = 6;
                if (!_loggedOnce) { Main.Log.Msg("[骰子] 骰子已锁定为最大值(6)"); _loggedOnce = true; }
            }
            else if (FixedDiceEnabled)
            {
                finalPoint = Mathf.Clamp(FixedDiceResult, 1, 6);
                if (!_loggedOnce) { Main.Log.Msg($"[骰子] 骰子已固定为 {finalPoint}"); _loggedOnce = true; }
            }
        }

        [HarmonyPatch(typeof(Il2Cpp.SudanDiceController), nameof(Il2Cpp.SudanDiceController.Lightup))]
        [HarmonyPrefix]
        private static void Prefix_DiceLightup(ref int limit)
        {
            if (AlwaysMaxDice) limit = 6;
            else if (FixedDiceEnabled) limit = Mathf.Clamp(FixedDiceResult, 1, 6);
        }

        [HarmonyPatch(typeof(Il2Cpp.DiceController), nameof(Il2Cpp.DiceController.Show))]
        [HarmonyPrefix]
        private static void Prefix_DiceShow(ref int point)
        {
            if (AlwaysMaxDice)
            {
                point = 6;
                if (!_loggedOnce) { Main.Log.Msg("[骰子] DiceController.Show 已强制设为 6"); _loggedOnce = true; }
            }
            else if (FixedDiceEnabled)
            {
                point = Mathf.Clamp(FixedDiceResult, 1, 6);
                if (!_loggedOnce) { Main.Log.Msg($"[骰子] DiceController.Show 已固定为 {point}"); _loggedOnce = true; }
            }
        }
    }
}
