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

        // ===== 运行时回合追踪 =====
        // 游戏在正常推进回合时不会更新存档 JSON 中的 Round 字段，
        // 因此需要通过 Harmony 补丁从游戏运行时状态追踪当前回合数。
        // 原理：
        //   - LoadRound(int) 捕获加载/跳转时的回合数
        //   - OnBeginRound 在每回合开始时递增（LoadRound 后的首次跳过）
        public static int CurrentRound = 1;
        private static bool _skipNextBeginRound = false;

        [HarmonyPatch(typeof(Il2Cpp.GameController), nameof(Il2Cpp.GameController.LoadRound))]
        [HarmonyPostfix]
        private static void Postfix_LoadRound(int round, bool begin)
        {
            CurrentRound = round;
            _skipNextBeginRound = true;
            Main.Log?.Msg($"[回合状态] 第{CurrentRound}回合");
        }

        [HarmonyPatch(typeof(Il2Cpp.GameController), nameof(Il2Cpp.GameController.OnBeginRound))]
        [HarmonyPostfix]
        private static void Postfix_OnBeginRound()
        {
            if (_skipNextBeginRound)
            {
                _skipNextBeginRound = false;
                Main.Log?.Msg($"[回合状态] 第{CurrentRound}回合");
                return;
            }
            CurrentRound++;
            Main.Log?.Msg($"[回合状态] 第{CurrentRound}回合");
        }

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

        [HarmonyPatch(typeof(Il2Cpp.GameController), nameof(Il2Cpp.GameController.OnPrevRound))]
        [HarmonyPostfix]
        private static void Postfix_OnPrevRound()
        {
            Main.Log?.Msg($"[检测] 玩家触发了\"返回上一回合\"确认弹窗");
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
        [HarmonyPostfix]
        private static void Postfix_RoundContinue()
        {
            Main.Log?.Msg($"[检测] 玩家点击了\"下一天\"按钮");
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
