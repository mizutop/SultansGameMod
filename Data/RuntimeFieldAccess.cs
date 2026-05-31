using System;
using Il2CppInterop.Runtime;

namespace SultansGameMod.Data
{
    /// <summary>
    /// 运行时修改卡牌字段 — 通过 Il2Cpp 属性直接赋值
    /// Card 类的 count/life/rareup/tag 等字段通过 Il2CppInterop 绑定暴露 get/set
    /// </summary>
    internal static class RuntimeFieldAccess
    {
        public static void SetCount(Il2Cpp.Card card, int value)
        {
            if (card == null) return;
            try
            {
                card.count = value;
                Main.Log?.Msg($"[FieldAccess] Card.count = {value} (UID:{card.uid})");
            }
            catch (Exception ex)
            {
                Main.Log?.Warning($"[FieldAccess] SetCount 失败: {ex}");
            }
        }

        public static void SetLife(Il2Cpp.Card card, int value)
        {
            if (card == null) return;
            try
            {
                card.life = value;
                Main.Log?.Msg($"[FieldAccess] Card.life = {value} (UID:{card.uid})");
            }
            catch (Exception ex)
            {
                Main.Log?.Warning($"[FieldAccess] SetLife 失败: {ex}");
            }
        }

        public static void SetRareup(Il2Cpp.Card card, int value)
        {
            if (card == null) return;
            try
            {
                card.rareup = value;
                Main.Log?.Msg($"[FieldAccess] Card.rareup = {value} (UID:{card.uid})");
            }
            catch (Exception ex)
            {
                Main.Log?.Warning($"[FieldAccess] SetRareup 失败: {ex}");
            }
        }

        /// <summary>
        /// 设置卡牌基础品级（CardNode.rare）
        /// 0=石 1=铜 2=银 3=金
        /// </summary>
        public static void SetBaseRare(Il2Cpp.Card card, int value)
        {
            if (card == null) return;
            try
            {
                var node = card.data;
                if (node == null) { Main.Log?.Warning("[FieldAccess] SetBaseRare: Card.data 为 null"); return; }
                node.rare = value;
                Main.Log?.Msg($"[FieldAccess] CardNode.rare = {value} (UID:{card.uid})");
            }
            catch (Exception ex)
            {
                Main.Log?.Warning($"[FieldAccess] SetBaseRare 失败: {ex}");
            }
        }

        /// <summary>
        /// 设置卡牌标签（通过 Card.data → CardNode.tag 字典）
        /// </summary>
        public static void SetTag(Il2Cpp.Card card, string key, int value)
        {
            if (card == null) return;
            try
            {
                var node = card.data;
                if (node == null) { Main.Log?.Warning("[FieldAccess] SetTag: Card.data 为 null"); return; }

                if (node.tag == null)
                    node.tag = new Il2CppSystem.Collections.Generic.Dictionary<string, int>();

                if (node.tag.ContainsKey(key))
                    node.tag[key] = value;
                else
                    node.tag.Add(key, value);

                Main.Log?.Msg($"[FieldAccess] Card.tag[{key}] = {value} (UID:{card.uid})");
            }
            catch (Exception ex)
            {
                Main.Log?.Warning($"[FieldAccess] SetTag 失败: {ex}");
            }
        }

        /// <summary>
        /// 移除卡牌标签
        /// </summary>
        public static void RemoveTag(Il2Cpp.Card card, string key)
        {
            if (card?.data?.tag == null) return;
            try
            {
                var node = card.data;
                if (node.tag.ContainsKey(key))
                    node.tag.Remove(key);
                Main.Log?.Msg($"[FieldAccess] Card.tag[{key}] 已移除 (UID:{card.uid})");
            }
            catch (Exception ex)
            {
                Main.Log?.Warning($"[FieldAccess] RemoveTag 失败: {ex}");
            }
        }
    }
}
