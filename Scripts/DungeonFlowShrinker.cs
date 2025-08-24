using DunGen.Graph;
using LethalLevelLoader;
using System.Collections.Generic;
using UnityEngine;

namespace NotezyLib.MultiDungeon
{
    public class DungeonFlowShrinker
    {
        private static Dictionary<DungeonFlow, ShrinkedFlow> _shrinkedFlows = new Dictionary<DungeonFlow, ShrinkedFlow>();

        public static ShrinkedFlow ShrinkFlow(DungeonFlow flow, float flowDivision, float minimumMinFlowLength, float minimumMaxFlowLength)
        {
            // If already shrunk, return the existing shrinked flow
            if (_shrinkedFlows.TryGetValue(flow, out ShrinkedFlow existingShrinked))
            {
                NotezyLib.LogInfo($"Flow '{flow.name}' is already shrunk. Current Length: ({flow.Length.Min}, {flow.Length.Max})");
                return existingShrinked;
            }

            // Store original values
            int prevMinLength = flow.Length.Min;
            int prevMaxLength = flow.Length.Max;

            // Calculate new values
            int divMin = Mathf.RoundToInt(flow.Length.Min / flowDivision);
            int divMax = Mathf.RoundToInt(flow.Length.Max / flowDivision);

            int newMinLength = flow.Length.Min < minimumMinFlowLength ? flow.Length.Min : divMin;
            int newMaxLength = flow.Length.Max < minimumMaxFlowLength ? flow.Length.Max : divMax;

            // Modify the original flow
            flow.Length.Min = newMinLength;
            flow.Length.Max = newMaxLength;

            // Create and store the shrinked flow info
            ShrinkedFlow shrinkedFlow = new ShrinkedFlow
            {
                Flow = flow,
                PrevMinLength = prevMinLength,
                PrevMaxLength = prevMaxLength,
                NewMinLength = newMinLength,
                NewMaxLength = newMaxLength
            };

            _shrinkedFlows[flow] = shrinkedFlow;

            NotezyLib.LogInfo($"Shrunk flow '{flow.name}' from  ({prevMaxLength}, {prevMinLength}) to ({newMaxLength}, {newMinLength})");
            return shrinkedFlow;
        }

        public static bool RestoreFlow(DungeonFlow flow)
        {
            if (_shrinkedFlows.TryGetValue(flow, out ShrinkedFlow shrinkedFlow))
            {
                flow.Length.Min = shrinkedFlow.PrevMinLength;
                flow.Length.Max = shrinkedFlow.PrevMaxLength;
                _shrinkedFlows.Remove(flow);
                return true;
            }
            return false;
        }

        public static bool IsFlowShrunk(DungeonFlow flow)
        {
            return _shrinkedFlows.ContainsKey(flow);
        }

        public static void ClearAllShrinkedFlows()
        {
            foreach (var kvp in _shrinkedFlows)
            {
                kvp.Key.Length.Min = kvp.Value.PrevMinLength;
                kvp.Key.Length.Max = kvp.Value.PrevMaxLength;
            }
            _shrinkedFlows.Clear();
        }
    }

    [System.Serializable]
    public struct ShrinkedFlow
    {
        public DungeonFlow Flow;

        public int PrevMinLength;
        public int PrevMaxLength;
        public int NewMinLength;
        public int NewMaxLength;
    }
}