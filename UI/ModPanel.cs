using UnityEngine;

namespace SultansGameMod.UI
{
    internal static class ModPanel
    {
        private static int _tab;
        private static Rect _windowRect = new Rect(60, 60, Styles.PanelWidth, Styles.PanelHeight);
        private static Vector2 _dragOffset;
        private static bool _isDragging;
        private static string _confirmTitle = "";
        private static string _confirmAction = "";
        private static System.Action? _confirmCallback;
        private static bool _showConfirm;

        private static readonly string[] TabNames = {
            "回合控制", "卡牌操作", "事件仪式", "自定义事件", "关于"
        };

        internal static void ShowConfirm(string title, string action, System.Action callback)
        {
            // v1.2.0: 优先使用游戏原生确认框 — 更融入游戏体验
            try
            {
                Harmony.UIPatches.ShowNativeConfirm(title, action, callback);
                return;
            }
            catch (System.Exception ex)
            {
                SultansGameMod.Main.Log?.Warning($"[ModPanel] 原生确认框调用失败，回退 OnGUI: {ex.Message}");
            }
            // 回退: OnGUI 确认框
            _confirmTitle = title; _confirmAction = action; _confirmCallback = callback; _showConfirm = true;
        }

        // ===== 每个区段独立 JIT，失败只影响该区段 =====

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void DrawDragHandle()
        {
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
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void DrawTitleBar(bool ready)
        {
            GUI.color = Styles.TitleBg;
            GUILayout.BeginHorizontal();
            GUI.color = Styles.TitleText;
            GUILayout.Label("修改器 v1.1.0 - 苏丹的游戏 [F1]");
            GUI.color = ready ? Styles.StatusOk : Styles.StatusErr;
            GUILayout.Label(ready ? "运行中" : "未连接");
            GUI.color = Styles.StatusErr;
            if (GUILayout.Button("X", GUILayout.Width(22)))
                SultansGameMod.Main.Instance?.SetPanelVisible(false);
            GUI.color = Color.white;
            GUILayout.EndHorizontal();
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void DrawTabBar()
        {
            GUILayout.BeginHorizontal();
            for (int i = 0; i < TabNames.Length; i++)
            {
                GUI.color = _tab == i ? Styles.TabActive : Styles.TabInactive;
                if (GUILayout.Button(TabNames[i], GUILayout.Height(Styles.TabBarHeight))) _tab = i;
            }
            GUI.color = Color.white;
            GUILayout.EndHorizontal();
        }



        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void DrawContentArea()
        {
            try
            {
                DrawCurrentTab();
            }
            catch (System.Exception ex)
            {
                SultansGameMod.Main.Log?.Warning($"[Tab {_tab} 异常] {ex}");
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void DrawCurrentTab()
        {
            switch (_tab)
            {
                case 0: RoundTab.Draw(); break;
                case 1: CardTab.Draw(); break;
                case 2: EventRiteTab.Draw(); break;
                case 3: CustomEventTab.Draw(); break;
                case 4: DrawAboutPage(); break;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void DrawAboutPage()
        {
            GUILayout.Space(20);
            GUI.color = Styles.RuntimeHeader;
            GUILayout.Label("==============================", GUILayout.Height(24));
            GUI.color = Color.yellow;
            GUILayout.Label("   苏丹的游戏 修改器 v1.1.0", GUILayout.Height(28));
            GUI.color = Styles.RuntimeHeader;
            GUILayout.Label("==============================", GUILayout.Height(24));
            GUI.color = Color.white;
            GUILayout.Space(10);

            GUILayout.Label("作者: Mizuof");
            GUILayout.Space(8);

            GUI.color = Styles.StatusInfo;
            if (GUILayout.Button("打开 B站 主页 (Mizuof)", GUILayout.Height(28)))
                Application.OpenURL("https://space.bilibili.com/516995192/dynamic");
            GUILayout.Space(4);
            if (GUILayout.Button("加入 QQ群: 624594852", GUILayout.Height(28)))
                Application.OpenURL("https://qm.qq.com/cgi-bin/qm/qr?k=624594852");
            GUILayout.Space(4);
            if (GUILayout.Button("访问网站: www.mizu7.top", GUILayout.Height(28)))
                Application.OpenURL("https://www.mizu7.top");
            GUI.color = Color.white;
            GUILayout.Space(16);

            GUI.color = new Color(1f, 0.5f, 0f);
            GUILayout.Label("本修改器完全免费，请勿用于商业用途！");
            GUI.color = Color.white;
            GUILayout.Space(8);
            GUILayout.Label("如有问题请前往B站动态留言反馈或加QQ群。");
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void DrawConfirmDialog()
        {
            GUI.color = Styles.SaveHeader;
            GUILayout.BeginVertical();
            GUI.color = Color.white;
            GUILayout.Label(_confirmTitle, GUILayout.Height(30));
            GUILayout.Space(5);
            GUILayout.Label("此操作不可撤销，确定要继续吗?");
            GUILayout.Label("操作: " + _confirmAction);
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("确认执行", GUILayout.Width(100), GUILayout.Height(30)))
            {
                try { _confirmCallback?.Invoke(); }
                catch (System.Exception ex) { SultansGameMod.Main.Log?.Warning($"[确认] {ex}"); }
                _showConfirm = false; _confirmCallback = null;
            }
            if (GUILayout.Button("取消", GUILayout.Width(100), GUILayout.Height(30)))
            { _showConfirm = false; _confirmCallback = null; }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        /// <summary>
        /// 主绘制入口 — 只负责 try-catch，不包含任何 Il2Cpp 类型的直接引用
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void DrawContent(bool ready)
        {
            DrawDragHandle();

            GUILayout.BeginArea(_windowRect);
            if (_showConfirm) { DrawConfirmDialog(); GUILayout.EndArea(); return; }

            DrawTitleBar(ready);
            DrawTabBar();
            DrawContentArea();

            GUILayout.EndArea();
        }

        internal static void Draw(bool ready)
        {
            try { DrawContent(ready); }
            catch (System.Exception ex) { SultansGameMod.Main.Log?.Warning($"[ModPanel.Draw][外层异常] {ex}"); }
        }
    }
}
