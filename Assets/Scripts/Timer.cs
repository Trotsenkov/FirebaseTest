using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class Timer : MonoBehaviour
{
    public float timer = 0;
    public bool started = false;

    Button RunButton;
    Button StopButton;
    Button SaveButton;
    TMP_Text text;
	GameObject SavePanel;
    TMP_InputField save_input;
    TMP_Text save_text;
    Transform  NotesContent;
    GameObject Note;
    int count = 0;

    void Start()
    {
        text = transform.Find("Timer Text").GetComponent<TMP_Text>();
        SavePanel = transform.Find("Save Panel").gameObject;
        save_text = SavePanel.transform.Find("Panel").Find("Save Timer Text").GetComponent<TMP_Text>();
        save_input = SavePanel.transform.Find("Panel").Find("Input Field").GetComponent<TMP_InputField>();
        RunButton = transform.Find("Run").GetComponent<Button>();
        StopButton = transform.Find("Stop").GetComponent<Button>();
        SaveButton = transform.Find("Save").GetComponent<Button>();
        NotesContent = transform.Find("Notes").Find("Scroll View").Find("Viewport").Find("Content");
        Note = Resources.Load("Note") as GameObject;

        string note;
        while((note = PlayerPrefs.GetString(count.ToString(), "")) != "")
            CreateNote(float.Parse(note.Split('')[0]), note.Split('')[1]);
    }
	 
	public void Stop() {
		started = false;
        RunButton.interactable = true;
        StopButton.interactable = false;
        SaveButton.interactable = true;
    }
	 
	public void Run() {
		timer = 0;
		started = true;
        RunButton.interactable = false;
        StopButton.interactable = true;
        SaveButton.interactable = false;
	}

	public void Save()
	{
		SavePanel.SetActive(true);

        TimeSpan t = TimeSpan.FromSeconds(timer);
        save_text.text = ((t.Hours > 0) ? (t.Hours).ToString() + ":" : "") + t.Minutes.ToString() + ":" + t.Seconds.ToString("D2") + "." + ((int)t.Milliseconds / 10).ToString();
    }

    public void Save_OK()
    {
        SavePanel.SetActive(false);
        RunButton.interactable = true;
        StopButton.interactable = false;
        SaveButton.interactable = false;

        PlayerPrefs.SetString(count.ToString(), timer.ToString() + '' + save_input.text);
        CreateNote(timer, save_input.text);
        timer = 0;
    }

    void CreateNote(float timer, string note)
    {
        var n = Instantiate(Note, NotesContent);
        (n.transform as RectTransform).position -= new Vector3(0, 140 * count++);
        TimeSpan t = TimeSpan.FromSeconds(timer);
        n.GetComponent<TMP_InputField>().text = ((t.Hours > 0) ? (t.Hours).ToString() + ":" : "") + t.Minutes.ToString() + ":" + t.Seconds.ToString("D2") + "." + ((int)t.Milliseconds / 10).ToString() + " - " + note;
        (NotesContent as RectTransform).sizeDelta += new Vector2(0, 140);

    }

    void Update()
    {
		if (started)
		{
			timer += Time.deltaTime;

            TimeSpan t = TimeSpan.FromSeconds(timer);
            text.text = ((t.Hours > 0) ? (t.Hours).ToString() + ":" : "") + t.Minutes.ToString() + ":" + t.Seconds.ToString("D2") + "." + ((int)t.Milliseconds / 10).ToString();
		}
    }
}
