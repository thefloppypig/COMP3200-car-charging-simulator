using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SumoVehicle : MonoBehaviour, ISelectable
{
    public string id;
    //public List<Color> cols;
    public MeshRenderer meshr;

    private void OnEnable()
    {
        Colouring();
    }

    private void OnValidate()
    {
        Colouring();
    }

    private void Colouring()
    {
        try
        {
            Color col = Random.ColorHSV(0,1, 0,0.5f, 0,1);//cols[Random.Range(0, cols.Count)];
            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            mpb.SetColor("_Color", col);
            Color col2 = Random.ColorHSV(0,1, 0,0, 0,1);
            mpb.SetColor("_SpecColor", col2);

            meshr.SetPropertyBlock(mpb,1);
        }
        catch (System.Exception)
        {
        }
    }

    private void OnMouseDown()
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;
        Debug.Log($"{gameObject.name} was selected");
        SumoSimulation.inst.Select(this);
    }

    public void SetID(string id)
    {
        this.id = id;
        name = id;
    }
}
