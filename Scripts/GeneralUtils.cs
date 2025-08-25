using DunGen.Graph;
using GameNetcodeStuff;
using LethalLevelLoader;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace NotezyLib.Utils
{
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
    }
}