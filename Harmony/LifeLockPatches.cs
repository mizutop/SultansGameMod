using HarmonyLib;

namespace SultansGameMod.Harmony
{
    [HarmonyPatch]
    internal static class LifeLockPatches
    {
        public static bool LifeLock = false;

        /// <summary>
        /// 锁定处刑日 — 每回合开始后重置苏丹卡 life 为初始值
        /// </summary>
        [HarmonyPatch(typeof(Il2Cpp.GameController), nameof(Il2Cpp.GameController.OnBeginRound))]
        [HarmonyPostfix]
        private static void Postfix_OnBeginRound()
        {
            if (!LifeLock) return;
            try
            {
                var gc = Il2Cpp.GameController.Inst;
                if (gc == null) return;
                var sudanCards = gc.GetCurrentSudanCard();
                if (sudanCards == null) return;

                int baseLife = SultansGameMod.Data.RuntimeEngine.Save?.SudanCardInitLife ?? 7;
                if (baseLife <= 0) baseLife = 7;
                int resetCount = 0;
                foreach (var card in sudanCards)
                {
                    if (card.life < baseLife)
                    {
                        card.life = baseLife;
                        resetCount++;
                    }
                }
                if (resetCount > 0)
                    Main.Log?.Msg($"[处刑日锁定] 已重置 {resetCount} 张苏丹牌 life={baseLife}");
            }
            catch (System.Exception ex)
            {
                Main.Log?.Warning($"[LifeLock] OnBeginRound Postfix 异常: {ex}");
            }
        }
    }
}
