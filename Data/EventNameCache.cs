namespace SultansGameMod.Data
{
    /// <summary>
    /// 事件中文名称缓存 — 从 GameDatabase.EventTexts 读取
    /// 数据在构建时从 config/event/*.json 编译进程序集
    /// </summary>
    internal static class EventNameCache
    {
        public static string? GetEventName(int id)
        {
            return GameDatabase.GetEventText(id);
        }
    }
}
