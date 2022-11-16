using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfoUI : MonoBehaviour
{
    public RectTransform content;

    public float viewHeight;
    public float editHeight;
    public float anaHeight;

    public void GoToView()
    {
        GoTo(viewHeight);

    }
    public void GoToEdit()
    {
        GoTo(editHeight);

    }
    public void GoToAnalyse()
    {
        GoTo(anaHeight);
    }
    void GoTo(float to)
    {
        Vector2 pos = content.anchoredPosition;
        pos.y = to;
        content.anchoredPosition = pos;
    }


}
