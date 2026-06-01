using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Il2Cpp;
using SultansGameMod.Data;
using SultansGameMod.Harmony;

namespace SultansGameMod.UI
{
    /// <summary>
    /// 原生对话框菜单系统 — 替代旧的 Canvas GameNativeUI。
    /// 所有菜单使用游戏自带的 OptionController（选项列表）和
    /// ConfirmController（确认对话框），风格与游戏本体完全一致。
    ///
    /// F1 / ESC菜单 → (首次)欢迎确认 → OptionController(主菜单) → 子菜单
    /// </summary>
    internal static class NativeMenuSystem
    {
        // ============================================================
        // 数据
        // ============================================================

        // 卡牌分类列表
        private static readonly string[] CardCategories =
        {
            "全部", "角色", "装备", "秘宝", "神怪", "动物",
            "消耗品", "苏丹卡", "大餐", "读物", "思想", "部队", "其他",
        };

        private const int CardsPerPage = 10;

        private static readonly int[] SudanCardIds =
        {
            2010001,2010002,2010003,2010004,2010005,
            2010006,2010007,2010008,2010009,2010010,
            2010011,2010012,2010013,2010014,2010015,2010016,
            2000512,2000513,2000514,2000515,
        };

        private static readonly string[] AllAttrKeys =
        {
            "physique","charm","wisdom","conceal","battle",
            "social","knowledge","survival","magic","influence","support",
        };

        // 结局 ID 分类
        private static readonly int[] BasicEndingIds =
        {
            1,2,3,4,5,7,8,9,10,11,12,13,14,15,16,17,18,19,20,
            21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,41,42,43,
        };

        private static readonly int[] SpecialEndingIds = { 100, 101, 102, 103 };

        // 高级结局 200-294 (排除废弃 ID)
        private static readonly int[] AdvancedEndingIds;
        private static readonly int[] RareEndingIds = { 301, 302, 303 };
        private static readonly int[] ExtendedEndingIds = { 401, 402, 403, 404, 405, 406 };
        private static readonly int[] HiddenEndingIds = { 501, 502, 601, 602, 603, 604 };

        // F1 欢迎记忆
        private static bool _welcomeShown;

        // 卡牌浏览位置记忆（查看立绘后可恢复）
        private static string? _lastCardCategory;
        private static int _lastCardRareFilter;
        private static int _lastCardPage;

        static NativeMenuSystem()
        {
            // 构建高级结局列表, 排除 271(同270) 等已知不存在的ID
            var adv = new List<int>();
            for (int i = 200; i <= 294; i++)
            {
                string? name = null;
                try { name = GameDatabase.GetEndingName(i); } catch { }
                if (!string.IsNullOrEmpty(name))
                    adv.Add(i);
            }
            AdvancedEndingIds = adv.ToArray();
        }

        // ============================================================
        // 辅助方法
        // ============================================================

