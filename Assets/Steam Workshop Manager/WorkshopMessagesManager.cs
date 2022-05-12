using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorkshopMessagesManager : MonoBehaviour
{
    public UnityEngine.UI.Text MessagesText;

    static WorkshopMessagesManager _Instance;

    // Start is called before the first frame update
    void Awake()
    {
        _Instance = this;
    }

    public static void SetMessageText(string text)
    {
        SetMessageText(text, Color.white);
    }
    public static void SetMessageText(string text, Color color)
    {
        if ((_Instance == null) || (_Instance.MessagesText == null))
        {
            return;
        }

        _Instance.MessagesText.text = text;
        _Instance.MessagesText.color = color;
    }
}
