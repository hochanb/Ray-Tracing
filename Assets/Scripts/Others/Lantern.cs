using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lantern : MonoBehaviour,ITickUpdate
{
    public AnimationCurve curve;
    public float emmisionTo = 5f;
    public int startTick = 0;

    float totalTime = 5f;

    float time;

    RayTracingMaterial mat;
    // Start is called before the first frame update
    void Awake()
    {
        Model model = GetComponent<Model>();
        mat = model.Material;
        mat.emissionStrength = 0;
    }

    // Update is called once per frame
    public void TickUpdate(float dt, bool skip)
    {
        time += dt;

        if(time >= startTick / 30.0f)
        {
            float t = (time - startTick / 30.0f) / totalTime;

            float y = curve.Evaluate(t) * 100f;
            mat.emissionStrength = y;

        }
    }
}
