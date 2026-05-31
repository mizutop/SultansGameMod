using UnityEngine;
using SultansGameMod.Data;
using SultansGameMod.Harmony;

namespace SultansGameMod.UI
{
    internal static class RoundTab
    {
        private static Vector2 _scroll;
        private static string _status = "";
        private static string _roundJump = "";
        private static string _promptText = "";
        private static float _gameSpeed = 1f;
        private static bool _speedChanged;
        private static string _overIdStr = "1";

        // 可选死亡方式（常用结局）
        private static readonly (int id, string label)[] DeathChoices = {
            (1,  "触怒苏丹"),
            (3,  "决斗而死"),
            (4,  "被刺杀"),
            (12, "处刑到期"),
            (7,  "啊！死了"),
            (2,  "永恒之梦"),
            (5,  "欢愉极致"),
        };

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        internal static void Draw()
        {
            bool ready = false;
            try { ready = RuntimeEngine.IsReady; }
            catch (System.Exception ex) { Main.Log?.Warning($"[RoundTab.Draw][IsReady] 异常: {ex}"); }

            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(Styles.ScrollHeight));

            // 状态栏
            GUI.color = ready ? Styles.StatusOk : Styles.StatusErr;
            GUILayout.Label(ready ? "游戏运行中 - API 就绪" : "游戏未加载 - 请先进入游戏");
            GUI.color = Color.white;
            GUILayout.Space(4);

            // ===== 当前游戏状态概览 =====
            Styles.DrawSection("当前游戏状态", Styles.RuntimeHeader);
            Styles.BeginGroup(Styles.RuntimeBg);

            // 回合信息 — 突出显示
            try
            {
                var s = RuntimeEngine.Save;
                if (s != null)
                {
                    GUI.color = Color.yellow;
                    GUILayout.Label("=== 第 " + s.Round + " 回合 ===", GUILayout.Height(24));
                    GUI.color = Color.white;
                    GUILayout.Label("角色: " + (s.Name ?? "未知") + "  |  处刑日基数: " + s.SudanCardInitLife + "  |  局内卡牌: " + (s.Cards?.Count ?? 0));
                }
            }
            catch (System.Exception ex) { Main.Log?.Warning($"[RoundTab.Draw][存档信息] 异常: {ex}"); GUILayout.Label("(存档读取异常)"); }

