using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Il2CppInterop.Runtime;

namespace SultansGameMod.Data
{
    internal static class RuntimeEngine
    {
        private static Il2Cpp.GameController? _gc;
        private static int _gcCheckFrame = -1;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Il2Cpp.GameController? ResolveGameController()
        {
            try { return Il2Cpp.GameController.Inst; }
            catch (System.Exception ex)
            {
                Main.Log?.Warning($"[ResolveGC] 获取失败: {ex}");
                return null;
            }
        }

        private static Il2Cpp.GameController? GC
        {
            get
            {
                int currentFrame = UnityEngine.Time.frameCount;
                if (_gcCheckFrame == currentFrame) return _gc;
                _gcCheckFrame = currentFrame;
                _gc = ResolveGameController();
                return _gc;
            }
        }

        public static bool IsReady
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get { var g = GC; return g != null; }
        }

        public static void AddCard(int cardId)
        {
            try
            {
                var gc = GC; if (gc == null) return;
                var card = gc.GenCard(cardId);
                if (card == null) return;

                // 自动标签：基于卡牌实际已有的 tag 来判断分类
                try
                {
                    var tag = card.data?.tag;
                    if (tag == null) { gc.AddCard(card, true); return; }

                    // 收集卡牌已有 tag 的 HashSet
                    var tagCodeSet = new System.Collections.Generic.HashSet<string>();
                    foreach (var kv in tag) tagCodeSet.Add(kv.Key);

                    // 判断卡牌是否已有追随者/角色类特征
                    bool isCharacter = tagCodeSet.Contains("man") || tagCodeSet.Contains("female")
                        || tagCodeSet.Contains("noble") || tagCodeSet.Contains("player")
                        || tagCodeSet.Contains("ally") || tagCodeSet.Contains("slave");

                    // === 分类补充标签 ===
                    // 角色卡 → 追随者
                    if (isCharacter && !tagCodeSet.Contains("adherent"))
                        tag.Add("adherent", 1);

                    // 部队 → 部队标签
                    if (tagCodeSet.Contains("army") && !tagCodeSet.Contains("adherent"))
                        tag.Add("adherent", 1);

                    // 装备/武器/服装/饰品/坐骑 → 已拥有
                    bool hasEquipmentTag = tagCodeSet.Contains("equipment") || tagCodeSet.Contains("weapon")
                        || tagCodeSet.Contains("cloth") || tagCodeSet.Contains("accessory")
                        || tagCodeSet.Contains("mount");
                    if (hasEquipmentTag && !tagCodeSet.Contains("own"))
                        tag.Add("own", 1);

                    // 大餐 → 已拥有
                    if (tagCodeSet.Contains("feast") && !tagCodeSet.Contains("own"))
                        tag.Add("own", 1);

                    // 读物/情报/天象/思潮 → 已拥有
                    if (tagCodeSet.Contains("read") || tagCodeSet.Contains("intelligent")
                        || tagCodeSet.Contains("sky") || tagCodeSet.Contains("thought"))
                    {
                        if (!tagCodeSet.Contains("own")) tag.Add("own", 1);
                    }

                    // 消耗品 → 已拥有
                    if (tagCodeSet.Contains("consumable") && !tagCodeSet.Contains("own"))
                        tag.Add("own", 1);

                    // 没有以上任何分类标签 → 兜底加已拥有（让卡牌能显示在手牌）
                    if (!isCharacter && !hasEquipmentTag && !tagCodeSet.Contains("feast")
                        && !tagCodeSet.Contains("read") && !tagCodeSet.Contains("intelligent")
                        && !tagCodeSet.Contains("sky") && !tagCodeSet.Contains("thought")
                        && !tagCodeSet.Contains("consumable") && !tagCodeSet.Contains("army")
                        && !tagCodeSet.Contains("own"))
                    {
                        tag.Add("own", 1);
                    }
                }
                catch { }

                gc.AddCard(card, true);
            }
            catch (System.Exception ex) { Main.Log?.Warning($"[AddCard] 异常: {ex}"); }
        }

