using System.Collections.Generic;
using System.Linq;
using System;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using DunGen.Graph;
using HarmonyLib;
using NotezyLib.MultiDungeon;
using UnityEngine;
using System.IO;

namespace NotezyLib
{
    [BepInDependency("imabatby.lethallevelloader")]
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class NotezyLib : BaseUnityPlugin
    {
        public static NotezyLib Instance { get; private set; } = null!;
        internal new static ManualLogSource Logger { get; private set; } = null!;
        internal static Harmony? Harmony { get; set; }
        public static List<DungeonFlow> DungeonFlows => Resources.FindObjectsOfTypeAll<DungeonFlow>().ToList();

        private void Awake()
        {
            Logger = base.Logger;
            Instance = this;

            NetcodePatcher();
            Patch();

            Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
        }

        internal static void Patch()
        {
            Harmony ??= new Harmony(MyPluginInfo.PLUGIN_GUID);

            Logger.LogDebug("Patching...");

            var types = GetTypesWithErrorHandling();
            foreach (var type in types)
            {
                // Skip anything under NotezyLib.Editor namespace
                if (type.Namespace != null && type.Namespace.StartsWith("NotezyLib.Editor", StringComparison.Ordinal))
                    continue;
                try
                {
                    Harmony.CreateClassProcessor(type).Patch();
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"Failed to process patches for {type.FullName}: {ex}");
                }
            }

            Logger.LogDebug("Finished patching!");
        }

        internal static void Unpatch()
        {
            Logger.LogDebug("Unpatching...");

            Harmony?.UnpatchSelf();

            Logger.LogDebug("Finished unpatching!");
        }

        private void NetcodePatcher()
        {
            var types = GetTypesWithErrorHandling();
            foreach (var type in types)
            {
                if (type.Namespace != null && type.Namespace.StartsWith("NotezyLib.Editor", StringComparison.Ordinal))
                    continue;
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }
        }
        internal static Type[] GetTypesWithErrorHandling()
        {
            try
            {
                return Assembly.GetExecutingAssembly().GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                Logger.LogWarning("ReflectionTypeLoadException caught while getting types. Some types will be skipped.");
                foreach (var loaderException in e.LoaderExceptions)
                {
                    Logger.LogDebug($"Loader Exception: {loaderException.Message}");
                    if (loaderException is FileNotFoundException fileNotFound)
                    {
                        Logger.LogDebug($"Could not load file: {fileNotFound.FileName}");
                    }
                }
                return e.Types.Where(t => t != null).ToArray();
            }
            catch (Exception e)
            {
                Logger.LogError($"Unexpected error while getting types: {e}");
                return new Type[0];
            }
        }

        public static void LogMessage(object message)
        {
            Logger.LogMessage(message);
        }

        public static void LogDebug(object message)
        {
            Logger.LogDebug(message);
        }

        public static void LogWarning(object message)
        {
            Logger.LogWarning(message);
        }

        public static void LogError(object message)
        {
            Logger.LogError(message);
        }

        public static void LogFatal(object message)
        {
            Logger.LogFatal(message);
        }

        public static void LogInfo(object message)
        {
            Logger.LogInfo(message);
        }
    }
}
