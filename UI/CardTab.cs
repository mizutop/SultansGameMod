using UnityEngine;
using SultansGameMod.Data;
using SultansGameMod.Harmony;

namespace SultansGameMod.UI
{
    internal static class CardTab
    {
        private static string _search = "";
        private static string _filterType = "全部";
        private static string _status = "";
        private static Vector2 _scroll;

        private static string _tagKey = "", _tagVal = "";
        private static string _customCount = "", _customLife = "", _customRareup = "";

        // 新分类体系（基于实际 tag 归类）
        private static readonly string[] CategoryTypes = {
            "全部", "角色", "装备", "秘宝", "神怪", "动物",
            "消耗品", "苏丹卡", "大餐", "读物", "思想", "部队", "其他"
        };

        // 稀有度筛选
        private static string _rarityFilter = "全部";

        // 属性标签：英文 code → 中文名
        private static readonly string[] AttrCodes = {
            "physique", "charm", "wisdom", "conceal", "battle", "social",
            "knowledge", "survival", "magic", "influence", "support"
        };
        private static readonly string[] AttrNames = {
            "体魄", "魅力", "智慧", "隐匿", "战斗", "社交",
            "学识", "生存", "魔力", "价值", "支持"
        };

        // 标签分组定义（完整版，涵盖全部高频交互标签）
        // 身份类：角色卡常见
        private static readonly string[] IdentityCodes = {
            "man", "female", "noble", "player", "ally", "slave", "adherent",
            "lock_23", "lock_39", "lock_5", "monster", "animal", "prisoner",
            "lock_79", "whore", "thief", "giaour", "criminal", "wanderer"
        };
        private static readonly string[] IdentityNames = {
            "男性", "女性", "贵族", "主角", "盟友", "奴隶", "追随者",
            "食客", "有跟班", "装备商人", "怪物", "动物", "囚徒",
            "孤儿", "妓女", "小偷", "密教徒", "黑街居民", "流浪者"
        };
        // 资源/物品类
        private static readonly string[] GenericCodes = {
            "money", "coin", "food", "army", "equipment",
            "consumable", "stackable", "read", "like_reading",
            "weapon", "cloth", "accessory", "mount",
            "feast", "intelligent", "sky", "thought",
            "oppose", "support", "reroll", "heirloom"
        };
        private static readonly string[] GenericNames = {
            "金钱", "金币", "食物", "部队", "装备",
            "消耗品", "可堆叠", "读物", "爱书人",
            "武器", "服装", "饰品", "坐骑",
            "大餐", "情报", "天象", "思潮",
            "反对", "支持", "重投", "奇珍"
        };
        // 苏丹标签（仅限苏丹牌）
        private static readonly string[] SudanTagCodes = { "desire", "kill", "war", "wastefulness" };
        private static readonly string[] SudanTagNames = { "纵欲", "杀戮", "征服", "奢靡" };
        private const string OwnedTagCode = "own";

        /// <summary>
        /// 根据苏丹牌 ID 返回对应类型的颜色
        /// </summary>
        private static Color GetSudanCardColor(int id)
        {
            if (id >= 2010001 && id <= 2010004) return new Color(0.8f, 0.1f, 0.1f);
            if (id >= 2010005 && id <= 2010008) return new Color(1f, 0.4f, 0.7f);
            if (id >= 2010009 && id <= 2010012) return new Color(1f, 0.84f, 0f);
            if (id >= 2010013 && id <= 2010016) return new Color(0.6f, 0.2f, 0.8f);
            string? n = GameDatabase.GetCardName(id);
            if (n == "杀戮") return new Color(0.8f, 0.1f, 0.1f);
            if (n == "纵欲") return new Color(1f, 0.4f, 0.7f);
            if (n == "奢靡") return new Color(1f, 0.84f, 0f);
            if (n == "征服") return new Color(0.6f, 0.2f, 0.8f);
            return new Color(1f, 0.5f, 0f);
        }