        public static void RemoveCard(int uid)
        {
            try { var gc = GC; if (gc != null) gc.RemoveCard(uid); }
            catch (System.Exception ex) { Main.Log?.Warning($"[RemoveCard] 异常: {ex}"); }
        }

        public static void AddSudanCard()
        {
            try { var gc = GC; if (gc != null) gc.GenSudanCard(); }
            catch (System.Exception ex) { Main.Log?.Warning($"[AddSudanCard] 异常: {ex}"); }
        }

        public static void RedrawSudan()
        {
            try
            {
                var gc = GC; if (gc == null) return;
                gc.GenSudanCard();
            }
            catch (System.Exception ex) { Main.Log?.Warning($"[RedrawSudan] 异常: {ex}"); }
        }

        public static void NextRound()
        {
            try
            {
                var gc = GC; if (gc == null) return;
                gc.OnNextRound();
                Load();
            }
            catch (System.Exception ex) { Main.Log?.Warning($"[NextRound] 异常: {ex}"); }
        }

        public static void PrevRound()
        {
            try
            {
                var gc = GC; if (gc == null) return;
                gc.OnPrevRound();
                Load();
            }
            catch (System.Exception ex) { Main.Log?.Warning($"[PrevRound] 异常: {ex}"); }
        }

        public static void LoadRound(int round)
        {
            try { var gc = GC; if (gc != null) gc.LoadRound(round); }
            catch (System.Exception ex) { Main.Log?.Warning($"[LoadRound] 异常: {ex}"); }
        }

        public static void TriggerEvent(int id)
        {
            try { var gc = GC; if (gc != null) gc.EventTrigger.Add(id, true); }
            catch (System.Exception ex) { Main.Log?.Warning($"[TriggerEvent] 异常: {ex}"); }
        }

        public static void RemoveEvent(int id)
        {
            try { var gc = GC; if (gc != null) gc.EventTrigger.Remove(id); }
            catch (System.Exception ex) { Main.Log?.Warning($"[RemoveEvent] 异常: {ex}"); }
        }

        public static void DoGameOver()
        {
            try { var gc = GC; if (gc != null) gc.DoGameOver(); }
            catch (System.Exception ex) { Main.Log?.Warning($"[DoGameOver] 异常: {ex}"); }
        }

        /// <summary>
        /// 以指定结局 ID 触发游戏结束
        /// 标准流程：SetGameOver 记录 reason → DoGameOver 触发完整流程
        /// OverNewController 在游戏结束 UI 中读取 reason 显示对应结局
        /// </summary>
        public static void SetGameOverById(int overId)
        {
            try
            {
                var gc = GC; if (gc == null) return;
                gc.SetGameOver(false, overId);
                gc.DoGameOver();
                Main.Log?.Msg($"[SetGameOverById] 触发结局 ID={overId}（SetGameOver+DoGameOver）");
            }
            catch (System.Exception ex) { Main.Log?.Warning($"[SetGameOverById] 异常: {ex}"); }
        }

        public static void SaveGame()
        {
            try
            {
                var gc = GC; if (gc != null) gc.RequestSavePlayer();
                Load();
            }
            catch (System.Exception ex) { Main.Log?.Warning($"[SaveGame] 异常: {ex}"); }
        }

        // ========== Save ==========
        private static string? _saveDir;
        public static string? SaveDir
        {
            get
            {
                if (_saveDir != null) return _saveDir;
                try
                {
                    string bp = UnityEngine.Application.persistentDataPath;
                    if (Directory.Exists(bp))
                    { var dirs = Directory.GetDirectories(bp); if (dirs.Length > 0) { _saveDir = dirs[0]; return _saveDir; } }
                }
                catch { }
                string fb = Path.Combine(
                    System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
                    "LocalLow", "DoubleCross", "Sultan's Game", "SAVE");
                if (Directory.Exists(fb))
                { var dirs = Directory.GetDirectories(fb); if (dirs.Length > 0) _saveDir = dirs[0]; }
                return _saveDir;
            }
        }

