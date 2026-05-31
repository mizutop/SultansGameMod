using System.Collections.Generic;

namespace SultansGameMod.Data
{
    internal static class ConfigManager
    {
        public static int CardCount => GameDatabase.CardNames.Count;
        public static int EventCount => GameDatabase.EventIds.Length;
        public static int RiteCount => GameDatabase.RiteIds.Length;
        public static int EndingCount => GameDatabase.EndingNames.Count;

        /// <summary>
        /// 卡牌基础稀有度 (1=石 2=铜 3=银 4=金)
        /// 精确数据从 config/cards.json 自动提取
        /// </summary>
        public static Dictionary<int, int> CardBaseRares => CardRaresData.Map;

        public static void Load()
        {
            RiteEventData.Init();
            Main.Log?.Msg($"游戏数据库加载完成: {CardCount} 卡牌, {EventCount} 事件, {RiteCount} 仪式, {EndingCount} 结局");
        }

        public static int GetCardBaseRare(int cardId)
        {
            return CardBaseRares.TryGetValue(cardId, out int rare) ? rare : 2;
        }

        public static string GetRarityText(int rare)
        {
            return rare switch
            {
                1 => "石", 2 => "铜", 3 => "银", 4 => "金",
                _ => "?"
            };
        }

        /// <summary>
        /// 获取卡牌分类（基于实际 tag 归类）
        /// 角色/装备/秘宝/神怪/动物/消耗品/苏丹卡/大餐/读物/思想/部队/其他
        /// </summary>
        public static string GetCardCategory(int cardId)
        {
            return CardCategoriesData.Map.TryGetValue(cardId, out var cat) ? cat : "其他";
        }

        public static string? GetCardName(int id) => GameDatabase.GetCardName(id);
        public static string? GetEndingName(int id) => GameDatabase.GetEndingName(id);
        public static string? GetCardType(int id) => GameDatabase.GetCardType(id);
    }
}
