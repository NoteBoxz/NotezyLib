using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DunGen;
using DunGen.Graph;
using Unity.Netcode;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.Events;

namespace NotezyLib.MultiDungeon
{
    [AddComponentMenu("NotezyLib/Multi-Dungeon Generator")]
    public class MultiDungeonGenerator : NetworkBehaviour
    {
        public static MultiDungeonGenerator? Instance;

        public void Awake()
        {
            Instance = this;
        }

        #region Settings
        [Header("Settings")]
        [Tooltip("List of extra dungeons to generate. These should be pre-placed in the scene.")]
        public List<RuntimeDungeon> ExtraDungeons = new();
        [Tooltip("Maximum number of buildings to attempt to generate. This is capped at the number of assigned dungeons.")]
        public int MaxBuildings => ExtraDungeons.Select(d => d != null).ToList().Count;
        [Tooltip("Divides the length of the dungeon flow to make shorter dungeons. Higher values make shorter dungeons, but can lead to generation failures if too high.")]
        public float FlowDevision = 1.5f;
        [Tooltip("Minimum allowed value for the Min length of a dungeon flow to be devided. This prevents extremely short flows that can cause generation failures.")]
        public int MinimumMinFlowLength = 5;
        [Tooltip("Minimum allowed value for the Max length of a dungeon flow be devided. This prevents extremely short flows that can cause generation failures.")]
        public int MinimumMaxFlowLength = 6;
        #endregion

        #region Extra Dungeons Generation
        [Header("(Status)")]
        public bool GeneratedExtraDungeons = false;
        public int currentDungeonIndex = 0;
        public bool isGeneratingDungeons = false;
        public UnityEvent OnExtraDungeonGenerationStarted = new();
        public UnityEvent<RuntimeDungeon> OnSingularExtraDungeonGenerationStarted = new();
        public UnityEvent<RuntimeDungeon> OnSingularExtraDungeonGenerated = new();
        public UnityEvent OnAllDungeonsGenerated = new();
        public UnityEvent OnExitIDsSet = new();
        #endregion

        #region Collections
        [Header("Set by game, do not modify")]
        public Dictionary<DungeonGenerator, (DungeonFlow OriginalFlow, DungeonFlow ClonedFlow)> FlowsToRemove = new();
        public List<IntWithRarity> LLLdungeonFlowTypes = new();
        public int[] RandomSeeds = new int[0];
        public int[] FlowIds = new int[0];
        [HideInInspector]
        public Dictionary<int, Dungeon> LinkedDungeons = new();
        [HideInInspector]
        public List<EntranceTeleport> SortedOutdoorEntrances = new();
        [HideInInspector]
        public List<EntranceTeleport> SortedIndoorEntrances = new();
        #endregion

        public void Start()
        {
            foreach (var dungeon in ExtraDungeons)
            {
                if (dungeon != null)
                    dungeon.gameObject.SetActive(false);
            }
        }

        [ClientRpc]
        public void SyncExDungeonDataClientRpc(NetworkIntWithRarity[] intWithRarities, int[] seeds, int[] flowIds)
        {
            try
            {
                if (!IsServer)
                    NotezyLib.LogMessage("Client received complete Extra Dungeon synchronization data");
                else
                    NotezyLib.LogDebug("Clients received completeExtra Dungeon synchronization data");

                // Set all data at once
                foreach (NetworkIntWithRarity NIWR in intWithRarities)
                {
                    LLLdungeonFlowTypes.Add(NIWR.ToIntWithRarity());
                }
                RandomSeeds = seeds;
                FlowIds = flowIds;

                for (int i = 0; i < MaxBuildings; i++)
                {
                    if (!IsServer)
                        NotezyLib.LogInfo($"Building {i}: Seed={seeds[i]}, FlowId={FlowIds[i]}");
                    else
                        NotezyLib.LogDebug($"(Client) Building {i}: Seed={seeds[i]}, FlowId={FlowIds[i]}");
                }
            }
            catch (System.Exception ex)
            {
                NotezyLib.LogError($"Error in SyncExDungeonDataClientRpc: {ex}");
            }
        }

        public void GenerateExtraDugens()
        {
            if (ExtraDungeons.Count == 0 || isGeneratingDungeons)
            {
                NotezyLib.LogError("No extra dungeons assigned or already generating");
                return;
            }

            OnExtraDungeonGenerationStarted.Invoke();

            isGeneratingDungeons = true;
            currentDungeonIndex = 0;
            StartCoroutine(GenerateNextDungeon());
        }

