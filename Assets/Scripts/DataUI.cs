using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEngine;
using TMPro;
using Unity.Jobs;
using System.Threading;
using System.Text.RegularExpressions;

public class DataUI : MonoBehaviour
{
    public GameObject allDataObject;
    public RectTransform content;
    public TextMeshProUGUI currentText;
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI totalVehText;
    public TextMeshProUGUI teleJamText;
    public TextMeshProUGUI teleYieldText;
    public TextMeshProUGUI teleWrongText;
    public TextMeshProUGUI teleTotalText;
    public TextMeshProUGUI safCollText;
    public TextMeshProUGUI safEBText;
    public TextMeshProUGUI totalCstationsText;
    public TextMeshProUGUI totalVehicleChargedText;
    public TextMeshProUGUI totalEnergyText;
    public TextMeshProUGUI totalUptimeText;

    float totalTasks = 7f;
    float currentTask = 1f;
    string currentTaskS = "";

    bool isLoading = false;

    static string simPath;
    UISystem uISystem;
    SumoOptions opt;

    string log = "";
    XmlDocument stat;
    XmlDocument cgs;
    XmlDocument batt;
    float totalTime = 0;
    int countCS = 0;
    int countVCharged = 0;
    float totalEnergy = 0;
    float totalUptime = 0;
    List<DataPane> dataPanes = new List<DataPane>();

    public GameObject dataPanePrefab;

    public void LoadData()
    {
        try
        {
            StartCoroutine(LoadDataRoutine());
        }
        catch (Exception e)
        {

            throw e;
        }

    }

    IEnumerator LoadDataRoutine()
    {
        opt = SumoSimulation.inst.simOptions;
        uISystem = SumoSimulation.inst.uISystem;
        if ("None".Equals(opt.dataTime))
        {
            allDataObject.SetActive(false);
            SetCurrentText("None");
        }
        else
        {
            simPath = SumoSimulation.inst.sumoSimPath;
            //get the data to use
            if (stat != null && cgs != null && batt != null)
            {
                //show data now
                DisplayData();
            }
            else
            {
                //get the data
                uISystem.SetLoading();
                uISystem.SetLoadingBarSpeed(0.001f);
                isLoading = true;
                Thread t = new Thread(LoadingData);
                t.Start();
                currentTask = 0f; currentTaskS = "Reading Data";
                while (isLoading)
                {
                    uISystem.LoadingProgress(currentTask / totalTasks, currentTaskS);
                    yield return null;
                }
                uISystem.ResetLoadingBarSpeed();
                uISystem.LoadingProgress(totalTasks / totalTasks, "Done");
                yield return new WaitForSeconds(0.2f);
                uISystem.FinishLoadingData();


                DisplayData();
            }
        }
    }

    void LoadingData()
    {
        try
        {
            currentTask = 1f; currentTaskS = "Reading Log Data";
            log = File.ReadAllText(simPath + "map.log");

            stat = new XmlDocument();
            cgs = new XmlDocument();
            batt = new XmlDocument();

            currentTask = 2f; currentTaskS = "Reading Stat Data";
            stat.Load(simPath + "map.stat.log");

            currentTask = 3f; currentTaskS = "Reading Charging Data";
            cgs.Load(simPath + "map.cgs.xml");

            currentTask = 4f; currentTaskS = "Reading Battery Data";
            //batt.Load(simPath + "map.batt.xml");

            currentTask = 5f; currentTaskS = "Calculating Data";
        }
        catch (Exception e)
        {
            Debug.LogWarning("Error reading data: "+e);
        }

        if (cgs !=null)
        {
            try
            {
                XmlNodeList listcgs = cgs.SelectNodes("chargingstations-export/chargingStation");
                countCS = listcgs.Count;
                XmlNodeList listveh = cgs.SelectNodes("chargingstations-export/chargingStation/vehicle[@totalEnergyChargedIntoVehicle>30]");
                countVCharged = listveh.Count;
                totalEnergy = 0;
                totalUptime = 0;
                dataPanes.Clear();
                try
                {
                    Match m = Regex.Match(log, @"(Simulation ended at time: )(\d+.\d+)");
                    totalTime = float.Parse(m.Value.Replace("Simulation ended at time: ", ""));
                }
                catch (Exception e) { Debug.Log("Unable to get Simulation time: " + e); }

                foreach (XmlElement nodecgs in listcgs)
                {
                    if (nodecgs != null && nodecgs.HasAttribute("id"))
                    {
                        string id = nodecgs.GetAttribute("id");
                        int vehno = 0;
                        float energy = 0;
                        float chargingtime = 0;
                        float uptime = 0;
                        try {
                            energy = float.Parse(nodecgs.GetAttribute("totalEnergyCharged"));
                            totalEnergy += energy; 
                        } catch (Exception e) { Debug.Log("Exception while adding total energy: " + e); }
                        try
                        {
                            chargingtime = float.Parse(nodecgs.GetAttribute("chargingSteps"));
                            uptime = chargingtime / totalTime * 100;
                            totalUptime += uptime;
                        } catch (Exception e) { Debug.Log("Exception while adding calculating uptime: " + e); }
                        XmlNodeList vehsAtCgs = nodecgs.SelectNodes("vehicle[@totalEnergyChargedIntoVehicle>30]");
                        vehno = vehsAtCgs.Count;

                        DataPane data = new DataPane(id, vehno, energy, chargingtime, uptime);
                        dataPanes.Add(data);
                    }
                }
                totalUptime = totalUptime/countCS;
            }
            catch (Exception)
            {

                throw;
            }
        }

        isLoading = false;
    }

