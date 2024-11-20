using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New RT Material",menuName = "RT Material")]
public class MaterialAsset : ScriptableObject
{
    public RayTracingMaterial material;

    private void Reset()
    {
        material = new RayTracingMaterial();
        material.SetDefaultValues();
    }
}
