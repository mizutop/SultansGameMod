using UnityEngine;
using SultansGameMod.Data;
using SultansGameMod.Harmony;
using System.Collections.Generic;

namespace SultansGameMod.UI
{
    internal static class CustomEventTab
    {
        private static string _status = "";
        private static Vector2 _scroll;

        private static readonly int[] AllSudanIds = {
            2010001, 2010002, 2010003, 2010004,
            2010005, 2010006, 2010007, 2010008,
            2010009, 2010010, 2010011, 2010012,
            2010013, 2010014, 2010015, 2010016,
            2000512, 2000513, 2000514, 2000515
        };

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        internal static void Draw()
        {
            bool ready = false;
            try { ready = RuntimeEngine.IsReady; }
            catch { }

            _status = "";
            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(Styles.ScrollHeight));

            // ===== 数值提升（主要功能）=====
            Styles.DrawSection("数值提升", Styles.RareGold);
            GUI.color = Styles.RareGold;
            GUILayout.Label("点击下方按钮，通过游戏原生选项面板选择要提升的数值 (+999)");
            GUI.color = Color.white;
            if (GUILayout.Button("✦ 声望选择 ✦", GUILayout.Height(40)) && ready)
            {
                try
                {
                    var gc = Il2Cpp.GameController.Inst;
                    if (gc != null)
                    {
                        NativeOptionHandler.ShowOption("选择你要提升的数值",
                            new List<(string, System.Action)>
                            {
                                ("善名+999", () => { NativeOptionHandler.AddCounterDirect(7100001, 999); _status = "善名+999"; }),
                                ("恶名+999", () => { NativeOptionHandler.AddCounterDirect(7100002, 999); _status = "恶名+999"; }),
                                ("权势+999", () => { NativeOptionHandler.AddCounterDirect(7100003, 999); _status = "权势+999"; }),
                                ("侠名+999", () => { NativeOptionHandler.AddCounterDirect(7100004, 999); _status = "侠名+999"; }),
                                ("灵视+999", () => { NativeOptionHandler.AddCounterDirect(7100005, 999); _status = "灵视+999"; }),
                                ("金骰子+999", () => { NativeOptionHandler.AddCounterDirect(7100006, 999); _status = "金骰子+999"; }),
                                ("上回合次数+999", () => { NativeOptionHandler.AddCounterDirect(7100007, 999); _status = "上回合次数+999"; }),
                                ("重抽次数+999", () => { NativeOptionHandler.AddCounterDirect(7100008, 999); _status = "重抽次数+999"; }),
                            }, gc);
                        _status = "请在游戏原生面板中选择";
                    }
                }
                catch (System.Exception ex) { _status = "打开失败: " + ex.Message; Main.Log?.Warning("[原生选项] " + ex); }
            }
            GUILayout.Space(4);
            GUI.color = Color.grey;
            GUILayout.Label("善名/恶名/权势/侠名/灵视  金骰子/上回合次数/重抽次数");
            GUI.color = Color.white;
            GUILayout.Space(8);

            // ---- 卡牌操作 ----
            Styles.DrawSection("卡牌操作", Styles.ToggleHeader);
            Styles.BeginGroup(Styles.ToggleBg);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("全部苏丹牌(20张)", GUILayout.Height(22)) && ready)
            {
                foreach (int sid in AllSudanIds) RuntimeEngine.AddCard(sid);
                _status = "获得全部 " + AllSudanIds.Length + " 张苏丹牌";
            }
            if (GUILayout.Button("获得100金币", GUILayout.Height(22)) && ready)
            {
                try
                {
                    var gc = Il2Cpp.GameController.Inst;
                    if (gc != null)
                    {
                        var coin = gc.GenCard(2000029);
                        if (coin != null)
                        {
                            coin.count = 100;
                            gc.AddCard(coin, true);
                        }
                    }
                }
                catch { }
                _status = "获得 1 张金币(数量100)";
            }
            if (GUILayout.Button("销毁选中卡牌", GUILayout.Height(22)) && ready)
            {
                RuntimeEngine.DestroySelectedCard();
                _status = "已销毁选中卡牌";
            }
            GUILayout.EndHorizontal();
            Styles.EndGroup();
            GUILayout.Space(4);

            // ---- 特殊机会 ----
            Styles.DrawSection("特殊机会", Styles.ToggleHeader);
            Styles.BeginGroup(Styles.ToggleBg);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("交换苏丹牌的机会", GUILayout.Height(22)) && ready)
            {
                try
                {
                    var gc = Il2Cpp.GameController.Inst;
                    if (gc == null) { _status = "GameController 不可用"; }
                    else
                    {
                        var save = RuntimeEngine.Save;
                        if (save != null)
                        {
                            save.SudanRedrawTimes = 99;
                            save.SudanRedrawTimesPerRound = 99;
                        }
                        bool prevFreeRedraw = AdvancedPatches.FreeRedrawSudan;
                        AdvancedPatches.FreeRedrawSudan = false;
                        gc.OnRedrawSudan();
                        AdvancedPatches.FreeRedrawSudan = prevFreeRedraw;
                        _status = "苏丹牌重抽机会已给予";
                    }
                }
                catch (System.Exception ex) { _status = "交换失败: " + ex.Message; Main.Log?.Warning("[交换苏丹牌] " + ex); }
            }
            if (GUILayout.Button("回到上一回合的机会", GUILayout.Height(22)) && ready)
            {
                try
                {
                    var save = RuntimeEngine.Save;
                    if (save != null)
                        save.BackToPrevRound = 9999;
                    RuntimeEngine.PrevRound();
                    _status = "已给予返回上回合机会";
                }
                catch (System.Exception ex) { _status = "返回失败: " + ex.Message; }
            }
            if (GUILayout.Button("打开原生确认弹窗", GUILayout.Height(22)) && ready)
            {
                try
                {
                    var gc = Il2Cpp.GameController.Inst;
                    if (gc != null)
                        gc.ShowConfirm("这是一条来自修改器的测试消息", null, "确认", "取消");
                    _status = "原生确认弹窗已打开";
                }
                catch (System.Exception ex) { _status = "打开失败: " + ex.Message; }
            }
            GUILayout.EndHorizontal();
            Styles.EndGroup();

            Styles.DrawStatus(_status, Styles.StatusInfo);
            GUILayout.EndScrollView();
        }
    }
}
