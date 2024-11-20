using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialGroups : MonoBehaviour
{
    public Model[] models;
    [Expandable]
    public MaterialAsset materialAsset;

    [Button]
    private void Apply()
    {
        if(models != null && materialAsset != null)
            foreach (var model in models)
                model.SetMaterialAsset(materialAsset);
    }
}
