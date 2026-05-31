using HarmonyLib;
using System.Collections.Generic;

namespace SultansGameMod.Harmony
{
    [HarmonyPatch]
    internal static class CounterPatches
    {
        public static Dictionary<int, long> Overrides = new();
        public static bool Enabled = false;

        /// <summary>
        /// 拦截 ModifyCounter.GetRealChangeValue — 覆盖局内计数器返回值
        /// </summary>
        [HarmonyPatch(typeof(Il2Cpp.ModifyCounter), nameof(Il2Cpp.ModifyCounter.GetRealChangeValue))]
        [HarmonyPostfix]
        private static void Postfix_ModifyCounterChange(Il2Cpp.ModifyCounter __instance, ref int __result)
        {
            if (!Enabled) return;
            int key = __instance.counter_id;
            if (Overrides.TryGetValue(key, out long val))
            {
                Main.Log.Msg($"[计数器] 拦截 ModifyCounter(id={key}): {__result} -> {val}");
                __result = (int)val;
            }
        }

        /// <summary>
        /// 拦截 ModifyGlobalCounter.Do — 日志记录全局计数器命中情况
        /// 全局计数器的运行时覆盖受限于 IL2CPP 绑定字段访问,
        /// 局内计数器覆盖已通过 GetRealChangeValue Postfix 处理
        /// </summary>
        [HarmonyPatch(typeof(Il2Cpp.ModifyGlobalCounter), nameof(Il2Cpp.ModifyGlobalCounter.Do))]
        [HarmonyPrefix]
        private static void Prefix_GlobalCounterDo(Il2Cpp.ModifyGlobalCounter __instance)
        {
            if (!Enabled) return;
            int key = __instance.counter_id;
            if (Overrides.TryGetValue(key, out long val))
            {
                Main.Log.Msg($"[计数器] ModifyGlobalCounter(id={key}) 命中覆盖={val} (IL2CPP field受限, 仅日志)");
            }
        }

        public static void SetOverride(string key, long val)
        {
            if (int.TryParse(key, out int k))
            {
                Overrides[k] = val;
                Enabled = true;
            }
        }

        public static void ClearOverrides()
        {
            Overrides.Clear();
            Enabled = false;
        }
    }
}
