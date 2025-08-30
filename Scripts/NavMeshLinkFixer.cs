using System.Collections.Generic;
using DunGen;
using TMPro;
using Unity.AI.Navigation;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace NotezyLib;

[AddComponentMenu("NotezyLib/NavMeshLinkFixer")]
public class NavMeshLinkFixer : MonoBehaviour
{
    [Header("If enabled, will attempt to fix the start and/or end point of the NavMeshLink to nearest valid NavMesh position on start")]
    public NavMeshLink link = null!;
    public bool FixStart, FixEnd;
    public void OnEnable()
    {
        StartCoroutine(DelayedFix());
    }

    private System.Collections.IEnumerator DelayedFix()
    {
        yield return new WaitForSeconds(1f);
        if (link == null)
            link = GetComponent<NavMeshLink>();
        if (FixStart && NavMesh.SamplePosition(link.transform.TransformPoint(link.startPoint), out NavMeshHit hitStart, 5.0f, NavMesh.AllAreas))
        {
            link.startPoint = link.transform.InverseTransformPoint(hitStart.position);
        }
        if (FixEnd && NavMesh.SamplePosition(link.transform.TransformPoint(link.endPoint), out NavMeshHit hitEnd, 5.0f, NavMesh.AllAreas))
        {
            link.endPoint = link.transform.InverseTransformPoint(hitEnd.position);
        }
        link.UpdateLink();
    }
}