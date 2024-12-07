using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class GameSettings : MonoBehaviour
{
    public static GameSettings Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            DestroyImmediate(Instance);
            Instance = null;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SavePath = "Frames/";
        framerate = 30;
    }

    public int StartTick { get; set; }
    public string SavePath { get; set; }    
    public int framerate { get; set; }

    public void OnStartTickTextValueChanged(string text)
    {
        int st = int.Parse(text);
        StartTick = st;
    }
}
