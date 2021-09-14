using HarmonyLib;
using static TheOtherRoles.TheOtherRoles;
using UnityEngine;
using System.Linq;

namespace TheOtherRoles.Patches {

    [HarmonyPatch(typeof(ShipStatus))]
    public class ShipStatusPatch {

        // Better Polus Value
        // Better Polus Positions
        public static readonly Vector3 DvdScreenNewPos = new Vector3(26.635f, -15.92f, 1f);
        public static readonly Vector3 VitalsNewPos = new Vector3(31.275f, -6.45f, 1f);
        public static readonly Vector3 WifiNewPos = new Vector3(15.975f, 0.084f, 1f);
        public static readonly Vector3 NavNewPos = new Vector3(11.07f, -15.298f, -0.015f);
        public static readonly Vector3 TempColdNewPos = new Vector3(7.772f, -17.103f, -0.017f);

        // Better Polus Scales
        public const float DvdScreenNewScale = 0.75f;

        // Better Polus Checks
        public static bool IsAdjustmentsDone;
        public static bool IsObjectsFetched;
        public static bool IsRoomsFetched;
        public static bool IsVentsFetched;

        // Better Polus Tasks Tweak
        public static Console WifiConsole;
        public static Console NavConsole;

        // Better Polus Vitals Tweak
        public static SystemConsole Vitals;
        public static GameObject DvdScreenOffice;

        // Better Polus Vents Tweak
        public static Vent ElectricBuildingVent;
        public static Vent ElectricalVent;
        public static Vent ScienceBuildingVent;
        public static Vent StorageVent;

        // Better Polus TempCold Tweak
        public static Console TempCold;

