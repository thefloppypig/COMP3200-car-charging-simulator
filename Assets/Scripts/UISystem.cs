using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.IO;
using System;

public class UISystem : MonoBehaviour
{
    public GameObject uimain;
    public GameObject uiswitch;
    public SumoOptionsUI uioptions;
    public SumoCSOptionsUI uicsoptions;
    public LoadingUI uiloading;
    public SelectionUI uiselection;
    public GameObject uiinfo;
    public DataUI uidata;
    public GameObject playbutton;
    
    public Sprite imgplay;
    public Sprite imgpause;

    public TextMeshProUGUI textSimName;

    public List<Toggle> modebuttons = new List<Toggle>();

    public SumoSimulation sumoSimulation;

    Image playicon;
    TextMeshProUGUI playtext;

    // Start is called before the first frame update
    void Start()
    {
        playicon = playbutton.transform.Find("Image").GetComponent<Image>();
        playtext = playbutton.GetComponentInChildren<TextMeshProUGUI>();
        if (textSimName != null) textSimName.text = Path.GetFileName(MainMenu.selectedSimulationPath);
        uidata.gameObject.SetActive(false);
    }

    public void ToMain()
    {
        uimain.SetActive(true);
        uiswitch.SetActive(false);
        uioptions.gameObject.SetActive(false);
        uicsoptions.gameObject.SetActive(false);
        uiinfo.SetActive(false);
        uidata.gameObject.SetActive(false);
    }
    public void ToSwitch()
    {
        uiswitch.SetActive(true);
        uimain.SetActive(false);
    }
    public void ToInfo()
    {
        uimain.SetActive(false);
        uiswitch.SetActive(false);
        uioptions.gameObject.SetActive(false);
        uicsoptions.gameObject.SetActive(false);
        uiinfo.SetActive(true);
        uidata.gameObject.SetActive(false);
    }
    public void TogglePlay()
    {
        sumoSimulation.PlayButtonAction();
        if (sumoSimulation.IsSimRunning())
        {
            playicon.sprite = imgpause;
            playtext.text = "Pause";
            foreach (Toggle button in modebuttons) button.interactable = false;
        }
        else
        {
            playicon.sprite = imgplay;
            playtext.text = "Play";
        }
    }

    public void StopPlay()
    {
        playicon.sprite = imgplay;
        playtext.text = "Play";
        foreach (Toggle button in modebuttons) button.interactable = true;
        if (sumoSimulation.IsConnected()) {
            sumoSimulation.TerminateSumo();
        }
    }

    public void Edit()
    {
        sumoSimulation.OpenSumoEditor();
    }

    public void Refresh()
    {
        StopPlay();
        sumoSimulation.RefreshMap();
    }

    public void ReloadSim() {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    public void ToMainMenu() {
        SceneManager.LoadScene("MainMenu");
    }

    public void OpenOptions()
    {
        uimain.SetActive(false);
        uioptions.gameObject.SetActive(true);
        uioptions.SetOptionObject(sumoSimulation.simOptions);
    }

    public void OpenCSOptions()
    {
        uimain.SetActive(false);
        uicsoptions.gameObject.SetActive(true);
        uicsoptions.SetOptionObject(sumoSimulation.simOptions);
    }

    public void SetLoading()
    {
        uimain.SetActive(false);
        uiloading.gameObject.SetActive(true);
        uiloading.Zero();
    }

    public void LoadingProgress(float percent, string task)
    {
        uiloading.loadingText.text = task+"...";
        uiloading.progressbarTarget = percent;
    }

    public void FinishLoading()
    {
        uiloading.gameObject.SetActive(false);
        ToMain();
    }

    public void DataAnalysisButton()
    {
        sumoSimulation.StartSimAnalysis();
    }

    public void SetLoadingBarSpeed(float s)
    {
        uiloading.lerpt = s;
    }

    public void ResetLoadingBarSpeed()
    {
        uiloading.lerpt = LoadingUI.defaultLerp;
    }
    public void ToData()
    {
        uidata.gameObject.SetActive(true);
        uidata.LoadData();
        uimain.SetActive(false);
    }


    public void FinishLoadingData()
    {
        uiloading.gameObject.SetActive(false);
        uidata.gameObject.SetActive(true);
        uimain.SetActive(false);
    }

    internal void ResetData()
    {
        uidata.ResetData();
    }
}
