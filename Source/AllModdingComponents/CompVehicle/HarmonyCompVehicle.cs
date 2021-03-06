﻿﻿﻿using Harmony;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using System.Reflection;
using UnityEngine;
using System.Reflection.Emit;
using RimWorld.Planet;
using System.Runtime.CompilerServices;
using RimWorld.BaseGen;
using System.Text;
using System;
using Verse.Sound;
using Verse.AI.Group;

namespace CompVehicle
{
    [StaticConstructorOnStartup]
    static class HarmonyCompVehicle
    {
        static HarmonyCompVehicle()
        {
            HarmonyInstance harmony = HarmonyInstance.Create("rimworld.jecrell.comps.vehicle");

            
            #region Functions
            ///
            /// VEHICLE FUNCTIONS
            /// Implements new systems to RimWorld when
            /// vehicles are present.
            ///
            harmony.Patch(AccessTools.Method(typeof(Pawn_RotationTracker), "RotationTrackerTick"), new HarmonyMethod(typeof(HarmonyCompVehicle),
                nameof(VehicleRotatorTick)), null);
            harmony.Patch(AccessTools.Method(typeof(DamageWorker_AddInjury), "FinalizeAndAddInjury", new Type[] { typeof(Pawn), typeof(Hediff_Injury), typeof(DamageInfo), AccessTools.TypeByName("DamageResult").MakeByRefType() }), null, 
                new HarmonyMethod(typeof(HarmonyCompVehicle), 
                nameof(TryInjureVehicleOccupants)));
            harmony.Patch(AccessTools.Method(typeof(HealthUtility), "GetGeneralConditionLabel"), 
                new HarmonyMethod(typeof(HarmonyCompVehicle), 
                nameof(ReplaceConditionLabel)), null);
            harmony.Patch(AccessTools.Method(typeof(HealthCardUtility), "DrawOverviewTab"), 
                new HarmonyMethod(typeof(HarmonyCompVehicle), 
                nameof(DisplayOperationalSystems)), null);
            harmony.Patch(AccessTools.Property(typeof(MapPawns), nameof(MapPawns.FreeColonistsSpawnedCount)).GetGetMethod(), null,
                new HarmonyMethod(typeof(HarmonyCompVehicle),
                nameof(IncludeVehicleOccupantsInMapPawns)));
            harmony.Patch(AccessTools.Property(typeof(MapPawns), nameof(MapPawns.FreeColonistsSpawnedOrInPlayerEjectablePodsCount)).GetGetMethod(), null, 
                new HarmonyMethod(typeof(HarmonyCompVehicle), 
                nameof(IncludeVehicleOccupantsInMapPawns)));
            harmony.Patch(AccessTools.Method(typeof(Pawn_HealthTracker), "ShouldBeDowned"), 
                new HarmonyMethod(typeof(HarmonyCompVehicle), 
                nameof(VehicleShouldBeDowned)), null);
            harmony.Patch(AccessTools.Method(typeof(PawnDownedWiggler), "WigglerTick"), 
                new HarmonyMethod(typeof(HarmonyCompVehicle), 
                nameof(VehicleShouldWiggle)), null);
            harmony.Patch(AccessTools.Method(typeof(FloatMenuUtility), nameof(FloatMenuUtility.GetRangedAttackAction)), null, null,
                new HarmonyMethod(typeof(HarmonyCompVehicle), 
                nameof(FightActionTranspiler)));
            harmony.Patch(AccessTools.Method(typeof(FloatMenuUtility), nameof(FloatMenuUtility.GetMeleeAttackAction)), null, null,
                new HarmonyMethod(typeof(HarmonyCompVehicle), 
                nameof(FightActionTranspiler)));
            #endregion Functions
            
            #region ErrorHandling
            ///
            /// VEHICLE ERROR HANDLING
            /// These patches help integrate vehicles safely
            /// into existing/new games and handle errors / exceptions.
            ///

            //harmony.Patch(AccessTools.)
            harmony.Patch(AccessTools.Method(typeof(PawnUtility), "IsTravelingInTransportPodWorldObject"), null, new HarmonyMethod(typeof(HarmonyCompVehicle),
                nameof(PreventAssigningRandomFaction)));  
            harmony.Patch(AccessTools.Method(typeof(SocialCardUtility), "Recache"), new HarmonyMethod(typeof(HarmonyCompVehicle), 
                nameof(SocialTabNullHandling)), null);  
            harmony.Patch(AccessTools.Method(typeof(Pawn), "get_IsColonistPlayerControlled"), null, new HarmonyMethod(typeof(HarmonyCompVehicle), 
                nameof(IncludeVehiclesInIsColonistPlayerControlled)));  
            harmony.Patch(AccessTools.Method(typeof(Pawn), "CurrentlyUsableForBills"), null, new HarmonyMethod(typeof(HarmonyCompVehicle), 
                nameof(CantUseMovingVehicles)));  
            harmony.Patch(AccessTools.Method(typeof(Pawn_DraftController), "set_Drafted"), new HarmonyMethod(typeof(HarmonyCompVehicle), 
                nameof(DraftedVehiclesCanMove)), null);  
            harmony.Patch(AccessTools.Method(typeof(Pawn_PathFollower), "StartPath"), new HarmonyMethod(typeof(HarmonyCompVehicle), 
                nameof(CanVehicleMove)), null);  
            harmony.Patch(AccessTools.Method(typeof(Verb_Shoot), "TryCastShot"), new HarmonyMethod(typeof(HarmonyCompVehicle), 
                nameof(CanVehicleShoot)), null);  
            harmony.Patch(AccessTools.Method(typeof(GameEnder), "IsPlayerControlledWithFreeColonist"), null, new HarmonyMethod(typeof(HarmonyCompVehicle), 
                nameof(CanEndGame)));  
            harmony.Patch(typeof(RestUtility).GetMethods(BindingFlags.Public | BindingFlags.Static).First(mi => mi.Name == "FindBedFor" && mi.GetParameters().Count() > 1), null, new HarmonyMethod(typeof(HarmonyCompVehicle), 
                nameof(DontRescueVehicles)), null);  
            //harmony.Patch(AccessTools.Method(typeof(FloatMenuMakerMap), "AddHumanlikeOrders"), null, new HarmonyMethod(typeof(HarmonyCompVehicle), 
               // nameof(DontRescueVehiclesInFloatMenus)));  

            harmony.Patch(typeof(SymbolResolver_RandomMechanoidGroup).GetMethods(BindingFlags.NonPublic | BindingFlags.Static).First(mi => mi.HasAttribute<CompilerGeneratedAttribute>() && mi.ReturnType == typeof(bool) && mi.GetParameters().Count() == 1 && mi.GetParameters()[0].ParameterType == typeof(PawnKindDef)), null, new HarmonyMethod(typeof(HarmonyCompVehicle), 
                nameof(MechanoidsFixerAncient)));  
            harmony.Patch(typeof(CompSpawnerMechanoidsOnDamaged).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).First(mi => mi.HasAttribute<CompilerGeneratedAttribute>() && mi.ReturnType == typeof(bool) && mi.GetParameters().Count() == 1 && mi.GetParameters()[0].ParameterType == typeof(PawnKindDef)), null, new HarmonyMethod(typeof(HarmonyCompVehicle), 
                nameof(MechanoidsFixer)));  
            harmony.Patch(AccessTools.Method(typeof(JobDriver_Wait), "CheckForAutoAttack"), null, null, new HarmonyMethod(typeof(HarmonyCompVehicle), 
                nameof(CheckForAutoAttackTranspiler)));  
            harmony.Patch(AccessTools.Method(typeof(VerbTracker).GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Instance).First(t => AccessTools.Method(t, "MoveNext") != null), "MoveNext"), null, null, new HarmonyMethod(typeof(HarmonyCompVehicle), 
                nameof(GetVerbsCommandsTranspiler)));  
            harmony.Patch(AccessTools.Method(typeof(ThinkNode_ConditionalColonist), "Satisfied"), null, new HarmonyMethod(typeof(HarmonyCompVehicle), 
                nameof(Satisfied_PostFix)), null);  
            #endregion ErrorHandling
            
            #region AIHandling
            /// AI HANDLING
            /// Handles various AI issues for vehicles to function
            /// properly.

            harmony.Patch(AccessTools.Method(typeof(JobGiver_Orders), "TryGiveJob"), null, new HarmonyMethod(typeof(HarmonyCompVehicle),
                nameof(PreventWaitAttackError)));  
            harmony.Patch(AccessTools.Method(typeof(LordToil_PrepareCaravan_GatherItems), "UpdateAllDuties"), new HarmonyMethod(typeof(HarmonyCompVehicle), 
                nameof(GiveVehiclesLoadItemDuties)), null);  
            harmony.Patch(AccessTools.Method(typeof(LordToil_PrepareCaravan_GatherAnimals), "UpdateAllDuties"), new HarmonyMethod(typeof(HarmonyCompVehicle), 
                nameof(GiveVehiclesLoadAnimalDuties)), null);  
            harmony.Patch(AccessTools.Method(typeof(LordToil_PrepareCaravan_GatherSlaves), "LordToilTick"), new HarmonyMethod(typeof(HarmonyCompVehicle), 
                nameof(GiveVehiclesLoadSlaveDuties)), null);  
            harmony.Patch(AccessTools.Method(typeof(LordToil_PrepareCaravan_GatherItems), "LordToilTick"), new HarmonyMethod(typeof(HarmonyCompVehicle), 
                nameof(GiveVehiclesLoadItemToil)), null);  
            harmony.Patch(AccessTools.Method(typeof(RimWorld.Planet.CaravanPawnsNeedsUtility), "TrySatisfyPawnNeeds"), new HarmonyMethod(typeof(HarmonyCompVehicle), 
                nameof(TrySatisfyFuelNeeds)), null);  
            //Reminder to check this code....
            harmony.Patch(AccessTools.Method(typeof(JobGiver_PrepareCaravan_GatherItems), "TryGiveJob"), new HarmonyMethod(typeof(HarmonyCompVehicle), 
                nameof(TryGiveItemJob_PreFix)), null);  
            #endregion AIHandling
            
            #region CaravanHandling
            ///
            /// CARAVAN HANDLING
            /// These patches handle exceptions and
            /// cases involving vehicles and caravans.
            ///
            harmony.Patch(AccessTools.Method(typeof(Caravan), "AddPawn"), new HarmonyMethod(typeof(HarmonyCompVehicle),
                nameof(AddVehiclePawnsToCaravan)), null);
            harmony.Patch(AccessTools.Method(typeof(Dialog_SplitCaravan), "CheckForErrors"), new HarmonyMethod(typeof(HarmonyCompVehicle),
                nameof(DontSplitCaravansWithVehicles)), null);
            harmony.Patch(AccessTools.Method(typeof(Dialog_FormCaravan), "CheckForErrors"), new HarmonyMethod(typeof(HarmonyCompVehicle), 
                nameof(CheckForErrors_PreFix)), null);
            harmony.Patch(AccessTools.Method(typeof(CaravanPeopleAndItemsTabUtility), "DoRows"), new HarmonyMethod(typeof(HarmonyCompVehicle), 
                nameof(DoRows_PreFix)), null);
            harmony.Patch(AccessTools.Method(typeof(CaravanUIUtility), "AddPawnsSections"), null, new HarmonyMethod(typeof(HarmonyCompVehicle), 
                nameof(AddPawnsSections_PostFix)));
            harmony.Patch(AccessTools.Method(typeof(CaravanExitMapUtility), "CanExitMapAndJoinOrCreateCaravanNow"), null, new HarmonyMethod(typeof(HarmonyCompVehicle), 
                nameof(CanExit_PostFix)), null);
            harmony.Patch(AccessTools.Method(typeof(MassUtility), "Capacity"), null, new HarmonyMethod(typeof(HarmonyCompVehicle), 
                nameof(Capacity_PostFix)), null);
            harmony.Patch(AccessTools.Method(typeof(CaravanTicksPerMoveUtility), "GetTicksPerMove", new Type[] { typeof(List<Pawn>) }), null, new HarmonyMethod(typeof(HarmonyCompVehicle), 
                nameof(GetTicksPerMove_PostFix)));
            //harmony.Patch(AccessTools.Method(typeof(RimWorld.Planet.CaravanMaker), "MakeCaravan"), null, new HarmonyMethod(typeof(HarmonyCompVehicle), 
            //    nameof(MakeCaravan_PostFix)));
            harmony.Patch(AccessTools.Method(typeof(RimWorld.Planet.CaravanExitMapUtility), "ExitMapAndJoinOrCreateCaravan"), new HarmonyMethod(typeof(HarmonyCompVehicle), 
                nameof(ExitMapAndJoinOrCreateCaravan_PreFix)), null);
            harmony.Patch(AccessTools.Method(typeof(RimWorld.Planet.CaravanEnterMapUtility), "Enter", new Type[] { typeof(Caravan), typeof(Map), typeof(Func<Pawn, IntVec3>), typeof(CaravanDropInventoryMode), typeof(bool) }), new HarmonyMethod(typeof(HarmonyCompVehicle), 
                nameof(Enter_PreFix)), null, null);
            harmony.Patch(AccessTools.Method(typeof(RimWorld.Planet.Caravan), "GetInspectString"), null, new HarmonyMethod(typeof(HarmonyCompVehicle), 
                nameof(GetInspectString_PostFix)));
            harmony.Patch(AccessTools.Method(typeof(CaravanPeopleAndItemsTabUtility), "DoRow", new Type[] { typeof(Rect), typeof(Thing), typeof(Caravan), typeof(Pawn).MakeByRefType(), typeof(bool), typeof(bool) }), null, null, new HarmonyMethod(typeof(HarmonyCompVehicle), 
                nameof(DoRow_Transpiler)));
            harmony.Patch(AccessTools.Method(typeof(RimWorld.Dialog_FormCaravan), "DoWindowContents"), new HarmonyMethod(typeof(HarmonyCompVehicle), 
                nameof(DoWindowContents_PreFix)), null);
            #endregion CaravanHandling
            
            #region Tests
            ///
            /// TESTS
            /// This is for testing or debugging for errors.
            /// Most of these methods are prefix detours to make tests.
            /// 

