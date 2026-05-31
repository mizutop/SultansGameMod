using HarmonyLib;

namespace SultansGameMod.Harmony
{
    [HarmonyPatch]
    internal static class RitePatches
    {
        public static bool NoCost = false;

        private static bool _logged = false;

        // 已知的仪式消耗型计数器 ID 列表
        private static readonly int[] CostCounterIds = new int[] {
            7200138, // 势力度/金币
            7200133, // 灵视值
            7200002, // 金骰子(旧)
            7100006, // 金骰子(事件系)
        };

        /// <summary>
        /// 仪式零消耗 — 拦截消耗型计数器的 GetRealChangeValue
        /// 只拦截已知的消耗计数器, 不影响奖励发放
        /// </summary>
        [HarmonyPatch(typeof(Il2Cpp.ModifyCounter), nameof(Il2Cpp.ModifyCounter.GetRealChangeValue))]
        [HarmonyPostfix]
        private static void Postfix_GetRealChangeValue(Il2Cpp.ModifyCounter __instance, ref int __result)
        {
            if (!NoCost) return;
            int key = __instance.counter_id;
            foreach (int costId in CostCounterIds)
            {
                if (key == costId && __result < 0)
                {
                    if (!_logged) { Main.Log.Msg("[仪式] 仪式零消耗已生效"); _logged = true; }
                    __result = 0;
                    return;
                }
            }
        }
    }
}
