using PathCreation;
using PathCreation.Examples;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

public class SumoEdge : MonoBehaviour
{
    public string id;
    public Transform from;
    public Transform to;
    public int numLanes;
    public string centered;
    public List<Vector3> shape;
    public PathCreator roadSpline;
    public RoadMeshCreator roadMesh;


    public void SetValues(string id, Transform from, Transform to, string numLanes, string centered, List<Vector3> shape, PathCreator roadSpline)
    {
        this.id = id;
        this.from = from;
        this.to = to;
        int.TryParse(numLanes, out this.numLanes);
        this.centered = centered;
        this.shape = shape;
        this.roadSpline = roadSpline;
        this.roadMesh = GetComponent<RoadMeshCreator>();
    }

    public bool IsOneSided()
    {
        if (centered.Equals("center"))
        {
            return false;
        }
        else
        {
            return true;
        }
    }
}
