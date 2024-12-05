using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.Rendering;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TickManager : MonoBehaviour
{
    [SerializeField,Tooltip("Number of unity updates per tick")] 
    long framesPerTick = 1000;
    [SerializeField, Tooltip("Framerate of ticks which would be played as video")]
    float tickRate = 30.0f;

    [SerializeField, Tooltip("Skip ticks and starts from here")]
    int startTicks = 0;
    [SerializeField, Tooltip("Stop if total ticks reaches the max value. Set 0 for infinite")]
    int maxTicks = 0;

    [SerializeField]
    bool runOnAwake = true;

    float dt => 1 / tickRate;


    ITickUpdate[] monos;
    bool running = false;
    [SerializeField, ReadOnly]
    long counter = 0;
    [SerializeField, ReadOnly]
    long totalTicks = 0;

    private void Awake()
    {
        if (runOnAwake)
            Run();
        var allMonos = FindObjectsOfType<MonoBehaviour>();
        monos = allMonos.Where(m=>m is ITickUpdate).Select(m=>m as ITickUpdate).ToArray();
    }

    private void Start()
    {
        if(startTicks > 0)
        {
            running = false;

            int max = Mathf.Min(startTicks, maxTicks);
            Debug.Log($"Simulating ticks from 0 to {max-1}. It could take some times");
            int i = 0;
            for (; i < max; i++)
            {
                TickUpdateAll(true);

                totalTicks++;
            }
            Debug.Log($"Starting tick from {i}");
            if (totalTicks >= maxTicks)
            {

                running = false;
#if UNITY_EDITOR
                if (EditorApplication.isPlaying)
                    EditorApplication.isPaused = true;
#endif
            }
            else
                running = true;

        }
    }

    private void Update()
    {
        if (!running) return;


        counter++;
        if(counter >= framesPerTick)
        {
            counter = 0;
            TickUpdateAll(false);
            totalTicks++;
            if(totalTicks >= maxTicks) 
            {
                running = false;
#if UNITY_EDITOR
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.isPaused = true;
                }
#endif
            }
        }
    }

    void TickUpdateAll(bool skiptick)
    {
        if(monos is null) return;

        foreach (var m in monos)
            m.EarlyTickUpdate(dt, skiptick);
        foreach (var m in monos)
            m.TickUpdate(dt, skiptick);
        foreach (var m in monos)
            m.LateTickUpdate(dt, skiptick);

        Debug.Log("Total ticks: " + (totalTicks+1) + " / elapsed time: " + Time.timeSinceLevelLoad);
    }

    [Button]
    public void Run()
    {
        if(running) return;
        running = true;

    }

    [Button]
    public void Pause()
    {
        if(!running) return;
        running = false;

    }


}