        // Better Polus Rooms
        public static GameObject Comms;
        public static GameObject DropShip;
        public static GameObject Outside;
        public static GameObject Science;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.CalculateLightRadius))]
        public static bool Prefix(ref float __result, ShipStatus __instance, [HarmonyArgument(0)] GameData.PlayerInfo player) {
            ISystemType systemType = __instance.Systems.ContainsKey(SystemTypes.Electrical) ? __instance.Systems[SystemTypes.Electrical] : null;
            if (systemType == null) return true;
            SwitchSystem switchSystem = systemType.TryCast<SwitchSystem>();
            if (switchSystem == null) return true;

            float num = (float)switchSystem.Value / 255f;
            
            if (player == null || player.IsDead) // IsDead
                __result = __instance.MaxLightRadius;
            else if (player.IsImpostor
                || (Jackal.jackal != null && Jackal.jackal.PlayerId == player.PlayerId && Jackal.hasImpostorVision)
                || (Sidekick.sidekick != null && Sidekick.sidekick.PlayerId == player.PlayerId && Sidekick.hasImpostorVision)
                || (Spy.spy != null && Spy.spy.PlayerId == player.PlayerId && Spy.hasImpostorVision)) // Impostor, Jackal/Sidekick or Spy with Impostor vision
                __result = __instance.MaxLightRadius * PlayerControl.GameOptions.ImpostorLightMod;
            else if (Lighter.lighter != null && Lighter.lighter.PlayerId == player.PlayerId && Lighter.lighterTimer > 0f) // if player is Lighter and Lighter has his ability active
                __result = Mathf.Lerp(__instance.MaxLightRadius * Lighter.lighterModeLightsOffVision, __instance.MaxLightRadius * Lighter.lighterModeLightsOnVision, num);
            else if (Trickster.trickster != null && Trickster.lightsOutTimer > 0f) {
                float lerpValue = 1f;
                if (Trickster.lightsOutDuration - Trickster.lightsOutTimer < 0.5f) lerpValue = Mathf.Clamp01((Trickster.lightsOutDuration - Trickster.lightsOutTimer) * 2);
                else if (Trickster.lightsOutTimer < 0.5) lerpValue = Mathf.Clamp01(Trickster.lightsOutTimer*2);
                __result = Mathf.Lerp(__instance.MinLightRadius, __instance.MaxLightRadius, 1 - lerpValue) * PlayerControl.GameOptions.CrewLightMod; // Instant lights out? Maybe add a smooth transition?
            }
            else
                __result = Mathf.Lerp(__instance.MinLightRadius, __instance.MaxLightRadius, num) * PlayerControl.GameOptions.CrewLightMod;
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.IsGameOverDueToDeath))]
        public static void Postfix2(ShipStatus __instance, ref bool __result)
        {
            __result = false;
        }

        private static int originalNumCommonTasksOption = 0;
        private static int originalNumShortTasksOption = 0;
        private static int originalNumLongTasksOption = 0;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Begin))]
        public static bool Prefix(ShipStatus __instance)
        {
            if (MapOptions.activateBetterPolus)
            {
                ApplyChanges(__instance);
            }
            var commonTaskCount = __instance.CommonTasks.Count;
            var normalTaskCount = __instance.NormalTasks.Count;
            var longTaskCount = __instance.LongTasks.Count;
            originalNumCommonTasksOption = PlayerControl.GameOptions.NumCommonTasks;
            originalNumShortTasksOption = PlayerControl.GameOptions.NumShortTasks;
            originalNumLongTasksOption = PlayerControl.GameOptions.NumLongTasks;
            if(PlayerControl.GameOptions.NumCommonTasks > commonTaskCount) PlayerControl.GameOptions.NumCommonTasks = commonTaskCount;
            if(PlayerControl.GameOptions.NumShortTasks > normalTaskCount) PlayerControl.GameOptions.NumShortTasks = normalTaskCount;
            if(PlayerControl.GameOptions.NumLongTasks > longTaskCount) PlayerControl.GameOptions.NumLongTasks = longTaskCount;
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Begin))]
        public static void Postfix3(ShipStatus __instance)
        {
            // Restore original settings after the tasks have been selected
            PlayerControl.GameOptions.NumCommonTasks = originalNumCommonTasksOption;
            PlayerControl.GameOptions.NumShortTasks = originalNumShortTasksOption;
            PlayerControl.GameOptions.NumLongTasks = originalNumLongTasksOption;
        }

        [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Awake))]
        public static class ShipStatusAwakePatch
        {
            [HarmonyPrefix]
            [HarmonyPatch]
            public static void Prefix(ShipStatus __instance)
            {
                if(MapOptions.activateBetterPolus)
                {
                    ApplyChanges(__instance);
                }                
            }
        }

        [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.FixedUpdate))]
        public static class ShipStatusFixedUpdatePatch
        {
            [HarmonyPrefix]
            [HarmonyPatch]
            public static void Prefix(ShipStatus __instance)
            {
                if ((!IsObjectsFetched || !IsAdjustmentsDone) && MapOptions.activateBetterPolus)
                {
                    ApplyChanges(__instance);
                }
            }
        }


        private static void ApplyChanges(ShipStatus instance)
        {
            if (instance.Type == ShipStatus.MapType.Pb)
            {
                FindPolusObjects();
                AdjustPolus();
            }
        }

        public static void FindPolusObjects()
        {
            FindVents();
            FindRooms();
            FindObjects();
        }

        public static void AdjustPolus()
        {
            if (IsObjectsFetched && IsRoomsFetched)
            {
                MoveVitals();
                SwitchNavWifi();
                MoveTempCold();
            }
            AdjustVents();

            IsAdjustmentsDone = true;
        }

        // --------------------
        // - Objects Fetching -
        // --------------------

        public static void FindVents()
        {
            var ventsList = Object.FindObjectsOfType<Vent>().ToList();

            if (ElectricBuildingVent == null)
            {
                ElectricBuildingVent = ventsList.Find(vent => vent.gameObject.name == "ElectricBuildingVent");
            }

            if (ElectricalVent == null)
            {
                ElectricalVent = ventsList.Find(vent => vent.gameObject.name == "ElectricalVent");
            }

            if (ScienceBuildingVent == null)
            {
                ScienceBuildingVent = ventsList.Find(vent => vent.gameObject.name == "ScienceBuildingVent");
            }

            if (StorageVent == null)
            {
                StorageVent = ventsList.Find(vent => vent.gameObject.name == "StorageVent");
            }

            IsVentsFetched = ElectricBuildingVent != null && ElectricalVent != null && ScienceBuildingVent != null &&
                              StorageVent != null;
        }

        public static void FindRooms()
        {
            if (Comms == null)
            {
                Comms = Object.FindObjectsOfType<GameObject>().ToList().Find(o => o.name == "Comms");
            }

            if (DropShip == null)
            {
                DropShip = Object.FindObjectsOfType<GameObject>().ToList().Find(o => o.name == "Dropship");
            }

            if (Outside == null)
            {
                Outside = Object.FindObjectsOfType<GameObject>().ToList().Find(o => o.name == "Outside");
            }

            if (Science == null)
            {
                Science = Object.FindObjectsOfType<GameObject>().ToList().Find(o => o.name == "Science");
            }

            IsRoomsFetched = Comms != null && DropShip != null && Outside != null && Science != null;
        }

        public static void FindObjects()
        {
            if (WifiConsole == null)
            {
                WifiConsole = Object.FindObjectsOfType<Console>().ToList()
                    .Find(console => console.name == "panel_wifi");
            }

            if (NavConsole == null)
            {
                NavConsole = Object.FindObjectsOfType<Console>().ToList()
                    .Find(console => console.name == "panel_nav");
            }

            if (Vitals == null)
            {
                Vitals = Object.FindObjectsOfType<SystemConsole>().ToList()
                    .Find(console => console.name == "panel_vitals");
            }

            if (DvdScreenOffice == null)
            {
                GameObject DvdScreenAdmin = Object.FindObjectsOfType<GameObject>().ToList()
                    .Find(o => o.name == "dvdscreen");

                if (DvdScreenAdmin != null)
                {
                    DvdScreenOffice = Object.Instantiate(DvdScreenAdmin);
                }
            }

            if (TempCold == null)
            {
                TempCold = Object.FindObjectsOfType<Console>().ToList()
                    .Find(console => console.name == "panel_tempcold");
            }

            IsObjectsFetched = WifiConsole != null && NavConsole != null && Vitals != null &&
                               DvdScreenOffice != null && TempCold != null;
        }

        // -------------------
        // - Map Adjustments -
        // -------------------

        public static void AdjustVents()
        {
            if (IsVentsFetched)
            {
                ElectricBuildingVent.Left = ElectricalVent;
                ElectricalVent.Center = ElectricBuildingVent;

                ScienceBuildingVent.Left = StorageVent;
                StorageVent.Center = ScienceBuildingVent;
            }
        }

        public static void MoveTempCold()
        {
            if (TempCold.transform.position != TempColdNewPos)
            {
                Transform tempColdTransform = TempCold.transform;
                tempColdTransform.parent = Outside.transform;
                tempColdTransform.position = TempColdNewPos;

                // Fixes collider being too high
                BoxCollider2D collider = TempCold.GetComponent<BoxCollider2D>();
                collider.isTrigger = false;
                collider.size += new Vector2(0f, -0.3f);
            }
        }

        public static void SwitchNavWifi()
        {
            if (WifiConsole.transform.position != WifiNewPos)
            {
                Transform wifiTransform = WifiConsole.transform;
                wifiTransform.parent = DropShip.transform;
                wifiTransform.position = WifiNewPos;
            }

            if (NavConsole.transform.position != NavNewPos)
            {
                Transform navTransform = NavConsole.transform;
                navTransform.parent = Comms.transform;
                navTransform.position = NavNewPos;

                // Prevents crewmate being able to do the task from outside
                NavConsole.checkWalls = true;
            }
        }

        public static void MoveVitals()
        {
            if (Vitals.transform.position != VitalsNewPos)
            {
                // Vitals
                Transform vitalsTransform = Vitals.gameObject.transform;
                vitalsTransform.parent = Science.transform;
                vitalsTransform.position = VitalsNewPos;
            }

            if (DvdScreenOffice.transform.position != DvdScreenNewPos)
            {
                // DvdScreen
                Transform dvdScreenTransform = DvdScreenOffice.transform;
                dvdScreenTransform.position = DvdScreenNewPos;

                var localScale = dvdScreenTransform.localScale;
                localScale =
                    new Vector3(DvdScreenNewScale, localScale.y,
                        localScale.z);
                dvdScreenTransform.localScale = localScale;
            }
        }

    }

}