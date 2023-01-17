using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public static class Message
{
    static TMP_Text text;

    static Message()
    {
        text = GameObject.Find("CanvasMessage").transform.Find("Text").GetComponent<TMP_Text>();
    }

    public static void Send(string message)
    {
        text.text = message;
    }

    public static  void SetActive(bool state)
    {
        text.gameObject.SetActive(state);
    }
}
