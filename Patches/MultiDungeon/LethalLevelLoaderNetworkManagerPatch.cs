using DunGen.Graph;
using HarmonyLib;
using LethalLevelLoader;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using StringContainer = LethalLevelLoader.LethalLevelLoaderNetworkManager.StringContainer;

namespace NotezyLib.MultiDungeon.Patches;

[HarmonyPatch(typeof(LethalLevelLoaderNetworkManager))]
public class LethalLevelLoaderNetworkManagerPatch
{
    [HarmonyPatch(nameof(LethalLevelLoaderNetworkManager.GetRandomExtendedDungeonFlowServerRpc))]
    [HarmonyPrefix]
    public static void GetRandomExtendedDungeonFlowServerRpcPrefix(LethalLevelLoaderNetworkManager __instance)
    {
        try
        {
            //Code to make sure only the server executes this RPC
            NetworkManager networkManager = __instance.NetworkManager;
            if ((object)networkManager == null || !networkManager.IsListening)
            {
                return;
            }
            if (__instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Server && (networkManager.IsClient || networkManager.IsHost))
            {
                if (__instance.OwnerClientId != networkManager.LocalClientId)
                {
                    return;
                }
            }
            if (__instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Server || (!networkManager.IsServer && !networkManager.IsHost))
            {
                return;
            }
            //Code to make sure only the server executes this RPC

            if (MultiDungeonGenerator.Instance == null)
            {
                return;
            }

            NotezyLib.LogMessage("Getting random extended dungeon flows for Extra buildings...");

            // Get available dungeon flows from LLL
            List<ExtendedDungeonFlowWithRarity> availableExtendedFlowsList = DungeonManager.GetValidExtendedDungeonFlows(
                LevelManager.CurrentExtendedLevel, false);

            // Process the dungeon flows
            List<NetworkIntWithRarity> dungeonFlowsList = new List<NetworkIntWithRarity>();
            Dictionary<string, int> dungeonFlowIds = new Dictionary<string, int>();
            int counter = 0;

            // Create mapping of dungeon flow names to IDs
            foreach (DungeonFlow dungeonFlow in LethalLevelLoader.Patches.RoundManager.GetDungeonFlows())
            {
                dungeonFlowIds.Add(dungeonFlow.name, counter);
                counter++;
            }

            // Process the available flows
            if (availableExtendedFlowsList.Count == 0)
            {
                NotezyLib.LogError("No ExtendedDungeonFlow's found - using default flow");

                // Add default dungeon flow
                NetworkIntWithRarity intWithRarity = new NetworkIntWithRarity(dungeonFlowIds[PatchedContent.ExtendedDungeonFlows[0].DungeonFlow.name], 300);
                dungeonFlowsList.Add(intWithRarity);
            }
            else
            {
                // Add all available dungeon flows with their rarities
                List<DungeonFlow> dungeonFlowTypes = LethalLevelLoader.Patches.RoundManager.GetDungeonFlows();
                foreach (ExtendedDungeonFlowWithRarity extendedFlow in availableExtendedFlowsList)
                {
                    string flowName = extendedFlow.extendedDungeonFlow.DungeonFlow.name;
                    if (dungeonFlowIds.ContainsKey(flowName))
                    {
                        NetworkIntWithRarity intWithRarity = new NetworkIntWithRarity(dungeonFlowIds[flowName], extendedFlow.rarity);
                        dungeonFlowsList.Add(intWithRarity);
                    }
                }
            }

            // Generate seeds for all buildings
            int[] generatedSeeds = new int[MultiDungeonGenerator.Instance.MaxBuildings];
            int[] selectedFlowIds = new int[MultiDungeonGenerator.Instance.MaxBuildings];

            for (int i = 0; i < MultiDungeonGenerator.Instance.MaxBuildings; i++)
            {
                int seed = UnityEngine.Random.Range(1, 100000000);
                generatedSeeds[i] = seed;

                int randomWeightedIndex = RoundManager.Instance.GetRandomWeightedIndex(dungeonFlowsList.Select(f => f.rarity).ToArray(), new System.Random(seed));
                selectedFlowIds[i] = dungeonFlowsList[randomWeightedIndex].id;

                NotezyLib.LogInfo($"Building {i}: Seed={seed}, FlowId={selectedFlowIds[i]}");
            }

            // Call SyncSeeds to generate random seeds and synchronize with clients
            MultiDungeonGenerator.Instance.SyncExDungeonDataClientRpc(dungeonFlowsList.ToArray(), generatedSeeds, selectedFlowIds);
        }
        catch (System.Exception ex)
        {
            NotezyLib.LogError($"Error in GetRandomExtendedDungeonFlowServerRpcPrefix: {ex}");
        }
    }
}

