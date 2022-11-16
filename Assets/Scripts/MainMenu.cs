using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TMPro;
using SimpleFileBrowser;
using System;

public class MainMenu : MonoBehaviour
{
    public static string selectedMenuPath;
    public static string selectedSimulationPath;
    public TextMeshProUGUI textPath;
    public RectTransform content;

    private const float buttonHeight = 300;
    List<GameObject> simbuttons;
    public GameObject simButtonPrefab;

    public TMP_InputField textCreate;

    public TextAsset sumocfg;
    public TextAsset sumonet;
    public TextAsset sumorou;
    public TextAsset sumoadd;

    // Start is called before the first frame update
    void Start()
    {
        SumoSimulation.inst = null;
        simbuttons = new List<GameObject>();
        if (selectedMenuPath == null)
        {
            if (PlayerPrefs.HasKey("lastpath"))
            {
                SetTextPath(PlayerPrefs.GetString("lastpath"));
            }
            else
            {
                SetTextPath(Directory.GetCurrentDirectory());
            }
        }
        else SetTextPath(selectedMenuPath);
    }

    void SetTextPath(string str)
    {
        ClearButtons();
        if (!str.EndsWith("\\")) str = str + "\\";
        selectedMenuPath = str;
        PlayerPrefs.SetString("lastpath", selectedMenuPath);
        if (textPath != null) textPath.text = selectedMenuPath;
        if (content != null) PopulateContent();
    }

    void ReloadContent()
    {
        SetTextPath(selectedMenuPath);
    }

    private void ClearButtons()
    {
        foreach (GameObject button in simbuttons) Destroy(button);
        simbuttons.Clear();
        Rect contentRect = content.rect;
        contentRect.height = buttonHeight;
    }

    public void PopulateContent()
    {
        //find all simulations
        List<string> confirmedFolders = new List<string>();
        if (Directory.GetFiles(selectedMenuPath, "map.sumocfg").Length != 0)
        {
            //has matching simulation files in main folder
            confirmedFolders.Add(selectedMenuPath);
            Debug.Log("found a simulation: " + selectedMenuPath);
        }
        string[] potentialFolders = Directory.GetDirectories(selectedMenuPath);
        foreach (string folder in potentialFolders)
        {
            if (Directory.GetFiles(folder, "map.sumocfg").Length != 0)
            {
                //has matching simulation files in subfolders
                confirmedFolders.Add(folder);
                Debug.Log("found a simulation: " + folder);
            }
        }

        //addbuttons
        content.sizeDelta = new Vector2(content.sizeDelta.x, confirmedFolders.Count * buttonHeight);
        float i=0;
        foreach (string simfolder in confirmedFolders)
        {
            //create panel in content for each found simulation
            GameObject sb = Instantiate(simButtonPrefab, content);
            simbuttons.Add(sb);
            if (sb.GetComponent<RectTransform>()!=null)
            {
                sb.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, i);
                i -= buttonHeight;
            }
            if (sb.GetComponent<MenuButton>() != null)
            {
                MenuButton mb = sb.GetComponent<MenuButton>();
                mb.SetPath(simfolder);
            }
        }
    }

    public void OpenFileBrowser()
    {
        FileBrowser.ShowLoadDialog(SetMenuPath, null, FileBrowser.PickMode.Folders, false, selectedMenuPath, null, "Load Simulation Folder", "Select");
    }


    void SetMenuPath(string[] paths)
    {
        if (paths.Length > 0) SetTextPath(paths[0]);
    }

    void CreateNewSim(string simName)
    {
        selectedSimulationPath = selectedMenuPath +"\\" + simName + "\\";
        if (!Directory.Exists(selectedSimulationPath))
        {
            try
            {
                Debug.Log($"Creating simulation with path: {selectedSimulationPath}");
                Directory.CreateDirectory(selectedSimulationPath);
                //create files
                File.WriteAllBytes(selectedSimulationPath+ "map.sumocfg", sumocfg.bytes);
                File.WriteAllBytes(selectedSimulationPath+ "map.net.xml", sumonet.bytes);
                File.WriteAllBytes(selectedSimulationPath+ "map.trips.xml", sumorou.bytes);
                File.WriteAllBytes(selectedSimulationPath+ "map.add.xml", sumoadd.bytes);

                //reload menu to display content with new sim
                ReloadContent();
            }
            catch (Exception e)
            {
                Debug.Log("Could not create directory " + simName + ": " + e.Message); ;
            }
        }
        else
        {
            Debug.Log("Could not create directory "+ simName+": Already exists");
        }
    }

    public void CreateButtonClick()
    {
        CreateNewSim(textCreate.text);
    }

    public void Quit()
    {
        Application.Quit();
        Debug.Log("Application was closed");
    }
}
