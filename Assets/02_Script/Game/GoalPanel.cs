using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GoalPanel : MonoBehaviour
{
    public Image goalImage;
    public Sprite goalSprite;
    public Text goalText;
    public string textString;

    void Start()
    {
        SetInfo();
    }

    void SetInfo()
    {
        goalImage.sprite = goalSprite;
        goalText.text = textString;
    }
}
