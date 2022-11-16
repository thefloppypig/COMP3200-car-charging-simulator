using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlaneMove : MonoBehaviour
{
    public GameObject cam;

    void FixedUpdate()
    {
        if (cam!=null)
        {
            transform.position = new Vector3(cam.transform.position.x, transform.position.y, cam.transform.position.z);
        }
    }

    private void OnMouseDown()
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;
        SumoSimulation.inst.Deselect();
    }
}
