using System;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using Il2Cpp;
using SultansGameMod.UI;

namespace SultansGameMod.Harmony
{
    /// <summary>
    /// 游戏原生 UI 集成补丁
    /// — 在 ESC 菜单注入"修改器"按钮
    /// — 将 OnGUI 确认框替换为游戏原生 ConfirmController
    /// </summary>
    [HarmonyPatch]
    internal static class UIPatches
    {
        private static bool _escButtonReInjected;
        private static int _lastEscFrameInjected = -1;

        internal static void ToggleNativePanel()
        {
            Main.Log?.Msg("[UIPatches] 打开修改器菜单");
            NativeMenuSystem.Open();
        }

        // ============================================================
        // 1. ESC 菜单注入"修改器"按钮
        // ============================================================
        [HarmonyPatch(typeof(ESCGameController), nameof(ESCGameController.OnShow))]
        [HarmonyPostfix]
        private static void Postfix_ESC_OnShow(ESCGameController __instance)
        {
            int frame = Time.frameCount;
            if (_escButtonReInjected && _lastEscFrameInjected == frame) return;

            try
            {
                InjectModButtonIntoESC(__instance);
                _escButtonReInjected = true;
                _lastEscFrameInjected = frame;
            }
            catch (Exception ex)
            {
                Main.Log?.Warning($"[UIPatches] ESC 按钮注入失败: {ex.Message}");
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void InjectModButtonIntoESC(ESCGameController esc)
        {
            if (esc == null) return;
            var go = esc.gameObject;
            if (go == null) return;

            // 防重复
            var existing = go.transform.Find("ModButton_SGM");
            if (existing != null) { existing.gameObject.SetActive(true); return; }

            // 找模板按钮（搜索 ESC 面板中任意带文本的按钮）
            Button? templateBtn = null;
            Transform? btnParent = null;

            var allBtns = go.GetComponentsInChildren<Button>(true);
            if (allBtns != null)
            {
                foreach (var b in allBtns)
                {
                    if (b == null || b.gameObject == null) continue;
                    // 尝试找 Text (UnityEngine.UI.Text) 子组件
                    var txt = b.GetComponentInChildren<Text>();
                    if (txt != null && !string.IsNullOrEmpty(txt.text))
                    {
                        templateBtn = b;
                        btnParent = b.transform.parent;
                        break;
                    }
                }
            }

            if (templateBtn == null || btnParent == null)
            {
                Main.Log?.Msg("[UIPatches] ESC 按钮模板未找到，将在下次尝试");
                return;
            }

            // 克隆模板
            var modBtnGo = UnityEngine.Object.Instantiate(templateBtn.gameObject, btnParent);
            modBtnGo.name = "ModButton_SGM";

            // 设置文本 (金色)
            var modTxt = modBtnGo.GetComponentInChildren<Text>();
            if (modTxt != null)
            {
                modTxt.text = "修改器";
                modTxt.color = new Color(1f, 0.84f, 0f);
            }

            // 设置点击事件
            var modBtn = modBtnGo.GetComponent<Button>();
            if (modBtn != null)
            {
                modBtn.onClick.RemoveAllListeners();
                modBtn.onClick.AddListener((Action)(() =>
                {
                    Main.Log?.Msg("[UIPatches] ESC '修改器' 被点击");
                    try { esc.OnClose(); } catch { }
                    ToggleNativePanel();
                }));
            }

            // 插入到"设置"按钮之前，而不是末尾
            try
            {
                var escBtns = go.GetComponentsInChildren<Button>(true);
                Button? settingsBtn = null;
                foreach (var b in escBtns)
                {
                    if (b == null || b.gameObject == null) continue;
                    var txt = b.GetComponentInChildren<Text>();
                    if (txt != null && (txt.text == "设置" || txt.text == "Settings"))
                    {
                        settingsBtn = b;
                        break;
                    }
                }
                if (settingsBtn != null)
                    modBtnGo.transform.SetSiblingIndex(settingsBtn.transform.GetSiblingIndex());
                else
                    modBtnGo.transform.SetAsLastSibling();
            }
            catch
            {
                modBtnGo.transform.SetAsLastSibling();
            }
            Main.Log?.Msg("[UIPatches] ESC '修改器' 按钮注入成功");
        }

        // ============================================================
        // 2. 原生确认框替代 OnGUI 确认框
        // ============================================================
        internal static void ShowNativeConfirm(string title, string action, Action callback)
        {
            try
            {
                var gc = GameController.Inst;
                if (gc == null) { callback?.Invoke(); return; }

                string msg = title + "\n操作: " + action + "\n确定要继续吗?";
                var promise = gc.ShowConfirm(msg, null, "确认", "取消", "", "");
                if (promise != null)
                {
                    promise.Then((Action<bool>)(result =>
                    {
                        if (result)
                        {
                            try { callback?.Invoke(); }
                            catch (Exception ex) { Main.Log?.Warning($"[原生确认] 回调异常: {ex}"); }
                        }
                    }));
                }
                else { callback?.Invoke(); }
            }
            catch (Exception ex)
            {
                Main.Log?.Warning($"[原生确认] 异常: {ex} — 直接执行回调");
                callback?.Invoke();
            }
        }

        // ============================================================
        // 3. ShowMenu 时重置注入标记
        // ============================================================
        [HarmonyPatch(typeof(GameController), nameof(GameController.ShowMenu))]
        [HarmonyPostfix]
        private static void Postfix_ShowMenu()
        {
            _escButtonReInjected = false;
        }
    }
}