        private IEnumerator GenerateNextDungeon()
        {
            if (currentDungeonIndex >= ExtraDungeons.Count)
            {
                // All dungeons have been generated
                isGeneratingDungeons = false;
                GeneratedExtraDungeons = true;
                NotezyLib.LogMessage("All extra dungeons have been generated");
                NotezyLib.LogDebug($"Seeds: (mapSeed: {StartOfRound.Instance.randomMapSeed}) {string.Join(", ", RandomSeeds)}");
                foreach (var flow in FlowsToRemove)
                {
                    flow.Key.DungeonFlow = flow.Value.Item1; // Restore original flow
                    Destroy(flow.Value.Item2); // Destroy cloned flow
                }
                FlowsToRemove.Clear();
                RoundManager.Instance.FinishGeneratingLevel();
                OnAllDungeonsGenerated.Invoke();
                yield break;
            }

            RuntimeDungeon dungeon = ExtraDungeons[currentDungeonIndex];
            int seed = RandomSeeds[currentDungeonIndex];
            int flowTypeId = FlowIds[currentDungeonIndex];
            dungeon.gameObject.SetActive(true);

            // Get weighted dungeon flow type
            DungeonFlow dungeonFlow = RoundManager.Instance.dungeonFlowTypes[flowTypeId].dungeonFlow;

            // Configure generator
            dungeon.Generator.DungeonFlow = Instantiate(dungeonFlow);
            dungeon.Generator.ShouldRandomizeSeed = false;
            dungeon.Generator.Seed = seed;
            int DivMin = Mathf.RoundToInt(dungeon.Generator.DungeonFlow.Length.Min / FlowDevision);
            int DivMax = Mathf.RoundToInt(dungeon.Generator.DungeonFlow.Length.Max / FlowDevision);
            dungeon.Generator.DungeonFlow.Length.Min = dungeon.Generator.DungeonFlow.Length.Min < MinimumMinFlowLength ? dungeon.Generator.DungeonFlow.Length.Min : DivMin;
            dungeon.Generator.DungeonFlow.Length.Max = dungeon.Generator.DungeonFlow.Length.Max < MinimumMaxFlowLength ? dungeon.Generator.DungeonFlow.Length.Max : DivMax;
            NotezyLib.LogInfo($"Previous Flow Length: Min {dungeonFlow.Length.Min}, Max {dungeonFlow.Length.Max}");
            NotezyLib.LogInfo($"New Flow Length: Min {dungeon.Generator.DungeonFlow.Length.Min}, Max {dungeon.Generator.DungeonFlow.Length.Max}");

            // Adjust length multiplier based on map size and factory size
            float num2;
            num2 = RoundManager.Instance.currentLevel.factorySizeMultiplier / RoundManager.Instance.dungeonFlowTypes[flowTypeId].MapTileSize * RoundManager.Instance.mapSizeMultiplier;
            num2 = (float)((double)Mathf.Round(num2 * 100f) / 100.0);
            dungeon.Generator.LengthMultiplier = num2;
            foreach (DungeonFlow.GlobalPropSettings globalPropSettings in dungeon.Generator.DungeonFlow.GlobalProps)
            {
                if (globalPropSettings.ID == 1231)
                {
                    globalPropSettings.Count = new IntRange(0, 0);
                    break;
                }
            }
            FlowsToRemove.Add(dungeon.Generator, (dungeonFlow, dungeon.Generator.DungeonFlow));

            OnSingularExtraDungeonGenerationStarted.Invoke(dungeon);

            NotezyLib.LogInfo($"Starting dungeon generation {currentDungeonIndex} with seed {seed} and dungeon {dungeon.name}");

            // Wait a frame to allow UI to update
            yield return new WaitForEndOfFrame();

            // Start generation
            dungeon.Generate();

            // Check if completed instantly
            if (dungeon.Generator.Status == GenerationStatus.Complete)
            {
                NotezyLib.LogInfo($"Dungeon {currentDungeonIndex} generated instantly with seed {seed}");
                OnDungeonGenerationComplete(dungeon.Generator);
            }
            else
            {
                // Register for generation completion event
                dungeon.Generator.OnGenerationStatusChanged += Generator_OnGenerationStatusChanged;
                NotezyLib.LogInfo($"Listening for completion of dungeon {currentDungeonIndex}");
            }
        }

        private void Generator_OnGenerationStatusChanged(DungeonGenerator generator, GenerationStatus status)
        {
            if (status == GenerationStatus.Complete)
            {
                OnDungeonGenerationComplete(generator);
            }
            else if (status == GenerationStatus.Failed)
            {
                NotezyLib.LogError($"Dungeon generation failed for dungeon {currentDungeonIndex}");

                // Clean up and try the next dungeon
                generator.OnGenerationStatusChanged -= Generator_OnGenerationStatusChanged;

                // Move to next dungeon
                currentDungeonIndex++;
                StartCoroutine(GenerateNextDungeon());
            }
        }

        private void OnDungeonGenerationComplete(DungeonGenerator generator)
        {
            // Unregister from event
            generator.OnGenerationStatusChanged -= Generator_OnGenerationStatusChanged;

            // Clean up
            Dungeon DG = generator.Root.GetComponentInChildren<Dungeon>();
            DG.DungeonFlow = FlowsToRemove[generator].Item1; // Restore original flow
            NotezyLib.LogInfo($"Dungeon {currentDungeonIndex} generation completed successfully");

            // Invoke event
            RuntimeDungeon? RD = ExtraDungeons.Find(d => d.Generator == generator);
            if (RD != null)
                OnSingularExtraDungeonGenerated.Invoke(RD);
            else
                NotezyLib.LogWarning($"Could not find RuntimeDungeon for generated dungeon {currentDungeonIndex} to invoke OnExtraDungeonGenerated");

            // Move to next dungeon
            currentDungeonIndex++;
            StartCoroutine(GenerateNextDungeon()); ;
        }
    }
}