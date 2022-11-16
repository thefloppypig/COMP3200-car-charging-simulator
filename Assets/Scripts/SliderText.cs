using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SliderText : MonoBehaviour
{
    public Slider slider;
    public TextMeshProUGUI tmp;

    public void UpdateText()
    {
        tmp.text = slider.value.ToString("P1");
    }
}
