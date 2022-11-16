using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadingUI : MonoBehaviour
{
    public static float defaultLerp = 0.1f;
    public TextMeshProUGUI loadingText;
    public Image progressbar;
    public float progressbarTarget;
    public float lerpt;
    private void Start()
    {
        lerpt = defaultLerp;
    }
    void FixedUpdate()
    {
        progressbar.fillAmount = Mathf.Lerp(progressbar.fillAmount, progressbarTarget, lerpt);
    }

    public void Zero()
    {
        progressbar.fillAmount = 0;
    }
}
