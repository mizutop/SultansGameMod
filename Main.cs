using MelonLoader;
using HarmonyLib;
using UnityEngine;
using SultansGameMod.UI;
using SultansGameMod.Data;
using SultansGameMod.Harmony;

[assembly: MelonInfo(typeof(SultansGameMod.Main), "Sultan's Game Mod Panel", "1.1.0", "modder")]
[assembly: MelonGame("DoubleCross", "Sultan's Game")]

namespace SultansGameMod
{
    public class Main : MelonMod
    {
        internal static Main Instance { get; private set; } = null!;
        internal static MelonLogger.Instance Log => Instance.LoggerInstance;
        private bool _panelVisible;
        private bool _welcomeShown;

        // X 按钮和 F1 共用此方法
        internal bool IsPanelVisible => _panelVisible;

        internal void SetPanelVisible(bool visible)
        {
            _panelVisible = visible;
        }

        internal void TogglePanel()
        {
            _panelVisible = !_panelVisible;
            Main.Log?.Msg($"[Main.TogglePanel] 面板 {( _panelVisible ? "打开" : "关闭")}");
            // 不加载存档 — 纯运行时 API 操作，无存档依赖
        }

        /// <summary>
        /// 在 OnGUI 中获取游戏就绪状态 — 独立方法，与 Draw() 分离
        /// </summary>
        private bool CheckGameReady()
        {
            try { return SultansGameMod.Data.RuntimeEngine.IsReady; }
            catch (System.Exception ex)
            {
                Log?.Warning($"[Main.CheckGameReady] 异常: {ex}");
                return false;
            }
        }

        public override void OnInitializeMelon()
        {
            Instance = this;
            ConfigManager.Load();
            foreach (var t in new[] { typeof(DicePatches), typeof(TimePatches), typeof(RitePatches), typeof(AdvancedPatches), typeof(CounterPatches), typeof(LifeLockPatches), typeof(NativeOptionHandler) })
                try { HarmonyInstance.PatchAll(t); }
                catch (System.Exception ex) { Log.Warning($"补丁注册失败: {t.Name} - {ex.Message}"); }
            Log.Msg($"补丁注册完成: 共 {7} 个补丁类型");
            Log.Msg("========================================");
            Log.Msg("  苏丹的游戏 修改器 v1.1.0");
            Log.Msg("  作者: Mizuof");
            Log.Msg("  B站: https://space.bilibili.com/516995192/dynamic");
            Log.Msg("  QQ群: 624594852");
            Log.Msg("  网站: www.mizu7.top");
            Log.Msg("  本修改器完全免费，请勿用于商业用途！");
            Log.Msg("========================================");
            Log.Msg("修改器 v1.1.0 已加载 — 按 F1 打开面板。");
        }

        public override void OnGUI()
        {
            // 主菜单欢迎弹窗 — 使用游戏原生 ConfirmController
            if (!_welcomeShown)
            {
                try
                {
                    var startCtrl = Il2Cpp.StartController.Inst;
                    if (startCtrl != null && startCtrl.confirmController != null)
                    {
                        startCtrl.confirmController.Show(
                            "欢迎使用 苏丹的游戏 修改器 v1.1.0\n\n本Mod由 Mizuof 制作\n按 F1 打开修改器面板",
                            null, "了解", "", "", ""
                        );
                        _welcomeShown = true;
                        Log.Msg("[Main] 主菜单欢迎弹窗已显示");
                    }
                }
                catch { }
            }

            if (Event.current != null && Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.F1)
            {
                TogglePanel();
                Event.current.Use();
            }
            if (_panelVisible)
            {
                bool ready = CheckGameReady();
                ModPanel.Draw(ready);
            }
        }
    }
}
