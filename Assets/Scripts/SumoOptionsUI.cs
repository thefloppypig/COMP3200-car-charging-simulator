using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SumoOptionsUI : MonoBehaviour
{
    SumoOptions options;

    public TMP_InputField simSpeed;
    public Toggle hideSumoWindow;
    public Toggle useSublaneModel;
    public TMP_InputField lateralResolution;

    public Toggle netConvertRefresh;
    public Toggle randomTripsRefresh;

    public TMP_InputField rTripsFringe;
    public TMP_InputField rTripsPeriod;

    public Toggle hideNetconvert;
    public Toggle hideRandomTrips;
    public Toggle waitForUserToClose;

    public void SetOptionObject(SumoOptions o)
    {
        simSpeed.text = o.simSpeed.ToString();
        hideSumoWindow.isOn = o.hideSumoWindow;
        useSublaneModel.isOn = o.useSublaneModel;
        lateralResolution.text = o.lateralResolution.ToString();

        netConvertRefresh.isOn = o.netConvertRefresh;
        randomTripsRefresh.isOn = o.randomTripsRefresh;

        rTripsFringe.text = o.rTripsFringe.ToString();
        rTripsPeriod.text = o.rTripsPeriod.ToString();

        hideNetconvert.isOn = o.hideNetconvert;
        hideRandomTrips.isOn = o.hideRandomTrips;
        waitForUserToClose.isOn = o.waitForUserToClose;


        options = o;
    }

    public void ApplyChanges()
    {
        options.simSpeed = float.Parse(simSpeed.text);
        options.hideSumoWindow = hideSumoWindow.isOn;
        options.useSublaneModel = useSublaneModel.isOn;
        options.lateralResolution= float.Parse(lateralResolution.text);

        options.netConvertRefresh=netConvertRefresh.isOn;
        options.randomTripsRefresh=randomTripsRefresh.isOn;

        options.rTripsFringe= float.Parse(rTripsFringe.text);
        options.rTripsPeriod= float.Parse(rTripsPeriod.text);

        options.hideNetconvert=hideNetconvert.isOn;
        options.hideRandomTrips = hideRandomTrips.isOn;
        options.waitForUserToClose = waitForUserToClose.isOn;

        SumoSimulation.inst.SaveSumoOptions();
    }
}
