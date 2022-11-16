using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.IO;
using UnityEngine.SceneManagement;

public class MenuButton : MonoBehaviour
{
    [SerializeField] string simpath;
    public TextMeshProUGUI textname;
    public TextMeshProUGUI textcreate;

    public void SetPath(string path)
    {
        simpath = path;
        textname.text = (Path.GetFileName(simpath).Equals("")) ? Path.GetFileName(Path.GetDirectoryName(simpath)) : (Path.GetFileName(simpath));
        textcreate.text = Directory.GetLastWriteTime(simpath).ToString();
    }

    public void ClickSelectedSimulation()
    {
        MainMenu.selectedSimulationPath = simpath;
        SceneManager.LoadScene("SumoUnity");
    }

}
