using System.Collections.Generic;

namespace SultansGameMod.Harmony
{
    /// <summary>
    /// 标签覆盖存储 — 无 Harmony 补丁
    /// CardTagController.Show 在当前游戏构建中可能被 IL2CPP 剥离
    /// 标签覆盖值由 UI 层 (CardTab) 直接管理
    /// </summary>
    internal static class TagPatches
    {
        // 标签覆盖字典: { card_uid -> { tag_name -> value } }
        public static Dictionary<int, Dictionary<string, int>> Overrides = new();

        public static bool Enabled = false;

        public static void SetTagOverride(int uid, string tagName, int value)
        {
            if (!Overrides.ContainsKey(uid))
                Overrides[uid] = new Dictionary<string, int>();

            Overrides[uid][tagName] = value;
            Enabled = true;
        }

        public static void ClearTagOverrides(int uid)
        {
            if (Overrides.ContainsKey(uid))
            {
                Overrides.Remove(uid);
                if (Overrides.Count == 0)
                    Enabled = false;
            }
        }

        public static void ClearAllOverrides()
        {
            Overrides.Clear();
            Enabled = false;
        }
    }
}