            //harmony.Patch(AccessTools.Method(typeof(RimWorld.Planet.CaravanMaker), "MakeCaravan"), new HarmonyMethod(typeof(HarmonyCompVehicle),
            //    nameof(MakeCaravan_PreFix)), null);
            //harmony.Patch(AccessTools.Method(typeof(CaravanExitMapUtility), "ExitMapAndCreateCaravan", new Type[] { typeof(IEnumerable<Pawn>), typeof(Faction), typeof(int) }), new HarmonyMethod(typeof(HarmonyCompVehicle),
            //    nameof(ExitMapAndCreateCaravan_Test)), null);
            //harmony.Patch(AccessTools.Method(typeof(Pawn), "ExitMap"), new HarmonyMethod(typeof(HarmonyCompVehicle),
            //    nameof(ExitMap_Test)), null);
            #endregion

        }

        /// CODE LEGEND
        /// J = Jec's patch.
        /// S = Swenzi's patch.
        /// E = Erdelf's patch.
        /// ??? = Needs cleaning

        #region FunctionsMethods

        //J Vehicles will no longer rotate when drafted.
        public static bool VehicleRotatorTick(Pawn_RotationTracker __instance)
        {
            if (Traverse.Create(__instance).Field("pawn").GetValue<Pawn>() is Pawn thisPawn &&
                thisPawn?.GetComp<CompVehicle>() is CompVehicle compVehicle)
            {
                if (thisPawn?.Destroyed ?? false)
                {
                    return false;
                }
                if (thisPawn?.jobs?.HandlingFacing ?? false)
                {
                    return false;
                }
                if (thisPawn?.pather?.Moving ?? false)
                {
                    if (thisPawn.pather.curPath == null || thisPawn.pather.curPath.NodesLeftCount < 1)
                    {
                        return false;
                    }
                    //this.FaceAdjacentCell(thisPawn.pather.nextCell);
                    AccessTools.Method(typeof(Pawn_RotationTracker), "FaceAdjacentCell").Invoke(__instance, new object[] { thisPawn.pather.nextCell });
                    //Traverse.Create(__instance).Method("FaceAdjacentCell", new object[] { thisPawn.pather.nextCell });
                    //compVehicle.lastDirection = thisPawn.Rotation;
                    return false;
                }
                else
                {
                    Stance_Busy stance_Busy = thisPawn.stances.curStance as Stance_Busy;
                    if (stance_Busy != null && stance_Busy.focusTarg.IsValid)
                    {
                        if (stance_Busy.focusTarg.HasThing)
                        {
                            //this.Face(stance_Busy.focusTarg.Thing.DrawPos);
                            //Traverse.Create(__instance).Method("Face", new object[] { stance_Busy.focusTarg.Thing.DrawPos });
                            AccessTools.Method(typeof(Pawn_RotationTracker), "Face").Invoke(__instance, new object[] { stance_Busy.focusTarg.Thing.DrawPos });
                        }
                        else
                        {
                            //this.FaceCell(stance_Busy.focusTarg.Cell);
                            //Traverse.Create(__instance).Method("FaceCell", new object[] { stance_Busy.focusTarg.Cell });
                            AccessTools.Method(typeof(Pawn_RotationTracker), "FaceCell").Invoke(__instance, new object[] { stance_Busy.focusTarg.Cell });
                        }
                        return false;
                    }
                    if (thisPawn?.jobs?.curJob != null)
                    {
                        LocalTargetInfo target = thisPawn.CurJob.GetTarget(thisPawn.jobs.curDriver.rotateToFace);
                        if (target.HasThing)
                        {
                            if (compVehicle.CanMove)
                            {
                                bool flag = false;
                                IntVec3 c = default(IntVec3);
                                CellRect cellRect = target.Thing.OccupiedRect();
                                for (int i = cellRect.minZ; i <= cellRect.maxZ; i++)
                                {
                                    for (int j = cellRect.minX; j <= cellRect.maxX; j++)
                                    {
                                        if (thisPawn.Position == new IntVec3(j, 0, i))
                                        {
                                            //this.Face(target.Thing.DrawPos);
                                            //Traverse.Create(__instance).Method("Face", new object[] { target.Thing.DrawPos });
                                            AccessTools.Method(typeof(Pawn_RotationTracker), "Face").Invoke(__instance, new object[] { target.Thing.DrawPos });
                                            return false;
                                        }
                                    }
                                }
                                for (int k = cellRect.minZ; k <= cellRect.maxZ; k++)
                                {
                                    for (int l = cellRect.minX; l <= cellRect.maxX; l++)
                                    {
                                        IntVec3 intVec = new IntVec3(l, 0, k);
                                        if (intVec.AdjacentToCardinal(thisPawn.Position))
                                        {
                                            //this.FaceAdjacentCell(intVec);
                                            AccessTools.Method(typeof(Pawn_RotationTracker), "FaceAdjacentCell").Invoke(__instance, new object[] { intVec });
                                            //Traverse.Create(__instance).Method("FaceAdjacentCell", new object[] { intVec });
                                            return false;
                                        }
                                        if (intVec.AdjacentTo8Way(thisPawn.Position))
                                        {
                                            flag = true;
                                            c = intVec;
                                        }
                                    }
                                }
                                if (flag)
                                {
                                    if (DebugViewSettings.drawPawnRotatorTarget)
                                    {
                                        thisPawn.Map.debugDrawer.FlashCell(thisPawn.Position, 0.6f, "jbthing");
                                        GenDraw.DrawLineBetween(thisPawn.Position.ToVector3Shifted(), c.ToVector3Shifted());
                                    }
                                    //this.FaceAdjacentCell(c);
                                    AccessTools.Method(typeof(Pawn_RotationTracker), "FaceAdjacentCell").Invoke(__instance, new object[] { c });
                                    //Traverse.Create(__instance).Method("FaceAdjacentCell", new object[] { c });
                                    return false;
                                }
                                AccessTools.Method(typeof(Pawn_RotationTracker), "Face").Invoke(__instance, new object[] { target.Thing.DrawPos });
                                //Traverse.Create(__instance).Method("Face", new object[] { target.Thing.DrawPos });
                                return false;
                            }
                            
                        }
                        else
                        {
                            if (thisPawn.Position.AdjacentTo8Way(target.Cell))
                            {
                                if (DebugViewSettings.drawPawnRotatorTarget)
                                {
                                    thisPawn.Map.debugDrawer.FlashCell(thisPawn.Position, 0.2f, "jbloc");
                                    GenDraw.DrawLineBetween(thisPawn.Position.ToVector3Shifted(), target.Cell.ToVector3Shifted());
                                }
                                //this.FaceAdjacentCell(target.Cell);
                                AccessTools.Method(typeof(Pawn_RotationTracker), "FaceAdjacentCell").Invoke(__instance, new object[] { target.Cell });
                                //Traverse.Create(__instance).Method("FaceAdjacentCell", new object[] { target.Cell });
                                return false;
                            }
                            if (target.Cell.IsValid && target.Cell != thisPawn.Position)
                            {
                                //this.Face(target.Cell.ToVector3());
                                //Traverse.Create(__instance).Method("Face", new object[] { target.Cell.ToVector3() });
                                AccessTools.Method(typeof(Pawn_RotationTracker), "Face").Invoke(__instance, new object[] { target.Cell.ToVector3() });
                                return false;
                            }
                        }
                    }
                    //if (thisPawn.Drafted)
                    //{
                    //    thisPawn.Rotation = compVehicle.lastDirection;
                    //}
                    return false;
                }
            }
            return true;

        }
            
        //J When characters fire upon the vehicle, if the vehicle's body part defs include a tag that references a vehicle role,
        //J there is a chance that a character holding that role can be injured. Critical injury chances also exist.
        // Verse.DamageWorker_AddInjury
        public static void TryInjureVehicleOccupants(DamageWorker_AddInjury __instance, Pawn pawn, Hediff_Injury injury, DamageInfo dinfo)
        {
            CompVehicle compPilotable = pawn.GetComp<CompVehicle>();
            if (compPilotable != null)
            {
                List<Pawn> affectedPawns = new List<Pawn>();

                if (compPilotable.handlers != null && compPilotable.handlers.Count > 0)
                {
                    foreach (VehicleHandlerGroup group in compPilotable.handlers)
                    {
                        if (group.OccupiedParts != null && (group.handlers != null && group.handlers.Count > 0))
                        {
                            if (group.OccupiedParts.Contains(injury.Part))
                            {
                                affectedPawns.AddRange(group.handlers);
                            }
                        }
                    }
                }

                //Attack the seatholder
                if (affectedPawns != null && affectedPawns.Count > 0)
                {
                    DamageInfo newDamageInfo = new DamageInfo(dinfo);
                    float criticalBonus = 0f;
                    if (Rand.Value < compPilotable.Props.seatHitCriticalHitChance) criticalBonus = dinfo.Amount * 2;
                    float newDamFloat = (dinfo.Amount * compPilotable.Props.seatHitDamageFactor) + criticalBonus;
                    newDamageInfo.SetAmount((int)newDamFloat);
                    affectedPawns.RandomElement<Pawn>().TakeDamage(newDamageInfo);
                }
            }
        }
        
        //J Allows for various condition labels to be changed in CompVehicle's properties.
        // Verse.HealthUtility
        public static bool ReplaceConditionLabel(ref string __result, Pawn pawn, bool shortVersion = false)
        {
            if (pawn != null)
            {
                var compPilotable = pawn.GetComp<CompVehicle>();
                if (compPilotable != null)
                {
                    if (pawn.Downed || !pawn.health.capacities.CanBeAwake)
                    {
                        __result = compPilotable.Props.labelInoperable;
                        return false;
                    }
                    if (pawn.Dead)
                    {
                        __result = compPilotable.Props.labelBroken;
                        return false;
                    }
                    if (pawn.health.summaryHealth.SummaryHealthPercent < 0.95)
                    {
                        __result = compPilotable.Props.labelDamaged;
                        return false;
                    }
                    __result = compPilotable.Props.labelUndamaged;
                    return false;
                }
            }
            return true;
        }

        //J Changes the CompVehicle health card to display which systems are operational rather than standard pawn capacities.
        // RimWorld.HealthCardUtility
        public static bool DisplayOperationalSystems(ref float __result, Rect leftRect, Pawn pawn, float curY)
        {
            if (pawn != null)
            {
                CompVehicle compPilotable = pawn.GetComp<CompVehicle>();
                if (compPilotable != null)
                {
                    curY += 4f;

                    if (compPilotable.Props.movementHandling > HandlingType.Incapable)
                    {
                        //Movement Systems: Online

                        Text.Font = GameFont.Tiny;
                        Text.Anchor = TextAnchor.UpperLeft;
                        GUI.color = new Color(0.9f, 0.9f, 0.9f);
                        string text = StringOf.Movement;
                        if (compPilotable.movingStatus == MovingState.able)
                        {
                            text = text + ": " + StringOf.On;
                        }
                        else
                        {
                            text = text + ": " + StringOf.Off;
                        }
                        Rect rect = new Rect(0f, curY, leftRect.width, 34f);
                        Widgets.Label(rect, text.CapitalizeFirst());
                    }

                    if (compPilotable.Props.manipulationHandling > HandlingType.Incapable)
                    {
                        //Manipulation Systems: Online

                        curY += 34f;
                        Text.Font = GameFont.Tiny;
                        Text.Anchor = TextAnchor.UpperLeft;
                        GUI.color = new Color(0.9f, 0.9f, 0.9f);
                        string textM = StringOf.Manipulation;
                        if (compPilotable.manipulationStatus == ManipulationState.able)
                        {
                            textM = textM + ": " + StringOf.On;
                        }
                        else
                        {
                            textM = textM + ": " + StringOf.Off;
                        }
                        Rect rectM = new Rect(0f, curY, leftRect.width, 34f);
                        Widgets.Label(rectM, textM.CapitalizeFirst());
                    }

                    if (compPilotable.Props.weaponHandling > HandlingType.Incapable)
                    {
                        //Weapons Systems: Online

                        curY += 34f;
                        Text.Font = GameFont.Tiny;
                        Text.Anchor = TextAnchor.UpperLeft;
                        GUI.color = new Color(0.9f, 0.9f, 0.9f);
                        string text2 = StringOf.Weapons;
                        if (compPilotable.weaponStatus == WeaponState.able)
                        {
                            text2 = text2 + ": " + StringOf.On;
                        }
                        else
                        {
                            text2 = text2 + ": " + StringOf.Off;
                        }
                        Rect rect2 = new Rect(0f, curY, leftRect.width, 34f);
                        Widgets.Label(rect2, text2.CapitalizeFirst());
                    }
                    curY += 34f;
                    __result = curY;
                    return false;
                }
            }
            return true;
        }

        //E Adds colonists to the map pawns list when they are inside vehicles.
        //
        public static void IncludeVehicleOccupantsInMapPawns(MapPawns __instance, ref int __result)
        {
            __result += __instance.AllPawns.Where(p => p.Faction == Faction.OfPlayer && p.TryGetComp<CompVehicle>() != null).Sum(p => p.TryGetComp<CompVehicle>().AllOccupants.Count);
        }

        //J Downed can be disabled from CompProperties_Vehicle.
        // Verse.Pawn_HealthTracker
        public static bool VehicleShouldBeDowned(Pawn_HealthTracker __instance, ref bool __result)
        {
            Pawn pawn = (Pawn)AccessTools.Field(typeof(Pawn_HealthTracker), "pawn").GetValue(__instance);
            if (pawn != null)
            {
                CompVehicle compPilotable = pawn.GetComp<CompVehicle>();
                if (compPilotable != null)
                {
                    if (!compPilotable.Props.canBeDowned)
                    {
                        __result = false;
                        return false;
                    }
                }
            }
            return true;
        }

        //J Wiggling can be disabled from CompProperties_Vehicle.
        // Verse.PawnDownedWiggler
        public static bool VehicleShouldWiggle(PawnDownedWiggler __instance)
        {
            Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
            if (pawn != null)
            {
                CompVehicle compPilotable = pawn.GetComp<CompVehicle>();
                if (compPilotable != null)
                {
                    if (!compPilotable.Props.canWiggleWhenDowned) return false;
                }
            }
            return true;
        }

        //E Right click attack options allowed.
        public static IEnumerable<CodeInstruction> FightActionTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            FieldInfo storyInfo = AccessTools.Field(typeof(Pawn), nameof(Pawn.story));
            bool done = false;
            List<CodeInstruction> instructionList = instructions.ToList();
            for (int i = 0; i < instructionList.Count; i++)
            {
                CodeInstruction instruction = instructionList[i];

                if (!done && instruction.operand == storyInfo)
                {
                    yield return instruction;
                    yield return new CodeInstruction(instructionList[i + 3]);
                    yield return new CodeInstruction(instructionList[i - 2]) { labels = new List<Label>() };
                    yield return new CodeInstruction(instructionList[i - 1]);
                    instruction = new CodeInstruction(instruction);
                    done = true;
                }

                yield return instruction;
            }
        }
        #endregion FunctionsMethods

        #region ErrorHandlingMethods
        // J This patch allows colonists to freely join vehicles 
        //   without having their factions randomly switched around.
        // RimWorld.PawnUtility
        public static void PreventAssigningRandomFaction(ref bool __result, Pawn pawn)
        {
            bool prevResult = __result;
            //Check for vehicles...
            __result = prevResult || (ThingOwnerUtility.AnyParentIs<VehicleHandlerGroup>(pawn));
        }


        

        //S Bug fixes social tab issue with vehicles
        //Purpose: Bug fixes IWTab social issue for caravans
        //Logic: Vehicles don't have socialinfo so skip them
        //Improvements: None I can think of
        public static bool SocialTabNullHandling(Pawn selPawnForSocialInfo)
        {
            if (selPawnForSocialInfo == null || selPawnForSocialInfo.relations == null)
                return false;
            return true;
        }

        //J IsColonistPlayerControlled includes vehicles.
        public static void IncludeVehiclesInIsColonistPlayerControlled(Pawn __instance, ref bool __result)
        {
            CompVehicle vehicle = __instance.GetComp<CompVehicle>();
            if (vehicle != null)
            {
                if (__instance.Faction == Faction.OfPlayer) __result = true;
            }
        }
        
        //J Moving vehicles can't be used.
        // Verse.Pawn
        public static void CantUseMovingVehicles(Pawn __instance, ref bool __result)
        {
            CompVehicle vehicle = __instance.GetComp<CompVehicle>();
            if (vehicle != null)
            {
                if (!__instance.pather.MovingNow) __result = true;
            }
        }

        //J Drafted vehicles can move.
        public static bool DraftedVehiclesCanMove(Pawn_DraftController __instance, bool value)
        {
            if (__instance?.pawn?.TryGetComp<CompVehicle>() is CompVehicle v)
            {
                if (value == true && !__instance.Drafted)
                {
                    if (!v.CanMove)
                    {
                        Messages.Message("CompVehicle_CannotMove".Translate(__instance.pawn.KindLabel), MessageTypeDefOf.RejectInput);
                        return false;
                    }
                }
            }
            return true;
        }

        //J If needed drivers are absent, Vehicles don't move.
        // Verse.AI.Pawn_PathFollower
        public static bool CanVehicleMove(Pawn_PathFollower __instance, LocalTargetInfo dest, PathEndMode peMode)
        {
            Pawn pawn = (Pawn)AccessTools.Field(typeof(Pawn_PathFollower), "pawn").GetValue(__instance);
            if (pawn != null)
            {
                CompVehicle compPilotable = pawn.GetComp<CompVehicle>();
                if (compPilotable != null)
                {
                    if (compPilotable.movingStatus == MovingState.frozen) return false;
                }
            }
            return true;
        }

        //J If needed gunners are absent, Vehicles don't shoot.
        // Verse.Verb_Shoot
        public static bool CanVehicleShoot(Verb_Shoot __instance, ref bool __result)
        {
            Pawn pawn = __instance.CasterPawn;
            if (pawn != null)
            {
                CompVehicle compPilotable = pawn.GetComp<CompVehicle>();
                if (compPilotable != null)
                {
                    if (compPilotable.weaponStatus == WeaponState.frozen)
                    {
                        __result = false;
                        return false;
                    }
                }
            }
            return true;
        }

        //E Prevents the game from suddenly ending if everyone is loaded in a vehicle.
        public static void CanEndGame(Caravan caravan, ref bool __result)
        {
            if (!__result)
                __result = caravan.PawnsListForReading.Any(p => p.Faction == Faction.OfPlayer && (p.TryGetComp<CompVehicle>()?.AllOccupants.Any() ?? false));
        }

        //J Prevents characters from finding beds to "rescue" vehicles. 
        public static void DontRescueVehicles(ref Building_Bed __result, Pawn sleeper) => __result = (sleeper?.GetComp<CompVehicle>() is CompVehicle compVehicle) ? null : __result;

        //J Removes option to *rescue* vehicles.
        // RimWorld.FloatMenuMakerMap
        public static void DontRescueVehiclesInFloatMenus(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
        {
            IntVec3 c = IntVec3.FromVector3(clickPos);
            Pawn groundPawn = c.GetThingList(pawn.Map).FirstOrDefault(x => x is Pawn) as Pawn;
            if (groundPawn != null)
            {
                //Handler for things on the ground
                if (groundPawn?.GetComp<CompVehicle>() is CompVehicle compVehicle)
                {
                    //Remove "Equip" option from right click.
                    string toCheck = "Rescue".Translate(new object[]
                    {
                        groundPawn.LabelCap
                    });
                    var optToRemove = opts.FirstOrDefault((x) => x.Label.Contains(toCheck));
                    if (optToRemove != null) opts.Remove(optToRemove);
                }
            }
        }

        //E No vehicles in ancient dangers.
        // RimWorld.Building_CrashedShipPart
        public static void MechanoidsFixerAncient(ref bool __result, PawnKindDef kind)
        {
            //Log.Message("1");
            if (kind.race.HasComp(typeof(CompVehicle))) __result = false;
        }

        //E No vehicles in crashed ships.
        // RimWorld.Building_CrashedShipPart
        public static void MechanoidsFixer(ref bool __result, PawnKindDef def)
        {
            //Log.Message("1");
            if (def.race.HasComp(typeof(CompVehicle))) __result = false;
        }

        //E Stops verb exceptions.
        //Logic: Checks for NREs that can result from the original method's check for a StoryTracker.
        public static IEnumerable<CodeInstruction> CheckForAutoAttackTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo playerFactionInfo = AccessTools.Property(typeof(Faction), nameof(Faction.OfPlayer)).GetGetMethod();
            bool done = false;
            List<CodeInstruction> instructionList = instructions.ToList();
            for (int i = 0; i < instructionList.Count; i++)
            {
                CodeInstruction instruction = instructionList[i];

                if (!done && instruction.operand == playerFactionInfo)
                {
                    done = true;
                    yield return instruction;
                    yield return instructionList[i + 1];
                    yield return instructionList[i + 2];
                    yield return instructionList[i + 3];
                    yield return instructionList[i + 4];
                    instruction = new CodeInstruction(OpCodes.Brfalse_S, instructionList[i + 1].operand);
                    i++;
                }

                yield return instruction;
            }
        }
        public static IEnumerable<CodeInstruction> GetVerbsCommandsTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            FieldInfo storyInfo = AccessTools.Field(typeof(Pawn), nameof(Pawn.story));
            bool done = false;
            List<CodeInstruction> instructionList = instructions.ToList();
            for (int i = 0; i < instructionList.Count; i++)
            {
                CodeInstruction instruction = instructionList[i];

                if (!done && instruction.operand == storyInfo)
                {
                    yield return instruction;
                    yield return new CodeInstruction(OpCodes.Brfalse_S, instructionList[i + 3].operand);
                    yield return new CodeInstruction(instructionList[i - 3]);
                    yield return new CodeInstruction(instructionList[i - 2]);
                    yield return new CodeInstruction(instructionList[i - 1]);
                    instruction = new CodeInstruction(instruction);
                    done = true;
                }

                yield return instruction;
            }
        }

        //J Vehicles with a movement handler are considered colonists.
        //public class ThinkNode_ConditionalColonist : ThinkNode_Conditional
        public static void Satisfied_PostFix(Pawn pawn, ref bool __result) => __result = pawn.IsColonist || (pawn.GetComp<CompVehicle>() is CompVehicle compVehicle && compVehicle.MovementHandlerAvailable);

        #endregion ErrorHandlingMethods

        #region AIHandlingMethods

        //J Disable the WaitCombat job when our vehicle cannot fire.
        //public class JobGiver_Orders : ThinkNode_JobGiver
        public static void PreventWaitAttackError(ref Job __result, Pawn pawn)
        {
            if (__result != null && pawn?.GetComp<CompVehicle>() is CompVehicle compVehicle && pawn.Drafted && !compVehicle.CanFireWeapons)
            {
                __result = null;
            }
        }

        //J Handles vehicles in preparing caravans. If this code isn't executed, they will never load to leave.
        // RimWorld.LordToil_PrepareCaravan_GatherItems
        // RimWorld.LordToil_PrepareCaravan_GatherAnimals
        // RimWorld.LordToil_PrepareCaravan_GatherSlaves
        public static bool GiveVehiclesLoadItemDuties(LordToil_PrepareCaravan_GatherItems __instance)
        {
        //Log.Message("1");
        if (__instance.lord.ownedPawns.FindAll(x => x.GetComp<CompVehicle>() != null) is List<Pawn> pawns && !pawns.NullOrEmpty())
        {
            //Log.Message("2");

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn.IsColonist || pawn.GetComp<CompVehicle>() is CompVehicle comp && comp.CanMove)
                {

                    pawn.mindState.duty = new PawnDuty(DutyDefOf.PrepareCaravan_GatherItems);
                }
                else if (pawn.RaceProps.Animal)
                {
                    IntVec3 meetingPoint = Traverse.Create(__instance).Field("meetingPoint").GetValue<IntVec3>();
                    pawn.mindState.duty = new PawnDuty(DutyDefOf.PrepareCaravan_Wait, meetingPoint, -1f);
                }
                else
                {
                    pawn.mindState.duty = new PawnDuty(DutyDefOf.PrepareCaravan_Wait);
                }
                //Log.Error(pawns[i].mindState.duty.def.defName);
            }
            //Log.Error("f");
            return false;
        }
        //Log.Error("t");
        return true;
    }
        public static void GiveVehiclesLoadAnimalDuties(LordToil_PrepareCaravan_GatherAnimals __instance)
        {
            //Log.Message("Two1");
            if (__instance.lord.ownedPawns is List<Pawn> pawns && !pawns.NullOrEmpty() && pawns.FirstOrDefault(x => x.GetComp<CompVehicle>() != null) != null)
            {
                //Log.Message("Two2");

                for (int i = 0; i < __instance.lord.ownedPawns.Count; i++)
                {
                    Pawn pawn = __instance.lord.ownedPawns[i];
                    if (pawn.IsColonist || pawn.RaceProps.Animal || (pawn.GetComp<CompVehicle>() is CompVehicle compVehicle && compVehicle.MovementHandlerAvailable))
                    {
                        IntVec3 meetingPoint = Traverse.Create(__instance).Field("meetingPoint").GetValue<IntVec3>();

                        pawn.mindState.duty = new PawnDuty(DutyDefOf.PrepareCaravan_GatherPawns, meetingPoint, -1f)
                        {
                            pawnsToGather = PawnsToGather.Animals
                        };
                    }
                    else
                    {
                        pawn.mindState.duty = new PawnDuty(DutyDefOf.PrepareCaravan_Wait);
                    }
                }
            }
        }
        public static void GiveVehiclesLoadSlaveDuties(LordToil_PrepareCaravan_GatherSlaves __instance)
        {
            if (Find.TickManager.TicksGame % 100 == 0)
            {
                IntVec3 meetingPoint = Traverse.Create(__instance).Field("meetingPoint").GetValue<IntVec3>();
                GatherAnimalsAndSlavesForCaravanUtility.CheckArrived(__instance.lord, meetingPoint, "AllSlavesGathered", (Pawn x) => (!x.IsColonist && !(x.GetComp<CompVehicle>() is CompVehicle compVehicle && compVehicle.CanMove)) && !x.RaceProps.Animal, (Pawn x) => GatherAnimalsAndSlavesForCaravanUtility.IsFollowingAnyone(x));
            }
        }

        //S Vehicles Item Pickup Fix!
        //RimWorld.LordToil_PrepareCaravan_GatherItems
        //Needs a transpiler i think.... Can't transpile cause my VM is down so i can't see IL code
        private static bool firstRun = false;
        public static bool GiveVehiclesLoadItemToil(LordToil_PrepareCaravan_GatherItems __instance)
		{
			if (Find.TickManager.TicksGame % 120 == 0)
			{
				bool flag = true;
				for (int i = 0; i < __instance.lord.ownedPawns.Count; i++)
				{
					Pawn pawn = __instance.lord.ownedPawns[i];
                    if ((pawn.IsColonist || pawn.GetComp<CompVehicle>() != null) && pawn.mindState.lastJobTag != JobTag.WaitingForOthersToFinishGatheringItems)
					{
                        //Rewrote this, it seems to fix the colonist + vehicle setups
                        //As to why? Dunno haha
                        if (!firstRun){
                            flag = false;
                            firstRun = true;
                            break;
                        }else{
                            if (pawn.CurJob.def != JobDefOf.WaitWander){
                                flag = false;
                                break;
                            }
                        }

						//flag = false;

						//break;
					}
				}
				if (flag)
				{
                    //Log.Error("2nd Statement");
					List<Pawn> allPawnsSpawned = __instance.Map.mapPawns.AllPawnsSpawned;
					for (int j = 0; j < allPawnsSpawned.Count; j++)
					{
						if (allPawnsSpawned[j].CurJob != null && allPawnsSpawned[j].jobs.curDriver is JobDriver_PrepareCaravan_GatherItems && allPawnsSpawned[j].CurJob.lord == __instance.lord)
						{
							flag = false;
							break;
						}
					}
				}
				if (flag)
				{
					__instance.lord.ReceiveMemo("AllItemsGathered");
                    firstRun = false;
				}
			}
            return false;


		}

        //S Tries to find satisfy the vehicle's fuel "need"
        //Purpose: Try and find satisfy the vehicle's fuel "need"
        //Logic: If the vehicle is using fuel, it needs to refuel while on caravan trips
        //Improvements: Effects of different fuel sources on vehicle performance or effectiveness of fuel source?
        //RimWorld.Planet.CaravanPawnsNeedsUtility
        public static bool TrySatisfyFuelNeeds(Pawn pawn, Caravan caravan)
        {
            //If the pawn's dead, not a vehicle, or doesn't need fuel, it's a regular pawn and has needs
            CompRefuelable refuelable = pawn.GetComp<CompRefuelable>();
            CompVehicle vehicle = pawn.GetComp<CompVehicle>();
            if (pawn.Dead || refuelable == null || vehicle == null)
                return true;

            //It's a vehicle and it uses fuel and the amount of fuel is less than the auto refuel percent
            if (refuelable.FuelPercentOfMax < refuelable.Props.autoRefuelPercent)
            {
                int num = refuelable.GetFuelCountToFullyRefuel();
                //Call the private function TryGetFuel to find fuel for the vehicle
                if (TryGetFuel(caravan, pawn, refuelable, out Thing thing, out Pawn pawn2))
                {
                    //Fuel the Vehicle
                    refuelable.Refuel((float)num);

                    //Remove fuel from inventory
                    if (thing.stackCount < num)
                        num = thing.stackCount;
                    thing.SplitOff(num);
                    if (thing.Destroyed)
                    {
                        if (pawn2 != null)
                        {
                            pawn2.inventory.innerContainer.Remove(thing);
                        }
                    }
                }
            }

            //Vehicles don't have other needs, continuing the original method is a waste of time
            return false;

        }

        //J TB
        // RimWorld.JobGiver_PrepareCaravan_GatherItems
        public static bool TryGiveItemJob_PreFix(ref Job __result, Pawn pawn)
        {
            if (pawn?.TryGetComp<CompVehicle>() is CompVehicle v)
            {
                if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                {
                    __result = null;
                    return false;
                }
                Lord lord = pawn.GetLord();
                Thing thing = GatherItemsForCaravanUtility.FindThingToHaul(pawn, lord);
                if (thing == null)
                {
                    __result = null;
                    return false;
                }
                __result = new Job(JobDefOf.PrepareCaravan_GatherItems, thing)
                {
                    lord = lord
                };
                return false;
            }
            return true;
        }

        #endregion AIHandlingMethods

        #region CaravanHandlingMethods

        //J This code checks all the characters inside a vehicle.
        // It then adds all of the characters into a temporary list to track them.
        // Then it adds them all to the caravan, as well.
        // RimWorld.Planet.Caravan
        public static bool AddVehiclePawnsToCaravan(Caravan __instance, Pawn p, bool addCarriedPawnToWorldPawnsIfAny)
        {
            //Log.Message("AddVehicles0");

            if (p?.GetComp<CompVehicle>() is CompVehicle compVehicle)
            {
                //Log.Message("AddVehicles1");
                if (p == null || p.Dead)
                {
                    Log.Warning("Tried to add a null or dead pawn to " + __instance);
                    return false;
                }
                if (__instance.pawns.TryAdd(p, true))
                {
                    //Log.Message("AddVehicles2");

                    //All passengers in the vehicle go into the caravan.
                    if (compVehicle.handlers != null && compVehicle.handlers.Count > 0)
                    {
                        //Log.Message("AddVehicles3");

                        //Store vehicle handler group structure in comp variable
                        //compVehicle.PawnsInVehicle.Clear();
                        compVehicle.PawnsInVehicle = new List<VehicleHandlerTemp>();
                        foreach (VehicleHandlerGroup group in compVehicle.handlers)
                        {
                            //Log.Message("AddVehicles4");
                            compVehicle.PawnsInVehicle.Add(new VehicleHandlerTemp(group));
                            //Log.Message("AddVehicles5");

                            for (int i = 0; i < group.handlers.Count; i++)
                            {
                                //Log.Message("AddVehicles6a");

                                Pawn innerPawn = group.handlers[i];

                                //Remove the pawn from the vehicle and add it to the caravan
                                if (innerPawn.holdingOwner != null)
                                {
                                    //Log.Message("AddVehicles7a");

                                    innerPawn.holdingOwner.Remove(innerPawn);
                                    if (!__instance.pawns.TryAdd(innerPawn, true))
                                    {
                                        Log.Message("Failed to load caravan with vehicle pawn: " + innerPawn.Label);
                                    }
                                    //Log.Message("AddVehicles7b");

                                }
                                else
                                {
                                    //Log.Message("AddVehicles8a");

                                    if (!__instance.pawns.TryAdd(innerPawn, true))
                                    {
                                        Log.Message("Failed to load caravan with vehicle pawn: " + innerPawn.Label);
                                    }
                                    //Log.Message("AddVehicles8b");

                                }
                                //Log.Message("AddVehicles6b");

                                //group.handlers.Remove(pawn);
                            }
                        }
                    }


#pragma warning disable IDE0019 // Use pattern matching
                    Pawn pawn = p.carryTracker.CarriedThing as Pawn;
#pragma warning restore IDE0019 // Use pattern matching
                    if (pawn != null)
                    {
                        //Anything in-hand also goes into the caravan.
                        p.carryTracker.innerContainer.Remove(pawn);
                        __instance.AddPawn(pawn, addCarriedPawnToWorldPawnsIfAny);
                        if (addCarriedPawnToWorldPawnsIfAny)
                        {
                            Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Decide);
                        }


                    }
                }
                else
                {
                    Log.Error("Couldn't add pawn " + p + " to caravan.");
                }
                return false;
            }
            return true;
        }


        //J Prevents caravans from being split while having colonists that are in vehicles.
        //  This is a lazy fix for now. It may need to be revised later.
        // RimWorld.Planet.Dialog_SplitCaravan
        public static bool DontSplitCaravansWithVehicles(Dialog_SplitCaravan __instance, ref bool __result, List<Pawn> pawns)
        {
            Caravan thisCaravan = Traverse.Create(__instance).Field("caravan").GetValue<Caravan>();
            if (thisCaravan?.pawns?.InnerListForReading?.Any(x => x.GetComp<CompVehicle>() != null) ?? false)
            {
                bool result = __result;
                //Check the caravan's pawn list.
                if ((!thisCaravan?.PawnsListForReading?.NullOrEmpty() ?? false) && !pawns.NullOrEmpty())
                {
                    foreach (Pawn p in thisCaravan.PawnsListForReading)
                    {
                        if (p.GetComp<CompVehicle>() is CompVehicle compVehicle && 
                            compVehicle?.PawnsInVehicle is List<VehicleHandlerTemp> vehicleHandlers && !vehicleHandlers.NullOrEmpty())
                        {
                            //Check every vehicle's occupants.
                            foreach (VehicleHandlerTemp temp in vehicleHandlers)
                            {
                                foreach (Pawn o in pawns)
                                {
                                    if (temp?.handlers?.Contains(o) ?? false)
                                    {
                                        Messages.Message("CompVehicle_CannotSplitWhileInVehicles".Translate(), MessageTypeDefOf.RejectInput);
                                        __result = false;
                                        return false;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return true;
        }


        //J ??? Forces caravan checks to be accepted if a vehicle is loaded.
        // ??? MESSY CODE ???
        // ??? NEEDS A TRANSPILER ???
        // RimWorld.Dialog_FormCaravan
        // Modified vanilla method, because I'm terrible.
        public static bool CheckForErrors_PreFix(Dialog_FormCaravan __instance, List<Pawn> pawns, ref bool __result)
        {
            if (pawns.FindAll((x) => x.GetComp<CompVehicle>() != null) is List<Pawn> vehicles)
            {
                //Log.Message("TestA");
                bool localReform = Traverse.Create(__instance).Field("reform").GetValue<bool>();
                if (!localReform &&
                    Traverse.Create(__instance).Field("startingTile").GetValue<int>() < 0)
                {
                    Messages.Message("NoExitDirectionForCaravanChosen".Translate(), MessageTypeDefOf.RejectInput);
                    return false;
                }
                if (!pawns.Any((Pawn x) => CaravanUtility.IsOwner(x, Faction.OfPlayer) && !x.Downed))
                {
                    if (!pawns.Any((Pawn y) => y.TryGetComp<CompVehicle>() is CompVehicle v))
                    {
                        if (!pawns.Any((Pawn z) => z.TryGetComp<CompVehicle>() is CompVehicle v && v.CanMove))
                        {
                            Messages.Message("CaravanMustHaveAtLeastOneColonist".Translate(), MessageTypeDefOf.RejectInput);
                            return false;
                        }
                    }
                }
                if (!localReform && Traverse.Create(__instance).Property("MassUsage").GetValue<float>() > Traverse.Create(__instance).Property("MassCapacity").GetValue<float>())
                {
                    AccessTools.Method(typeof(Dialog_FormCaravan), "FlashMass").Invoke(__instance, null);
                    Messages.Message("TooBigCaravanMassUsage".Translate(), MessageTypeDefOf.RejectInput);
                    return false;
                }
                Pawn pawn = pawns.Find((Pawn x) =>
                (x.TryGetComp<CompVehicle>() == null || x.TryGetComp<CompVehicle>() is CompVehicle v && !v.CanMove) &&
                !pawns.Any((Pawn y) => !y.IsPrisoner && !y.RaceProps.Animal && y.CanReach(x, PathEndMode.Touch, Danger.Deadly, false, TraverseMode.ByPawn)));
                if (pawn != null)
                {
                    Messages.Message("CaravanPawnIsUnreachable".Translate(new object[]
                    {
            pawn.LabelShort
                    }).CapitalizeFirst(), pawn, MessageTypeDefOf.RejectInput);
                    return false;
                }
                for (int i = 0; i < __instance.transferables.Count; i++)
                {
                    if (__instance.transferables[i].ThingDef.category == ThingCategory.Item)
                    {
                        int countToTransfer = __instance.transferables[i].CountToTransfer;
                        int num = 0;
                        if (countToTransfer > 0)
                        {
                            for (int j = 0; j < __instance.transferables[i].things.Count; j++)
                            {
                                Thing t = __instance.transferables[i].things[j];
                                if (!t.Spawned || pawns.Any((Pawn x) => (x.IsColonist || x.TryGetComp<CompVehicle>() is CompVehicle v && v.CanMove) && x.CanReach(t, PathEndMode.Touch, Danger.Deadly, false, TraverseMode.ByPawn)))
                                {
                                    num += t.stackCount;
                                    if (num >= countToTransfer)
                                    {
                                        break;
                                    }
                                }
                            }
                            if (num < countToTransfer)
                            {
                                if (countToTransfer == 1)
                                {
                                    Messages.Message("CaravanItemIsUnreachableSingle".Translate(new object[]
                                    {
                            __instance.transferables[i].ThingDef.label
                                    }), MessageTypeDefOf.RejectInput);
                                }
                                else
                                {
                                    Messages.Message("CaravanItemIsUnreachableMulti".Translate(new object[]
                                    {
                            countToTransfer,
                            __instance.transferables[i].ThingDef.label
                                    }), MessageTypeDefOf.RejectInput);
                                }
                                //Log.Message("TestB");
                            }
                        }
                    }
                }
                __result = true;
                return false;
            }
            return true;
        }

        //J ??? Shows "Vehicles" in the caravan tab. Messy patch.
        // ??? MESSY CODE ???
        // ??? NEEDS A TRANSPILER ???
        // This is a lazy prefix to reroute the code to use our list if they have a vehicle in the list.
        // RimWorld.Planet.CaravanPeopleAndItemsTabUtility
        public static bool DoRows_PreFix(Vector2 size, ref List<Thing> things, Caravan caravan, ref Vector2 scrollPosition, ref float scrollViewHeight, bool alwaysShowItemsSection, ref Pawn specificNeedsTabForPawn, bool doNeeds = true)
        {
            if (things.Any(x => x is Pawn p && p.TryGetComp<CompVehicle>() != null))
            {
                if (specificNeedsTabForPawn != null && (!things.Contains(specificNeedsTabForPawn) || specificNeedsTabForPawn.Dead))
                {
                    specificNeedsTabForPawn = null;
                }
                var methodDoRow = (typeof(CaravanPeopleAndItemsTabUtility).GetMethods(BindingFlags.NonPublic | BindingFlags.Static).First(mi => mi.Name == "DoRow" && mi.GetParameters().Count() == 9));
                var methodAnyItemOrEmpty = AccessTools.Method(typeof(CaravanPeopleAndItemsTabUtility), "AnyItemOrEmpty");

                Text.Font = GameFont.Small;
                Rect rect = new Rect(0f, 0f, size.x, size.y).ContractedBy(10f);
                Rect viewRect = new Rect(0f, 0f, rect.width - 16f, scrollViewHeight);
                bool listingUsesAbandonSpecificCountButtons = (bool)methodAnyItemOrEmpty.Invoke(null, new object[] { things });
                Widgets.BeginScrollView(rect, ref scrollPosition, viewRect, true);
                float num = 0f;
                bool flag = false;
                for (int i = 0; i < things.Count; i++)
                {
#pragma warning disable IDE0019 // Use pattern matching
                    Pawn pawn = things[i] as Pawn;
#pragma warning restore IDE0019 // Use pattern matching
                    if (pawn != null && pawn.IsColonist)
                    {
                        if (!flag)
                        {
                            Widgets.ListSeparator(ref num, viewRect.width, "CaravanColonists".Translate());
                            flag = true;
                        }
                        var args = new object[] { num, viewRect, rect, scrollPosition, pawn, caravan, specificNeedsTabForPawn, doNeeds, listingUsesAbandonSpecificCountButtons };
                        methodDoRow.Invoke(null, args);
                        num = (float)args[0];
                        specificNeedsTabForPawn = (Pawn)args[6];
                    }
                }
                bool flagV = false;
                for (int k = 0; k < things.Count; k++)
                {
#pragma warning disable IDE0019 // Use pattern matching
                    Pawn pawnV = things[k] as Pawn;
#pragma warning restore IDE0019 // Use pattern matching
                    if (pawnV != null && pawnV.TryGetComp<CompVehicle>() != null)
                    {
                        if (!flagV)
                        {
                            Widgets.ListSeparator(ref num, viewRect.width, "CompVehicle_VehicleSection".Translate());
                            flagV = true;
                        }
                        var args = new object[] { num, viewRect, rect, scrollPosition, pawnV, caravan, specificNeedsTabForPawn, doNeeds, listingUsesAbandonSpecificCountButtons };
                        methodDoRow.Invoke(null, args);
                        num = (float)args[0];
                        specificNeedsTabForPawn = (Pawn)args[6];
                    }
                }

                bool flag2 = false;
                for (int j = 0; j < things.Count; j++)
                {
#pragma warning disable IDE0019 // Use pattern matching
                    Pawn pawn2 = things[j] as Pawn;
#pragma warning restore IDE0019 // Use pattern matching
                    if (pawn2 != null && !pawn2.IsColonist && pawn2.TryGetComp<CompVehicle>() == null)
                    {
                        if (!flag2)
                        {
                            Widgets.ListSeparator(ref num, viewRect.width, "CaravanPrisonersAndAnimals".Translate());
                            flag2 = true;
                        }
                        var args = new object[] { num, viewRect, rect, scrollPosition, pawn2, caravan, specificNeedsTabForPawn, doNeeds, listingUsesAbandonSpecificCountButtons };
                        methodDoRow.Invoke(null, args);
                        num = (float)args[0];
                        specificNeedsTabForPawn = (Pawn)args[6];
                    }
                }
                bool flag3 = false;
                if (alwaysShowItemsSection)
                {
                    Widgets.ListSeparator(ref num, viewRect.width, "CaravanItems".Translate());
                }
                for (int k = 0; k < things.Count; k++)
                {
                    if (!(things[k] is Pawn))
                    {
                        if (!flag3)
                        {
                            if (!alwaysShowItemsSection)
                            {
                                Widgets.ListSeparator(ref num, viewRect.width, "CaravanItems".Translate());
                            }
                            flag3 = true;
                        }
                        var args = new object[] { num, viewRect, rect, scrollPosition, things[k], caravan, specificNeedsTabForPawn, doNeeds, listingUsesAbandonSpecificCountButtons };
                        methodDoRow.Invoke(null, args);
                        num = (float)args[0];
                        specificNeedsTabForPawn = (Pawn)args[6];
                    }
                }
                if (alwaysShowItemsSection && !flag3)
                {
                    GUI.color = Color.gray;
                    Text.Anchor = TextAnchor.UpperCenter;
                    Widgets.Label(new Rect(0f, num, viewRect.width, 25f), "NoneBrackets".Translate());
                    Text.Anchor = TextAnchor.UpperLeft;
                    num += 25f;
                    GUI.color = Color.white;
                }
                if (Event.current.type == EventType.Layout)
                {
                    scrollViewHeight = num + 30f;
                }
                Widgets.EndScrollView();
                return false;
            }
            return true;

        }

        //J Adds a vehicles section to Caravan forming UI.
        // RimWorld.CaravanUIUtility
        public static void AddPawnsSections_PostFix(TransferableOneWayWidget widget, List<TransferableOneWay> transferables)
        {
            IEnumerable<TransferableOneWay> source = from x in transferables
                                                     where x.ThingDef.category == ThingCategory.Pawn
                                                     select x;
            widget.AddSection("CompVehicle_VehicleSection".Translate(), from x in source
                                                                        where ((Pawn)x.AnyThing).GetComp<CompVehicle>() != null &&
                                                                        ((Pawn)x.AnyThing).GetComp<CompVehicle>().MovementHandlerAvailable
                                                                        select x);
        }

        //J Movement handlers in vehicles are counted in caravan forming.
        // RimWorld.Planet.CaravanExitMapUtility
        public static void CanExit_PostFix(Pawn pawn, ref bool __result) => __result = pawn.Spawned && pawn.Map.exitMapGrid.MapUsesExitGrid && ((pawn.IsColonist || (pawn.GetComp<CompVehicle>() is CompVehicle compVehicle && compVehicle.CanMove)) || CaravanExitMapUtility.FindCaravanToJoinFor(pawn) != null);

        //J Changes the caravan capacity for the vehicle.
        // RimWorld.MassUtility
        public static void Capacity_PostFix(ref float __result, Pawn p)
        {
            if (p?.TryGetComp<CompVehicle>() is CompVehicle v)
            {
                __result = (__result != 0) ? (v?.Props?.cargoCapacity ?? p.BodySize * 35) : 0;
            }
        }

        /* S Modifies caravan movement speed if vehicles are present
        S The math on this is sound, I just don't know what the game is doing to turn the result into tiny values
        
        Purpose: Modifies caravan speed if vehicles are present
        Logic: If a vehicle is present than the caravan should move faster, if there are multiple vehicles it should be an average, 
        Improvements:Different algorithm for calculating world movement speed?
        Algorithm explanation:
         given 5 pawns with vehicle status denoted with V and their corresponding ticks per speed being: 
         100V, 150, 200V, 250, 300V
         The none modified ticks per move would be 190f * sigma(100,150,200,250,300)/5 aka the average (200) * 190f or 38,000
         Since 190f is a constant we can remove that number and our postfixed calculations will be modified from sigma(100,150,200,250,300)/5 aka 200

         Case 1: All vehicles are fueled and fueled vehicles travel twice as fast aka half the ticks needed or Speed Modifer of 2
         The original equation would now become sigma(50V,150,100V,250,150V)/5 or 140
         To prevent the recalculation of TicksPerMove we can rewrite that equation as
         sigma(100V,150,200V,250,300V)/5 - sigma(50V,0,100V,0,150V)/5 or 140
         which can be rewritten as __result/190 - sigma((SpeedModifier-1) * (originaTickSpeed)/SpeedModifier) for all pawns)/(number of pawns)
         I.e.__result/190 - ((2-1) * 100/2 + (1-1) * 150/1 + (2-1) * 200/2 + (1-1) * 250/1 + (2-1) * 300/2) or 140

        Case 2: Vehicle 1 is fueled(100V) but the other two vehicles aren't (200V and 300V)
         Fueled Vehicles travel twice as fast(speed modifier of 2) and nonfueled vehicles travel half as a fast(speed modifier of 0.5)
         The original equation would now become sigma(50V, 150, 400V, 250, 600V)/5 or 290
         As again to prevent the recalculation of TicksPerMove we can rewrite it as the following
         sigma(100V,150,200V,250,300V)/5 - sigma(50V,0,0V,0,0V)/5 + sigma(0V,0,200V,0,300V)/5 or 290
         this can be rewritten as __result/190 - (sigma function from case 1) + sigma((originalTickSpeed / speedModifier - originalTickSpeed for all pawns)/(number of pawns)
         or __result/190 - ((2-1) * 100/2 + 0 + 0 + 0 + 0)/5 + (0 + 0 + (200 / 0.5 - 200) + 0 + (300/0.5 - 300))/5 or 290


        The math on this is sound, the game is being weird though:
        Game function:

        public static int GetTicksPerMove(List<Pawn> pawns)
        {
            if (pawns.Any<Pawn>())
            {
                float num = 0f;
                for (int i = 0; i < pawns.Count; i++)
                {
                    int num2 = (!pawns[i].Downed) ? pawns[i].TicksPerMoveCardinal : 450;
                    num += (float)num2 / (float)pawns.Count;
                }
                num *= 190f;
                return Mathf.Max(Mathf.RoundToInt(num), 1);
            }
            return 2500;
        }

        Given the above ^^^ if there were two pawns who had the following TicksPerMoveCardinal value and were not downed
        Colonist: 18
        Wagon: 12
        the value returned should be 190(12/2 + 18/2) or 15*190 which is NOT equal to the value returned
        the error logging in the postfix(2.101948E-44). Even if it were, the Mathf.Max(Mathf.RoundToInt(num), 1);
        should have returned 1 as 1 > 2.101948E-44. Something is weird with the function, I can't catch what is happening,
        I believe that I'm not breaking any math/logic rules.

        Addendum by Jecrell
         I've modified this method to be based on another concept.
         No matter how much we "average" out vehicle speed, I believe it makes more sense, logically, to assume that the caravan
         will move at the speed of the slowest pawn.So therefore, we should check firstly to see if characters are outside
         the vehicles.If they are, do not apply vehicle bonus speed.If everyone is inside vehicles, then we should consider
         which vehicle is the slowest and then make caravan speed go at that rate.
        
        //RimWorld.Planet.CaravanTicksPerMoveUtility
        */
        public static void GetTicksPerMove_PostFix(List<Pawn> pawns, ref int __result)
        {
            //Only do this if a vehicle is present. Then make a list of vehicles.
            if (!pawns.NullOrEmpty() && pawns.FindAll(x => x?.GetComp<CompVehicle>() != null && !x.Dead && !x.Downed) is List<Pawn> vehicles && vehicles.Count > 0)
            {
                //Make a list of non-vehicle characters that are not inside vehicles.
                //This method is long and ugly, so bear with me...
                List<Pawn> pawnsOutsideVehicle = new List<Pawn>(pawns.FindAll(x => x?.GetComp<CompVehicle>() == null));
                if (pawnsOutsideVehicle != null && pawnsOutsideVehicle.Count > 0)
                {
                    if ((vehicles?.Count ?? 0) > 0)
                    {
                        foreach (Pawn vehicle in vehicles)
                        {
                            if ((vehicle?.GetComp<CompVehicle>().PawnsInVehicle?.Count ?? 0) > 0)
                            {
                                foreach (VehicleHandlerTemp group in vehicle?.GetComp<CompVehicle>().PawnsInVehicle)
                                {
                                    if ((group?.handlers?.Count ?? 0) > 0)
                                    {
                                        foreach (Pawn p in group.handlers)
                                        {
                                            if (pawnsOutsideVehicle.Count == 0) break;
                                            if (pawnsOutsideVehicle.Contains(p)) pawnsOutsideVehicle.Remove(p);
                                        }
                                    }
                                    if (pawnsOutsideVehicle.Count == 0) break;
                                }
                            }
                            if (pawnsOutsideVehicle.Count == 0) break;
                        }
                    }
                }

                //Are there any characters not inside vehicles?
                //If so, make no changes to default speeds.
                //This will be similar to vehicles slowly coasing alongside walking characters.
                if ((pawnsOutsideVehicle?.Count ?? 0) > 0) { return; }

                //Log.Message("2");
                var slowestLandSpeed = 999f;
                foreach (Pawn vehicle in vehicles)
                {
                    slowestLandSpeed = Math.Min(vehicle.GetComp<CompVehicle>().Props.worldSpeedFactor, slowestLandSpeed);
                }
                __result = (int)(__result / slowestLandSpeed);
            }

            //Previous code.
            //float speedModifier;
            //remove constant to make math easier, put it back later
            //__result = (int)(__result / 190f);
            //if (pawns.Any<Pawn>())
            //{
            //__result *= pawns.Count;
            //Log.Error(pawns.Count.ToString());
            //for (int i = 0; i < pawns.Count; i++)
            //{
            //Log.Error(pawns[i].def.defName);
            //Log.Error((pawns[i].TicksPerMoveCardinal.ToString()));
            //CompVehicle compVehicle = pawns[i].GetComp<CompVehicle>();
            //Movement magic only occurs if it's a vehicle
            //if (compVehicle != null)
            //{

            //if (pawns[i].GetComp<CompRefuelable>() != null && !pawns[i].GetComp<CompRefuelable>().HasFuel)
            //{
            //Vehicle has no fuel, add ticks
            //Log.Error(("no fuel"));
            //speedModifier = pawns[i].GetComp<CompVehicle>().Props.worldSpeedFactorNoFuel;
            //Log.Error(("result: " + __result.ToString()));
            //Log.Error(("TPMC: " + pawns[i].TicksPerMoveCardinal.ToString()));
            //Log.Error("smod: " + (speedModifier.ToString()));
            //Log.Error("math: " + ((pawns[i].TicksPerMoveCardinal / speedModifier) - pawns[i].TicksPerMoveCardinal).ToString());
            //__result += (int)(pawns[i].TicksPerMoveCardinal / speedModifier) - pawns[i].TicksPerMoveCardinal;
            //Log.Error(("result2: " + __result.ToString()));
            //}
            //else
            //{
            //Vehicle has fuel, subtract ticks
            ///Log.Error(("fuel"));
            //speedModifier = pawns[i].GetComp<CompVehicle>().Props.worldSpeedFactor;
            //Log.Error(("result: " + __result.ToString()));
            //Log.Error(("TPMC: " + pawns[i].TicksPerMoveCardinal.ToString()));
            //Log.Error("smod: " + (speedModifier.ToString()));
            //Log.Error("math: " + (((speedModifier - 1) * pawns[i].TicksPerMoveCardinal / speedModifier)));

            //__result -= (int)((speedModifier - 1) * pawns[i].TicksPerMoveCardinal / speedModifier);
            //Log.Error(("result2: " + __result.ToString()));
            //}

            //}
            //}
            //__result /= pawns.Count;
            //}
            //multiply by 190f (the constant)
            //Log.Error(("b" + __result.ToString()));
            //__result *= 190;
        }

        /*S Remove pawns from the vehicle when making a caravan
        Purpose: Remove pawns from the vehicle when making a caravan
        Logic: Pawns should be displayable while in the caravan, this allows needs to be calculated by the game instead of through ResolveNeeds()
        Improvements: Make pawn cards appear while in a vehicle but not in the caravan? 
        Modify vehicle Needs card while not in caravan to display needs of pawns inside it?


        Addendum by Jecrell:
          Added WorldPawn considerations.
        */
        //RimWorld.Planet.CaravanMaker
        //public static void MakeCaravan_PostFix(IEnumerable<Pawn> pawns, bool addToWorldPawnsIfNotAlready, Caravan __result)
        //{
        //    if (pawns != null && pawns.Count() > 0 && pawns.Any(x => x.GetComp<CompVehicle>() != null))
        //    {
        //        foreach (Pawn vpawn in pawns)
        //        {
        //            if (vpawn.GetComp<CompVehicle>() is CompVehicle vehicle)
        //            {
        //                if (!vehicle.handlers.NullOrEmpty())
        //                {
        //                    //Store vehicle handler group structure in comp variable
        //                    vehicle.PawnsInVehicle = new List<VehicleHandlerTemp>();
        //                    foreach (VehicleHandlerGroup group in vehicle.handlers)
        //                    {
        //                        vehicle.PawnsInVehicle.Add(new VehicleHandlerTemp(group));
        //                        for (int i = 0; i < group.handlers.Count; i++)
        //                        {
        //                            Pawn pawn = group.handlers[i];

        //                            //Remove the pawn from the vehicle and add it to the caravan
        //                            if (!__result.ContainsPawn(pawn))
        //                            {
        //                                if (pawn.holdingOwner != null) pawn.holdingOwner = null;
        //                                __result.AddPawn(pawn, addToWorldPawnsIfNotAlready);
        //                                if (addToWorldPawnsIfNotAlready && !pawn.IsWorldPawn())
        //                                {
        //                                    if (pawn.Spawned)
        //                                    {
        //                                        pawn.DeSpawn();
        //                                    }
        //                                    if (!pawn.IsWorldPawn()) Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Decide);
        //                                }
        //                            }
        //                            group.handlers.Remove(pawn);
        //                        }
        //                    }
        //                }

        //            }

        //        }

        //    }

        //}

        //S Remove pawns from the vehicle when exiting the map in an already formed caravan
        //Purpose: Remove pawns from the vehicle when exiting the map in a already formed caravan
        //Logic: Pawns should be displayable while in the caravan, this vehicles to remove pawns and remember them for already made caravans
        //Improvements: Combine this with the previous patch method?

        //RimWorld.Planet.CaravanExitMapUtility

        //Bad code, needs transpiler cause i nuked vanilla code again
        public static bool ExitMapAndJoinOrCreateCaravan_PreFix(Pawn pawn)
        {
            if (pawn.GetComp<CompVehicle>() is CompVehicle compVehicle)
            {
                //Vanilla code start
                //Log.Message("EMJOC_1");
                Caravan caravan = CaravanExitMapUtility.FindCaravanToJoinFor(pawn);
                //Log.Message("EMJOC_2");

                if (caravan != null)
                {
                    //Log.Message("EMJOC_3a");

                    pawn.DeSpawn();
                    //Log.Message("EMJOC_3b");

                    caravan.AddPawn(pawn, true);
                    //Log.Message("EMJOC_3c");

                    pawn.ExitMap(false);
                    //Log.Message("EMJOC_3d");

                }
                //Vanilla code edit transpiler here for if statement
                //Log.Message("EMJOC_4");

                List<int> list = CaravanExitMapUtility.AvailableExitTilesAt(pawn.Map);
                //Log.Message("EMJOC_4a");

                Caravan caravan2 = CaravanExitMapUtility.ExitMapAndCreateCaravan(Gen.YieldSingle<Pawn>(pawn), pawn.Faction, pawn.Map.Tile, (!list.Any<int>()) ? pawn.Map.Tile : list.RandomElement<int>());
                //Log.Message("EMJOC_4b");

                caravan2.autoJoinable = true;
                //Log.Message("EMJOC_4c");

                if (pawn.Faction == Faction.OfPlayer)
                {
                    //Log.Message("EMJOC_4d");

                    Messages.Message("MessagePawnLeftMapAndCreatedCaravan".Translate(new object[]
                    {
                    pawn.LabelShort
                    }).CapitalizeFirst(), caravan2, MessageTypeDefOf.PositiveEvent);
                    //Log.Message("EMJOC_4e");

                }
                //Log.Message("EMJOC_4f");
                    
                //Vanilla Code end

                //Apparently commenting out this code fixed the errors from JobDriver when
                //leaving an invasion map
                //caravan = CaravanExitMapUtility.FindCaravanToJoinFor(pawn);
                //if (!compVehicle.AllOccupants.NullOrEmpty())
                //{
                //    if (compVehicle.PawnsInVehicle == null)
                //    {
                //        compVehicle.PawnsInVehicle = new List<VehicleHandlerTemp>();
                //        foreach (VehicleHandlerGroup group in compVehicle.handlers)
                //        {
                //            //Log.Message("Adding group to " + compVehicle.Pawn.ToString());
                //            compVehicle.PawnsInVehicle.Add(new VehicleHandlerTemp(group));
                //        }
                //    }
                //}
                //if (vehicle.handlers != null && vehicle.handlers.Count > 0)
                //{
                //    foreach (VehicleHandlerGroup group in vehicle.handlers)
                //    {
                //        for (int i = 0; i < group.handlers.Count; i++)
                //        {
                //            Pawn tpawn = group.handlers[i];
                //            //Store vehicle handler group structure in comp variable
                //            if (vehicle.AllOccupants.Count > 0)
                //            {
                //                //Add pawns to the comp variable for usage on reentering the map
                //                foreach (VehicleHandlerGroup vgroup in vehicle.PawnsInVehicle)
                //                {
                //                    if (vgroup.role == group.role)
                //                    {
                //                        vgroup.handlers.Add(tpawn);
                //                    }
                //                }
                //            }
                //            //Remove the pawn from the vehicle and add it to the caravan
                //            caravan.AddPawn(tpawn, true);
                //            group.handlers.Remove(tpawn);
                //        }
                //    }
                //}
                Log.Message("EMJOC_6");

                return false;
            }
            return true;
        }

        //S Add pawns back to the vehicle and remove them from the caravan when entering the map
        //S Converted to prefix which should fix map enter cloning (Cloning did occur previously but clones were removed via resolve ejection)
        //Purpose: Put pawns back into vehicles on entering the map
        //Logic: Pawns should remember which vehicle they were in when entering the map
        //Improvements: Have this function for incidents? Or leave pawns outside since incidents "catch the player (pawns by extension) off guard"
        //RimWorld.Planet.CaravanExitMapUtility
        //Converted to Prefix as the pawns weren't being removed since they were all being spawned before the postfix ran
        public static void Enter_PreFix(Caravan caravan)
        {
            List<Pawn> members = caravan.PawnsListForReading;
            //Log.Error(members.Count().ToString());
            //Log.Error();
            for (int i = 0; i < members.Count; i++)
            {
                CompVehicle vehicle = members[i].GetComp<CompVehicle>();
                //Did the vehicle have pawns in it previously?
                if (vehicle != null && vehicle.PawnsInVehicle != null && vehicle.PawnsInVehicle.Count > 0)
                {
                    for (int j = 0; j < members.Count; j++)
                    {
                        for (int l = 0; l < vehicle.PawnsInVehicle.Count; l++)
                        {
                            VehicleHandlerTemp group = vehicle.PawnsInVehicle[l];
                            for (int k = 0; k < group.handlers.Count; k++)
                            {
                                //Is the pawn still in the caravan?
                                Pawn pawn = group.handlers[k];
                                if (pawn == members[j])
                                {
                                    //Old Addition and Removal code wasn't working, led to additional groups being created
                                    foreach (VehicleHandlerGroup vgroup in vehicle.handlers)
                                    {
                                        if (vgroup.role.label == group.role.label)
                                        {
                                            vgroup.handlers.Add(pawn);
                                            caravan.RemovePawn(pawn);
                                            //Log.Message("RemovedPawn " + pawn.LabelShort);
                                            //Log.Message(caravan.PawnsListForReading.ToString());
                                            //if (pawn.IsWorldPawn()) Find.WorldPawns.RemovePawn(pawn);
                                            break;
                                        }
                                    }
                                }
                            }

                        }
                    }
                    //Clear the comp variable to allow an empty one to be created when forming a caravan/exiting map
                    vehicle.PawnsInVehicle = null;
                }
            }
            //Log.Error(members.Count().ToString());
        }

        //S Modifies the caravan inspect string so fuel is shown
        //Purpose: Modifies the caravan inspect string so fuel is shown 
        //Logic: Players should be able to see how much fuel their caravan is carrying
        //Improvements: None I can think of
        //7/3/17 Jec- Bugfix for end of line errors.
        //RimWorld.Planet.Caravan
        public static void GetInspectString_PostFix(Caravan __instance, ref string __result)
        {
            //Grab the original String
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(__result);

            //Check if Caravan is out of fuel
            if (AnythingOutOfFuel(__instance))
            {
                //Display No Fuel
                stringBuilder.AppendLine("CaravanOutOfFuel".Translate());
            }
            else
            {
                //Display how much fuel the caravan has left, over 1000 is infinite (game mechanic) 
                if (AnythingNeedsFuel(__instance, out List<Pawn> needfuel))
                {
                    //Call function that calculates the amount of fuel left
                    float daysLeftOfFuel = ApproxDaysWorthOfFuel(needfuel, __instance.Goods);
                    if (daysLeftOfFuel < 1000f)
                    {
                        stringBuilder.AppendLine("DaysWorthOfFuelInfo".Translate(new object[]
                        {
                    daysLeftOfFuel.ToString("0.#")
                        }));
                    }
                    else
                    {
                        stringBuilder.AppendLine("InfiniteDaysWorthOfFuelInfo".Translate());
                    }
                }
                else
                    stringBuilder.AppendLine("InfiniteDaysWorthOfFuelInfo".Translate());
            }
            __result = stringBuilder.ToString().TrimEndNewlines();
        }

        //S Modifies the Caravan Needs WITab to show vehicle fuel
        //Purpose: Modifies the Caravan Needs WITab to show vehicle fuel
        //Logic: Players should be able to see how much fuel each vehicle has
        public static IEnumerable<CodeInstruction> DoRow_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
        {
            MethodInfo downedInfo = AccessTools.Property(typeof(Pawn), nameof(Pawn.Downed)).GetGetMethod();
            MethodInfo getCompInfo = AccessTools.Method(typeof(ThingWithComps), nameof(Pawn.GetComp)).MakeGenericMethod(typeof(CompRefuelable));
            MethodInfo thisMethodInfo = AccessTools.Method(typeof(CaravanPeopleAndItemsTabUtility), "DoRow", new Type[] { typeof(Rect), typeof(Thing), typeof(Caravan), typeof(Pawn).MakeByRefType(), typeof(bool), typeof(bool) });


            List<CodeInstruction> instructionList = instructions.ToList();

            for (int i = 0; i < instructionList.Count; i++)
            {
                CodeInstruction instruction = instructionList[i];

                if (instruction.operand == downedInfo)
                {
                    yield return new CodeInstruction(OpCodes.Callvirt, getCompInfo);
                    yield return new CodeInstruction(OpCodes.Ldnull);
                    yield return new CodeInstruction(OpCodes.Cgt_Un);
                    Label endLabel = ilg.DefineLabel();
                    yield return new CodeInstruction(OpCodes.Brfalse_S, endLabel);
                    yield return new CodeInstruction(OpCodes.Nop);
                    yield return new CodeInstruction(instructionList[i - 1]) { labels = new List<Label>() };
                    yield return new CodeInstruction(OpCodes.Callvirt, getCompInfo);
                    yield return new CodeInstruction(OpCodes.Ldc_R4, 135f);
                    yield return new CodeInstruction(OpCodes.Ldc_R4, 0.0f);
                    yield return new CodeInstruction(OpCodes.Ldc_R4, 100f);
                    yield return new CodeInstruction(OpCodes.Ldc_R4, 50f);
                    yield return new CodeInstruction(OpCodes.Newobj, AccessTools.Constructor(typeof(Rect), new Type[] { typeof(float), typeof(float), typeof(float), typeof(float) }));
                    yield return new CodeInstruction(OpCodes.Ldc_I4_1);
                    yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                    yield return new CodeInstruction(OpCodes.Ldc_R4, 10f);
                    yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(HarmonyCompVehicle), nameof(DrawOnGUI)));
                    yield return new CodeInstruction(OpCodes.Nop);
                    yield return new CodeInstruction(OpCodes.Nop);
                    yield return new CodeInstruction(instructionList[i - 1]) { labels = new List<Label>() { endLabel } };
                }
                yield return instruction;
            }
        }

        //S Modifies the Caravan Contents Window when forming a caravan to show the fuel carried by the caravan
        //  ---------- BAD CODE...... but...... it works---------
        //Purpose: Modifies the Caravan Contents Window when forming a caravan to show the fuel carried by the caravan
        //Modifies the food count to include pawns still inside a vehicle
        //Logic: Players should be able to see how much fuel the caravan is carrying when forming a caravan
        //Improvements: Transpiler? Brrainz said that copying the code is bad
        //Don't know how to do a transpiler and didn't have time to try
        //Variable needed to keep track of the tab last the player was last at for window refresh
        //__instance version of this was not working
        static object tab;
        public static bool DoWindowContents_PreFix(Rect inRect, Dialog_FormCaravan __instance)
        {
            Traverse traverseobj = Traverse.Create(__instance);
            List<TransferableOneWay> transferables = traverseobj.Field("transferables").GetValue<List<TransferableOneWay>>();
            var vehicleTransferrable = transferables?.FirstOrDefault(x => x.HasAnyThing && x.AnyThing is Pawn p && p.GetComp<CompVehicle>() is CompVehicle vehicle);
            if (vehicleTransferrable != null)
            {
                //Create a traverse object and grab private variables from the instance
                bool reform = traverseobj.Field("reform").GetValue<bool>();
                List<TabRecord> tabsList = traverseobj.Field("tabsList").GetValue<List<TabRecord>>();
                float MassUsage = traverseobj.Property("MassUsage").GetValue<float>();
                float MassCapacity = traverseobj.Property("MassCapacity").GetValue<float>();
                float lastMassFlashTime = traverseobj.Field("lastMassFlashTime").GetValue<float>();
                Map map = traverseobj.Field("map").GetValue<Map>();
                if (tab == null)
                    tab = traverseobj.Field("tab").GetValue();

                bool EnvironmentAllowsEatingVirtualPlantsNow = traverseobj.Property("EnvironmentAllowsEatingVirtualPlantsNow").GetValue<bool>();
                TransferableOneWayWidget pawnsTransfer = traverseobj.Field("pawnsTransfer").GetValue<TransferableOneWayWidget>();
                TransferableOneWayWidget itemsTransfer = traverseobj.Field("itemsTransfer").GetValue<TransferableOneWayWidget>();


                List<ThingCount> tmpThingCounts = new List<ThingCount>();
                List<Pawn> list = new List<Pawn>();
                for (int i = 0; i < transferables.Count; i++)
                {
                    TransferableOneWay transferableOneWay = transferables[i];
                    if (transferableOneWay.HasAnyThing)
                    {
                        //If it's a pawn
                        if (transferableOneWay.AnyThing is Pawn)
                        {
                            for (int l = 0; l < transferableOneWay.CountToTransfer; l++)
                            {
                                Pawn pawn = (Pawn)transferableOneWay.things[l];
                                //Look at the contents of the vehicle and if it has any pawns in it, add it to the list
                                if (pawn.GetComp<CompVehicle>() != null && pawn.GetComp<CompVehicle>().AllOccupants != null)
                                {
                                    for (int j = 0; j < pawn.GetComp<CompVehicle>().AllOccupants.Count; j++)
                                    {
                                        list.Add(pawn.GetComp<CompVehicle>().AllOccupants[j]);
                                    }
                                }
                            }
                        }
                        else
                        {
                            //It's not a pawn so it's an item
                            tmpThingCounts.Add(new ThingCount(transferableOneWay.ThingDef, transferableOneWay.CountToTransfer));
                        }
                    }
                }

                //Calculate days worth of food using the list with pawns in vehicles
                Pair<float, float> DaysWorthOfFood = new Pair<float, float>((float)AccessTools.Method(typeof(DaysWorthOfFoodCalculator), "ApproxDaysWorthOfFood", new Type[] { typeof(List<Pawn>), typeof(List<ThingCount>), typeof(bool), typeof(IgnorePawnsInventoryMode) }).Invoke(__instance, new object[] { list, tmpThingCounts, EnvironmentAllowsEatingVirtualPlantsNow, IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload }), DaysUntilRotCalculator.ApproxDaysUntilRot(transferables, map.Tile, IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload));

                //Calculate the days worth of fuel
                float DaysWorthOfFuel = ApproxDaysWorthOfFuel(transferables, IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload);
                Rect rect = new Rect(0f, 0f, inRect.width, 40f);
                Text.Font = GameFont.Medium;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(rect, ((!reform) ? "FormCaravan" : "ReformCaravan").Translate());
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.UpperLeft;
                tabsList.Clear();
                //Tabs: get the current tab
                tabsList.Add(new TabRecord("PawnsTab".Translate(), delegate
                {
                    Traverse.Create(tab).Field("value__").SetValue(0);//Since Tab.Pawns == 0
                }, tab.ToString() == "Pawns"));
                tabsList.Add(new TabRecord("ItemsTab".Translate(), delegate
                {
                    Traverse.Create(tab).Field("value__").SetValue(1);//Since Tab.Items == 1
                }, tab.ToString() == "Items"));
                if (!reform)
                {
                    tabsList.Add(new TabRecord("CaravanConfigTab".Translate(), delegate
                    {
                        Traverse.Create(tab).Field("value__").SetValue(2);//Since Tab.Pawns == 3
                    }, tab.ToString() == "Config"));
                }
                inRect.yMin += 72f;
                Widgets.DrawMenuSection(inRect);
                TabDrawer.DrawTabs(inRect, tabsList);
                inRect = inRect.ContractedBy(17f);
                GUI.BeginGroup(inRect);
                Rect rect2 = inRect.AtZero();
                //Show the info stuff if it's not the config tab
                if (tab.ToString() != "Config")
                {
                    Rect rect3 = rect2;
                    rect3.xMin += rect2.width - 515f;
                    rect3.y += 32f;
                    TransferableUIUtility.DrawMassInfo(rect3, MassUsage, MassCapacity, "CaravanMassUsageTooltip".Translate(), lastMassFlashTime, true);
                    CaravanUIUtility.DrawDaysWorthOfFoodInfo(new Rect(rect3.x, rect3.y + 19f, rect3.width, rect3.height), DaysWorthOfFood.First, DaysWorthOfFood.Second, EnvironmentAllowsEatingVirtualPlantsNow, true, 3.40282347E+38f);
                    //Draw fuel info
                    DrawDaysWorthOfFuelInfo(new Rect(rect3.x, rect3.y + 38f, rect3.width, rect3.height), DaysWorthOfFuel, true, 3.40282347E+38f);
                }
                DoBottomButtons(rect2, __instance, DaysWorthOfFood, traverseobj, reform, transferables, DaysWorthOfFuel, StuffHasNoFuel(transferables, IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload));
                Rect inRect2 = rect2;
                inRect2.yMax -= 59f;
                bool flag = false;
                switch (tab.ToString())
                {
                    case "Pawns":
                        pawnsTransfer.OnGUI(inRect2, out flag);
                        break;
                    case "Items":
                        itemsTransfer.OnGUI(inRect2, out flag);
                        break;
                    case "Config":
                        //There was an issue calling the private method DrawConfig, I forget why
                        AccessTools.Method(typeof(Dialog_FormCaravan), "DrawConfig").Invoke(__instance, new object[] { rect2 });
                        break;
                }
                if (flag)
                {
                    AccessTools.Method(typeof(Dialog_FormCaravan), "CountToTransferChanged").Invoke(__instance, new object[] { });
                    transferables = traverseobj.Field("transferables").GetValue<List<TransferableOneWay>>();
                }
                GUI.EndGroup();
                return false;
            }
            return true;
        }

        #endregion CaravanHandlingMethods

        #region TestMethods

        // RimWorld.Planet.CaravanMaker
        public static bool MakeCaravan_PreFix(ref Caravan __result, IEnumerable<Pawn> pawns, Faction faction, int startingTile, bool addToWorldPawnsIfNotAlready)
        {
            Log.Message("MC1");
            if (startingTile < 0 && addToWorldPawnsIfNotAlready)
            {
                Log.Warning("Tried to create a caravan but chose not to spawn a caravan but pass pawns to world. This can cause bugs because pawns can be discarded.");
            }

            Log.Message("MC2");

            List<Pawn> tmpPawns = (List<Pawn>)AccessTools.Field(typeof(CaravanMaker), "tmpPawns").GetValue(null);

            tmpPawns.Clear();
            tmpPawns.AddRange(pawns);
            Log.Message("MC3");

            Caravan caravan = (Caravan)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Caravan);
            Log.Message("MC4");

            if (startingTile >= 0)
            {
                Log.Message("MC5a");

                caravan.Tile = startingTile;
                Log.Message("MC5b");

            }
            Log.Message("MC6");

            caravan.SetFaction(faction);
            Log.Message("MC7");

            caravan.Name = CaravanNameGenerator.GenerateCaravanName(caravan);
            Log.Message("MC8");

            if (startingTile >= 0)
            {
                Log.Message("MC9a");

                Find.WorldObjects.Add(caravan);
                Log.Message("MC9b");

            }
            for (int i = 0; i < tmpPawns.Count; i++)
            {
                Log.Message("MC10a");

                Pawn pawn = tmpPawns[i];
                if (pawn.Spawned)
                {
                    Log.Message("MC11a");

                    pawn.DeSpawn();
                    Log.Message("MC11b");

                }
                if (pawn.Dead)
                {

                    Log.Warning("Tried to form a caravan with a dead pawn " + pawn);
                }
                else
                {
                    Log.Message("MC12a");

                    caravan.AddPawn(pawn, addToWorldPawnsIfNotAlready);
                    Log.Message("MC12b");

                    if (addToWorldPawnsIfNotAlready && !pawn.IsWorldPawn())
                    {
                        Log.Message("MC13a");

                        if (pawn.Spawned)
                        {
                            pawn.DeSpawn();
                        }
                        Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Decide);
                        Log.Message("MC13b");

                    }
                    Log.Message("MC12c");

                }
            }
            __result = caravan;
            return false;
        }
        
        // RimWorld.Planet.CaravanExitMapUtility
        public static bool ExitMapAndCreateCaravan_Test(ref Caravan __result, IEnumerable<Pawn> pawns, Faction faction, int startingTile)
        {
            Log.Message("EMACC1");
            if (!GenWorldClosest.TryFindClosestPassableTile(startingTile, out startingTile))
            {
                Log.Error("Could not find any passable tile for a new caravan.");
                __result = null;
                return false;
            }
            Log.Message("EMACC2");

            List<Pawn> tmpPawns = (List<Pawn>)AccessTools.Field(typeof(CaravanExitMapUtility), "tmpPawns").GetValue(null);
            Log.Message("EMACC3");

            tmpPawns.Clear();
            tmpPawns.AddRange(pawns);
            Log.Message("EMACC4");

            Map map = null;
            for (int i = 0; i < tmpPawns.Count; i++)
            {
                map = tmpPawns[i].MapHeld;
                if (map != null)
                {
                    break;
                }
            }
            Log.Message("EMACC5");

            Caravan caravan = CaravanMaker.MakeCaravan(tmpPawns, faction, startingTile, false);
            Log.Message("EMACC6");

            for (int j = 0; j < tmpPawns.Count; j++)
            {
                Log.Message("EMACC7");

                tmpPawns[j].ExitMap(false);
                Log.Message("EMACC7b");

            }
            Log.Message("EMACC8");

            List<Pawn> pawnsListForReading = caravan.PawnsListForReading;
            Log.Message("EMACC9");

            for (int k = 0; k < pawnsListForReading.Count; k++)
            {
                Log.Message("EMACC10a");

                if (!pawnsListForReading[k].IsWorldPawn())
                {
                    Log.Message("EMACC10b");

                    Find.WorldPawns.PassToWorld(pawnsListForReading[k], PawnDiscardDecideMode.Decide);
                    Log.Message("EMACC10c");

                }
            }
            if (map != null)
            {
                Log.Message("EMACC11");

                map.info.parent.Notify_CaravanFormed(caravan);
                Log.Message("EMACC11b");

            }
            Log.Message("EMACC12");

            __result = caravan;
            return false;
        }

        // Verse.Pawn
        public static bool ExitMap_Test(Pawn __instance, bool allowedToJoinOrCreateCaravan)
        {
            if (__instance?.GetComp<CompVehicle>() is CompVehicle compVehicle)
            {
                Log.Message("ExitMap1");
                if (__instance.IsWorldPawn())
                {
                    Log.Warning("Called ExitMap() on world pawn " + __instance);
                    return false;
                }
                if (allowedToJoinOrCreateCaravan && CaravanExitMapUtility.CanExitMapAndJoinOrCreateCaravanNow(__instance))
                {
                    Log.Message("ExitMap2");

                    CaravanExitMapUtility.ExitMapAndJoinOrCreateCaravan(__instance);
                    Log.Message("ExitMap2b");

                    return false;
                }
                Lord lord = __instance.GetLord();
                Log.Message("ExitMap3");

                if (lord != null)
                {
                    Log.Message("ExitMap3a");

                    lord.Notify_PawnLost(__instance, PawnLostCondition.ExitedMap);
                    Log.Message("ExitMap3b");

                }
                if (__instance.carryTracker != null && __instance.carryTracker.CarriedThing != null)
                {
                    Log.Message("ExitMap4");

                    var carriedThing = __instance?.carryTracker?.CarriedThing;
                    Log.Message("ExitMap4a");

                    if (carriedThing is Pawn pawn)
                    {
                        Log.Message("ExitMap4b");

                        if (__instance.Faction != null && __instance.Faction != pawn.Faction)
                        {
                            Log.Message("ExitMap4c");

                            __instance.Faction.kidnapped.KidnapPawn(pawn, __instance);
                            Log.Message("ExitMap4d");

                        }
                        else
                        {
                            Log.Message("ExitMap4e");

                            __instance.carryTracker.innerContainer.Remove(pawn);
                            Log.Message("ExitMap4f");

                            pawn.ExitMap(false);
                            Log.Message("ExitMap4g");

                        }
                    }
                    else
                    {
                        Log.Message("ExitMap4h");

                        __instance.carryTracker.CarriedThing.Destroy(DestroyMode.Vanish);
                    }
                    Log.Message("ExitMap4i");

                    __instance.carryTracker.innerContainer.Clear();
                }
                Log.Message("ExitMap5");

                bool flag = !__instance.IsCaravanMember() && !PawnUtility.IsTravelingInTransportPodWorldObject(__instance);
                Log.Message("ExitMap5a");

                if (flag && __instance.HostFaction != null && __instance.guest != null && (__instance.guest.Released || !__instance.IsPrisoner) && !__instance.InMentalState && __instance.health.hediffSet.BleedRateTotal < 0.001f && __instance.Faction.def.appreciative && !__instance.Faction.def.hidden)
                {
                    Log.Message("ExitMap6");

                    float num = 15f;
                    if (PawnUtility.IsFactionLeader(__instance))
                    {
                        num += 50f;
                    }
                    Messages.Message("MessagePawnExitMapRelationsGain".Translate(new object[]
                    {
                        __instance.LabelShort,
                        __instance.Faction.Name,
                        num.ToString("F0")
                    }), MessageTypeDefOf.PositiveEvent);
                    __instance.Faction.AffectGoodwillWith(__instance.HostFaction, num);
                }
                if (__instance.ownership != null)
                {
                    Log.Message("ExitMap7");

                    __instance.ownership.UnclaimAll();
                    Log.Message("ExitMap7b");

                }
                if (__instance.guest != null && flag)
                {
                    Log.Message("ExitMap8a");

                    __instance.guest.SetGuestStatus(null, false);
                    Log.Message("ExitMap8b");

                }
                if (__instance.Spawned)
                {
                    Log.Message("ExitMap9a");

                    __instance.DeSpawn();
                    Log.Message("ExitMap9b");

                }
                __instance.inventory.UnloadEverything = false;
                Log.Message("ExitMap10");

                __instance.ClearMind(false);
                Log.Message("ExitMap11");

                __instance.ClearAllReservations(true);
                Log.Message("ExitMap12");

                Find.WorldPawns.PassToWorld(__instance, PawnDiscardDecideMode.Decide);
                Log.Message("ExitMap13");

                return false;
            }
            return true;

        }
        #endregion TestMethods

        // RimWorld.RestUtility
        //public static bool DrawMedOperationsTab(Rect leftRect, Pawn pawn, Thing thingForMedBills, float curY, ref float __result)
        //{
        //    curY += 2f;
        //    Func<List<FloatMenuOption>> recipeOptionsMaker = delegate
        //    {
        //        List<FloatMenuOption> list = new List<FloatMenuOption>();
        //        foreach (RecipeDef current in thingForMedBills.def.AllRecipes)
        //        {
        //            if (current.AvailableNow)
        //            {
        //                IEnumerable<ThingDef> enumerable = current.PotentiallyMissingIngredients(null, thingForMedBills.Map);
        //                if (!enumerable.Any((ThingDef x) => x.isBodyPartOrImplant))
        //                {
        //                    if (!enumerable.Any((ThingDef x) => x.IsDrug))
        //                    {
        //                        if (current.targetsBodyPart)
        //                        {
        //                            foreach (BodyPartRecord current2 in current.Worker.GetPartsToApplyOn(pawn, current))
        //                            {
        //                                list.Add((FloatMenuOption)AccessTools.Method(typeof(HealthCardUtility), "GenerateSurgeryOption").Invoke(null, new object[] { pawn, thingForMedBills, current, enumerable, current2 }));
        //                            }
        //                        }
        //                        else
        //                        {
        //                            list.Add((FloatMenuOption)AccessTools.Method(typeof(HealthCardUtility), "GenerateSurgeryOption").Invoke(null, new object[] { pawn, thingForMedBills, current, enumerable, null }));
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //        return list;
        //    };
        //    Rect rect = new Rect(leftRect.x - 9f, curY, leftRect.width, leftRect.height - curY - 20f);
        //    ((IBillGiver)thingForMedBills).BillStack.DoListing(rect, recipeOptionsMaker, ref HealthCardUtility.billsScrollPosition, ref HealthCardUtility.billsScrollHeight);
        //    return curY;
        //}

        #region SwenzisPrivateMethods
        // ------- Additions Made By Swenzi --------
        // -------    Private Methods       --------

        //Purpose: Find fuel for the vehicle 
        //Corresponding Patch Class: RimWorld.Planet.CaravanPawnsNeedsUtility
        //Corresponding Patch Method: TrySatisfyPawnNeeds_PreFix
        //Improvements: None I can think of
        private static bool TryGetFuel(Caravan caravan, Pawn forPawn, CompRefuelable refuelable, out Thing fuel, out Pawn owner)
		{
            //Find fuel for the vehicle by looking through all items
			List<Thing> list = CaravanInventoryUtility.AllInventoryItems(caravan);
			
            //Get acceptable fuel items
            ThingFilter filter = refuelable.Props.fuelFilter;
			for (int i = 0; i < list.Count; i++)
			{
                //If the thing found is an acceptable fuel source
				Thing thing2 = list[i];
				if (filter.Allows(thing2))
				{
					fuel = thing2;
					owner = CaravanInventoryUtility.GetOwnerOf(caravan, thing2);
                    //Reset the spammer preventer since the vehicle now has fuel
                    forPawn.GetComp<CompVehicle>().WarnedOnNoFuel = false;
					return true;
				}
			}
			fuel = null;
			owner = null;
            //Couldn't find a fuel source, check if vehicle is out of fuel
			if (!forPawn.GetComp<CompRefuelable>().HasFuel)
			{
                //Spam preventer Boolean Check
				if (forPawn.GetComp<CompVehicle>().WarnedOnNoFuel == false)
				{
                    //Notify player that caravan is out of fuel
					Messages.Message("MessageCaravanRunOutOfFuel".Translate(new object[] { caravan.LabelCap, forPawn.Label }), caravan, MessageTypeDefOf.ThreatBig);
					//No more spam
                    forPawn.GetComp<CompVehicle>().WarnedOnNoFuel = true;
				}
			}
			return false;
		}

		//Purpose: Check if anything in the caravan is out of fuel
		//Corresponding Patch Class: RimWorld.Planet.CaravanPawnsNeedsUtility
		//Corresponding Patch Method: TrySatisfyPawnNeeds_PreFix
		private static bool AnythingOutOfFuel(Caravan caravan)
		{
            if (AnythingNeedsFuel(caravan, out List<Pawn> needfuel))
            {
                if (needfuel != null)
                {
                    for (int i = 0; i < needfuel.Count; i++)
                    {
                        if (!needfuel[i].GetComp<CompRefuelable>().HasFuel)
                        {
                            return true;
                        }
                    }
                }
            }
			return false;
		}

		//Purpose: Check if anything in the caravan needs fuel and adds to a list
		//Corresponding Patch Class: RimWorld.Planet.CaravanPawnsNeedsUtility
		//Corresponding Patch Method: TrySatisfyPawnNeeds_PreFix
		private static bool AnythingNeedsFuel(Caravan caravan, out List<Pawn> needfuel)
        {
            List<Pawn> pawns = caravan.PawnsListForReading;
            needfuel = new List<Pawn>();
            if (pawns != null)
            {
                for (int i = 0; i < pawns.Count; i++)
                {
                    if (pawns[i].GetComp<CompRefuelable>() != null)
                    {
                        needfuel.Add((pawns[i]));
                    }
                }
            }
            if (needfuel.Count > 0)
                return true;
            else
                return false;
        }

		//Purpose: Calculate the amount of fuel left in the caravan
		//Corresponding Patch Class: RimWorld.Planet.Caravan
        //Corresponding Patch Method: GetInspectString_PostFix
        //Algorithm Explanation:
        //Assemble a list of ThingDefs that are acceptable fuel sources
        //Assemble the total fuel usage of all things in the caravan
        //Iterate through the caravan's items to find acceptable fuel sources
        //Grab the stack count of acceptable fuel items
        //Divide stack count by fuel usage
        //Improvements: Better ratios instead of 1 to 1 for fuel source refuel?
		private static float ApproxDaysWorthOfFuel(List<Pawn> pawns,IEnumerable<Thing> goods){
            int supplies = 0;
            float totalFuelUse = 0;
            List<ThingDef> allowed = new List<ThingDef>();
            for (int i = 0; i < pawns.Count; i++)
            {
                CompRefuelable refuel = pawns[i].GetComp<CompRefuelable>();
                foreach (ThingDef thing in refuel.Props.fuelFilter.AllowedThingDefs){
                    if (!allowed.Contains(thing))
                        allowed.Add(thing);
                }
                totalFuelUse += pawns[i].GetComp<CompRefuelable>().Props.fuelConsumptionRate/60000;
 
            }
            foreach (Thing item in goods)
            {
                if (allowed.Contains(item.def))
                    supplies += item.stackCount;
            }

			if (Math.Abs(totalFuelUse) > double.Epsilon)
				return (supplies / (GenDate.TicksPerDay * totalFuelUse));
			else
				return 10000;
        }

		//Purpose: Draw the Vehicle fuel bar in the WITab for Needs
		//Corresponding Patch Class: RimWorld.Planet.CaravanPeopleAndItemsTabUtility
		//Corresponding Patch Method: DoRow_PreFix
		private static void DrawOnGUI(CompRefuelable fuel_comp, Rect rect, bool doTooltip, int maxThresholdMarkers = 2147483647, float customMargin = -1f, bool drawArrows = true)
		{
            //Code is modified from the DrawOnGui method for Needs
			float CurLevelPercentage = fuel_comp.FuelPercentOfMax;
			if (rect.height > 70f)
			{
				float num = (rect.height - 70f) / 2f;
				rect.height = 70f;
				rect.y += num;
			}
			if (Mouse.IsOver(rect))
			{
				Widgets.DrawHighlight(rect);
			}
			if (doTooltip)
			{
                //Draw the tooltip on mouse over
				TooltipHandler.TipRegion(rect, new TipSignal(() => GetTipString(fuel_comp), rect.GetHashCode()));
			}
			float num2 = 14f;
			float num3 = (customMargin < 0f) ? (num2 + 15f) : customMargin;
			if (rect.height < 50f)
			{
				num2 *= Mathf.InverseLerp(0f, 50f, rect.height);
			}
			Text.Font = ((rect.height <= 55f) ? GameFont.Tiny : GameFont.Small);
			Text.Anchor = TextAnchor.LowerLeft;
			Rect rect2 = new Rect(rect.x + num3 + rect.width * 0.1f, rect.y, rect.width - num3 - rect.width * 0.1f, rect.height / 2f);
			Widgets.Label(rect2, "Fuel");
			Text.Anchor = TextAnchor.UpperLeft;
			Rect rect3 = new Rect(rect.x, rect.y + rect.height / 2f, rect.width, rect.height / 2f);
			rect3 = new Rect(rect3.x + num3, rect3.y, rect3.width - num3 * 2f, rect3.height - num2);
			//Fill the rectangle up to the current level of fuel
            Widgets.FillableBar(rect3, CurLevelPercentage);
			if (drawArrows)
			{
				Widgets.FillableBarChangeArrows(rect3, 0);
			}
			float curInstantLevelPercentage = CurLevelPercentage;
			if (curInstantLevelPercentage >= 0f)
			{
                //Draw the Marker 
				DrawBarInstantMarkerAt(rect3, curInstantLevelPercentage);
			}
			Text.Font = GameFont.Small;
		}

        //Purpose: Get the Tip String explaining what fuel does
        //Corresponding Patch Class: RimWorld.Planet.CaravanPeopleAndItemsTabUtility
        //Corresponding Patch Method: DoRow_PreFix
        private static string GetTipString(CompRefuelable refuel) => string.Concat(new string[]
            {
                "Fuel: ",
                refuel.FuelPercentOfMax.ToStringPercent(),
                "\n",
                "Fuel is necessary for vehicles and other machines to operate."
            });

        //Purpose: Draw the bar marker
        //Corresponding Patch Class: RimWorld.Planet.CaravanPeopleAndItemsTabUtility
        //Corresponding Patch Method: DoRow_PreFix
        //Improvements: Find a way to use the bar marker method from Needs?
        private static void DrawBarInstantMarkerAt(Rect barRect, float pct)
		{
			float num = 12f;
			if (barRect.width < 150f)
			{
				num /= 2f;
			}
			Vector2 vector = new Vector2(barRect.x + barRect.width * pct, barRect.y + barRect.height);
			Rect position = new Rect(vector.x - num / 2f, vector.y, num, num);
			GUI.DrawTexture(position, ContentFinder<Texture2D>.Get("UI/Misc/BarInstantMarker", true));
		}

		//Purpose: Calculate the amount of fuel left in the caravan
		//Corresponding Patch Class: RimWorld.Dialog_FormCaravan
		//Corresponding Patch Method: DoWindowContents_PreFix
		//Algorithm Explanation:
		//Assemble a list of ThingDefs that are acceptable fuel sources
		//Assemble the total fuel usage of all things in the caravan
		//Iterate through the caravan's transferable items to find acceptable fuel sources
		//Grab the stack count of acceptable fuel items
		//Divide stack count by fuel usage
		//Improvements: Better ratios instead of 1 to 1 for fuel source refuel?
		private static float ApproxDaysWorthOfFuel(List<TransferableOneWay> transferables, IgnorePawnsInventoryMode ignoreInventory)
		{
			List<Pawn> needsfuel = new List<Pawn>();
			float FuelCounts = 0;
			float fueluse = 0;
			for (int i = 0; i < transferables.Count; i++)
			{
				TransferableOneWay transferableOneWay = transferables[i];
				if (transferableOneWay.HasAnyThing)
				{
					bool included = false;
					if (transferableOneWay.AnyThing is Pawn)
					{
						for (int l = 0; l < transferableOneWay.CountToTransfer; l++)
						{
							Pawn pawn = (Pawn)transferableOneWay.things[l];
							if (pawn.GetComp<CompRefuelable>() != null)
							{
								needsfuel.Add(pawn);
                                fueluse += pawn.GetComp<CompRefuelable>().Props.fuelConsumptionRate;
							}
						}
					}
					else
					{
						for (int j = 0; j < needsfuel.Count; j++)
						{
							IEnumerable<ThingDef> allowedfuel = needsfuel[j].GetComp<CompRefuelable>().Props.fuelFilter.AllowedThingDefs;
							foreach (ThingDef fueldef in allowedfuel)
							{
								if (transferableOneWay.AnyThing.def.defName == fueldef.defName && !included)
								{
									included = true;
									FuelCounts += transferableOneWay.AnyThing.stackCount;
									break;
								}
							}
						}
					}
				}

			}
			if (Math.Abs(fueluse) > double.Epsilon)
				return (FuelCounts / (GenDate.TicksPerDay * fueluse));
			else
				return 10000;
		}

		//Purpose: Draw the information for days worth of fuel
		//Corresponding Patch Class: RimWorld.Dialog_FormCaravan
		//Corresponding Patch Method: DoWindowContents_PreFix
		private static void DrawDaysWorthOfFuelInfo(Rect rect, float daysWorthOfFuel, bool alignRight = false, float truncToWidth = 3.40282347E+38f)
		{
			GUI.color = Color.gray;
			string text;
            //Text if infinite
			if (daysWorthOfFuel >= 1000f)
			{
				text = "InfiniteDaysWorthOfFuelInfo".Translate();
			}
			else
			{
                //Text otherwise
				text = "DaysWorthOfFuelInfo".Translate(new object[]
				{
					daysWorthOfFuel.ToString("0.#")
				});
			}
			string text2 = text;
			if (truncToWidth != 3.40282347E+38f)
			{
				text2 = text.Truncate(truncToWidth, null);
			}
			Vector2 vector = Text.CalcSize(text2);
			Rect rect2;
			if (alignRight)
			{
				rect2 = new Rect(rect.xMax - vector.x, rect.y, vector.x, vector.y);
			}
			else
			{
				rect2 = new Rect(rect.x, rect.y, vector.x, vector.y);
			}
			Widgets.Label(rect2, text2);
			string text3 = string.Empty;
			if (truncToWidth != 3.40282347E+38f && Text.CalcSize(text).x > truncToWidth)
			{
				text3 = text3 + text + "\n\n";
			}
            //Tool tip explaining what fuel is
			text3 = text3 + "DaysWorthOfFuelTooltip".Translate() + "\n\n";
			TooltipHandler.TipRegion(rect2, text3);
			GUI.color = Color.white;
		}

		//Purpose: Draw the Config Tab, copied from the original method
		//Corresponding Patch Class: RimWorld.Dialog_FormCaravan
		//Corresponding Patch Method: DoWindow_PreFix
		private static void DrawConfig(Rect rect, Traverse traverseobj)
		{
			Vector2 ExitDirectionRadioSize = traverseobj.Field("ExitDirectionRadioSize").GetValue<Vector2>();
			Map map = traverseobj.Field("map").GetValue<Map>();
			int CurrentTile = traverseobj.Property("CurrentTile").GetValue<int>();
			int startingTile = traverseobj.Field("startingTile").GetValue<int>();
			Rect rect2 = new Rect(0f, rect.y, rect.width, 30f);
			Text.Font = GameFont.Medium;
			Widgets.Label(rect2, "ExitDirection".Translate());
			Text.Font = GameFont.Small;
			List<int> list = CaravanExitMapUtility.AvailableExitTilesAt(map);
			if (list.Any<int>())
			{
				for (int i = 0; i < list.Count; i++)
				{
					Direction8Way direction8WayFromTo = Find.WorldGrid.GetDirection8WayFromTo(CurrentTile, list[i]);
					float y = rect.y + (float)i * ExitDirectionRadioSize.y + 30f + 4f;
					Rect rect3 = new Rect(rect.x, y, ExitDirectionRadioSize.x, ExitDirectionRadioSize.y);
					Vector2 vector = Find.WorldGrid.LongLatOf(list[i]);
					string labelText = "ExitDirectionRadioButtonLabel".Translate(new object[]
					{
						direction8WayFromTo.LabelShort(),
						vector.y.ToStringLatitude(),
						vector.x.ToStringLongitude()
					});
					if (Widgets.RadioButtonLabeled(rect3, labelText, startingTile == list[i]))
					{
						startingTile = list[i];
					}
				}
			}
			else
			{
				GUI.color = Color.gray;
				Widgets.Label(new Rect(rect.x, rect.y + 30f + 4f, rect.width, 100f), "NoCaravanExitDirectionAvailable".Translate());
				GUI.color = Color.white;
			}
		}

		//Purpose: Draw the BottomButtons aka what happens after accept/cancel is pressed
		//Corresponding Patch Class: RimWorld.Dialog_FormCaravan
		//Corresponding Patch Method: DoWindowContents_PreFix
		//Code was modified from the original method and Fuel stuff was inserted
		//Improvements: Seperate Patch via transpiler
		private static void DoBottomButtons(Rect rect, Dialog_FormCaravan instance, Pair<float, float> DaysWorthOfFood, Traverse traverseobj, bool reform, List<TransferableOneWay> transferables, float DaysWorthOfFuel, bool StuffHasNoFuel)
		{

		    //traverse object was passed along, grab more private variables
			Vector2 BottomButtonSize = traverseobj.Field("BottomButtonSize").GetValue<Vector2>();
			Rect rect2 = new Rect(rect.width / 2f - BottomButtonSize.x / 2f, rect.height - 55f, BottomButtonSize.x, BottomButtonSize.y);
			bool MostFoodWillRotSoon = traverseobj.Property("MostFoodWillRotSoon").GetValue<bool>();
			bool showEstTimeToDestinationButton = traverseobj.Field("showEstTimeToDestinationButton").GetValue<bool>();

            //If they pressed Accept
			if (Widgets.ButtonText(rect2, "AcceptButton".Translate(), true, false, true))
			{
				if (reform)
				{

					if ((bool)traverseobj.Method("TryReformCaravan").GetValue(new object[] { }))
					{
						SoundDefOf.TickHigh.PlayOneShotOnCamera(null);
						instance.Close(false);
						tab = traverseobj.Field("tab").GetValue();
					}
				}
				else
				{
					string text = null;
					Pair<float, float> daysWorthOfFood = DaysWorthOfFood;
					if (daysWorthOfFood.First < 5f)
					{
						text = ((daysWorthOfFood.First >= 0.1f) ? "DaysWorthOfFoodWarningDialog".Translate(new object[]
						{
							daysWorthOfFood.First.ToString("0.#")
						}) : "DaysWorthOfFoodWarningDialog_NoFood".Translate());
					}
					else if (MostFoodWillRotSoon)
					{
						text = "CaravanFoodWillRotSoonWarningDialog".Translate();
					}
					else if (DaysWorthOfFuel < 5f)
					{
                        //Warn if there's less than 5 days of fuel
						text = ((DaysWorthOfFuel >= 0.1f) ? "DaysWorthOfFuelWarningDialog".Translate(new object[]
{
							DaysWorthOfFuel.ToString("0.#")
}) : "DaysWorthOfFuelWarningDialog_NoFuel".Translate());
					}

					if (!text.NullOrEmpty())
					{
						if ((bool)AccessTools.Method(typeof(Dialog_FormCaravan), "CheckForErrors").Invoke(instance, new object[] { TransferableUtility.GetPawnsFromTransferables(transferables) }))
						{
							if (StuffHasNoFuel)
							{
								Messages.Message("CaravanVehicleNoFuelWarningDialog".Translate(), MessageTypeDefOf.RejectInput);
							}
							else
							{
								Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(text, delegate
								{
									if ((bool)AccessTools.Method(typeof(Dialog_FormCaravan), "TryFormAndSendCaravan").Invoke(instance, new object[] { }))
									{
                                        instance.Close(false);
										tab = traverseobj.Field("tab").GetValue();
									}
								}, false, null));
							}
						}

					}
					else if ((bool)AccessTools.Method(typeof(Dialog_FormCaravan), "TryFormAndSendCaravan").Invoke(instance, new object[] { }))
					{
						SoundDefOf.TickHigh.PlayOneShotOnCamera(null);
						instance.Close(false);
						tab = traverseobj.Field("tab").GetValue();
					}
				}

			}
			Rect rect3 = new Rect(rect2.x - 10f - BottomButtonSize.x, rect2.y, BottomButtonSize.x, BottomButtonSize.y);
			if (Widgets.ButtonText(rect3, "ResetButton".Translate(), true, false, true))
			{
				SoundDefOf.TickLow.PlayOneShotOnCamera(null);
				AccessTools.Method(typeof(Dialog_FormCaravan), "CalculateAndRecacheTransferables").Invoke(instance, new object[] { });
			}
			Rect rect4 = new Rect(rect2.xMax + 10f, rect2.y, BottomButtonSize.x, BottomButtonSize.y);
			if (Widgets.ButtonText(rect4, "CancelButton".Translate(), true, false, true))
			{
				instance.Close(true);
				tab = traverseobj.Field("tab").GetValue();
			}
			if (showEstTimeToDestinationButton)
			{
				Rect rect5 = new Rect(rect.width - BottomButtonSize.x, rect2.y, BottomButtonSize.x, BottomButtonSize.y);
				if (Widgets.ButtonText(rect5, "EstimatedTimeToDestinationButton".Translate(), true, false, true))
				{
					List<Pawn> pawnsFromTransferables = TransferableUtility.GetPawnsFromTransferables(transferables);
					if (!pawnsFromTransferables.Any((Pawn x) => (CaravanUtility.IsOwner(x, Faction.OfPlayer) && !x.Downed) || (x.GetComp<CompVehicle>() is CompVehicle)))
					{
						Messages.Message("CaravanMustHaveAtLeastOneColonist".Translate(), MessageTypeDefOf.RejectInput);
					}
					else
					{
						Find.WorldRoutePlanner.Start(instance);
					}
				}
			}
			if (Prefs.DevMode)
			{
				float width = 200f;
				float num = BottomButtonSize.y / 2f;
				Rect rect6 = new Rect(0f, rect.height - 55f, width, num);
				if (Widgets.ButtonText(rect6, "Dev: Send instantly", true, false, true) && (bool)AccessTools.Method(typeof(Dialog_FormCaravan), "DebugTryFormCaravanInstantly").Invoke(instance, new object[] { }))
				{
					SoundDefOf.TickHigh.PlayOneShotOnCamera(null);
					instance.Close(false);
					tab = traverseobj.Field("tab").GetValue();
				}
				Rect rect7 = new Rect(0f, rect.height - 55f + num, width, num);
				if (Widgets.ButtonText(rect7, "Dev: Select everything", true, false, true))
				{
					SoundDefOf.TickHigh.PlayOneShotOnCamera(null);
					AccessTools.Method(typeof(Dialog_FormCaravan), "SetToSendEverything").Invoke(instance, new object[] { });
				}
			}
		}

		//Purpose: Return a bool on whether vehicles in the caravan don't have fuel in them
		//Corresponding Patch Class: RimWorld.Dialog_FormCaravan
		//Corresponding Patch Method: DoWindowContents_PreFix
		private static bool StuffHasNoFuel(List<TransferableOneWay> transferables, IgnorePawnsInventoryMode ignoreInventory)
		{
			List<Pawn> needsfuel = new List<Pawn>();
			for (int i = 0; i < transferables.Count; i++)
			{
				TransferableOneWay transferableOneWay = transferables[i];
				if (transferableOneWay.HasAnyThing)
				{
					if (transferableOneWay.AnyThing is Pawn)
					{
						for (int l = 0; l < transferableOneWay.CountToTransfer; l++)
						{
                            //Get a list of pawns that need fuel
							Pawn pawn = (Pawn)transferableOneWay.things[l];
							if (pawn.GetComp<CompRefuelable>() != null)
							{
								needsfuel.Add(pawn);
							}
						}
					}
				}
			}
			for (int i = 0; i < needsfuel.Count; i++)
			{
                //If it needs fuel and doesn't have fuel the caravan shouldn't form since the pawn can't move
				if (!needsfuel[i].GetComp<CompRefuelable>().HasFuel)
					return true;
			}
			return false;

		}
        #endregion SwenzisPrivateMethods
    }
}
