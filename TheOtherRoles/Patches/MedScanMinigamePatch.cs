using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace TheOtherRoles.Patches
{
    [HarmonyPatch(typeof(MedScanMinigame), nameof(MedScanMinigame.FixedUpdate))]
    class MedScanMinigamePatch
    {
        public static void Prefix(MedScanMinigame __instance)
        {
            if (__instance.MyNormTask.IsComplete)
            {
                return;
            }
            byte playerId = PlayerControl.LocalPlayer.PlayerId;
            if (__instance.medscan.CurrentUser != playerId)
            {
                __instance.medscan.CurrentUser = playerId;
            }
        }
    }
}
