using DunGen;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

namespace NotezyLib.MultiDungeon.Patches;

[HarmonyPatch(typeof(RoundManager))]
public class RoundManagerPatch
{
    [HarmonyPatch(nameof(RoundManager.FinishGeneratingLevel))]
    [HarmonyPrefix]
    public static bool FinishGeneratingLevelPrefix(RoundManager __instance)
    {
        if (MultiDungeonGenerator.Instance == null)
        {
            return true; // Proceed with original method if MultiDungeonGenerator is not present
        }
        if (MultiDungeonGenerator.Instance.GeneratedExtraDungeons)
        {
            return true; // Proceed with original method if extra dungeons have already been generated
        }

        NotezyLib.LogMessage("!!!OVERTAKING RoundManager.FinishGeneratingLevel!!!");
        MultiDungeonGenerator.Instance.GenerateExtraDugens();

        return false; // Skip original method
    }

    [HarmonyPatch(nameof(RoundManager.SetExitIDs))]
    [HarmonyPriority(Priority.First)]
    [HarmonyPostfix]
    private static void SetBuildings(Vector3 mainEntrancePosition)
    {
        if (MultiDungeonGenerator.Instance == null)
        {
            return;
        }
        List<Dungeon> AllDungeons = GameObject.FindObjectsOfType<Dungeon>().ToList();
        MultiDungeonGenerator.Instance.LinkedDungeons = new Dictionary<int, Dungeon>();
        EntranceTeleport[] entrances = GameObject.FindObjectsOfType<EntranceTeleport>();

        List<EntranceTeleport> IndoorEntrances = entrances.Where(e => !e.isEntranceToBuilding).ToList();
        List<EntranceTeleport> SortedIndoorEntrances = IndoorEntrances.OrderBy(e => Vector3.Distance(e.transform.position, mainEntrancePosition)).ToList();
        for (int i = 0; i < SortedIndoorEntrances.Count; i++)
        {
            SortedIndoorEntrances[i].entranceId = i;
            Dungeon ClosetDungeon = null!;
            float ClosestDist = float.MaxValue;
            foreach (Dungeon dun in AllDungeons)
            {
                float dist = Vector3.Distance(SortedIndoorEntrances[i].transform.position, dun.transform.position);
                if (dist < ClosestDist)
                {
                    ClosestDist = dist;
                    ClosetDungeon = dun;
                }
            }
            if (ClosetDungeon != null)
            {
                MultiDungeonGenerator.Instance.LinkedDungeons[SortedIndoorEntrances[i].entranceId] = ClosetDungeon;
                AllDungeons.Remove(ClosetDungeon);
            }
            else
            {
                NotezyLib.LogWarning($"Could not find a dungeon for indoor entrance at position {SortedIndoorEntrances[i].transform.position}");
            }
            NotezyLib.LogInfo($"Setting ExitID {SortedIndoorEntrances[i].entranceId} for entrance at position {SortedIndoorEntrances[i].transform.position}");
        }
        MultiDungeonGenerator.Instance.SortedIndoorEntrances = SortedIndoorEntrances;

        List<EntranceTeleport> OutdoorEntrances = entrances.Where(e => e.isEntranceToBuilding).ToList();
        List<EntranceTeleport> SortedOutdoorEntrances = OutdoorEntrances.OrderBy(e => Vector3.Distance(e.transform.position, mainEntrancePosition)).ToList();

        for (int i = 0; i < SortedOutdoorEntrances.Count; i++)
        {
            SortedOutdoorEntrances[i].entranceId = i;

            NotezyLib.LogInfo($"Setting ExitID {SortedOutdoorEntrances[i].entranceId}({SortedOutdoorEntrances[i].gameObject.name}) for outdoor entrance at position {SortedOutdoorEntrances[i].transform.position}");
        }
        MultiDungeonGenerator.Instance.SortedOutdoorEntrances = SortedOutdoorEntrances;
        MultiDungeonGenerator.Instance.OnExitIDsSet.Invoke();
    }
}

