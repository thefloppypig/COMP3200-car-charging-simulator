using PathCreation;
using PathCreation.Examples;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SumoCStation : MonoBehaviour, ISelectable
{
    public SumoEdge sumoEdge;
    public PathCreator roadSpline;
    public RoadMeshCreator roadMesh;
    public List<string> cStationIds = new List<string>();

    public void SetValues(SumoEdge sumoEdge, PathCreator roadSpline, RoadMeshCreator roadMesh)
    {
        this.sumoEdge = sumoEdge;
        this.roadSpline = roadSpline;
        this.roadMesh = roadMesh;
    }

    public void AddStationID(string c)
    {
        cStationIds.Add(c);
    }

    public void Selected()
    {
        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        mpb.SetColor("_Color", Color.red);
        roadMesh.meshRenderer.SetPropertyBlock(mpb);
        transform.localScale = Vector3.one +Vector3.up;
    }

    public void Unselected()
    {
        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        roadMesh.meshRenderer.SetPropertyBlock(mpb);
        transform.localScale = Vector3.one;
    }

    private void OnMouseDown()
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;
        Debug.Log($"{gameObject.name} has been selected");
        Selected();
        SumoSimulation.inst.Select(this);
    }
}
