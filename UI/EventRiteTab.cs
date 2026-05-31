using UnityEngine;
using SultansGameMod.Data;

namespace SultansGameMod.UI
{
    internal static class EventRiteTab
    {
        private static string _evSearch = "";
        private static string _evRemove = "";
        private static string _riteId = "";
        private static string _riteSearch = "";
        private static string _status = "";
        private static Vector2 _scroll;

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        internal static void Draw()
        {
            bool ready = false;
            try { ready = RuntimeEngine.IsReady; }
            catch { }

            _status = "";
            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(Styles.ScrollHeight));

            // ===== 三栏布局：活跃事件 + 事件搜索 + 仪式搜索 =====
            GUILayout.BeginHorizontal();

            // ================== 左栏：活跃事件 ==================
            GUILayout.BeginVertical(GUILayout.Width(240));
            Styles.DrawSection("活跃事件", Styles.RuntimeHeader);
            if (ready)
            {
                try
                {
                    var gc = Il2Cpp.GameController.Inst;
                    if (gc != null)
                    {
                        var activeEvents = gc.EventTrigger.GetActiveEvents();
                        if (activeEvents != null && activeEvents.Count > 0)
                        {
                            GUILayout.Label("共 " + activeEvents.Count + " 个");
                            int idx = 0;
                            foreach (var evt in activeEvents)
                            {
                                idx++;
                                int evtId = 0;
                                try { var idObj = evt.id; if (idObj != null) evtId = System.Convert.ToInt32(idObj.ToString()); } catch { }
                                string name = evtId > 0 ? (EventNameCache.GetEventName(evtId) ?? "[ID:" + evtId + "]") : "(unknown)";
                                GUILayout.BeginHorizontal();
                                GUILayout.Label("[" + idx + "] " + name);
                                if (GUILayout.Button("移除", GUILayout.Width(50)) && ready && evtId > 0)
                                { RuntimeEngine.RemoveEvent(evtId); _status = "已移除事件 " + evtId; }
                                GUILayout.EndHorizontal();
                            }
                        }
                        else GUILayout.Label("(无活跃事件)");
                    }
                }
                catch { GUILayout.Label("(获取失败)"); }
            }
            else GUILayout.Label("(游戏未运行)");
            GUILayout.EndVertical();

            // ================== 中栏：事件搜索与触发 ==================
            GUILayout.BeginVertical(GUILayout.Width(240));
            Styles.DrawSection("事件搜索与触发", Styles.RuntimeHeader);
            GUILayout.BeginHorizontal();
            GUILayout.Label("搜索:", GUILayout.Width(35));
            _evSearch = GUILayout.TextField(_evSearch, GUILayout.Width(100));
            GUILayout.Label("移除ID:", GUILayout.Width(50));
            _evRemove = GUILayout.TextField(_evRemove, GUILayout.Width(60));
            if (GUILayout.Button("移除", GUILayout.Width(45)) && ready && int.TryParse(_evRemove, out int rid) && rid >= 5300000 && rid <= 5399999)
            { RuntimeEngine.RemoveEvent(rid); _status = "已移除事件 " + rid; }
            GUILayout.EndHorizontal();

            bool searched = !string.IsNullOrEmpty(_evSearch);
            string sl = _evSearch.ToLower();
            int evShown = 0;
            foreach (int id in GameDatabase.EventIds)
            {
                string evName = EventNameCache.GetEventName(id) ?? "";
                if (searched)
                {
                    bool idMatch = id.ToString().Contains(_evSearch);
                    bool nameMatch = !string.IsNullOrEmpty(evName) && evName.ToLower().Contains(sl);
                    if (!idMatch && !nameMatch) continue;
                }
                evShown++;
                if (evShown > 300) { GUILayout.Label("... (前300)"); break; }
                GUILayout.BeginHorizontal();
                GUI.color = Styles.GetRareColor(2);
                GUILayout.Label("[" + id + "]", GUILayout.Width(65));
                GUI.color = Color.white;
                GUILayout.Label(string.IsNullOrEmpty(evName) ? "(无名称)" : evName, GUILayout.Width(110));
                if (GUILayout.Button("触发", GUILayout.Width(40)) && ready)
                { RuntimeEngine.TriggerEvent(id); _status = "已触发 " + (evName ?? id.ToString()); }
                GUILayout.EndHorizontal();
            }
            if (evShown == 0) GUILayout.Label("(无匹配)");
            GUILayout.EndVertical();

            // ================== 右栏：仪式搜索与添加 ==================
            GUILayout.BeginVertical(GUILayout.Width(240));
            Styles.DrawSection("仪式搜索与添加", Styles.RuntimeHeader);

            GUILayout.BeginHorizontal();
            GUILayout.Label("ID:", GUILayout.Width(25));
            _riteId = GUILayout.TextField(_riteId, GUILayout.Width(70));
            string ritePreview = "";
            if (int.TryParse(_riteId, out int rp))
            {
                string? rn = GameDatabase.GetRiteName(rp);
                if (rn != null) ritePreview = rn;
            }
            if (!string.IsNullOrEmpty(ritePreview))
            {
                GUI.color = Styles.StatusOk;
                GUILayout.Label(ritePreview, GUILayout.Width(90));
                GUI.color = Color.white;
            }
            if (GUILayout.Button("添加", GUILayout.Width(40)) && ready && int.TryParse(_riteId, out int rid2))
            {
                try
                {
                    var gc = Il2Cpp.GameController.Inst;
                    if (gc != null) { gc.AddRitePin(rid2); _status = "已添加 " + (GameDatabase.GetRiteName(rid2) ?? rid2.ToString()); }
                }
                catch { _status = "添加失败"; }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("搜索:", GUILayout.Width(35));
            _riteSearch = GUILayout.TextField(_riteSearch, GUILayout.Width(130));
            GUILayout.EndHorizontal();

            bool riteSearched = !string.IsNullOrEmpty(_riteSearch);
            string riteSl = _riteSearch.ToLower();
            int riteShown = 0;
            foreach (int riteId in GameDatabase.RiteIds)
            {
                string rn = GameDatabase.GetRiteName(riteId) ?? "";
                if (riteSearched)
                {
                    bool idMatch = riteId.ToString().Contains(_riteSearch);
                    bool nameMatch = !string.IsNullOrEmpty(rn) && rn.ToLower().Contains(riteSl);
                    if (!idMatch && !nameMatch) continue;
                }
                riteShown++;
                if (riteShown > 300) { GUILayout.Label("... (前300)"); break; }
                GUILayout.BeginHorizontal();
                GUI.color = Styles.GetRareColor(3);
                GUILayout.Label("[" + riteId + "]", GUILayout.Width(65));
                GUI.color = Color.white;
                GUILayout.Label(string.IsNullOrEmpty(rn) ? "(无名称)" : rn, GUILayout.Width(110));
                if (GUILayout.Button("添加", GUILayout.Width(40)) && ready)
                {
                    try
                    {
                        var gc = Il2Cpp.GameController.Inst;
                        if (gc != null) { gc.AddRitePin(riteId); _status = "已添加 " + (rn ?? riteId.ToString()); }
                    }
                    catch { }
                }
                GUILayout.EndHorizontal();
            }
            if (riteShown == 0) GUILayout.Label("(无匹配)");
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            Styles.DrawStatus(_status, Styles.StatusInfo);
            GUILayout.EndScrollView();
        }
    }
}
