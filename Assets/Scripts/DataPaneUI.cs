using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DataPaneUI : MonoBehaviour
{
    public TextMeshProUGUI csTitle;
    public TextMeshProUGUI vehTxt;
    public TextMeshProUGUI energyTxt;
    public TextMeshProUGUI chargingTxt;
    public TextMeshProUGUI uptimeTxt;

    public void SetValues(string id, int vehNo, float energyCharged, float chargingTime, float uptime)
    {
        csTitle.text = $"Data for {id}";
        vehTxt.text = $"Number of Vehicles Charged: {vehNo}";
        energyTxt.text = $"Energy Charged into Vehicles: {energyCharged.ToString("F2")}Wh";
        chargingTxt.text = $"Time Spent Charging: {chargingTime.ToString("F2")}s";
        uptimeTxt.text = $"Charging Uptime: {uptime.ToString("F1")}%";
    }

    private void OnDisable()
    {
        Destroy(gameObject);
    }
}
