using MelonLoader;
using HarmonyLib;
using UnityEngine;
using SultansGameMod.UI;
using SultansGameMod.Data;
using SultansGameMod.Harmony;

[assembly: MelonInfo(typeof(SultansGameMod.Main), "Sultan's Game Mod Panel", "1.2.0", "modder")]
[assembly: MelonGame("DoubleCross", "Sultan's Game")]

namespace SultansGameMod
{
    public class Main : MelonMod
    {
        internal static Main Instance { get; private set; } = null!;
        internal static MelonLogger.Instance Log => Instance.LoggerInstance;

        // OnGUI 面板 (旧版，F2 切换)
        private bool _onguiPanelVisible;

        // 窗口拖拽状态 (OnGUI)
        private Rect _windowRect = new Rect(60, 60, Styles.PanelWidth, Styles.PanelHeight);
        private Vector2 _dragOffset;
        private bool _isDragging;

        private bool _welcomeShown;

        internal bool IsPanelVisible => _onguiPanelVisible;

        internal void SetPanelVisible(bool visible)
        {
            _onguiPanelVisible = visible;
        }

        // ============================================================
        // 初始化
        // ============================================================
        public override void OnInitializeMelon()
        {
            Instance = this;
            ConfigManager.Load();

            // 注册所有 Harmony 补丁（包括新的 UIPatches）
            foreach (var t in new[] {
                typeof(DicePatches), typeof(TimePatches), typeof(RitePatches),
                typeof(AdvancedPatches), typeof(CounterPatches), typeof(LifeLockPatches),
                typeof(NativeOptionHandler), typeof(UIPatches)
            })
                try { HarmonyInstance.PatchAll(t); }
                catch (System.Exception ex) { Log.Warning($"补丁注册失败: {t.Name} - {ex.Message}"); }

            Log.Msg($"补丁注册完成: 共 {8} 个补丁类型");
            Log.Msg("========================================");
            Log.Msg("  苏丹的游戏 修改器 v1.2.0");
            Log.Msg("  作者: Mizuof");
            Log.Msg("  B站: https://space.bilibili.com/516995192/dynamic");
            Log.Msg("  QQ群: 624594852");
            Log.Msg("  网站: www.mizu7.top");
            Log.Msg("  本修改器完全免费，请勿用于商业用途！");
            Log.Msg("========================================");
            Log.Msg("修改器 v1.2.0 已加载");
            Log.Msg("  F1 = 游戏原生对话框 → 修改面板");
            Log.Msg("  F2 = 传统 OnGUI 面板");
            // ESC 菜单中也增加了「修改器」入口
        }

        // ============================================================
        // (回合状态日志已移除)
        // ============================================================

        // ============================================================
        // OnGUI — 保留传统面板作为备选 (F2)
        // ============================================================
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
                            "欢迎使用 苏丹的游戏 修改器 v1.2.0\n\n" +
                            "本Mod由 Mizuof 制作\n" +
                            "F1 = 打开修改器 | F2 = OnGUI 面板",
                            null, "了解", "", "", ""
                        );
                        _welcomeShown = true;
                        Log.Msg("[Main] 主菜单欢迎弹窗已显示");
                    }
                }
                catch { }
            }

            // === 键盘快捷键 ===
            if (Event.current != null && Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == KeyCode.F1)
                {
                    // F1: 游戏原生对话框菜单
                    NativeMenuSystem.Open();
                    Event.current.Use();
                    return;
                }
                if (Event.current.keyCode == KeyCode.F2)
                {
                    // F2: 传统 OnGUI 面板
                    _onguiPanelVisible = !_onguiPanelVisible;
                    Log?.Msg($"[Main] OnGUI 面板 {(_onguiPanelVisible ? "打开" : "关闭")}");
                    Event.current.Use();
                    return;
                }
                if (Event.current.keyCode == KeyCode.F3)
                {
                    // F3: 随机角色立绘
                    PortraitViewer.ShowRandomCharacter();
                    Event.current.Use();
                    return;
                }
            }

            // === OnGUI 面板绘制 (F2 开启时) ===
            if (_onguiPanelVisible)
            {
                bool ready = CheckGameReady();
                DrawOnGUIPanel(ready);
            }
        }

        // ============================================================
        // OnGUI 面板绘制（从原 ModPanel 提取）
        // ============================================================
        private bool CheckGameReady()
        {
            try { return RuntimeEngine.IsReady; }
            catch (System.Exception ex)
            {
                Log?.Warning($"[Main.CheckGameReady] 异常: {ex}");
                return false;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void DrawOnGUIPanel(bool ready)
        {
            // 拖拽处理
            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                Rect titleBar = new Rect(_windowRect.x, _windowRect.y, _windowRect.width, Styles.TitleBarHeight);
                if (titleBar.Contains(e.mousePosition)) { _isDragging = true; _dragOffset = e.mousePosition - _windowRect.position; e.Use(); }
            }
            if (e.type == EventType.MouseUp && e.button == 0) _isDragging = false;
            if (_isDragging && e.type == EventType.MouseDrag)
            {
                _windowRect.position = e.mousePosition - _dragOffset;
                _windowRect.x = Mathf.Clamp(_windowRect.x, 0, Screen.width - _windowRect.width);
                _windowRect.y = Mathf.Clamp(_windowRect.y, 0, Screen.height - _windowRect.height);
                e.Use();
            }

            GUILayout.BeginArea(_windowRect);

            // 标题栏
            GUI.color = Styles.TitleBg;
            GUILayout.BeginHorizontal();
            GUI.color = Styles.TitleText;
            GUILayout.Label("修改器 v1.2.0 - OnGUI 模式 [F2]");
            GUI.color = ready ? Styles.StatusOk : Styles.StatusErr;
            GUILayout.Label(ready ? "运行中" : "未连接");
            GUI.color = Styles.StatusErr;
            if (GUILayout.Button("X", GUILayout.Width(22)))
                _onguiPanelVisible = false;
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.Label("提示: F1 = 打开修改器 (推荐) | F3 = 随机角色立绘");
            ModPanel.Draw(ready);

            GUILayout.EndArea();
        }
    }
}
