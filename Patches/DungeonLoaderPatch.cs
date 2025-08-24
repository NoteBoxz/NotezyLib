using DunGen;
using DunGen.Graph;
using HarmonyLib;
using LethalLevelLoader;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace NotezyLib.MultiDungeon.Patches
{
    [HarmonyPatch(typeof(DungeonLoader))]
    public class DungeonLoaderPatch
    {
        [HarmonyPatch(nameof(DungeonLoader.PrepareDungeon))]
        [HarmonyPrefix]
        public static bool PrepareDungeonPrefix()
        {
            try
            {
                if (MultiDungeonGenerator.Instance == null)
                {
                    return true;
                }
                if (MultiDungeonGenerator.Instance.isGeneratingDungeons)
                {
                    NotezyLib.LogDebug("!!!SKIPPING DungeonLoader.PrepareDungeon DURING EXTRA DUNGEN GENERATION!!!");
                    return false;
                }
                return true;
            }
            catch (System.Exception ex)
            {
                NotezyLib.LogError($"Error in DungeonLoader.Generate Prefix: {ex}");
                return true;
            }
        }
        [HarmonyPatch(nameof(DungeonLoader.PrepareDungeon))]
        [HarmonyPostfix]
        public static void PrepareDungeonostfix()
        {
            try
            {
                if (MultiDungeonGenerator.Instance == null)
                {
                    return;
                }
                if (MultiDungeonGenerator.Instance.isGeneratingDungeons)
                {
                    return;
                }

                NotezyLib.LogMessage($"!!!OVERIDING DUNGEN SIZE!!!");
                DungeonGenerator dungeonGenerator = LethalLevelLoader.Patches.RoundManager.dungeonGenerator.Generator;
                DungeonFlow OGflow = dungeonGenerator.DungeonFlow;
                dungeonGenerator.DungeonFlow = Object.Instantiate(dungeonGenerator.DungeonFlow);
                int DivMin = Mathf.RoundToInt(dungeonGenerator.DungeonFlow.Length.Min / MultiDungeonGenerator.Instance.FlowDevision);
                int DivMax = Mathf.RoundToInt(dungeonGenerator.DungeonFlow.Length.Max / MultiDungeonGenerator.Instance.FlowDevision);
                dungeonGenerator.DungeonFlow.Length.Min = dungeonGenerator.DungeonFlow.Length.Min < MultiDungeonGenerator.Instance.MinimumMinFlowLength ?
                 dungeonGenerator.DungeonFlow.Length.Min : DivMin;
                dungeonGenerator.DungeonFlow.Length.Max = dungeonGenerator.DungeonFlow.Length.Max < MultiDungeonGenerator.Instance.MinimumMaxFlowLength ?
                 dungeonGenerator.DungeonFlow.Length.Max : DivMax;
                NotezyLib.LogInfo($"Original Flow Length: Min {OGflow.Length.Min}, Max {OGflow.Length.Max}");
                NotezyLib.LogInfo($"New Flow Length: Min {dungeonGenerator.DungeonFlow.Length.Min}, Max {dungeonGenerator.DungeonFlow.Length.Max}");
                foreach (DungeonFlow.GlobalPropSettings globalPropSettings in dungeonGenerator.DungeonFlow.GlobalProps)
                {
                    if (globalPropSettings.ID == 1231)
                    {
                        globalPropSettings.Count = new IntRange(0, 0);
                        break;
                    }
                }
                MultiDungeonGenerator.Instance.FlowsToRemove.Add(dungeonGenerator, (OGflow, dungeonGenerator.DungeonFlow));
            }
            catch (System.Exception ex)
            {
                NotezyLib.LogError($"Error in DungeonLoader.Generate Postfix: {ex}");
            }
        }
    }
}