            // 苏丹牌信息 — 通过运行时 API 获取
            if (ready)
            {
                try
                {
                    var gc = Il2Cpp.GameController.Inst;
                    if (gc == null) { GUILayout.Label("(GameController 不可用)"); }
                    else
                    {
                        var sudanCards = gc.GetCurrentSudanCard();
                        if (sudanCards != null)
                        {
                            int sudanCount = sudanCards.Count;
                            int baseLife = 7;
                            try { baseLife = RuntimeEngine.Save?.SudanCardInitLife ?? 7; } catch { }
                            GUILayout.Label("活跃苏丹牌: " + sudanCount + " 张");

                            int cardIdx = 0;
                            if (sudanCount > 0 && sudanCount <= 10)
                            {
                                foreach (var card in sudanCards)
                                {
                                    cardIdx++;
                                    int cardId = 0, cardLife = 0, cardRare = 1;
                                    try
                                    {
                                        cardId = (int)card.id;
                                        cardLife = (int)card.life;
                                        if (card.data != null) cardRare = (int)card.data.rare;
                                    }
                                    catch { }
                                    string cardName = GameDatabase.GetCardName(cardId) ?? "苏丹牌";
                                    int remainDays = baseLife - cardLife;
                                    if (remainDays < 0) remainDays = 0;
                                    Color rc = Styles.GetRareColor(cardRare);
                                    GUI.color = rc;
                                    GUILayout.Label($"  {Styles.GetRareIcon(cardRare)} {cardName} (剩余{remainDays}天)");
                                    GUI.color = Color.white;
                                }
                            }
                            else if (sudanCount > 10)
                            {
                                GUILayout.Label("  (苏丹牌过多，请使用卡牌操作查看详情)");
                            }
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Main.Log?.Warning($"[RoundTab.Draw][GetCurrentSudanCard] 异常: {ex}");
                    GUILayout.Label("(无法获取苏丹牌信息)");
                }
            }
            else
            {
                GUILayout.Label("(未加载存档)");
            }

            Styles.EndGroup();
            GUILayout.Space(4);

            // ===== 核心控制 =====
            Styles.DrawSection("核心控制", Styles.RuntimeHeader);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("下一回合", GUILayout.Height(Styles.ButtonMinHeight)) && ready)
            {
                if (TimePatches.FreezeTime) { _status = "回合已冻结，请先解冻后再操作下一回合"; }
                else { RuntimeEngine.NextRound(); _status = "已进入下一回合"; }
            }
            if (GUILayout.Button("上一回合", GUILayout.Height(Styles.ButtonMinHeight)) && ready)
            { RuntimeEngine.PrevRound(); _status = "已返回上一回合"; }
            GUILayout.Label("回合数:", GUILayout.Width(50));
            _roundJump = GUILayout.TextField(_roundJump, GUILayout.Width(50));
            if (GUILayout.Button("跳转", GUILayout.Width(50)) && ready && int.TryParse(_roundJump, out int r) && r >= 0 && r <= 999)
            { RuntimeEngine.LoadRound(r); _status = "已跳转到回合 " + r; }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUI.color = TimePatches.FreezeTime ? Styles.StatusOk : Styles.StatusWarn;
            if (GUILayout.Button(TimePatches.FreezeTime ? "回合已冻结" : "回合未冻结", GUILayout.Width(100), GUILayout.Height(Styles.ButtonMinHeight)))
            { TimePatches.FreezeTime = !TimePatches.FreezeTime; _status = TimePatches.FreezeTime ? "回合已冻结" : "回合已解冻"; }
            GUI.color = Color.white;
            if (GUILayout.Button("强制保存", GUILayout.Height(Styles.ButtonMinHeight)) && ready)
            { RuntimeEngine.SaveGame(); _status = "保存请求已发出"; }
            if (GUILayout.Button("重载当前回合", GUILayout.Height(Styles.ButtonMinHeight)) && ready)
            { RuntimeEngine.ApplySaveChanges(); _status = "已保存并重载回合"; }
            GUILayout.EndHorizontal();
            GUILayout.Space(4);

            // ===== 游戏变速 =====
            Styles.DrawSection("游戏变速", Styles.RuntimeHeader);
            GUILayout.BeginHorizontal();
            GUILayout.Label("速度: " + _gameSpeed.ToString("F1") + "x", GUILayout.Width(80));
            float newSpeed = GUILayout.HorizontalSlider(_gameSpeed, 0.1f, 10f, GUILayout.Width(160));
            if (newSpeed != _gameSpeed)
            {
                _gameSpeed = newSpeed;
                _speedChanged = true;
            }
            if (_speedChanged || GUILayout.Button("应用", GUILayout.Width(50)))
            {
                Time.timeScale = _gameSpeed;
                _speedChanged = false;
                _status = "速度设为 " + _gameSpeed.ToString("F1") + "x";
            }
            if (GUILayout.Button("重置 1x", GUILayout.Width(65)))
            {
                _gameSpeed = 1f;
                Time.timeScale = 1f;
                _status = "速度已重置为 1x";
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(4);

            // ===== 游戏内提示 =====
            Styles.DrawSection("游戏内提示框", Styles.RuntimeHeader);
            GUILayout.BeginHorizontal();
            _promptText = GUILayout.TextField(_promptText, GUILayout.Width(250));
            if (GUILayout.Button("显示提示", GUILayout.Height(22)) && ready && !string.IsNullOrEmpty(_promptText))
            { RuntimeEngine.ShowGamePrompt(_promptText); _status = "已显示提示"; }
            GUILayout.EndHorizontal();
            GUILayout.Space(4);

            // ===== 游戏结束 =====
            Styles.DrawSection("游戏结束", Styles.RuntimeHeader);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("默认游戏结束", GUILayout.Height(Styles.ButtonMinHeight)) && ready)
            { RuntimeEngine.DoGameOver(); _status = "游戏结束触发"; }
            GUILayout.EndHorizontal();
            GUILayout.Space(2);
            Styles.DrawSection("自选死亡方式 (SetGameOver)", Styles.RuntimeHeader);
            GUILayout.BeginHorizontal();
            foreach (var choice in DeathChoices)
            {
                if (GUILayout.Button(choice.label, GUILayout.Width(80), GUILayout.Height(Styles.ButtonMinHeight)) && ready)
                { RuntimeEngine.SetGameOverById(choice.id); _status = "触发结局: " + choice.label + " (ID:" + choice.id + ")"; }
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("自定义结局 ID:", GUILayout.Width(90));
            _overIdStr = GUILayout.TextField(_overIdStr, GUILayout.Width(60));
            if (GUILayout.Button("触发", GUILayout.Width(50)) && ready && int.TryParse(_overIdStr, out int oid))
            { RuntimeEngine.SetGameOverById(oid); _status = "触发结局 ID:" + oid; }
            GUILayout.EndHorizontal();
            GUILayout.Space(4);



            Styles.DrawStatus(_status, Styles.StatusInfo);
            GUILayout.EndScrollView();

        }
    }
}