        private static Il2Cpp.GameController? GC
        {
            get
            {
                try { return GameController.Inst; }
                catch { return null; }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool CheckReady()
        {
            try { return GameController.Inst != null; }
            catch { return false; }
        }

        private static string GetStatus()
        {
            return "";
        }

        private static string GetCardInfo()
        {
            try
            {
                var sel = GC?.CurrentSelect;
                if (sel == null) return "";
                int rare = (int)(sel.data?.rare ?? 0);
                string[] rt = { "", "石", "铜", "银", "金" };
                string rl = rare >= 1 && rare <= 4 ? rt[rare] : "?";
                string nm = "";
                try { nm = GameDatabase.GetCardName((int)sel.id) ?? ""; } catch { }
                int cnt = (int)sel.count;
                return $" | 选中: [{rl}] {nm} x{cnt}";
            }
            catch { return ""; }
        }

        private static void RunSafe(Action a, string? label = null)
        {
            try { a(); }
            catch (Exception ex) { Main.Log?.Warning($"[NativeMenu] {label ?? "操作"} 异常: {ex.Message}"); }
        }

        /// <summary>
        /// 弹出原生确认框 → 确认后执行 onConfirm → 然后执行 afterAction
        /// 可选传入 Card 对象作为 icon 参数（在对话框右侧显示立绘）
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ConfirmThen(string message, Action onConfirm, Action afterAction,
            Il2CppSystem.Object? icon = null)
        {
            var gc = GC;
            if (gc == null) { afterAction(); return; }

            var promise = gc.ShowConfirm(message, icon, "确认", "取消", "", "");
            if (promise != null)
            {
                promise.Then((Action<bool>)(result =>
                {
                    if (result)
                    {
                        RunSafe(onConfirm, message);
                        Main.Log?.Msg($"[NativeMenu] 确认执行: {message}");
                    }
                    else
                    {
                        Main.Log?.Msg($"[NativeMenu] 取消: {message}");
                    }
                    afterAction();
                }));
            }
            else
            {
                RunSafe(onConfirm, message);
                afterAction();
            }
        }

        private static void DoThenReload(Action action, Action reloadMenu)
        {
            RunSafe(action);
            reloadMenu();
        }

        /// <summary>执行会改变游戏状态的操作，不重显菜单（操作后游戏可能关闭所有UI）</summary>
        private static void DoThenClose(Action action)
        {
            RunSafe(action);
        }

        /// <summary>确认后执行状态改变操作</summary>
        private static void ConfirmThenClose(string message, Action onConfirm)
        {
            ConfirmThen(message, onConfirm, () => { });
        }

        // ===== 翻页工具 =====

        private const int MenuPageSize = 9;

        /// <summary>从完整列表构建单页菜单项，自动加上翻页/关闭/返回</summary>
        private static List<(string, Action)> BuildPage(
            List<(string, Action)> allItems, int page,
            Action<int> goToPage, Action backAction)
        {
            int totalPages = (allItems.Count + MenuPageSize - 1) / MenuPageSize;
            if (totalPages == 0) totalPages = 1;
            if (page >= totalPages) page = totalPages - 1;

            int start = page * MenuPageSize;
            int end = Math.Min(start + MenuPageSize, allItems.Count);
            var result = allItems.GetRange(start, end - start);

            if (page > 0)
                result.Insert(0, ($"< 上一页 ({page}/{totalPages})", () => goToPage(page - 1)));
            if (page < totalPages - 1)
                result.Add(($"下一页 > ({page + 2}/{totalPages})", () => goToPage(page + 1)));

            result.Add(("< 返回", backAction));
            return result;
        }

        /// <summary>从完整列表构建单页菜单项，保留列表前 prefixItems 项不动</summary>
        private static List<(string, Action)> BuildPageWithPrefix(
            List<(string, Action)> prefixItems,
            List<(string, Action)> allItems, int page,
            Action<int> goToPage, Action backAction)
        {
            int totalPages = (allItems.Count + MenuPageSize - 1) / MenuPageSize;
            if (totalPages == 0) totalPages = 1;
            if (page >= totalPages) page = totalPages - 1;

            int start = page * MenuPageSize;
            int end = Math.Min(start + MenuPageSize, allItems.Count);
            var result = new List<(string, Action)>(prefixItems);
            result.AddRange(allItems.GetRange(start, end - start));

            if (page > 0)
                result.Add(($"< 上一页 ({page}/{totalPages})", () => goToPage(page - 1)));
            if (page < totalPages - 1)
                result.Add(($"下一页 > ({page + 2}/{totalPages})", () => goToPage(page + 1)));

            result.Add(("< 返回", backAction));
            return result;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ApplyAllAttrs(int val)
        {
            foreach (var key in AllAttrKeys)
                RuntimeEngine.EditSelectedCardTag(key, val);
            Main.Log?.Msg($"[NativeMenu] 全属性 +{val}");
        }

        /// <summary>判断分类是否属于可堆叠类型（添加时设置 count 而非多张卡）</summary>
        private static bool IsStackableCategory(string cat)
        {
            return cat == "消耗品" || cat == "部队" || cat == "大餐";
        }

        /// <summary>添加卡牌：普通卡循环生成多张，可堆叠卡生成1张设 count</summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void AddCardWithCount(int cardId, int count)
        {
            string cat = "其他";
            try { cat = ConfigManager.GetCardCategory(cardId); } catch { }

            if (IsStackableCategory(cat))
            {
                var gc = GC;
                if (gc == null) return;
                var card = gc.GenCard(cardId);
                if (card != null) { card.count = count; gc.AddCard(card, true); }
                Main.Log?.Msg($"[NativeMenu] 添加可堆叠卡 ID:{cardId} (类别:{cat}) x{count}");
            }
            else
            {
                for (int i = 0; i < count; i++)
                    RuntimeEngine.AddCard(cardId);
                Main.Log?.Msg($"[NativeMenu] 添加卡牌 ID:{cardId} (类别:{cat}) x{count}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ClearAllTags()
        {
            try
            {
                var gc = GC;
                var sel = gc?.CurrentSelect;
                if (sel?.data?.tag == null) { Main.Log?.Msg("[NativeMenu] 无卡牌选中或无标签"); return; }
                var keys = new List<string>();
                foreach (var kv in sel.data.tag) keys.Add(kv.Key);
                foreach (var k in keys) RuntimeEngine.RemoveSelectedCardTag(k);
                Main.Log?.Msg($"[NativeMenu] 已清除 {keys.Count} 个标签");
            }
            catch (Exception ex) { Main.Log?.Warning($"[NativeMenu] ClearAllTags: {ex.Message}"); }
        }

        /// <summary>获取当前选中卡牌（用于 dialog icon）</summary>
        private static Il2Cpp.Card? GetSelectedCard()
        {
            try { return GC?.CurrentSelect; }
            catch { return null; }
        }

        /// <summary>
        /// 根据结局 ID 数组构建菜单项列表
        /// </summary>
        private static List<(string, Action)> BuildEndingItems(int[] endingIds, Action backAction)
        {
            var items = new List<(string, Action)>();
            foreach (int overId in endingIds)
            {
                string name;
                try { name = GameDatabase.GetEndingName(overId) ?? $"结局{overId}"; }
                catch { name = $"结局{overId}"; }
                int id = overId;
                items.Add(($"[{id}] {name}", () => ConfirmThen(
                    $"确定要触发结局「{name}」(ID:{id})吗？\n游戏将结束。",
                    () =>
                    {
                        RuntimeEngine.SetGameOverById(id);
                        Main.Log?.Msg($"[NativeMenu] 触发结局: {name} (ID:{id})");
                    },
                    backAction)));
            }
            return items;
        }

        // ============================================================
        // 入口 — F1 / ESC 按钮
        // ============================================================

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Open()
        {
            if (!_welcomeShown)
            {
                _welcomeShown = true;
                var gc = GC;
                if (gc != null)
                {
                    var promise = gc.ShowConfirm(
                        "欢迎使用 苏丹的游戏 修改器 v1.2.0\n\n" +
                        "本Mod由 Mizuof 制作\n\n" +
                        "是否打开修改器？",
                        null, "确认", "取消", "", ""
                    );
                    if (promise != null)
                    {
                        promise.Then((Action<bool>)(result =>
                        {
                            if (result)
                            {
                                Main.Log?.Msg("[NativeMenu] F1 → 用户确认，打开主菜单");
                                ShowMainMenu();
                            }
                            else
                            {
                                Main.Log?.Msg("[NativeMenu] F1 → 用户取消");
                            }
                        }));
                        return;
                    }
                }
            }

            ShowMainMenu();
        }

        // ============================================================
        // 主菜单
        // ============================================================

        private static void ShowMainMenu()
        {
            if (!CheckReady()) { Main.Log?.Warning("[NativeMenu] 游戏未就绪，无法打开菜单"); return; }
            var gc = GC!;

            var items = new List<(string, Action)>
            {
                ("回合控制", () => ShowRoundMenu(0)),
                ("卡牌操作", () => ShowCardMenu(0)),
                ("自定义", () => ShowCustomMenu(0)),
                ("事件仪式", ShowEventRiteMenu),
                ("添加卡牌", () => ShowAddCardMenu(0)),
            };

            // 继续浏览卡牌（查看立绘后可恢复位置）
            if (_lastCardCategory != null)
            {
                string resumeLabel = _lastCardRareFilter > 0
                    ? $"继续浏览 [{_lastCardCategory}] 品质{_lastCardRareFilter}"
                    : $"继续浏览 [{_lastCardCategory}]";
                items.Add((resumeLabel, () => ShowCardListPage(_lastCardCategory, _lastCardRareFilter, _lastCardPage)));
            }

            items.Add(("随机角色立绘", () => { PortraitViewer.ShowRandomCharacter(); }));
            items.Add(("关于修改器", ShowAboutDialog));
            items.Add(("< 返回", () => { }));

            NativeOptionHandler.ShowOption($"修改器 v1.2.0", items, gc);
        }

        // ============================================================
        // 1. 回合控制
        // ============================================================

        private static void ShowRoundMenu(int page = 0)
        {
            if (!CheckReady()) return;
            var gc = GC!;

            string freezeText = TimePatches.FreezeTime ? "解除冻结回合 [冻结]" : "冻结回合 [未冻结]";

            var allItems = new List<(string, Action)>
            {
                ("下一回合", () => ConfirmThenClose("确定要进入下一回合吗？", () =>
                {
                    if (TimePatches.FreezeTime) { Main.Log?.Msg("[NativeMenu] 回合已冻结"); return; }
                    RuntimeEngine.NextRound();
                })),
                ("上一回合", () => ConfirmThenClose("返回上一回合将丢失当前回合进度, 是否继续?", () =>
                {
                    if (TimePatches.FreezeTime) { Main.Log?.Msg("[NativeMenu] 回合已冻结"); return; }
                    RuntimeEngine.PrevRound();
                })),
                (freezeText, () => DoThenReload(() =>
                {
                    TimePatches.FreezeTime = !TimePatches.FreezeTime;
                }, () => ShowRoundMenu(page))),
                ("强制保存", () => ConfirmThenClose("确定要强制保存吗？", () => RuntimeEngine.SaveGame())),
                ("重载当前回合", () => ConfirmThenClose("确定要重载当前回合吗？未保存的进度将丢失。", () => RuntimeEngine.ApplySaveChanges())),
                ("游戏变速 →", () => ShowSpeedMenu(0)),
                ("触发结局 →", () => ShowDeathMenu(0)),
            };

            var pageItems = BuildPage(allItems, page, p => ShowRoundMenu(p), ShowMainMenu);
            NativeOptionHandler.ShowOption($"回合控制 ({page + 1}/{(allItems.Count + MenuPageSize - 1) / MenuPageSize})", pageItems, gc);
        }

        private static void ShowSpeedMenu(int page = 0)
        {
            if (!CheckReady()) return;
            var gc = GC!;

            var allItems = new List<(string, Action)>
            {
                ("0.5x", () => DoThenReload(() => { UnityEngine.Time.timeScale = 0.5f; }, () => ShowSpeedMenu(page))),
                ("1x (正常)", () => DoThenReload(() => { UnityEngine.Time.timeScale = 1f; }, () => ShowSpeedMenu(page))),
                ("2x", () => DoThenReload(() => { UnityEngine.Time.timeScale = 2f; }, () => ShowSpeedMenu(page))),
                ("5x", () => DoThenReload(() => { UnityEngine.Time.timeScale = 5f; }, () => ShowSpeedMenu(page))),
                ("10x", () => DoThenReload(() => { UnityEngine.Time.timeScale = 10f; }, () => ShowSpeedMenu(page))),
            };

            var pageItems = BuildPage(allItems, page, p => ShowSpeedMenu(p), () => ShowRoundMenu(0));
            NativeOptionHandler.ShowOption("游戏变速", pageItems, gc);
        }

        // ============================================================
        // 触发结局（含分类子菜单）
        // ============================================================

        private static void ShowDeathMenu(int page = 0)
        {
            if (!CheckReady()) return;
            var gc = GC!;

            var quickDeaths = new (string label, int overId)[]
            {
                ("触怒苏丹", 1), ("决斗而死", 3), ("被刺杀", 4), ("处刑到期", 12),
                ("啊！死了", 7), ("永恒之梦", 2), ("欢愉极致", 5),
            };

            var allItems = new List<(string, Action)>();
            foreach (var (label, overId) in quickDeaths)
            {
                int id = overId;
                allItems.Add(($"{label}", () => ConfirmThenClose($"确定要触发结局「{label}」吗？", () =>
                {
                    RuntimeEngine.SetGameOverById(id);
                    Main.Log?.Msg($"[NativeMenu] 触发结局: {label}");
                })));
            }
            allItems.Add(("基础结局 (42个) >", () => ShowBasicEndingsMenu(0)));
            allItems.Add(("特殊结局 (4个) >", () => ShowSpecialEndingsMenu(0)));
            allItems.Add(("高级结局 (95+个) >", () => ShowAdvancedEndingsMenu(0)));
            allItems.Add(("默认游戏结束", () => ConfirmThenClose("确定要触发默认游戏结束吗？", () =>
            {
                RuntimeEngine.DoGameOver();
                Main.Log?.Msg("[NativeMenu] 默认游戏结束");
            })));

            var pageItems = BuildPage(allItems, page, p => ShowDeathMenu(p), () => ShowRoundMenu(0));
            NativeOptionHandler.ShowOption($"触发结局 ({page + 1}/{(allItems.Count + MenuPageSize - 1) / MenuPageSize})", pageItems, gc);
        }

        private static void ShowBasicEndingsMenu(int page = 0)
        {
            if (!CheckReady()) return;
            var gc = GC!;
            var allItems = BuildEndingItems(BasicEndingIds, () => ShowDeathMenu(0));
            var pageItems = BuildPage(allItems, page, p => ShowBasicEndingsMenu(p), () => ShowDeathMenu(0));
            NativeOptionHandler.ShowOption($"基础结局 ({BasicEndingIds.Length}个)", pageItems, gc);
        }

        private static void ShowSpecialEndingsMenu(int page = 0)
        {
            if (!CheckReady()) return;
            var gc = GC!;
            var allItems = BuildEndingItems(SpecialEndingIds, () => ShowDeathMenu(0));
            var pageItems = BuildPage(allItems, page, p => ShowSpecialEndingsMenu(p), () => ShowDeathMenu(0));
            NativeOptionHandler.ShowOption($"特殊结局 ({SpecialEndingIds.Length}个)", pageItems, gc);
        }

        private static void ShowAdvancedEndingsMenu(int page = 0)
        {
            if (!CheckReady()) return;
            var gc = GC!;
            var allItems = BuildEndingItems(AdvancedEndingIds, () => ShowDeathMenu(0));
            var pageItems = BuildPage(allItems, page, p => ShowAdvancedEndingsMenu(p), () => ShowDeathMenu(0));
            NativeOptionHandler.ShowOption($"高级结局 ({AdvancedEndingIds.Length}个)", pageItems, gc);
        }

        // ============================================================
        // 2. 卡牌操作
        // ============================================================

        private static void ShowCardMenu(int page = 0)
        {
            if (!CheckReady()) return;
            var gc = GC!;

            var allItems = new List<(string, Action)>
            {
                ("数量 = 999", () => DoThenReload(() => RuntimeEngine.EditSelectedCardCount(999), () => ShowCardMenu(page))),
                ("数量 = 99", () => DoThenReload(() => RuntimeEngine.EditSelectedCardCount(99), () => ShowCardMenu(page))),
                ("数量 = 1", () => DoThenReload(() => RuntimeEngine.EditSelectedCardCount(1), () => ShowCardMenu(page))),
                ("品级: 石 (1)", () => DoThenReload(() => RuntimeEngine.EditSelectedCardBaseRare(1), () => ShowCardMenu(page))),
                ("品级: 铜 (2)", () => DoThenReload(() => RuntimeEngine.EditSelectedCardBaseRare(2), () => ShowCardMenu(page))),
                ("品级: 银 (3)", () => DoThenReload(() => RuntimeEngine.EditSelectedCardBaseRare(3), () => ShowCardMenu(page))),
                ("品级: 金 (4)", () => DoThenReload(() => RuntimeEngine.EditSelectedCardBaseRare(4), () => ShowCardMenu(page))),
                ("从手牌移除", () => ConfirmThen("确定要从手牌移除选中卡牌吗？", () =>
                {
                    RuntimeEngine.DestroySelectedCard();
                    Main.Log?.Msg("[NativeMenu] 已从手牌移除");
                }, () => ShowCardMenu(page), GetSelectedCard())),
                ("销毁苏丹牌 (动画)", () => ConfirmThen("确定要销毁苏丹牌并触发动画吗？", () =>
                {
                    RuntimeEngine.VanishSelectedCard();
                    Main.Log?.Msg("[NativeMenu] 已销毁苏丹牌");
                }, () => ShowCardMenu(page), GetSelectedCard())),
                ("全属性 +10", () => DoThenReload(() => ApplyAllAttrs(10), () => ShowCardMenu(page))),
                ("全属性 +100", () => DoThenReload(() => ApplyAllAttrs(100), () => ShowCardMenu(page))),
                ("全属性 +999", () => DoThenReload(() => ApplyAllAttrs(999), () => ShowCardMenu(page))),
                ("清除所有标签", () => ConfirmThen("确定要清除选中卡牌的所有标签吗？", () => { ClearAllTags(); }, () => ShowCardMenu(page))),
            };

            var pageItems = BuildPage(allItems, page, p => ShowCardMenu(p), ShowMainMenu);
            NativeOptionHandler.ShowOption($"卡牌操作{GetCardInfo()} ({page + 1}/{(allItems.Count + MenuPageSize - 1) / MenuPageSize})", pageItems, gc);
        }

        // ============================================================
        // 3. 自定义
        // ============================================================

        private static void ShowCustomMenu(int page = 0)
        {
            if (!CheckReady()) return;
            var gc = GC!;

            var counterDefs = new (string label, int counterId)[]
            {
                ("善名 +999", 7100001), ("恶名 +999", 7100002), ("权势 +999", 7100003), ("侠名 +999", 7100004),
                ("灵视 +999", 7100005), ("金骰子 +999", 7100006), ("上回合次数 +999", 7100007), ("重抽次数 +999", 7100008),
            };

            var allItems = new List<(string, Action)>();

            foreach (var (label, counterId) in counterDefs)
            {
                int cid = counterId; string lbl = label;
                allItems.Add((lbl, () => DoThenReload(() =>
                {
                    NativeOptionHandler.AddCounterDirect(cid, 999);
                    Main.Log?.Msg($"[NativeMenu] {lbl}");
                }, () => ShowCustomMenu(page))));
            }

            allItems.Add(("全部声望 +999", () => DoThenReload(() =>
            {
                foreach (var (_, cid) in counterDefs) NativeOptionHandler.AddCounterDirect(cid, 999);
                Main.Log?.Msg("[NativeMenu] 全部声望 +999");
            }, () => ShowCustomMenu(page))));

            allItems.Add(("全部苏丹牌 (20张)", () => DoThenClose(() =>
            {
                RuntimeEngine.AddSudanCardsViaPool(SudanCardIds);
            })));

            allItems.Add(("销毁全部苏丹牌 (动画)", () => ConfirmThenClose("确定要销毁当前全部苏丹牌吗？牌面将被撕毁。", () =>
            {
                int count = RuntimeEngine.VanishAllSudanCards();
                Main.Log?.Msg($"[NativeMenu] 已销毁 {count} 张苏丹牌");
            })));

            allItems.Add(("获得 100 金币", () => DoThenReload(() =>
            {
                try { var gc2 = GC; if (gc2 != null) { var coin = gc2.GenCard(2000029); if (coin != null) { coin.count = 100; gc2.AddCard(coin, true); } } }
                catch { }
                Main.Log?.Msg("[NativeMenu] 已获得 100 金币");
            }, () => ShowCustomMenu(page))));

            var pageItems = BuildPage(allItems, page, p => ShowCustomMenu(p), ShowMainMenu);
            NativeOptionHandler.ShowOption($"自定义 ({page + 1}/{(allItems.Count + MenuPageSize - 1) / MenuPageSize})", pageItems, gc);
        }

        // ============================================================
        // 4. 事件仪式（分类浏览全部事件和仪式）
        // ============================================================

        private const int EventsPerPage = 10;
        private const int RitesPerPage = 10;

        private static void ShowEventRiteMenu()
        {
            if (!CheckReady()) return;
            var gc = GC!;
            var items = new List<(string, Action)>
            {
                ("查看活跃事件", () => ShowActiveEvents(0)),
                ("浏览事件 (按ID范围)", () => ShowEventRangeMenu(0)),
                ("浏览仪式 (按ID范围)", () => ShowRiteRangeMenu(0)),
                ("< 返回主菜单", ShowMainMenu),
            };
            NativeOptionHandler.ShowOption("事件仪式", items, gc);
        }

        // ----- 活跃事件 -----

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ShowActiveEvents(int page = 0)
        {
            if (!CheckReady()) return;
            var gc = GC!;
            var allItems = new List<(string, Action)>();

            try
            {
                var active = gc.EventTrigger.GetActiveEvents();
                if (active != null && active.Count > 0)
                {
                    foreach (var evt in active)
                    {
                        if (evt == null) continue;
                        int evtId = (int)evt.id;
                        string evtName;
                        try { evtName = EventNameCache.GetEventName(evtId) ?? $"事件 {evtId}"; }
                        catch { evtName = $"事件 {evtId}"; }
                        int id = evtId;
                        allItems.Add(($"[活跃] {evtName} (ID:{id})", () => DoThenReload(() =>
                        {
                            RuntimeEngine.RemoveEvent(id);
                        }, () => ShowActiveEvents(page))));
                    }
                }
            }
            catch (Exception ex)
            {
                Main.Log?.Warning($"[NativeMenu] 获取活跃事件异常: {ex.Message}");
            }

            if (allItems.Count == 0)
                allItems.Add(("(当前无活跃事件)", () => { }));

            var pageItems = BuildPage(allItems, page, p => ShowActiveEvents(p), ShowEventRiteMenu);
            NativeOptionHandler.ShowOption("活跃事件", pageItems, gc);
        }

        // ----- 事件浏览 -----

        private static void ShowEventRangeMenu(int page = 0)
        {
            if (!CheckReady()) return;
            var gc = GC!;

            var ranges = new List<(string, Action)>();
            try
            {
                var ids = GameDatabase.EventIds;
                if (ids != null && ids.Length > 0)
                {
                    int rangeStart = ids[0];
                    int rangeEnd = ids[0];
                    for (int i = 1; i < ids.Length; i++)
                    {
                        if (ids[i] / 10000 == rangeStart / 10000)
                            rangeEnd = ids[i];
                        else
                        {
                            ranges.Add(($"{rangeStart / 10000}xxxx ({rangeStart}-{rangeEnd})", () => ShowEventListPage(rangeStart, rangeEnd, 0)));
                            rangeStart = ids[i];
                            rangeEnd = ids[i];
                        }
                    }
                    ranges.Add(($"{rangeStart / 10000}xxxx ({rangeStart}-{rangeEnd})", () => ShowEventListPage(rangeStart, rangeEnd, 0)));
                }
            }
            catch { }

            var pageItems = BuildPage(ranges, page, p => ShowEventRangeMenu(p), ShowEventRiteMenu);
            NativeOptionHandler.ShowOption($"事件浏览 (共 {GameDatabase.EventIds?.Length ?? 0} 个事件)", pageItems, gc);
        }

        private static void ShowRiteRangeMenu(int page = 0)
        {
            if (!CheckReady()) return;
            var gc = GC!;

            var ranges = new List<(string, Action)>();
            try
            {
                var ids = GameDatabase.RiteIds;
                if (ids != null && ids.Length > 0)
                {
                    int rangeStart = ids[0];
                    int rangeEnd = ids[0];
                    for (int i = 1; i < ids.Length; i++)
                    {
                        if (ids[i] / 100 == rangeStart / 100)
                            rangeEnd = ids[i];
                        else
                        {
                            ranges.Add(($"{rangeStart / 100}xx ({rangeStart}-{rangeEnd})", () => ShowRiteListPage(rangeStart, rangeEnd, 0)));
                            rangeStart = ids[i];
                            rangeEnd = ids[i];
                        }
                    }
                    ranges.Add(($"{rangeStart / 100}xx ({rangeStart}-{rangeEnd})", () => ShowRiteListPage(rangeStart, rangeEnd, 0)));
                }
            }
            catch { }

            var pageItems = BuildPage(ranges, page, p => ShowRiteRangeMenu(p), ShowEventRiteMenu);
            NativeOptionHandler.ShowOption($"仪式浏览 (共 {GameDatabase.RiteIds?.Length ?? 0} 个仪式)", pageItems, gc);
        }

        private static void ShowEventListPage(int rangeMin, int rangeMax, int page)
        {
            if (!CheckReady()) return;
            var gc = GC!;

            var evtList = new List<(int id, string name)>();
            try
            {
                foreach (int eid in GameDatabase.EventIds)
                {
                    if (eid >= rangeMin && eid <= rangeMax)
                    {
                        string ename = EventNameCache.GetEventName(eid) ?? $"事件 {eid}";
                        evtList.Add((eid, ename));
                    }
                }
            }
            catch { }

            int totalPages = (evtList.Count + EventsPerPage - 1) / EventsPerPage;
            if (totalPages == 0) totalPages = 1;
            if (page >= totalPages) page = totalPages - 1;
            int start = page * EventsPerPage;
            int end = Math.Min(start + EventsPerPage, evtList.Count);

            var items = new List<(string, Action)>();

            if (page > 0)
                items.Add(("<- 上一页", () => ShowEventListPage(rangeMin, rangeMax, page - 1)));

            for (int i = start; i < end; i++)
            {
                var (eid, ename) = evtList[i];
                int eventId = eid;
                string eventName = ename;
                items.Add(($"触发: {eventName} (ID:{eventId})", () => DoThenReload(() =>
                {
                    RuntimeEngine.TriggerEvent(eventId);
                    Main.Log?.Msg($"[NativeMenu] 触发事件: {eventName} (ID:{eventId})");
                }, () => ShowEventListPage(rangeMin, rangeMax, page))));
            }

            if (page < totalPages - 1)
                items.Add(("下一页 ->", () => ShowEventListPage(rangeMin, rangeMax, page + 1)));

            items.Add(("< 返回范围", () => ShowEventRangeMenu(0)));

            NativeOptionHandler.ShowOption($"事件 — 第 {page + 1}/{totalPages} 页 (共 {evtList.Count} 个)", items, gc);
        }

        private static void ShowRiteListPage(int rangeMin, int rangeMax, int page)
        {
            if (!CheckReady()) return;
            var gc = GC!;

            var riteList = new List<(int id, string name)>();
            try
            {
                foreach (int rid in GameDatabase.RiteIds)
                {
                    if (rid >= rangeMin && rid <= rangeMax)
                    {
                        string rname;
                        try { rname = GameDatabase.GetRiteName(rid) ?? $"仪式 {rid}"; }
                        catch { rname = $"仪式 {rid}"; }
                        riteList.Add((rid, rname));
                    }
                }
            }
            catch { }

            int totalPages = (riteList.Count + RitesPerPage - 1) / RitesPerPage;
            if (totalPages == 0) totalPages = 1;
            if (page >= totalPages) page = totalPages - 1;
            int start = page * RitesPerPage;
            int end = Math.Min(start + RitesPerPage, riteList.Count);

            var items = new List<(string, Action)>();

            if (page > 0)
                items.Add(("<- 上一页", () => ShowRiteListPage(rangeMin, rangeMax, page - 1)));

            for (int i = start; i < end; i++)
            {
                var (rid, rname) = riteList[i];
                int riteId = rid;
                string riteName = rname;
                items.Add(($"添加: {riteName} (ID:{riteId})", () => DoThenReload(() =>
                {
                    gc.AddRitePin(riteId);
                    Main.Log?.Msg($"[NativeMenu] 添加仪式: {riteName} (ID:{riteId})");
                }, () => ShowRiteListPage(rangeMin, rangeMax, page))));
            }

            if (page < totalPages - 1)
                items.Add(("下一页 ->", () => ShowRiteListPage(rangeMin, rangeMax, page + 1)));

            items.Add(("< 返回范围", () => ShowRiteRangeMenu(0)));

            NativeOptionHandler.ShowOption($"仪式 — 第 {page + 1}/{totalPages} 页 (共 {riteList.Count} 个)", items, gc);
        }

        // ============================================================
        // 5. 添加卡牌（按分类浏览，全部 1295 张）
        // ============================================================

        private static void ShowAddCardMenu(int page = 0)
        {
            if (!CheckReady()) return;
            var gc = GC!;

            var allItems = new List<(string, Action)>();
            foreach (var cat in CardCategories)
            {
                string category = cat;
                allItems.Add((category, () => ShowCardRarityFilter(category, 0)));
            }

            var pageItems = BuildPage(allItems, page, p => ShowAddCardMenu(p), ShowMainMenu);
            NativeOptionHandler.ShowOption($"添加卡牌 — 选择分类 ({page + 1}/{(allItems.Count + MenuPageSize - 1) / MenuPageSize})", pageItems, gc);
        }

        private static void ShowCardRarityFilter(string category, int page = 0)
        {
            if (!CheckReady()) return;
            var gc = GC!;

            var allItems = new List<(string, Action)>
            {
                ("全部品质", () => ShowCardListPage(category, 0, 0)),
                ("石 (白)", () => ShowCardListPage(category, 1, 0)),
                ("铜 (绿)", () => ShowCardListPage(category, 2, 0)),
                ("银 (紫)", () => ShowCardListPage(category, 3, 0)),
                ("金 (橙)", () => ShowCardListPage(category, 4, 0)),
            };

            var pageItems = BuildPage(allItems, page, p => ShowCardRarityFilter(category, p),
                () => ShowAddCardMenu(0));
            NativeOptionHandler.ShowOption($"品质筛选: {category}", pageItems, gc);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ShowCardListPage(string category, int rareFilter, int page)
        {
            if (!CheckReady()) return;
            var gc = GC!;

            var cardList = new List<(int id, string name, string cat, int rare)>();
            try
            {
                foreach (var kv in GameDatabase.CardNames)
                {
                    int cid = kv.Key;
                    string cname = kv.Value;
                    string ccat;
                    try { ccat = ConfigManager.GetCardCategory(cid); } catch { ccat = "其他"; }
                    if (category != "全部" && ccat != category) continue;
                    int crare;
                    try { crare = ConfigManager.GetCardBaseRare(cid); } catch { crare = 2; }
                    if (rareFilter > 0 && crare != rareFilter) continue;
                    cardList.Add((cid, cname, ccat, crare));
                }
            }
            catch { }

            int totalPages = (cardList.Count + CardsPerPage - 1) / CardsPerPage;
            if (totalPages == 0) totalPages = 1;
            if (page >= totalPages) page = totalPages - 1;
            int start = page * CardsPerPage;
            int end = Math.Min(start + CardsPerPage, cardList.Count);

            string[] rareTexts = { "", "石", "铜", "银", "金" };

            var items = new List<(string, Action)>();

            // 上一页
            if (page > 0)
                items.Add(("<- 上一页", () => ShowCardListPage(category, rareFilter, page - 1)));

            // 卡牌列表
            for (int i = start; i < end; i++)
            {
                var (cid, cname, ccat, crare) = cardList[i];
                int cardId = cid;
                string cardName = cname;
                string rareLabel = crare >= 1 && crare <= 4 ? rareTexts[crare] : "?";
                string stackLabel = IsStackableCategory(ccat) ? "[堆叠]" : "";
                items.Add(($"[{rareLabel}] {cardName} {stackLabel}[{ccat}]", () =>
                {
                    _lastCardCategory = category;
                    _lastCardRareFilter = rareFilter;
                    _lastCardPage = page;
                    ShowAddQuantity(cardId, cardName, ccat);
                }));
            }

            // 下一页
            if (page < totalPages - 1)
                items.Add(("下一页 ->", () => ShowCardListPage(category, rareFilter, page + 1)));

            items.Add(("< 返回品质筛选", () => ShowCardRarityFilter(category)));

            string title = category == "全部"
                ? $"全部卡牌 — 第 {page + 1}/{totalPages} 页 (共 {cardList.Count} 张)"
                : $"[{category}] — 第 {page + 1}/{totalPages} 页 (共 {cardList.Count} 张)";
            NativeOptionHandler.ShowOption(title, items, gc);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ShowAddQuantity(int cardId, string cardName, string category)
        {
            if (!CheckReady()) return;
            var gc = GC!;

            bool isStackable = IsStackableCategory(category);

            var items = new List<(string, Action)>();

            if (isStackable)
            {
                items.Add(($"{cardName} — 添加 100 数量", () =>
                {
                    AddCardWithCount(cardId, 100);
                    Main.Log?.Msg($"[NativeMenu] {cardName} x100 (堆叠)");
                }));
                items.Add(($"{cardName} — 添加 500 数量", () =>
                {
                    AddCardWithCount(cardId, 500);
                    Main.Log?.Msg($"[NativeMenu] {cardName} x500 (堆叠)");
                }));
                items.Add(($"{cardName} — 添加 999 数量", () =>
                {
                    AddCardWithCount(cardId, 999);
                    Main.Log?.Msg($"[NativeMenu] {cardName} x999 (堆叠)");
                }));
            }
            else
            {
                items.Add(($"{cardName} — 添加 1 张", () =>
                {
                    AddCardWithCount(cardId, 1);
                    Main.Log?.Msg($"[NativeMenu] {cardName} x1");
                }));
                items.Add(($"{cardName} — 添加 2 张", () =>
                {
                    AddCardWithCount(cardId, 2);
                    Main.Log?.Msg($"[NativeMenu] {cardName} x2");
                }));
                items.Add(($"{cardName} — 添加 5 张", () =>
                {
                    AddCardWithCount(cardId, 5);
                    Main.Log?.Msg($"[NativeMenu] {cardName} x5");
                }));
            }

            // 查看立绘（仅角色/苏丹卡）
            try
            {
                string ctype = GameDatabase.GetCardType(cardId) ?? "";
                if (ctype == "char" || ctype == "sudan" || category == "角色")
                {
                    items.Add(($"查看立绘: {cardName}", () =>
                    {
                        RunSafe(() =>
                        {
                            var gc2 = GC;
                            if (gc2 == null) return;
                            var card = gc2.GenCard(cardId);
                            if (card != null) gc2.ShowCardInfo(card, null, false);
                            Main.Log?.Msg($"[NativeMenu] 查看立绘: {cardName} (ID:{cardId})");
                        }, $"查看立绘 {cardName}");
                    }));
                }
            }
            catch { }

            items.Add(("< 返回卡牌列表", () => { }));

            string stackHint = isStackable ? " [可堆叠 — 数量写入单张卡]" : " [添加多张独立卡牌]";
            NativeOptionHandler.ShowOption($"添加卡牌: {cardName}{stackHint}", items, gc);
        }

        // ============================================================
        // 6. 关于
        // ============================================================

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ShowAboutDialog()
        {
            var gc = GC;
            if (gc == null) return;

            string aboutText =
                "    苏丹的游戏 修改器 v1.2.0\n\n" +
                "    作者: Mizuof | QQ群: 624594852\n\n" +
                "    本修改器完全免费\n" +
                "    F1=修改器 F2=OnGUI F3=立绘";

            gc.ShowConfirm(aboutText, null, "关闭", "", "", "");
            Main.Log?.Msg("[NativeMenu] 显示关于对话框");
        }
    }
}
