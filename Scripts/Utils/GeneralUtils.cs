using DunGen.Graph;
using GameNetcodeStuff;
using LethalLevelLoader;
using System.Collections.Generic;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;

namespace NotezyLib.Utils;

public static class GeneralUtils
{
    public static T? FindClosestInstanceOfScript<T>(Vector3 position) where T : MonoBehaviour
    {
        T? closestInstance = null;
        float closestDistance = float.MaxValue;
        foreach (T instance in Object.FindObjectsOfType<T>())
        {
            float distance = Vector3.Distance(position, instance.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestInstance = instance;
            }
        }
        return closestInstance;
    }

    public static bool DoesTypeUseMethod(System.Type type, string methodName)
    {
        if (type == null) return false;

        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);

        foreach (var method in methods)
        {
            try
            {
                var methodBody = method.GetMethodBody();
                if (methodBody == null) continue;

                var ilBytes = methodBody.GetILAsByteArray();
                if (ilBytes == null) continue;

                // Look for call/callvirt instructions followed by method tokens
                for (int i = 0; i < ilBytes.Length - 4; i++)
                {
                    // Check for call (0x28) or callvirt (0x6F) opcodes
                    if (ilBytes[i] == 0x28 || ilBytes[i] == 0x6F)
                    {
                        // Extract the 4-byte method token that follows the opcode
                        int methodToken = System.BitConverter.ToInt32(ilBytes, i + 1);

                        try
                        {
                            // Resolve the method from the token
                            var calledMethod = method.Module.ResolveMethod(methodToken);

                            // Check if this is the method we're looking for
                            if (calledMethod.Name == methodName)
                            {
                                //NotezyLib.LogDebug($"Type {type.FullName} uses method {methodName} in method {method.Name} at IL offset {i}");
                                return true;
                            }
                        }
                        catch (System.Exception)
                        {
                            // Skip invalid tokens
                            continue;
                        }
                    }
                }
            }
            catch (System.Exception)
            {
                //NotezyLib.LogWarning($"Could not analyze method {method.Name}: {ex.Message}");
            }
        }

        return false;
    }
}