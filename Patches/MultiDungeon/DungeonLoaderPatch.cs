using DunGen;
using DunGen.Graph;
using HarmonyLib;
using LethalLevelLoader;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace NotezyLib.MultiDungeon.Patches;

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
            ShrinkedFlow shrinkedFlow = DungeonFlowShrinker.ShrinkFlow(dungeonGenerator.DungeonFlow,
            MultiDungeonGenerator.Instance.FlowDevision, MultiDungeonGenerator.Instance.MinimumMinFlowLength,
            MultiDungeonGenerator.Instance.MinimumMaxFlowLength);
            dungeonGenerator.DungeonFlow = shrinkedFlow.Flow;

            foreach (DungeonFlow.GlobalPropSettings globalPropSettings in dungeonGenerator.DungeonFlow.GlobalProps)
            {
                if (globalPropSettings.ID == 1231)
                {
                    globalPropSettings.Count = new IntRange(0, 0);
                    break;
                }
            }
            MultiDungeonGenerator.Instance.ShrinkedFlows.Add(shrinkedFlow);
        }
        catch (System.Exception ex)
        {
            NotezyLib.LogError($"Error in DungeonLoader.Generate Postfix: {ex}");
        }
    }
}

