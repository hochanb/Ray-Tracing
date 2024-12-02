using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TickManager : MonoBehaviour
{
    [SerializeField,Tooltip("Number of unity updates per tick")] 
    long framesPerTick = 1000;
    [SerializeField, Tooltip("Framerate of ticks which would be played as video")]
    float tickRate = 30.0f;

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

    private void Update()
    {
        if (!running) return;


        counter++;
        if(counter >= framesPerTick)
        {
            counter = 0;
            TickUpdateAll();
            totalTicks++;
            if(totalTicks >= maxTicks) 
            {
                running = false;
            }
        }
    }

    void TickUpdateAll()
    {
        if(monos is null) return;

        foreach (var m in monos)
            m.EarlyTickUpdate(dt);
        foreach (var m in monos)
            m.TickUpdate(dt);
        foreach (var m in monos)
            m.LateTickUpdate(dt);

        Debug.Log("Total ticks: " + totalTicks + " / elapsed time: " + Time.timeSinceLevelLoad);
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
