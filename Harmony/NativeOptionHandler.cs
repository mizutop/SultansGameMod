using System;
using System.Collections.Generic;
using HarmonyLib;

namespace SultansGameMod.Harmony
{
    /// <summary>
    /// 原生面板帮助器 + 直接运行时计数器修改
    /// </summary>
    internal static class NativeOptionHandler
    {
        // ===== 待处理的选项回调状态 =====
        private static List<(string text, Action action)>? _pendingOptions;
        private static bool _pendingOptionActive;

        /// <summary>
        /// 显示原生选项面板 — 用户确认后通过 Harmony 补丁捕获结果
        /// </summary>
        public static void ShowOption(string title, List<(string text, Action action)> options, Il2Cpp.GameController gc)
        {
            if (options == null || options.Count == 0 || gc == null) return;

            try
            {
                var items = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<Il2Cpp.OptionBase.Item>(options.Count);
                for (int i = 0; i < options.Count; i++)
                    items[i] = new Il2Cpp.OptionBase.Item { text = options[i].text };

                _pendingOptions = options;
                _pendingOptionActive = true;
                gc.ShowOption(title, items, null);
            }
            catch (Exception ex)
            {
                _pendingOptionActive = false;
                _pendingOptions = null;
                Main.Log?.Warning($"[NativeOption] 异常: {ex}");
            }
        }

        /// <summary>
        /// 显示原生确认框 — 用户确认后执行 action
        /// bool 是 blittable 类型，Promise.Then 可以正常回调
        /// </summary>
        public static void ShowConfirm(string text, Action onConfirm, Il2Cpp.GameController gc)
        {
            try
            {
                var promise = gc.ShowConfirm(text, null, "确认", "取消", "", "");
                if (promise != null)
                {
                    promise.Then((Action<bool>)(result =>
                    {
                        if (result) onConfirm?.Invoke();
                    }));
                }
            }
            catch (Exception ex)
            {
                Main.Log?.Warning($"[NativeOption] ShowConfirm 异常: {ex}");
            }
        }

        /// <summary>
        /// Harmony 补丁: 拦截 OptionController.OnConfirm 捕获用户选择
        /// </summary>
        [HarmonyPatch(typeof(Il2Cpp.OptionController), nameof(Il2Cpp.OptionController.OnConfirm))]
        [HarmonyPostfix]
        private static void Postfix_OnConfirm(Il2Cpp.OptionController __instance)
        {
            if (!_pendingOptionActive || _pendingOptions == null) return;

            try
            {
                int idx = __instance.CurrentOptionIndex;
                if (idx >= 0 && idx < _pendingOptions.Count)
                {
                    var (text, action) = _pendingOptions[idx];
                    Main.Log?.Msg($"[NativeOption] 用户选择: {text}");

                    // 在调用回调前保存当前 pending 引用。
                    // 如果回调内部调用了 ShowOption()（嵌套菜单），
                    // _pendingOptions 会被替换为新菜单的选项。
                    // 此时不能清除，否则会破坏子菜单的状态。
                    var savedOptions = _pendingOptions;
                    _pendingOptionActive = false;
                    _pendingOptions = null;

                    action?.Invoke();

                    // 仅当回调没有设置新的 pendingOptions 时才最终确认清除
                    // （正常情况下 already null，但防止外层再次清除）
                }
            }
            catch (Exception ex)
            {
                Main.Log?.Warning($"[NativeOption] 回调异常: {ex}");
                _pendingOptionActive = false;
                _pendingOptions = null;
            }
        }

        // ===== 运行时直接计数器修改（不碰存档） =====

        /// <summary>
        /// 直接设置局内计数器值 — ModifyCounter.Do(OperationContext) 即时生效
        /// </summary>
        public static void SetCounterDirect(int counterId, long value)
        {
            try
            {
                var counter = new Il2Cpp.ModifyCounter();
                counter.counter_id = counterId;
                counter.Value = (int)value;
                counter.op = Il2Cpp.CounterOp.SET;
                var ctx = new Il2Cpp.OperationContext();
                AdvancedPatches.ManualCounterModification = true;
                counter.Do(ctx);
                AdvancedPatches.ManualCounterModification = false;
                Main.Log?.Msg($"[DirectCounter] 计数器 {counterId} 设为 {value}");
            }
            catch (Exception ex)
            {
                Main.Log?.Warning($"[DirectCounter] 异常: {ex}");
            }
        }

        /// <summary>
        /// 直接增加局内计数器值
        /// </summary>
        public static void AddCounterDirect(int counterId, long value)
        {
            try
            {
                var counter = new Il2Cpp.ModifyCounter();
                counter.counter_id = counterId;
                counter.Value = (int)value;
                counter.op = Il2Cpp.CounterOp.ADD;
                var ctx = new Il2Cpp.OperationContext();
                AdvancedPatches.ManualCounterModification = true;
                counter.Do(ctx);
                AdvancedPatches.ManualCounterModification = false;
                Main.Log?.Msg($"[DirectCounter] 计数器 {counterId} +{value}");
            }
            catch (Exception ex)
            {
                Main.Log?.Warning($"[DirectCounter] 异常: {ex}");
            }
        }


    }
}
