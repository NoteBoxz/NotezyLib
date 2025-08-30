using DunGen.Graph;
using LethalLevelLoader;
using System.Collections.Generic;
using UnityEngine;

namespace NotezyLib.MultiDungeon;


[System.Serializable]
public struct ShrinkedFlow
{
    public DungeonFlow Flow;

    public int PrevMinLength;
    public int PrevMaxLength;
    public int NewMinLength;
    public int NewMaxLength;
}