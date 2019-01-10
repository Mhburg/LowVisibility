﻿using BattleTech;
using Harmony;
using LowVisibility.Helper;
using System;
using System.Linq;
using System.Reflection;
using static LowVisibility.Helper.ActorHelper;

namespace LowVisibility.Patch {

    // Setup the actor and pilot states at the start of the encounter
    [HarmonyPatch(typeof(TurnDirector), "OnEncounterBegin")]
    public static class TurnDirector_OnEncounterBegin {

        public static void Prefix(TurnDirector __instance) {
            LowVisibility.Logger.LogIfDebug("=== TurnDirector:OnEncounterBegin:pre - entered.");

            // Do a pre-encounter populate 
            if (__instance != null && __instance.Combat != null && __instance.Combat.AllActors != null) {
                AbstractActor randomPlayerActor = null;
                foreach (AbstractActor actor in __instance.Combat.AllActors) {
                    if (actor != null) {
                        // Parse their EW config
                        ActorEWConfig actorEWConfig = new ActorEWConfig(actor);
                        State.ActorEWConfig[actor.GUID] = actorEWConfig;

                        // Make a pre-encounter detectCheck for them\
                        RoundDetectRange detectRange = MakeSensorRangeCheck(actor, false);
                        LowVisibility.Logger.LogIfDebug($"  Actor:{ActorLabel(actor)} has detectRange:{detectRange} at load/start");
                        State.RoundDetectResults[actor.GUID] = detectRange;

                        bool isPlayer = actor.TeamId == __instance.Combat.LocalPlayerTeamGuid;
                        if (isPlayer && randomPlayerActor == null) {
                            randomPlayerActor = actor;
                        }

                    } else {
                        LowVisibility.Logger.LogIfDebug($"  Actor:{ActorLabel(actor)} was NULL!");
                    }
                }
                State.UpdateDetectionForAllActors(__instance.Combat, randomPlayerActor);
            }
        }
    }

    [HarmonyPatch(typeof(TurnDirector), "InitFromSave")]
    public static class TurnDirector_InitFromSave {
        public static void Postfix(TurnDirector __instance) {
            LowVisibility.Logger.LogIfDebug("TurnDirector:InitFromSave:post - entered.");
        }
    }

    [HarmonyPatch()]
    public static class TurnDirector_BeginNewRound {

        // Private method can't be patched by annotations, so use MethodInfo
        public static MethodInfo TargetMethod() {
            return AccessTools.Method(typeof(TurnDirector), "BeginNewRound", new Type[] { typeof(int) });
        }

        public static void Prefix(TurnDirector __instance) {
            LowVisibility.Logger.LogIfDebug("=== TurnDirector:BeginNewRound:post - entered.");

            // Update the current vision for all allied and friendly units
            AbstractActor randomPlayerActor = __instance.Combat.AllActors
                .Where(aa => aa.TeamId == __instance.Combat.LocalPlayerTeamGuid)
                .First();
            State.UpdateDetectionForAllActors(__instance.Combat, randomPlayerActor);
        }
    }

    [HarmonyPatch(typeof(TurnDirector), "OnCombatGameDestroyed")]
    public static class TurnDirector_OnCombatGameDestroyed {
        public static void Postfix(TurnDirector __instance) {
            // Remove all combat state
            State.RoundDetectResults.Clear();
            State.ActorEWConfig.Clear();
            State.SourceActorLockStates.Clear();
            State.LastPlayerActivatedActorGUID = null;
            State.JammedActors.Clear();
        }
    }


}