    void SetCurrentText(string text)
    {
        currentText.text = "Current Data: " + text;
    }

    void DisplayData()
    {
        allDataObject.SetActive(true);

        SetCurrentText(opt.dataTime);

        timeText.text = $"Simulation time: {totalTime.ToString("F1")}s";

        XmlElement sNodeVeh = stat.SelectSingleNode("statistics/vehicles") as XmlElement;
        XmlElement sNodeTele = stat.SelectSingleNode("statistics/teleports") as XmlElement;
        XmlElement sNodeSaf = stat.SelectSingleNode("statistics/safety") as XmlElement;
        try { totalVehText.text = $"Vehicles Loaded: {sNodeVeh.GetAttribute("loaded")}"; } catch (Exception e) { Debug.Log("Unable to get Vehicles Loaded: " + e); }
        
        try { teleJamText.text = $"Jams: {sNodeTele.GetAttribute("jam")}"; } catch (Exception e) { Debug.Log("Unable to get Jams: " + e); }
        try { teleYieldText.text = $"Yields: {sNodeTele.GetAttribute("yield")}"; } catch (Exception e) { Debug.Log("Unable to get Yield: " + e); }
        try { teleWrongText.text = $"Wrong Lane: {sNodeTele.GetAttribute("wrongLane")}"; } catch (Exception e) { Debug.Log("Unable to get Wrong Lane: " + e); }
        try { teleTotalText.text = $"Total: {sNodeTele.GetAttribute("total")}"; } catch (Exception e) { Debug.Log("Unable to get Total: " + e); }

        try { safCollText.text = $"Collisions: {sNodeSaf.GetAttribute("collisions")}"; } catch (Exception e) { Debug.Log("Unable to get Collisions: " + e); }
        try { safEBText.text = $"Emergency Brakes: {sNodeSaf.GetAttribute("emergencyStops")}"; } catch (Exception e) { Debug.Log("Unable to get Emergency Brakes: " + e); }

        try { totalCstationsText.text = $"Total Number of Charging Stations:  {countCS.ToString()}"; } catch (Exception e) { Debug.Log("Unable to get Total Number of Charging Stations: " + e); }
        try { totalVehicleChargedText.text = $"Total Number of Vehicles Charged:  {countVCharged.ToString()}"; } catch (Exception e) { Debug.Log("Unable to get Total Number of Vehicles Charged: " + e); }
        try { totalEnergyText.text = $"Total Energy Charged into Vehicles:   {totalEnergy.ToString("F1")}Wh"; } catch (Exception e) { Debug.Log("Unable to get Total Energy Charged into Vehicles: " + e); }
        try { totalUptimeText.text = $"Average Charging Uptime:   {totalUptime.ToString("F1")}%"; } catch (Exception e) { Debug.Log("Unable to get Average Charging Uptime: " + e); }

        AddDataPanes();
    }

    internal void ResetData()
    {
        log = "";

        stat = null;
        cgs = null;
        batt = null;
    }

    struct DataPane
    {
        public string id;
        public int vehNo;
        public float energyCharged;
        public float chargingTime;
        public float uptime;

        public DataPane(string id, int vehNo, float energyCharged, float chargingTime, float uptime)
        {
            this.id = id;
            this.vehNo = vehNo;
            this.energyCharged = energyCharged;
            this.chargingTime = chargingTime;
            this.uptime = uptime;
        }
    }

    void AddDataPanes()
    {
        float height = -1500;
        foreach (DataPane dp in dataPanes)
        {
            //add datapanes
            try
            {
                GameObject dataObject = Instantiate(dataPanePrefab, allDataObject.transform);
                dataObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, height);
                DataPaneUI datapaneui = dataObject.GetComponent<DataPaneUI>();
                datapaneui.SetValues(dp.id, dp.vehNo, dp.energyCharged, dp.chargingTime, dp.uptime);

                height += -600;
            }
            catch (Exception e)
            {

                Debug.Log("Error adding datapane " + dp.id + ": " + e);
            }
        }
        content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, -height+600);
    }
}