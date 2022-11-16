using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using CodingConnected.TraCI.NET;
using System.Xml;
using System.IO;

public class SelectionUI : MonoBehaviour, ISelectable
{
    public GameObject mains;
    public TextMeshProUGUI title;
    public RectTransform content;

    public GameObject selTextPrefab;
    const float selTextHeight = 25f;

    bool updating = false;

    ISelectable selected;
    const int updatereg = 4;
    float contentheight = 0;

    void Start()
    {
        SetSelected(null);
    }

    public void SetSelected(ISelectable obj)
    {
        if (obj == null)
        {
            SetDeselected();
            return;
        }
        mains.SetActive(true);
        if (obj is SumoCStation cStat)
        {
            selected = cStat;
            title.text = "Road: "+cStat.sumoEdge.id;
            if (!updating)
            {
                AddCSContentXML(cStat);
            }
            else
            {
                AddCSContent(cStat);
            }
        }
        else if (obj is SumoVehicle car) {
            selected = car;
            title.text = "Vehicle: " + car.name;
            AddVehContent(car);
        }
        else
        {
            Debug.Log("Could not select: " + obj);
        }
    }

    private void SetDeselected()
    {
        selected = null;
        ClearContent();
        mains.SetActive(false);
    }

    void ClearContent()
    {
        contentheight = 0;
        foreach (Transform obj in content.transform)
        {
            Destroy(obj.gameObject);
        }
    }

    //cstat content when sim is not running
    void AddCSContentXML(SumoCStation cStat)
    {
        XmlDocument add = new XmlDocument();
        string addpath = SumoSimulation.inst.sumoSimPath + "map.add.xml";
        add.Load(addpath);
        if (!File.Exists(addpath))
        {
            Debug.Log("Additional File not found for : " + addpath);
            return;
        }
        try
        {
            List<string> data = new List<string>();
            foreach (string id in cStat.cStationIds)
            {
                //get data from xml
                XmlElement node = add.SelectSingleNode($"additional/chargingStation[@id='{id}']") as XmlElement;

                data.Add($"<u>{id}</u>");

                string power = node.GetAttribute("power");
                if ("".Equals(power)) power = "0";
                data.Add($"Power: {power}W");

                string eff = node.GetAttribute("efficiency");
                if ("".Equals(eff)) eff = "0";
                data.Add($"Efficiency: {eff}");
            }
            //display the data
            ReplaceContent(data);
        }
        catch (Exception e)
        {                
            Debug.LogWarning($"Something wrong happened while getting data from { selected}:{e}");
            throw;
        }
    }

    //cstat content when sim is running
    void AddCSContent(SumoCStation cStat)
    {
        TraCIClient traci = SumoSimulation.inst.client;
        List<string> ids = cStat.cStationIds;
        if (SumoSimulation.inst.IsConnected())
        {
            try
            {
                //get data
                List<string> data = new List<string>();

                foreach (string id in ids)
                {
                    int stopCount = traci.ChargingStation.GetVehicleCount(id).Content;
                    data.Add($"<u>{id}</u>");
                    data.Add($"Vehicles charging: {stopCount.ToString()}");
                    
                    List<string> stops = traci.ChargingStation.GetIdList(id).Content;
                    string stopss = String.Join(", ", stops);
                    if ("".Equals(stopss)) stopss = "None";
                    data.Add($"VehicleIDs: {stopss}");
                }

                //display the data
                ReplaceContent(data);

            }
            catch (Exception e)
            {
                Debug.LogWarning($"Something wrong happened while getting data from { selected}:{e}");
                throw e;
            }
        }
    }

    //vehicle content when sim is running
    void AddVehContent(SumoVehicle car)
    {
        TraCIClient traci = SumoSimulation.inst.client;
        string id = car.id;
        if (SumoSimulation.inst.IsConnected())
        {
            try
            {
                //get data
                List<string> data = new List<string>();

                double speed = traci.Vehicle.GetSpeed(id).Content;
                data.Add($"Speed: {speed.ToString("F2")}m/s");

                string battery = traci.Vehicle.GetParameter(id, "has.battery.device").Content;
                data.Add($"Is Electric: {battery}");

                string bActual = traci.Vehicle.GetParameter(id, "device.battery.actualBatteryCapacity").Content;
                if (bActual==null || "".Equals(bActual)) bActual = "0";
                data.Add($"Battery Charge: {bActual}Wh");

                string bCapacity = traci.Vehicle.GetParameter(id, "device.battery.maximumBatteryCapacity").Content;
                if (bCapacity == null || "".Equals(bCapacity)) bCapacity = "0";
                data.Add($"Battery Capacity: {bCapacity}Wh");

                string eConsupmtion = traci.Vehicle.GetParameter(id, "constantPowerIntake").Content;
                if (eConsupmtion == null || "".Equals(eConsupmtion)) eConsupmtion = "0";
                data.Add($"Avg. Elec. Consumption: {eConsupmtion}W");

                /*double fConsupmtion = traci.Vehicle.GetFuelConsumption(id).Content;
                data.Add($"Fuel Consumption: {eConsupmtion.ToString("F2")}ml");*/

                List<string> via = traci.Vehicle.GetVia(id).Content;
                string vias = "";
                if (via != null) {
                    vias = String.Join(", ", via);
                    if ("".Equals(vias)) vias = "No Stops";
                }
                else
                {
                    vias = "No Stops";
                }
                data.Add($"Stop at: { vias}");


                int stopstates = traci.Vehicle.GetStopState(id).Content;
                /*bool stopped = (stopstates & (1)) != 0;
                data.Add($"Stopped: {stopped.ToString()}");*/
                bool stopatCS = (stopstates & (1 << 6)) != 0;
                data.Add($"Charging: {stopatCS.ToString()}");

                //display the data
                ReplaceContent(data);
            }
            catch (InvalidProgramException e)
            {
                Debug.LogWarning($"Something wrong happened while getting data from { selected}:{e}");
            }
            
        }
        
    }

    private void ReplaceContent(List<string> data)
    {
        //clear old data objs
        ClearContent();
        //add new data objs
        foreach (string d in data)
        {
            if (d.StartsWith("Button: "))
            {
                //add button
            }
            else
            {
                //add text
                GameObject sb = Instantiate(selTextPrefab, content);
                if (sb.GetComponent<RectTransform>() != null)
                {
                    sb.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, contentheight);
                    contentheight -= selTextHeight;
                }
                if (sb.GetComponent<TextMeshProUGUI>() != null)
                {
                    sb.GetComponent<TextMeshProUGUI>().text = d+"  ";
                }
            }
        }
        Rect cr = content.rect;
        cr.height = -contentheight;
        content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, -contentheight);
    }

    public void SetUpdating(bool upd)
    {
        if (upd)
        {
            updating = true;
        }
        else
        {
            updating = false;
            if (selected is SumoVehicle) SetDeselected();
            if (selected is SumoCStation cStat) AddCSContentXML(cStat);
        }
    }

    float fixedFrames = 0;
    private void FixedUpdate()
    {
        fixedFrames++;
        if (updating && selected != null && fixedFrames % updatereg == 0)
        {
            if (selected is SumoCStation cStat)
            {
                AddCSContent(cStat);
            }
            else if (selected is SumoVehicle car)
            {
                AddVehContent(car);
            }
        }
    }
}