        public class RootSave
        {
            public string? Name { get; set; }
            public int Round { get; set; }
            public int Difficulty { get; set; }
            public int SudanCardInitLife { get; set; }
            public int SudanRedrawTimes { get; set; }
            public int SudanRedrawTimesPerRound { get; set; }
            public int BackToPrevRound { get; set; }
            public Dictionary<string, long>? Counter { get; set; }
            public List<CardSave>? Cards { get; set; }
        }

        public class CardSave
        {
            public int Uid { get; set; }
            public int Id { get; set; }
            public int Count { get; set; }
            public int Life { get; set; }
            public int Rareup { get; set; }
            public string? CustomName { get; set; }
            public Dictionary<string, int>? Tag { get; set; }
            public int Bag { get; set; }
            public int Bagpos { get; set; }
        }

        public class GlobalSave
        {
            public long TotalPoint { get; set; }
            public long UsedPoint { get; set; }
            public int TotalRound { get; set; }
            public Dictionary<string, int>? Upgrade { get; set; }
            public List<int>? OverID { get; set; }
            public List<int>? DoneRite { get; set; }
            public List<int>? DoneEvent { get; set; }
            public Dictionary<string, long>? Counter { get; set; }
            public Dictionary<string, int>? Achievements { get; set; }
            public List<int>? ShowedGalleryCards { get; set; }
        }

        private static RootSave? _save;
        private static GlobalSave? _global;
        public static RootSave? Save => _save;
        public static GlobalSave? Global => _global;

        private static string? AutoSavePath => SaveDir != null ? Path.Combine(SaveDir, "auto_save.json") : null;
        private static string? GlobalPath => SaveDir != null ? Path.Combine(SaveDir, "global.json") : null;

