using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Il2Cpp;
using SultansGameMod.Data;

namespace SultansGameMod.UI
{
    /// <summary>
    /// 角色立绘浏览器
    /// 使用游戏的 CardRender / ShowCardInfo 系统显示角色卡牌立绘
    ///
    /// 两种查看模式:
    ///   1. ShowCardInfo — 游戏原生卡牌详情面板（含完整立绘、属性、标签）
    ///   2. Instantiate cardPrefab → CardRender.Init() — 在自定义面板中渲染卡牌
    /// </summary>
    internal static class PortraitViewer
    {
        private static GameObject? _portraitPanel;
        private static GameObject? _cardContainer;
        private static bool _isVisible;
        private static readonly List<GameObject> _renderedCards = new();

        /// <summary>
        /// 使用游戏原生卡牌信息面板展示角色立绘
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        internal static void ShowCharacterPortrait(int cardId, string? characterName = null)
        {
            try
            {
                var gc = GameController.Inst;
                if (gc == null) { Main.Log?.Msg("[PortraitViewer] 游戏未运行"); return; }

                var card = gc.GenCard(cardId);
                if (card == null) { Main.Log?.Msg($"[PortraitViewer] 无法生成卡牌 ID:{cardId}"); return; }

                gc.ShowCardInfo(card, null, false);
                Main.Log?.Msg($"[PortraitViewer] 查看立绘: ID={cardId} Name={characterName}");
                Main.Log?.Msg($"[PortraitViewer] 显示立绘: ID={cardId} Name={characterName}");
            }
            catch (Exception ex)
            {
                Main.Log?.Warning($"[PortraitViewer] 异常: {ex}");
                Main.Log?.Warning($"[PortraitViewer] 显示失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 批量展示角色立绘画廊
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        internal static void ShowPortraitGallery(List<int> cardIds)
        {
            try
            {
                var gc = GameController.Inst;
                if (gc == null) return;
                var cardPrefab = gc.cardPrefab;
                if (cardPrefab == null) { Main.Log?.Warning("[PortraitViewer] 无卡牌预制体"); return; }

                Canvas? pc = null;
                try { if (gc.ui != null) pc = gc.ui.GetComponentInParent<Canvas>(); } catch { }
                if (pc == null) { var cs = UnityEngine.Object.FindObjectsOfType<Canvas>(); if (cs != null && cs.Length > 0) pc = cs[0]; }
                if (pc == null) return;

                HidePortraitGallery();

                _portraitPanel = new GameObject("SGM_PortraitGallery");
                _portraitPanel.transform.SetParent(pc.transform, false);
                _portraitPanel.layer = pc.gameObject.layer;
                var pr = _portraitPanel.AddComponent<RectTransform>();
                pr.anchorMin = Vector2.zero; pr.anchorMax = Vector2.one; pr.sizeDelta = Vector2.zero;
                _portraitPanel.AddComponent<Image>().color = new Color(0, 0, 0, 0.85f);
                var cb = _portraitPanel.AddComponent<Button>();
                cb.onClick.AddListener((Action)(() => HidePortraitGallery()));
                cb.transition = Selectable.Transition.None;

                // 标题
                var tg = new GameObject("Title"); tg.transform.SetParent(_portraitPanel.transform, false);
                var tr2 = tg.AddComponent<RectTransform>();
                tr2.anchorMin = new Vector2(0.5f, 1); tr2.anchorMax = new Vector2(0.5f, 1);
                tr2.pivot = new Vector2(0.5f, 1); tr2.sizeDelta = new Vector2(400, 40);
                tr2.anchoredPosition = new Vector2(0, -20);
                var tt = tg.AddComponent<Text>();
                tt.text = $"角色立绘浏览 ({cardIds.Count})"; tt.fontSize = 22;
                tt.color = new Color(1f, 0.84f, 0f); tt.alignment = TextAnchor.MiddleCenter;
                tt.font = Resources.GetBuiltinResource<Font>("Arial.ttf"); tt.raycastTarget = false;

                // 卡牌容器
                _cardContainer = new GameObject("CardContainer");
                _cardContainer.transform.SetParent(_portraitPanel.transform, false);
                var cr2 = _cardContainer.AddComponent<RectTransform>();
                cr2.anchorMin = new Vector2(0.5f, 0.5f); cr2.anchorMax = new Vector2(0.5f, 0.5f);
                cr2.pivot = new Vector2(0.5f, 0.5f); cr2.sizeDelta = new Vector2(cardIds.Count * 220 + 40, 420);
                cr2.anchoredPosition = Vector2.zero;
                var layout = _cardContainer.AddComponent<HorizontalLayoutGroup>();
                layout.spacing = 12; layout.padding = new RectOffset(20, 20, 20, 20);
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.childControlWidth = false; layout.childControlHeight = false;
                layout.childForceExpandWidth = false; layout.childForceExpandHeight = false;

                foreach (int cid in cardIds)
                {
                    try
                    {
                        var card = gc.GenCard(cid); if (card == null) continue;
                        var cgo = UnityEngine.Object.Instantiate(cardPrefab, _cardContainer.transform);
                        cgo.name = $"PortraitCard_{cid}"; _renderedCards.Add(cgo);
                        var crr = cgo.GetComponent<RectTransform>();
                        if (crr != null) crr.sizeDelta = new Vector2(200, 360);
                        var render = cgo.GetComponent<CardRender>();
                        if (render != null)
                        {
                            int rare = card.data != null ? (int)card.data.rare : ConfigManager.GetCardBaseRare(cid);
                            render.Init(card.data, false, 1, rare, false);
                        }
                        var btn = cgo.GetComponent<Button>() ?? cgo.AddComponent<Button>();
                        int capId = cid; btn.onClick.AddListener((Action)(() => ShowCharacterPortrait(capId)));
                    }
                    catch (Exception ex) { Main.Log?.Warning($"[PortraitViewer] 渲染卡牌{cid}失败: {ex.Message}"); }
                }
                _isVisible = true;
            }
            catch (Exception ex) { Main.Log?.Warning($"[PortraitViewer] 画廊异常: {ex}"); }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        internal static void HidePortraitGallery()
        {
            foreach (var go in _renderedCards) { try { if (go != null) UnityEngine.Object.Destroy(go); } catch { } }
            _renderedCards.Clear();
            if (_portraitPanel != null) { try { UnityEngine.Object.Destroy(_portraitPanel); } catch { } _portraitPanel = null; }
            _cardContainer = null; _isVisible = false;
        }

        internal static bool IsVisible => _isVisible;

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        internal static List<int> GetCharacterCardIds()
        {
            var r = new List<int>();
            try { foreach (var kv in GameDatabase.CardNames) if (ConfigManager.GetCardCategory(kv.Key) == "角色") r.Add(kv.Key); } catch { }
            return r;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        internal static void ShowRandomCharacter()
        {
            var chars = GetCharacterCardIds();
            if (chars.Count == 0) { Main.Log?.Warning("[PortraitViewer] 未找到角色卡牌"); return; }
            int rid = chars[new System.Random().Next(chars.Count)];
            ShowCharacterPortrait(rid, GameDatabase.GetCardName(rid) ?? "未知");
        }
    }
}
