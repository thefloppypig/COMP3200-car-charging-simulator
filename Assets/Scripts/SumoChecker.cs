using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SumoChecker : MonoBehaviour
{
    TextMeshProUGUI textmesh;
    public Image image;

    public Sprite tick;
    public Sprite cross;

    public GameObject sumoPrompt;
    // Start is called before the first frame update
    void Start()
    {
        textmesh = GetComponent<TextMeshProUGUI>();
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SUMO_HOME"))) {
            textmesh.text = "Sumo has been installed correctly";
            textmesh.faceColor = Color.blue;
            if (image != null)
            {
                image.color = Color.blue;
                image.sprite = tick;
            }
        }
        else
        {
            textmesh.text = "Sumo was not detected!";
            textmesh.faceColor = Color.red;
            if (image != null) {
                image.color = Color.red;
                image.sprite = cross;
                if (sumoPrompt != null) sumoPrompt.SetActive(true);
            }
        }
    }

    public void OpenSumoWebsite()
    {
        Application.OpenURL("https://sumo.dlr.de/docs/Installing/index.html");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
