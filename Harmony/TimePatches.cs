using HarmonyLib;

namespace SultansGameMod.Harmony
{
    [HarmonyPatch]
    internal static class TimePatches
    {
        public static bool FreezeTime = false;
        public static bool SkipRiteWait = false;

        // 触发计数器 — 仅首次日志使用
        private static bool _loggedFreeze = false;

        [HarmonyPatch(typeof(Il2Cpp.GameController), nameof(Il2Cpp.GameController.OnNextRound))]
        [HarmonyPrefix]
        private static bool Prefix_OnNextRound()
        {
            if (FreezeTime)
            {
                if (!_loggedFreeze) { Main.Log.Msg("[回合冻结] GameController.OnNextRound 已拦截 — 回合推进已冻结"); _loggedFreeze = true; }
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(Il2Cpp.GameController), nameof(Il2Cpp.GameController.OnBeginRound))]
        [HarmonyPrefix]
        private static bool Prefix_OnBeginRound()
        {
            if (FreezeTime)
            {
                if (!_loggedFreeze) { Main.Log.Msg("[回合冻结] GameController.OnBeginRound 已拦截"); _loggedFreeze = true; }
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(Il2Cpp.NextDay_Round_Helper), nameof(Il2Cpp.NextDay_Round_Helper.Continue))]
        [HarmonyPrefix]
        private static bool Prefix_RoundContinue()
        {
            if (FreezeTime)
            {
                if (!_loggedFreeze) { Main.Log.Msg("[回合冻结] NextDay_Round_Helper.Continue 已拦截"); _loggedFreeze = true; }
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(Il2Cpp.NextDay_Round_Helper), nameof(Il2Cpp.NextDay_Round_Helper.Reset))]
        [HarmonyPrefix]
        private static bool Prefix_RoundReset()
        {
            if (FreezeTime)
            {
                if (!_loggedFreeze) { Main.Log.Msg("[回合冻结] NextDay_Round_Helper.Reset 已拦截"); _loggedFreeze = true; }
                return false;
            }
            return true;
        }
    }
}
