using System;
using UnityEngine;

namespace NotezyLib;

public static class Extensions
{
    /// <summary>
    /// Adds an item to the list if it is not already present.
    /// </summary>
    /// <typeparam name="T">The type of items in the list</typeparam>
    /// <param name="list">The list to add the item to</param>
    /// <param name="item">The item to add</param>
    public static void AddIfNotAlreadyInList<T>(this System.Collections.Generic.List<T> list, T item)
    {
        if (!list.Contains(item))
        {
            list.Add(item);
        }
    }

    /// <summary>
    /// Checks if a list of strings contains a string, ignoring case.
    /// </summary>
    /// <param name="list">The list to check</param>
    /// <param name="item">The string to look for</param>
    /// <returns>True if the list contains the string, false otherwise</returns>
    public static bool ContainsLowercase(this System.Collections.Generic.List<string> list, string item)
    {
        foreach (string element in list)
        {
            if (element.ToLower() == item.ToLower())
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Returns a new array with <paramref name="obj"/> appended to the end.
    /// </summary>
    /// <typeparam name="T">The type of objects in the array</typeparam>
    /// <param name="array">The source array (can be null)</param>
    /// <param name="obj">The object to add to the array</param>
    /// <returns>A new array containing the original elements followed by <paramref name="obj"/></returns>
    public static T[] AddToArray<T>(this T[] array, T obj)
    {
        if (array == null)
            return new T[] { obj };

        T[] newArray = new T[array.Length + 1];
        for (int i = 0; i < array.Length; i++)
        {
            newArray[i] = array[i];
        }
        newArray[array.Length] = obj;
        return newArray;
    }

    /// <summary>
    /// Returns a new array with the first occurrence of <paramref name="obj"/> removed.
    /// If the object is not found the original array is returned.
    /// </summary>
    /// <typeparam name="T">The type of objects in the array</typeparam>
    /// <param name="array">The source array (can be null)</param>
    /// <param name="obj">The object to remove from the array</param>
    /// <returns>A new array with the object removed, or the original array if not found</returns>
    public static T[] RemoveFromArray<T>(this T[] array, T obj)
    {
        if (array == null || array.Length == 0)
            return new T[] { };

        int indexToRemove = GetIndexFromArray(array, obj);
        if (indexToRemove == -1)
            return array!;

        T[] newArray = new T[array.Length - 1];
        if (indexToRemove > 0)
            Array.Copy(array, 0, newArray, 0, indexToRemove);
        if (indexToRemove < array.Length - 1)
            Array.Copy(array, indexToRemove + 1, newArray, indexToRemove, array.Length - indexToRemove - 1);

        return newArray;
    }

    /// <summary>
    /// Gets the index of the first occurrence of an object in an array
    /// </summary>
    /// <typeparam name="T">The type of objects in the array</typeparam>
    /// <param name="array">The array to search</param>
    /// <param name="obj">The object to find</param>
    /// <returns>The index of the object if found, -1 otherwise</returns>
    public static int GetIndexFromArray<T>(this T[] array, T obj)
    {
        if (array == null || array.Length == 0)
        {
            return -1;
        }

        for (int i = 0; i < array.Length; i++)
        {
            if (array[i]?.Equals(obj) == true || (array[i] == null && obj == null))
            {
                return i;
            }
        }

        return -1;
    }


    /// <summary>
    /// Tries to get a component of type T from the parent of the given GameObject
    /// </summary>
    /// <typeparam name="T">The type of component to get</typeparam>
    /// <param name="component">The output component if found, default value of T otherwise</param>
    /// <param name="go">The GameObject whose parent to search</param>
    /// <returns>True if the component was found, false otherwise</returns>
    public static bool TryGetComponentInParent<T>(this MonoBehaviour mono, out T component)
    {
        component = default!;
        Transform current = mono.transform;
        if (current.parent == null)
            return false;

        component = mono.GetComponentInParent<T>();
        return component != null;
    }
    
    /// <summary>
    /// Tries to get a component of type T from the children of the given GameObject
    /// </summary>
    /// <typeparam name="T">The type of component to get</typeparam>
    /// <param name="component">The output component if found, default value of T otherwise
    /// </param> <param name="go">The GameObject whose children to search</param>
    /// <returns>True if the component was found, false otherwise</returns>
    public static bool TryGetComponentInChildren<T>(this MonoBehaviour mono, out T component)
    {
        component = default!;
        Transform current = mono.transform;
        if (current.childCount == 0)
            return false;

        component = mono.GetComponentInChildren<T>();
        return component != null;
    }
}