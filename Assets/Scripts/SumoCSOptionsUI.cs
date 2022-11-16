using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SumoCSOptionsUI : MonoBehaviour
{
    SumoOptions options;

    public Toggle csStopsEnabled;
    public Slider csCarsProbabilityS;
    public TextMeshProUGUI csCarsProbability;
    public TMP_InputField csStopDurationMin;
    public TMP_InputField csStopDurationMax;

    public Toggle csSetValues;
    public TMP_InputField csPowerValue;
    public Slider csEfficiencyS;
    public TextMeshProUGUI csEfficiency;




    public void SetOptionObject(SumoOptions o)
    {
        csStopsEnabled.isOn = o.csStopsEnabled;
        csCarsProbabilityS.value = o.csCarsProbability;
        csCarsProbability.text = o.csCarsProbability.ToString("P1");
        csStopDurationMin.text = o.csStopDurationMin.ToString();
        csStopDurationMax.text = o.csStopDurationMax.ToString();

        csSetValues.isOn = o.csSetValues;
        csPowerValue.text = o.csPowerValue.ToString();
        csEfficiencyS.value = o.csEfficiency;
        csEfficiency.text = o.csEfficiency.ToString("P1");

        options = o;
    }

    public void ApplyChanges()
    {
        options.csStopsEnabled = csStopsEnabled.isOn;
        options.csCarsProbability = csCarsProbabilityS.value;
        options.csStopDurationMin = float.Parse(csStopDurationMin.text);
        options.csStopDurationMax = float.Parse(csStopDurationMax.text);
        if (options.csStopDurationMax > options.csStopDurationMin) options.csStopDurationMin = options.csStopDurationMax;

        options.csSetValues = csSetValues.isOn;
        options.csEfficiency = csEfficiencyS.value;
        options.csPowerValue = float.Parse(csPowerValue.text);

        SumoSimulation.inst.SaveSumoOptions();
    }
}
