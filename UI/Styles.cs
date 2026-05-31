using UnityEngine;

namespace SultansGameMod.UI
{
    internal static class Styles
    {
        // ===== 配色常量 =====
        // 标题栏
        public static readonly Color TitleBg = new Color(0.545f, 0f, 0f, 0.85f);       // #8B0000
        public static readonly Color TitleText = Color.white;

        // 运行时操作区块 — 绿色系
        public static readonly Color RuntimeHeader = new Color(0.18f, 0.55f, 0.34f);    // #2E8B57
        public static readonly Color RuntimeBg = new Color(0.12f, 0.35f, 0.22f, 0.15f); // 半透明绿底

        // Harmony 开关区块 — 紫色系
        public static readonly Color ToggleHeader = new Color(0.54f, 0.17f, 0.89f);     // #8A2BE2
        public static readonly Color ToggleBg = new Color(0.35f, 0.11f, 0.58f, 0.12f);

        // 存档操作区块 — 红色警告
        public static readonly Color SaveHeader = new Color(0.8f, 0.2f, 0.2f);          // #CC3333
        public static readonly Color SaveBg = new Color(0.4f, 0.1f, 0.1f, 0.12f);
        public static readonly Color SaveWarning = new Color(1f, 0.5f, 0f);             // 橙色

        // 卡牌品级色（rare: 1=石 2=铜 3=银 4=金）
        public static readonly Color RareStone = Color.white;                          // 石 — 白色文字
        public static readonly Color RareCopper = new Color(0f, 0.7f, 0f);            // 铜 — 绿色文字
        public static readonly Color RareSilver = new Color(0.6f, 0.2f, 0.8f);        // 银 — 紫色文字
        public static readonly Color RareGold = new Color(1f, 0.84f, 0f);             // 金 — 金色文字

        // 状态色
        public static readonly Color StatusOk = Color.green;
        public static readonly Color StatusWarn = new Color(1f, 0.55f, 0f);            // 橙色
        public static readonly Color StatusErr = new Color(1f, 0.27f, 0.27f);          // 红色
        public static readonly Color StatusInfo = Color.cyan;

        // UI 元素
        public static readonly Color Separator = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        public static readonly Color TabActive = Color.green;
        public static readonly Color TabInactive = Color.white;
        public static readonly Color ButtonDefault = Color.white;
        public static readonly Color ButtonDanger = new Color(1f, 0.3f, 0.3f);

        // ===== 字体尺寸 =====
        // 字体尺寸常量(当前未在代码中直接使用, 保留供未来 font style 系统引用)
        public const int TitleSize = 16;
        public const int TabSize = 13;
        public const int ButtonSize = 12;
        public const int BodySize = 11;
        public const int StatusSize = 10;

        // ===== 面板尺寸 =====
        public const float PanelWidth = 780f;
        public const float PanelHeight = 600f;
        public const float TitleBarHeight = 26f;
        public const float TabBarHeight = 30f;
        public const float ScrollHeight = 380f;
        public const float ButtonMinHeight = 24f;

        // ===== 辅助方法 =====

        /// <summary>
        /// 绘制区块标题（带底色）
        /// </summary>
        public static void DrawSection(string title, Color headerColor)
        {
            GUI.color = headerColor;
            GUILayout.Label("--- " + title + " ---", GUILayout.Height(22));
            GUI.color = Color.white;
        }

        /// <summary>
        /// 绘制带背景 Box 的分组
        /// </summary>
        public static void BeginGroup(Color bgColor)
        {
            GUI.color = bgColor;
            GUILayout.BeginVertical(GUI.skin.box);
            GUI.color = Color.white;
        }

        public static void EndGroup()
        {
            GUILayout.EndVertical();
        }

        /// <summary>
        /// 获取卡牌品级颜色 (rare: 1=石 2=铜 3=银 4=金)
        /// </summary>
        public static Color GetRareColor(int rare)
        {
            return rare switch
            {
                1 => RareStone,
                2 => RareCopper,
                3 => RareSilver,
                4 => RareGold,
                _ => RareStone
            };
        }

        /// <summary>
        /// 获取品级显示文本 (rare: 1=石 2=铜 3=银 4=金)
        /// </summary>
        public static string GetRareText(int rare)
        {
            return rare switch
            {
                1 => "石",
                2 => "铜",
                3 => "银",
                4 => "金",
                _ => "?"
            };
        }

        /// <summary>
        /// 获取品级图标字符
        /// </summary>
        public static string GetRareIcon(int rare)
        {
            return rare switch
            {
                1 => "*",
                2 => "**",
                3 => "***",
                4 => "****",
                _ => "?"
            };
        }

        /// <summary>
        /// 绘制状态提示
        /// </summary>
        public static void DrawStatus(string status, Color? color = null)
        {
            if (string.IsNullOrEmpty(status)) return;
            GUI.color = color ?? StatusInfo;
            GUILayout.Space(3);
            GUILayout.Label("> " + status);
            GUI.color = Color.white;
        }

        /// <summary>
        /// 绘制存档操作警告
        /// </summary>
        public static void DrawSaveWarning()
        {
            GUI.color = SaveWarning;
            GUILayout.Label("[!] 存档操作 — 修改后需保存并重载回合才能生效");
            GUI.color = Color.white;
        }

        /// <summary>
        /// 绘制分隔线
        /// </summary>
        public static void DrawSeparator()
        {
            GUI.color = Separator;
            GUILayout.Box("", GUILayout.Height(1));
            GUI.color = Color.white;
        }
    }
}
