using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackLightControl : MonoBehaviour, ITickUpdate
{
    public AnimationCurve curve;
    public int startTick = 0;

    float totalTime = 3f;

    float time;

    RayTracingMaterial mat;
    // Start is called before the first frame update
    void Awake()
    {
        Model model = GetComponent<Model>();
        mat = model.Material;
        mat.emissionStrength = 0f;
    }

    // Update is called once per frame
    public void TickUpdate(float dt, bool skip)
    {
        time += dt;

        if (time >= startTick / 30.0f)
        {
            float t = (time - startTick / 30.0f) / totalTime;

            float y = curve.Evaluate(t) * 5f;
            mat.emissionStrength = y;

        }
    }
}
