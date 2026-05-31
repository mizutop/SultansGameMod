using HarmonyLib;

namespace SultansGameMod.Harmony
{
    [HarmonyPatch]
    internal static class AdvancedPatches
    {
        public static bool FreeRedrawSudan = false;
        public static bool UnlockCounters = false;
        public static bool AutoMaxCardOnGen = false;
        public static bool SkipRiteSettlement = false;
        public static bool ManualCounterModification = false;

        private static bool _loggedRedraw = false;
        private static bool _loggedCounter = false;
        private static bool _loggedRite = false;
        private static bool _loggedCardGen = false;

        /// <summary>
        /// 苏丹卡重抽免费 — return false 跳过 OnRedrawSudan 原方法
        /// </summary>
        [HarmonyPatch(typeof(Il2Cpp.GameController), nameof(Il2Cpp.GameController.OnRedrawSudan))]
        [HarmonyPrefix]
        private static bool Prefix_OnRedrawSudan()
        {
            if (FreeRedrawSudan)
            {
                if (!_loggedRedraw) { Main.Log.Msg("[高级] 苏丹牌重抽免费已生效"); _loggedRedraw = true; }
                return false;
            }
            return true;
        }

        /// <summary>
        /// 仪式结算 — Settlement 完成后自动推进到下一步（跳过动画等待）
        /// </summary>
        [HarmonyPatch(typeof(Il2Cpp.RiteResultPanelController), nameof(Il2Cpp.RiteResultPanelController.Settlement))]
        [HarmonyPostfix]
        private static void Postfix_RiteSettlement(Il2Cpp.RiteResultPanelController __instance)
        {
            if (SkipRiteSettlement && __instance != null)
            {
                if (!_loggedRite) { Main.Log.Msg("[高级] 仪式结算动画已跳过"); _loggedRite = true; }
                __instance.DoNext();
            }
        }

        /// <summary>
        /// 生成卡牌时自动设置属性 — Postfix 修改 GenCard 返回的卡牌
        /// </summary>
        [HarmonyPatch(typeof(Il2Cpp.GameController), nameof(Il2Cpp.GameController.GenCard))]
        [HarmonyPostfix]
        private static void Postfix_GenCard(ref Il2Cpp.Card __result)
        {
            if (AutoMaxCardOnGen && __result != null)
            {
                __result.life = 99;
                __result.rareup = 9;
                if (!_loggedCardGen) { Main.Log.Msg("[高级] 生成卡牌自动满级 (life=99, rareup=9)"); _loggedCardGen = true; }
            }
        }

        /// <summary>
        /// 计数器锁定 — 跳过 ModifyCounter.Do(), 阻止游戏发起的计数器变更
        /// ManualCounterModification=true 时放行（允许修改器自身修改）
        /// </summary>
        [HarmonyPatch(typeof(Il2Cpp.ModifyCounter), nameof(Il2Cpp.ModifyCounter.Do))]
        [HarmonyPrefix]
        private static bool Prefix_ModifyCounter()
        {
            if (ManualCounterModification) return true;
            if (UnlockCounters)
            {
                if (!_loggedCounter) { Main.Log.Msg("[高级] 计数器锁定已生效"); _loggedCounter = true; }
                return false;
            }
            return true;
        }

    }
}
