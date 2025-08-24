using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor.SceneManagement;
using UnityEditor;
using NotezyLib.MultiDungeon;
using DunGen;
using Unity.Netcode;

namespace NotezyLib.Editor;

public class MultiDungeonSetUp : EditorWindow
{
    [MenuItem("Tools/NotezyLib/Set Up Multi-Dungeon")]
    public static void ShowWindow()
    {
        GetWindow<MultiDungeonSetUp>("Set Up Multi-Dungeon");
    }

    public RuntimeDungeon? DungeonToCopyFrom;
    public GameObject? MainEntranceToCopyFrom;
    public bool MoveDungeonInsteadOfRoot = false;
    public bool ReuseExistingMultiDungeon = false;
    public int DungeonCount = 0;
    public float Spacing = 500f;
    public float BaseYPosition = -218;

    void OnGUI()
    {
        GUILayout.Label("Multi-Dungeon Setup", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("This tool helps set up multiple dungeons in the current scene. It creates a parent GameObject named 'Multi-Dungeon' then creates"
        + " and arranges the specified number of dungeons around the origin based on the chosen spacing." +
        " Each dungeon is instantiated from the selected RuntimeDungeon object, it does not need to be a prefab.", MessageType.Info);
        EditorGUILayout.HelpBox("NotezyLib currently doesn't support Fire Exits, so each EntranceTeleport must lead to a dungeon.", MessageType.Warning);
        float prevLabelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 300f;
        DungeonToCopyFrom = (RuntimeDungeon)EditorGUILayout.ObjectField("Dungeon to Copy From", DungeonToCopyFrom, typeof(RuntimeDungeon), true);

        MainEntranceToCopyFrom = (GameObject)EditorGUILayout.ObjectField("Main Entrance to Copy From", MainEntranceToCopyFrom, typeof(GameObject), true);

        DungeonCount = EditorGUILayout.IntField("Number of Extra Dungeons", DungeonCount);

        Spacing = EditorGUILayout.FloatField("Spacing Between Dungeons", Spacing);

        BaseYPosition = EditorGUILayout.FloatField("Base Y Position", BaseYPosition);

        MoveDungeonInsteadOfRoot = EditorGUILayout.Toggle("Move Dungeon Instead of Root", MoveDungeonInsteadOfRoot);

        ReuseExistingMultiDungeon = EditorGUILayout.Toggle("Reuse Existing Multi-Dungeon Gen", ReuseExistingMultiDungeon);
        EditorGUIUtility.labelWidth = prevLabelWidth;

        if (GUILayout.Button("Set Up Multi-Dungeon in Current Scene"))
        {
            SetUpMultiDungeonInCurrentScene();
        }
    }

    public void SetUpMultiDungeonInCurrentScene()
    {
        MultiDungeonGenerator multiDungeonGenerator = null!;
        bool shouldCreateNew = true;
        if (ReuseExistingMultiDungeon)
        {
            MultiDungeonGenerator? existingGen = FindObjectOfType<MultiDungeonGenerator>();
            if (existingGen != null)
            {
                multiDungeonGenerator = existingGen;
            }
            else
            {
                shouldCreateNew = true;
            }
        }
        if (shouldCreateNew)
        {
            GameObject multiDungeonGO = new("Multi-Dungeon");
            multiDungeonGenerator = multiDungeonGO.AddComponent<MultiDungeonGenerator>();
            multiDungeonGO.AddComponent<NetworkObject>();
        }

        for (int i = 0; i < DungeonCount; i++)
        {
            if (DungeonToCopyFrom == null)
            {
                Debug.LogError("Dungeon to copy from is not assigned.");
                return;
            }

            GameObject dungeonGO = new($"Dungeon_{i + 1}");

            // Calculate position based on dungeon arrangement
            Vector3 dungeonPosition = CalculateDungeonPosition(i, DungeonCount, Spacing, BaseYPosition);
            dungeonGO.transform.position = dungeonPosition;

            RuntimeDungeon newDungeon = null!;
            if (DungeonToCopyFrom != null)
            {
                newDungeon = Instantiate(DungeonToCopyFrom, dungeonGO.transform);
                newDungeon.gameObject.name.Replace("(Clone)", "");
                newDungeon.transform.localPosition = Vector3.zero;
            }
            else
            {
                GameObject GeneratorGO = new("Generator");
                GeneratorGO.transform.SetParent(dungeonGO.transform);
                newDungeon = GeneratorGO.AddComponent<RuntimeDungeon>();
            }
            if (MainEntranceToCopyFrom != null)
            {
                GameObject entranceGO = Instantiate(MainEntranceToCopyFrom);
                entranceGO.transform.position = MainEntranceToCopyFrom.transform.position + (i + 1) * MainEntranceToCopyFrom.transform.right * 5f;
                newDungeon.gameObject.name.Replace("(Clone)", "");
                entranceGO.name += $"_Dungeon_{i + 1}_Entrance";
            }
            GameObject RootGO = new("Root");
            RootGO.transform.SetParent(dungeonGO.transform);
            newDungeon.Root = RootGO;
            if (!MoveDungeonInsteadOfRoot)
            {
                RootGO.transform.localPosition = dungeonPosition;
                dungeonGO.transform.localPosition = Vector3.zero;
            }
            else
            {
                RootGO.transform.localPosition = Vector3.zero;
                dungeonGO.transform.localPosition = dungeonPosition;
            }

            dungeonGO.transform.SetParent(multiDungeonGenerator.transform, true);

            multiDungeonGenerator.ExtraDungeons.Add(newDungeon);
        }
    }

    private Vector3 CalculateDungeonPosition(int index, int totalDungeons, float spacing, float baseY)
    {
        if (totalDungeons == 1)
        {
            // Single dungeon - place it off-center to avoid (0,0,0)
            return new Vector3(spacing * 0.5f, baseY, 0);
        }

        if (totalDungeons <= 8)
        {
            // Arrange in hollow square pattern for base layer
            return CalculateHollowSquarePosition(index, totalDungeons, spacing, baseY);
        }
        else
        {
            // More than 8 dungeons: arrange in multiple layers below base Y
            int dungeonsPerLayer = 8;
            int layer = index / dungeonsPerLayer;
            int indexInLayer = index % dungeonsPerLayer;

            Vector3 layerPosition;
            if (layer == 0)
            {
                // Base layer: hollow square
                layerPosition = CalculateHollowSquarePosition(indexInLayer, dungeonsPerLayer, spacing, baseY);
            }
            else
            {
                // Lower layers: filled 3x3 grid (9 positions)
                layerPosition = CalculateFilledGridPosition(indexInLayer, spacing, baseY - (layer * spacing));
            }

            return layerPosition;
        }
    }

    private Vector3 CalculateHollowSquarePosition(int index, int totalDungeons, float spacing, float baseY)
    {
        int perSide = Mathf.CeilToInt(totalDungeons / 4f);
        int side = index / perSide;
        int positionOnSide = index % perSide;

        float offset = spacing;
        float x = 0, z = 0;

        switch (side)
        {
            case 0: // Top side
                x = (positionOnSide - perSide * 0.5f + 0.5f) * spacing;
                z = offset;
                break;
            case 1: // Right side
                x = offset;
                z = -(positionOnSide - perSide * 0.5f + 0.5f) * spacing;
                break;
            case 2: // Bottom side
                x = -(positionOnSide - perSide * 0.5f + 0.5f) * spacing;
                z = -offset;
                break;
            case 3: // Left side
                x = -offset;
                z = (positionOnSide - perSide * 0.5f + 0.5f) * spacing;
                break;
        }

        return new Vector3(x, baseY, z);
    }

    private Vector3 CalculateFilledGridPosition(int index, float spacing, float y)
    {
        // 3x3 grid positions (9 total)
        int gridSize = 3;
        int row = index / gridSize;
        int col = index % gridSize;

        // Center the grid around origin
        float x = (col - 1) * spacing; // -1, 0, 1 * spacing
        float z = (row - 1) * spacing; // -1, 0, 1 * spacing

        return new Vector3(x, y, z);
    }
}