        /// <summary>
        /// 获取卡牌列表中的颜色：按稀有度着色
        /// </summary>
        private static Color GetListCardColor(int id, string subType)
        {
            if (subType == "苏丹卡") return GetSudanCardColor(id);

            int rare = ConfigManager.GetCardBaseRare(id);
            Color c = subType switch
            {
                "角色" => Styles.GetRareColor(rare),
                "部队" => Styles.GetRareColor(rare),
                "金币" => Styles.GetRareColor(rare),
                "情报" => Styles.GetRareColor(rare),
                "天象" => Styles.GetRareColor(rare),
                "读物" => Styles.GetRareColor(rare),
                "思潮" => Styles.GetRareColor(rare),
                "装备" => Styles.GetRareColor(rare),
                _ => Styles.GetRareColor(rare)
            };
            return c;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        internal static void Draw()
        {
            bool ready = false;
            try { ready = RuntimeEngine.IsReady; }
            catch { }

            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(Styles.ScrollHeight));

            // ===== 当前选中卡牌 =====
            Styles.DrawSection("当前选中卡牌 — 游戏中点击卡牌后查看", Styles.RuntimeHeader);

            if (ready)
            {
                try
                {
                    var gc = Il2Cpp.GameController.Inst;
                    if (gc == null) { GUILayout.Label("(GameController 不可用)"); }
                    else
                    {
                        var selected = gc.CurrentSelect;
                        if (selected != null)
                        {
                            int cardId = 0, uid = 0, life = 0, rareup = 0, count = 0;
                            string name = "", subType = "";
                            try
                            {
                                cardId = (int)selected.id;
                                uid = (int)selected.uid;
                                life = (int)selected.life;
                                rareup = (int)selected.rareup;
                                count = (int)selected.count;
                                name = GameDatabase.GetCardName(cardId) ?? "未知";
                                subType = GameDatabase.GetCardSubType(cardId);
                            }
                            catch { }

                            int baseRare = rareup;
                            string tagsStr = "";
                            try
                            {
                                var node = selected.data;
                                if (node != null)
                                {
                                    baseRare = (int)node.rare;
                                    if (node.tag != null)
                                    {
                                        var parts = new System.Collections.Generic.List<string>();
                                        foreach (var kv in node.tag)
                                            parts.Add($"{kv.Key}:{kv.Value}");
                                        tagsStr = string.Join("  ", parts);
                                    }
                                }
                            }
                            catch { }

                            Color rareColor = Styles.GetRareColor(baseRare);
                            GUI.color = rareColor;
                            GUILayout.Label($"{Styles.GetRareIcon(baseRare)} [UID:{uid}][ID:{cardId}] {name} ({subType})");
                            GUI.color = Color.white;

                            GUILayout.BeginHorizontal();
                            GUILayout.Space(20);
                            GUILayout.Label($"品级: {baseRare}({Styles.GetRareText(baseRare)})", GUILayout.Width(130));

                            // === 苏丹牌显示剩余天数而非生命 ===
                            bool isSudan = subType == "苏丹卡";
                            if (isSudan)
                            {
                                int baseLife = 7;
                                try { baseLife = RuntimeEngine.Save?.SudanCardInitLife ?? 7; } catch { }
                                int remainDays = baseLife - life;
                                if (remainDays < 0) remainDays = 0;
                                GUILayout.Label($"剩余天数: {remainDays}", GUILayout.Width(100));
                            }
                            else
                            {
                                GUILayout.Label($"生命: {life}", GUILayout.Width(70));
                            }
                            GUILayout.Label($"数量: {count}", GUILayout.Width(70));
                            GUILayout.EndHorizontal();

                            if (!string.IsNullOrEmpty(tagsStr))
                            {
                                GUILayout.BeginHorizontal();
                                GUILayout.Space(20);
                                GUILayout.Label($"标签: {tagsStr}");
                                GUILayout.EndHorizontal();
                            }

                            // === 运行时属性编辑 ===
                            GUILayout.Space(4);
                            Styles.DrawSection("运行时属性编辑 (即时生效)", Styles.RuntimeHeader);

                            // 自定义输入行 - 区分苏丹牌和普通牌
                            GUILayout.BeginHorizontal();
                            GUILayout.Space(10);
                            GUILayout.Label("数量:", GUILayout.Width(35));
                            _customCount = GUILayout.TextField(_customCount, GUILayout.Width(40));
                            if (GUILayout.Button("设置", GUILayout.Width(40)) && int.TryParse(_customCount, out int cv))
                            { RuntimeEngine.EditSelectedCardCount(cv); _status = $"数量={cv}"; }

                            GUILayout.Space(10);
                            if (isSudan)
                            {
                                GUILayout.Label("剩余天数:", GUILayout.Width(60));
                                _customLife = GUILayout.TextField(_customLife, GUILayout.Width(40));
                                if (GUILayout.Button("设置", GUILayout.Width(40)) && int.TryParse(_customLife, out int rd))
                                {
                                    int baseLife2 = 7;
                                    try { baseLife2 = RuntimeEngine.Save?.SudanCardInitLife ?? 7; } catch { }
                                    RuntimeEngine.EditSelectedCardLife(baseLife2 - rd);
                                    _status = $"剩余天数={rd} (life={baseLife2-rd})";
                                }
                            }
                            // 非苏丹牌：不显示生命编辑

                            GUILayout.Space(10);
                            GUILayout.Label("品级(1-4):", GUILayout.Width(60));
                            _customRareup = GUILayout.TextField(_customRareup, GUILayout.Width(30));
                            if (GUILayout.Button("设置", GUILayout.Width(40)) && int.TryParse(_customRareup, out int rv))
                            { RuntimeEngine.EditSelectedCardRareup(Mathf.Clamp(rv, 0, 3)); _status = $"品级={rv}"; }
                            GUILayout.EndHorizontal();

                            // 快捷按钮 - 苏丹牌特化
                            GUILayout.BeginHorizontal();
                            GUILayout.Space(10);
                            if (isSudan)
                            {
                                if (GUILayout.Button("剩余天数=99", GUILayout.Width(85)))
                                {
                                    int baseLife3 = 7;
                                    try { baseLife3 = RuntimeEngine.Save?.SudanCardInitLife ?? 7; } catch { }
                                    RuntimeEngine.EditSelectedCardLife(baseLife3 - 99);
                                    _status = "剩余天数=99 (life=-92)";
                                }
                                if (GUILayout.Button("剩余天数=7", GUILayout.Width(80)))
                                {
                                    int baseLife3 = 7;
                                    try { baseLife3 = RuntimeEngine.Save?.SudanCardInitLife ?? 7; } catch { }
                                    RuntimeEngine.EditSelectedCardLife(baseLife3 - 7);
                                    _status = "剩余天数=7";
                                }
                                if (GUILayout.Button("剩余天数=1", GUILayout.Width(80)))
                                {
                                    int baseLife3 = 7;
                                    try { baseLife3 = RuntimeEngine.Save?.SudanCardInitLife ?? 7; } catch { }
                                    RuntimeEngine.EditSelectedCardLife(baseLife3 - 1);
                                    _status = "剩余天数=1";
                                }
                            }
                            else
                            {
                                if (GUILayout.Button("数量=999", GUILayout.Width(70))) { RuntimeEngine.EditSelectedCardCount(999); _status = "数量=999"; }
                                if (GUILayout.Button("数量=99", GUILayout.Width(60))) { RuntimeEngine.EditSelectedCardCount(99); _status = "数量=99"; }
                            }

                            // 品级按钮带颜色
                            GUI.color = Styles.RareStone;  // 白色
                            if (GUILayout.Button("石(1)", GUILayout.Width(55))) { RuntimeEngine.EditSelectedCardBaseRare(1); _status = "石"; }
                            GUI.color = Styles.RareCopper;  // 绿色
                            if (GUILayout.Button("铜(2)", GUILayout.Width(55))) { RuntimeEngine.EditSelectedCardBaseRare(2); _status = "铜"; }
                            GUI.color = Styles.RareSilver;  // 紫色
                            if (GUILayout.Button("银(3)", GUILayout.Width(55))) { RuntimeEngine.EditSelectedCardBaseRare(3); _status = "银"; }
                            GUI.color = Styles.RareGold;  // 金色
                            if (GUILayout.Button("金(4)", GUILayout.Width(55))) { RuntimeEngine.EditSelectedCardBaseRare(4); _status = "金"; }
                            GUI.color = Color.white;
                            GUILayout.EndHorizontal();

                            // 移除按钮 - 从手牌移除(静默) vs 销毁苏丹牌(带动画)
                            GUILayout.BeginHorizontal();
                            GUILayout.Space(10);
                            GUI.color = Styles.ButtonDanger;
                            if (GUILayout.Button("从手牌移除", GUILayout.Width(100), GUILayout.Height(22)))
                            { RuntimeEngine.DestroySelectedCard(); _status = "已移除"; }
                            GUI.color = Color.white;
                            if (isSudan)
                            {
                                GUI.color = Styles.StatusErr;
                                if (GUILayout.Button("销毁苏丹牌(动画)", GUILayout.Width(120), GUILayout.Height(22)))
                                { RuntimeEngine.VanishSelectedCard(); _status = "苏丹牌销毁动画已触发"; }
                                GUI.color = Color.white;
                            }
                            GUILayout.EndHorizontal();

                            // === 标签编辑 (动态+读写+点击切换) ===
                            GUILayout.Space(4);
                            Styles.DrawSection("标签编辑 (点击添加+1, 再次点击删除)", Styles.RuntimeHeader);

                            // 读取当前标签
                            var currentTags = new System.Collections.Generic.Dictionary<string, int>();
                            try
                            {
                                var node = selected.data;
                                if (node?.tag != null)
                                {
                                    foreach (var kv in node.tag)
                                        currentTags[kv.Key] = kv.Value;
                                }
                            }
                            catch { }

                            // 根据卡牌类型选择显示的标签组
                            bool showIdentity = true, showAttr = true, showTag = true;
                            if (subType == "苏丹卡") { showIdentity = false; showAttr = false; }
                            else if (subType == "金币") { showIdentity = false; showAttr = false; }
                            else if (subType == "情报") { showIdentity = false; }

                            // ---- 属性标签 ----
                            if (showAttr)
                            {
                                GUILayout.BeginHorizontal();
                                GUILayout.Label("属性:", GUILayout.Width(35));
                                for (int ti = 0; ti < AttrCodes.Length; ti++)
                                {
                                    string code = AttrCodes[ti];
                                    string label = AttrNames[ti];
                                    bool exists = currentTags.ContainsKey(code);
                                    int val = exists ? currentTags[code] : 0;
                                    GUI.color = exists ? Styles.StatusOk : Styles.ButtonDefault;
                                    string btnLabel = exists ? $"{label}+{val}" : label;
                                    if (GUILayout.Button(btnLabel, GUILayout.Width(52), GUILayout.Height(20)))
                                    {
                                        if (exists) RuntimeEngine.RemoveSelectedCardTag(code);
                                        else RuntimeEngine.EditSelectedCardTag(code, 1);
                                        _status = exists ? $"已删除标签 {code}" : $"标签 {code}=1";
                                    }
                                }
                                GUI.color = Color.white;
                                GUILayout.EndHorizontal();
                            }

                            // ---- 身份/资源标签 ----
                            if (showIdentity)
                            {
                                GUILayout.BeginHorizontal();
                                GUILayout.Label("身份:", GUILayout.Width(35));
                                for (int ti = 0; ti < IdentityCodes.Length; ti++)
                                {
                                    string code = IdentityCodes[ti];
                                    bool exists = currentTags.ContainsKey(code);
                                    int val = exists ? currentTags[code] : 0;
                                    GUI.color = exists ? Styles.StatusOk : Styles.ButtonDefault;
                                    string btnLabel = exists ? $"{IdentityNames[ti]}+{val}" : IdentityNames[ti];
                                    if (GUILayout.Button(btnLabel, GUILayout.Width(52), GUILayout.Height(20)))
                                    {
                                        if (exists) RuntimeEngine.RemoveSelectedCardTag(code);
                                        else RuntimeEngine.EditSelectedCardTag(code, 1);
                                        _status = exists ? $"已删除标签 {code}" : $"标签 {code}=1";
                                    }
                                }
                                GUI.color = Color.white;
                                GUILayout.EndHorizontal();
                            }

                            // ---- 通用标签 ----
                            if (showTag)
                            {
                                GUILayout.BeginHorizontal();
                                GUILayout.Label("资源:", GUILayout.Width(35));
                                for (int ti = 0; ti < GenericCodes.Length; ti++)
                                {
                                    string code = GenericCodes[ti];
                                    bool exists = currentTags.ContainsKey(code);
                                    int val = exists ? currentTags[code] : 0;
                                    GUI.color = exists ? Styles.StatusOk : Styles.ButtonDefault;
                                    string btnLabel = exists ? $"{GenericNames[ti]}+{val}" : GenericNames[ti];
                                    if (GUILayout.Button(btnLabel, GUILayout.Width(52), GUILayout.Height(20)))
                                    {
                                        if (exists) RuntimeEngine.RemoveSelectedCardTag(code);
                                        else RuntimeEngine.EditSelectedCardTag(code, 1);
                                        _status = exists ? $"已删除标签 {code}" : $"标签 {code}=1";
                                    }
                                }
                                GUI.color = Color.white;
                                GUILayout.EndHorizontal();

                                // 苏丹标签仅限苏丹牌
                                if (isSudan) {
                                GUILayout.BeginHorizontal();
                                GUILayout.Label("苏丹:", GUILayout.Width(35));
                                for (int ti = 0; ti < SudanTagCodes.Length; ti++)
                                {
                                    string code = SudanTagCodes[ti];
                                    bool exists = currentTags.ContainsKey(code);
                                    int val = exists ? currentTags[code] : 0;
                                    GUI.color = exists ? Styles.StatusOk : Styles.ButtonDefault;
                                    string btnLabel = exists ? $"{SudanTagNames[ti]}+{val}" : SudanTagNames[ti];
                                    if (GUILayout.Button(btnLabel, GUILayout.Width(52), GUILayout.Height(20)))
                                    {
                                        if (exists) RuntimeEngine.RemoveSelectedCardTag(code);
                                        else RuntimeEngine.EditSelectedCardTag(code, 1);
                                        _status = exists ? $"已删除标签 {code}" : $"标签 {code}=1";
                                    }
                                }
                                GUI.color = Color.white;
                                GUILayout.EndHorizontal();
                                } // end if(isSudan) 苏丹标签专属

                                // 已拥有标签 (code: own)
                                GUILayout.BeginHorizontal();
                                bool hasFixed = currentTags.ContainsKey(OwnedTagCode);
                                int fixedVal = hasFixed ? currentTags[OwnedTagCode] : 0;
                                GUI.color = hasFixed ? Styles.StatusOk : Styles.ButtonDefault;
                                string fixedLabel = hasFixed ? $"已拥有+{fixedVal}" : "已拥有+";
                                if (GUILayout.Button(fixedLabel, GUILayout.Width(60), GUILayout.Height(20)))
                                {
                                    if (hasFixed) RuntimeEngine.RemoveSelectedCardTag(OwnedTagCode);
                                    else RuntimeEngine.EditSelectedCardTag(OwnedTagCode, 1);
                                    _status = hasFixed ? "已删除标签 own" : "标签 own=1";
                                }
                                GUI.color = Color.white;
                                // 列出不在预设中的额外标签
                                var allPreset = new System.Collections.Generic.HashSet<string>(AttrCodes);
                                allPreset.UnionWith(IdentityCodes);
                                allPreset.UnionWith(GenericCodes);
                                allPreset.UnionWith(SudanTagCodes);
                                allPreset.Add(OwnedTagCode);
                                foreach (var kv in currentTags)
                                {
                                    if (!allPreset.Contains(kv.Key))
                                    {
                                        GUI.color = new Color(1f, 0.6f, 0f);
                                        if (GUILayout.Button($"{kv.Key}+{kv.Value}", GUILayout.Width(65), GUILayout.Height(20)))
                                        { RuntimeEngine.RemoveSelectedCardTag(kv.Key); _status = $"已删除标签 {kv.Key}"; }
                                        GUI.color = Color.white;
                                    }
                                }
                                GUILayout.EndHorizontal();
                            }

                            // ---- 自定义 Key/Val 输入 ----
                            GUILayout.BeginHorizontal();
                            GUILayout.Label("自定义Key:", GUILayout.Width(65));
                            _tagKey = GUILayout.TextField(_tagKey, GUILayout.Width(70));
                            GUILayout.Label("Val:", GUILayout.Width(25));
                            _tagVal = GUILayout.TextField(_tagVal, GUILayout.Width(40));
                            if (GUILayout.Button("设置", GUILayout.Width(45)) && !string.IsNullOrEmpty(_tagKey) && int.TryParse(_tagVal, out int tv))
                            {
                                RuntimeEngine.EditSelectedCardTag(_tagKey, tv);
                                _status = $"标签 {_tagKey}={tv}";
                            }
                            if (!string.IsNullOrEmpty(_tagKey) && currentTags.ContainsKey(_tagKey))
                            {
                                GUI.color = Styles.StatusErr;
                                if (GUILayout.Button("删除", GUILayout.Width(45)))
                                { RuntimeEngine.RemoveSelectedCardTag(_tagKey); _status = $"已删除标签 {_tagKey}"; }
                                GUI.color = Color.white;
                            }
                            GUILayout.EndHorizontal();

                            // ---- 批量操作 ----
                            GUILayout.BeginHorizontal();
                            GUILayout.Space(10);
                            if (GUILayout.Button("全属性+10", GUILayout.Width(80)))
                            {
                                foreach (string tc in AttrCodes)
                                    RuntimeEngine.EditSelectedCardTag(tc, 10);
                                _status = "全属性+10";
                            }
                            if (GUILayout.Button("全属性+100", GUILayout.Width(80)))
                            {
                                foreach (string tc in AttrCodes)
                                    RuntimeEngine.EditSelectedCardTag(tc, 100);
                                _status = "全属性+100";
                            }
                            if (GUILayout.Button("全属性+999", GUILayout.Width(80)))
                            {
                                foreach (string tc in AttrCodes)
                                    RuntimeEngine.EditSelectedCardTag(tc, 999);
                                _status = "全属性+999";
                            }
                            if (GUILayout.Button("清除所有标签", GUILayout.Width(85)))
                            {
                                foreach (var kv in currentTags)
                                    RuntimeEngine.RemoveSelectedCardTag(kv.Key);
                                _status = "所有标签已清除";
                            }
                            GUILayout.EndHorizontal();
                        }
                        else
                        {
                            GUILayout.Label("(未选中任何卡牌 - 请在游戏中点击一张卡牌)");
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Main.Log?.Warning($"[CardTab] 异常: {ex}");
                    GUILayout.Label("(获取选中卡牌失败)");
                }
            }
            else
            {
                GUILayout.Label("(游戏未运行)");
            }
            GUILayout.Space(8);

            // ===== 注入新卡牌 =====
            Styles.DrawSection("注入新卡牌", Styles.RuntimeHeader);
            GUILayout.Label($"共 {ConfigManager.CardCount} 张");

            GUILayout.BeginHorizontal();
            GUILayout.Label("搜索:", GUILayout.Width(40));
            _search = GUILayout.TextField(_search, GUILayout.Width(150));
            GUILayout.EndHorizontal();

            // 分类筛选行（新分类体系）
            GUILayout.BeginHorizontal();
            GUILayout.Label("分类:", GUILayout.Width(35));
            for (int i = 0; i < CategoryTypes.Length; i++)
            {
                bool isActive = _filterType == CategoryTypes[i];
                GUI.color = isActive ? Styles.TabActive : Styles.ButtonDefault;
                if (GUILayout.Button(CategoryTypes[i], GUILayout.Width(50), GUILayout.Height(22))) _filterType = CategoryTypes[i];
            }
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            // 稀有度筛选行
            GUILayout.BeginHorizontal();
            GUILayout.Label("品级:", GUILayout.Width(35));
            string[] rarityFilters = { "全部", "石", "铜", "银", "金" };
            Color[] rarityColors = { Color.white, Styles.RareStone, Styles.RareCopper, Styles.RareSilver, Styles.RareGold };
            for (int ri = 0; ri < rarityFilters.Length; ri++)
            {
                bool isActive = _rarityFilter == rarityFilters[ri];
                GUI.color = isActive ? Styles.TabActive : rarityColors[ri];
                if (GUILayout.Button(rarityFilters[ri], GUILayout.Width(45), GUILayout.Height(22))) _rarityFilter = rarityFilters[ri];
            }
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            // 卡牌列表
            bool searched = !string.IsNullOrEmpty(_search);
            string sl = _search.ToLower();
            int shown = 0;

            // 显示上限：全部=1000, 搜索=500, 子分类筛选=500
            int maxShow;
            if (searched) maxShow = 500;
            else if (_filterType == "全部" && _rarityFilter == "全部") maxShow = 1000;
            else maxShow = 500;

            foreach (var kvp in GameDatabase.CardNames)
            {
                int id = kvp.Key;
                string name = kvp.Value;
                string category = ConfigManager.GetCardCategory(id);
                if (_filterType != "全部" && category != _filterType) continue;
                if (searched && !name.ToLower().Contains(sl) && !id.ToString().Contains(_search)) continue;

                // 稀有度筛选
                int cardRare = ConfigManager.GetCardBaseRare(id);
                string rareName = ConfigManager.GetRarityText(cardRare);
                if (_rarityFilter != "全部" && rareName != _rarityFilter) continue;

                shown++;
                if (shown > maxShow) { GUILayout.Label($"... 前{maxShow}张 (共{GameDatabase.CardNames.Count}张, 搜索缩小范围)"); break; }

                // 按稀有度着色
                Color c = GetListCardColor(id, "全部"); // 使用稀有度颜色而非子类型

                GUI.color = c;
                GUILayout.BeginHorizontal();
                GUILayout.Label($"[{id}] {name}  [{rareName}]", GUILayout.Width(270));
                GUI.color = Color.white;
                GUILayout.Label($"({category})", GUILayout.Width(50));
                if (GUILayout.Button("+1", GUILayout.Width(45), GUILayout.Height(22)) && ready)
                { RuntimeEngine.AddCard(id); _status = $"已注入: {name}"; }
                if (GUILayout.Button("+10", GUILayout.Width(45), GUILayout.Height(22)) && ready)
                { for (int i2 = 0; i2 < 10; i2++) RuntimeEngine.AddCard(id); _status = $"已注入: {name} x10"; }
                GUILayout.EndHorizontal();
            }
            if (shown == 0) GUILayout.Label("(无匹配)");

            Styles.DrawStatus(_status, Styles.StatusOk);
            GUILayout.EndScrollView();
        }
    }
}