        private static readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Formatting = Formatting.None
        };

        public static void Load()
        {
            var ap = AutoSavePath;
            if (ap != null && File.Exists(ap))
            {
                try { _save = JsonConvert.DeserializeObject<RootSave>(File.ReadAllText(ap), _jsonSettings); }
                catch { _save = null; }
            }
            var gp = GlobalPath;
            if (gp != null && File.Exists(gp))
            {
                try { _global = JsonConvert.DeserializeObject<GlobalSave>(File.ReadAllText(gp), _jsonSettings); }
                catch { _global = null; }
            }
        }

        public static void SaveRound()
        {
            if (_save == null || AutoSavePath == null) return;
            try
            {
                string bak2 = AutoSavePath + ".bak2";
                string bak1 = AutoSavePath + ".bak";
                if (File.Exists(bak1)) { if (File.Exists(bak2)) File.Delete(bak2); File.Move(bak1, bak2); }
                if (File.Exists(AutoSavePath)) File.Copy(AutoSavePath, bak1, true);
                string tmp = AutoSavePath + ".tmp";
                File.WriteAllText(tmp, JsonConvert.SerializeObject(_save, _jsonSettings));
                if (File.Exists(AutoSavePath)) File.Delete(AutoSavePath);
                File.Move(tmp, AutoSavePath);
            }
            catch (System.Exception ex) { Main.Log?.Warning($"[SaveRound] 失败: {ex}"); }
        }

        public static void SaveGlobal()
        {
            if (_global == null || GlobalPath == null) return;
            try
            {
                if (File.Exists(GlobalPath)) File.Copy(GlobalPath, GlobalPath + ".bak", true);
                File.WriteAllText(GlobalPath, JsonConvert.SerializeObject(_global, _jsonSettings));
            }
            catch (System.Exception ex) { Main.Log?.Warning($"[SaveGlobal] 失败: {ex}"); }
        }

        public static long GetCounter(string key)
        {
            if (_save?.Counter != null && _save.Counter.TryGetValue(key, out long v)) return v;
            return 0;
        }

        public static void SetCounter(string key, long val)
        {
            if (_save == null) return;
            _save.Counter ??= new Dictionary<string, long>();
            _save.Counter[key] = val;
        }

        public static void InitDefaults()
        {
            if (_save == null) return;
            _save.SudanCardInitLife = 7;
            _save.SudanRedrawTimes = 3;
            _save.SudanRedrawTimesPerRound = 3;
            _save.BackToPrevRound = 9999;
        }

        public static void UnlockAllEndings()
        {
            if (_global == null) return;
            _global.OverID ??= new List<int>();
            foreach (var id in GameDatabase.EndingNames.Keys)
                if (!_global.OverID.Contains(id)) _global.OverID.Add(id);
        }

        public static void UnlockAllUpgrades()
        {
            if (_global?.Upgrade == null) return;
            for (int id = 3300002; id <= 3300047; id++)
                _global.Upgrade[id.ToString()] = 1;
        }

        public static void ShowGamePrompt(string text)
        {
            try { var gc = GC; if (gc != null) gc.ShowPrompt(text); }
            catch { }
        }

        public static void GenCardRuntime(int cardId)
        {
            try
            {
                var gc = GC; if (gc == null) return;
                var card = gc.GenCard(cardId);
                if (card != null) gc.AddCard(card, true);
            }
            catch (System.Exception ex) { Main.Log.Warning($"[GenCardRuntime] 异常: {ex}"); }
        }

        public static void SetRuntimeCounter(string key, long val)
        {
            SetCounter(key, val);
            Harmony.CounterPatches.SetOverride(key, val);
        }

        public static void ClearRuntimeCounters()
        {
            Harmony.CounterPatches.ClearOverrides();
        }

        public static void ApplySaveChanges()
        {
            try
            {
                int currentRound = _save?.Round ?? 1;
                SaveRound();
                var gc = GC; if (gc == null) return;
                gc.LoadRound(currentRound, false);
                Load();
            }
            catch (System.Exception ex) { Main.Log.Warning($"[ApplySaveChanges] 异常: {ex}"); }
        }

        public static void DestroySelectedCard()
        {
            try
            {
                var gc = GC; if (gc == null) return;
                var selected = gc.CurrentSelect;
                if (selected == null) return;
                gc.RemoveCard(selected);
                Il2Cpp.CardHandler.OnHandBagChanged();
            }
            catch (System.Exception ex) { Main.Log.Warning($"[DestroySelectedCard] 异常: {ex}"); }
        }

        /// <summary>
        /// 销毁苏丹牌并触发撕毁动画
        /// 通过 Il2CppInterop 反射调用 CardExtensions.DoVanish（绕过编译期程序集冲突）
        /// </summary>
        public static void VanishSelectedCard()
        {
            try
            {
                var gc = GC; if (gc == null) return;
                var selected = gc.CurrentSelect;
                if (selected == null) return;
                int uid = (int)selected.uid;

                // 尝试通过 Unity 资源系统找到对应的 CardRender
                try
                {
                    var renders = UnityEngine.Resources.FindObjectsOfTypeAll<Il2Cpp.CardRender>();
                    if (renders != null)
                    {
                        foreach (var r in renders)
                        {
                            if (r != null && r.card != null && r.card.uid == selected.uid)
                            {
                                // 触发渲染器的销毁动画
                                var go = r.gameObject;
                                if (go != null)
                                {
                                    UnityEngine.Object.Destroy(go);
                                }
                            }
                        }
                    }
                }
                catch { }

                gc.RemoveCard(selected);
                Il2Cpp.CardHandler.OnHandBagChanged();
                Main.Log?.Msg($"[VanishCard] 苏丹牌 UID:{selected.uid} 已销毁并触发动画");
            }
            catch (System.Exception ex) { Main.Log.Warning($"[VanishSelectedCard] 异常: {ex}"); }
        }

        public static void RemoveCardDual(int uid)
        {
            try { var gc = GC; if (gc != null) gc.RemoveCard(uid); } catch { }
            RemoveCardByUid(uid);
            try { Il2Cpp.CardHandler.OnHandBagChanged(); } catch { }
        }

        public static void SaveRoundDual()
        {
            SaveRound();
            try { var gc = GC; if (gc != null) gc.RequestSavePlayer(); } catch { }
        }

        public static void SetPrestigeCounter(string name, string counterKey, long val)
        {
            SetRuntimeCounter(counterKey, val);
        }

        private static string PresetPath => SaveDir != null ? Path.Combine(SaveDir, "mod_preset.json") : null;

        public static void ExportPreset()
        {
            if (_save == null || PresetPath == null) return;
            try
            {
                var preset = new
                {
                    Version = "1.0",
                    ExportedAt = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    SudanCardInitLife = _save.SudanCardInitLife,
                    SudanRedrawTimes = _save.SudanRedrawTimes,
                    SudanRedrawTimesPerRound = _save.SudanRedrawTimesPerRound,
                    BackToPrevRound = _save.BackToPrevRound,
                    Difficulty = _save.Difficulty,
                    Counters = _save.Counter,
                    Cards = _save.Cards?.Select(c => new { c.Id, c.Count, c.Life, c.Rareup, c.Tag, c.Bag }).ToList()
                };
                File.WriteAllText(PresetPath, JsonConvert.SerializeObject(preset, Formatting.Indented));
            }
            catch (System.Exception ex) { Main.Log?.Warning($"[ExportPreset] 异常: {ex}"); }
        }

        public static void ImportPreset()
        {
            if (_save == null || PresetPath == null || !File.Exists(PresetPath)) return;
            try
            {
                var json = File.ReadAllText(PresetPath);
                var preset = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(json);
                if (preset == null) return;
                string? version = preset.Value<string>("Version");
                if (version != "1.0") return;
                if (preset["SudanCardInitLife"] != null) _save.SudanCardInitLife = preset.Value<int>("SudanCardInitLife");
                if (preset["SudanRedrawTimes"] != null) _save.SudanRedrawTimes = preset.Value<int>("SudanRedrawTimes");
                if (preset["SudanRedrawTimesPerRound"] != null) _save.SudanRedrawTimesPerRound = preset.Value<int>("SudanRedrawTimesPerRound");
                if (preset["BackToPrevRound"] != null) _save.BackToPrevRound = preset.Value<int>("BackToPrevRound");
                if (preset["Difficulty"] != null) _save.Difficulty = preset.Value<int>("Difficulty");
            }
            catch (System.Exception ex) { Main.Log?.Warning($"[ImportPreset] 异常: {ex}"); }
        }

        public static CardSave? FindCard(int uid) => _save?.Cards?.FirstOrDefault(c => c.Uid == uid);
        public static void RemoveCardByUid(int uid) => _save?.Cards?.RemoveAll(c => c.Uid == uid);
        public static void RemoveCardsById(int id) => _save?.Cards?.RemoveAll(c => c.Id == id);

        public static int RemoveAllNonPlayerCards()
        {
            if (_save?.Cards == null) return 0;
            int before = _save.Cards.Count;
            _save.Cards.RemoveAll(c => { string? type = GameDatabase.GetCardType(c.Id); return type != "char" && type != "sudan"; });
            return before - _save.Cards.Count;
        }

        public static int RemoveCardsByType(string typeKey)
        {
            if (_save?.Cards == null) return 0;
            int before = _save.Cards.Count;
            _save.Cards.RemoveAll(c => GameDatabase.GetCardType(c.Id) == typeKey);
            return before - _save.Cards.Count;
        }

        public static long GetGlobalCounter(string key)
        {
            if (_global?.Counter != null && _global.Counter.TryGetValue(key, out long v)) return v;
            return 0;
        }

        public static void SetGlobalCounter(string key, long val)
        {
            if (_global?.Counter != null) _global.Counter[key] = val;
        }

        public static void UnlockGalleryCards()
        {
            if (_global == null) return;
            _global.ShowedGalleryCards ??= new List<int>();
            foreach (var id in GameDatabase.CardNames.Keys)
                if (!_global.ShowedGalleryCards.Contains(id)) _global.ShowedGalleryCards.Add(id);
        }

        // ====== 运行时卡牌字段直接修改 ======
        public static void EditSelectedCardCount(int newCount)
        {
            try
            {
                var gc = GC; if (gc == null) return;
                var selected = gc.CurrentSelect; if (selected == null) return;
                RuntimeFieldAccess.SetCount(selected, newCount);
            }
            catch (System.Exception ex) { Main.Log?.Warning($"[EditCard] 数量修改失败: {ex}"); }
        }

        public static void EditSelectedCardLife(int newLife)
        {
            try
            {
                var gc = GC; if (gc == null) return;
                var selected = gc.CurrentSelect; if (selected == null) return;
                RuntimeFieldAccess.SetLife(selected, newLife);
            }
            catch (System.Exception ex) { Main.Log?.Warning($"[EditCard] 生命修改失败: {ex}"); }
        }

        public static void EditSelectedCardRareup(int newRareup)
        {
            try
            {
                var gc = GC; if (gc == null) return;
                var selected = gc.CurrentSelect; if (selected == null) return;
                RuntimeFieldAccess.SetRareup(selected, newRareup);
            }
            catch (System.Exception ex) { Main.Log?.Warning($"[EditCard] 品级修改失败: {ex}"); }
        }

        public static void EditSelectedCardBaseRare(int newRare)
        {
            try
            {
                var gc = GC; if (gc == null) return;
                var selected = gc.CurrentSelect; if (selected == null) return;
                RuntimeFieldAccess.SetBaseRare(selected, newRare);
                try
                {
                    var renders = UnityEngine.Resources.FindObjectsOfTypeAll<Il2Cpp.CardRender>();
                    if (renders != null)
                    {
                        foreach (var r in renders)
                        {
                            try
                            {
                                if (r != null && r.card != null && r.card.uid == selected.uid)
                                    r.Init(selected.data, false, (int)selected.count, newRare, true);
                            }
                            catch { }
                        }
                    }
                }
                catch { }
            }
            catch (System.Exception ex) { Main.Log?.Warning($"[EditCard] 基础品级修改失败: {ex}"); }
        }

        public static void RemoveSelectedCardTag(string key)
        {
            try
            {
                var gc = GC; if (gc == null) return;
                var selected = gc.CurrentSelect; if (selected == null) return;
                RuntimeFieldAccess.RemoveTag(selected, key);
            }
            catch (System.Exception ex) { Main.Log?.Warning($"[EditCard] 标签删除失败: {ex}"); }
        }

        public static void EditSelectedCardTag(string key, int value)
        {
            try
            {
                var gc = GC; if (gc == null) return;
                var selected = gc.CurrentSelect; if (selected == null) return;
                RuntimeFieldAccess.SetTag(selected, key, value);
            }
            catch (System.Exception ex) { Main.Log?.Warning($"[EditCard] 标签修改失败: {ex}"); }
        }

        public static readonly Dictionary<string, string> KnownCounters = new()
        {
            { "7200138", "ShiLiDu" },
            { "7200133", "LingShi" },
            { "7200002", "JinTouZi" },
            { "7220001", "TotalGames" },
            { "7220002", "Wins" },
            { "7230001", "Tutorial" },
            { "7230003", "Counter" },
            { "7210001", "DaysAlive" },
        };
    }
}
