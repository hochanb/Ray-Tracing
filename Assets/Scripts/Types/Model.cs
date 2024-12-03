using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Model : MonoBehaviour
{
    public MeshFilter meshFilter;
    public bool logBVHStats;

    [Expandable]
    public MaterialAsset materialAsset;
    public RayTracingMaterial Material => materialAsset is not null ? materialAsset.material : new RayTracingMaterial();

    public MeshRenderer meshRenderer;
    [SerializeField, HideInInspector] int materialObjectID;

    public Mesh Mesh => meshFilter.sharedMesh;

    private void OnValidate()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        SetUpMaterialDisplay();
    }

    void SetUpMaterialDisplay()
    {
        if (gameObject.GetInstanceID() != materialObjectID)
        {
            materialObjectID = gameObject.GetInstanceID();
            Material[] originalMaterials = meshRenderer.sharedMaterials;
            Material[] newMaterials = new Material[originalMaterials.Length];
            Shader shader = Shader.Find("Standard");
            for (int i = 0; i < meshRenderer.sharedMaterials.Length; i++)
            {
                newMaterials[i] = new Material(shader);
            }
            meshRenderer.sharedMaterials = newMaterials;
        }


        RayTracingMaterial mat = Material;
        bool displayEmissiveCol = mat.colour.maxColorComponent < mat.emissionColour.maxColorComponent * mat.emissionStrength;
        Color displayCol = displayEmissiveCol ? mat.emissionColour * mat.emissionStrength : mat.colour;
        if(meshRenderer ==null) 
            meshRenderer = GetComponent<MeshRenderer>();
        if(meshRenderer !=null && meshRenderer.sharedMaterial != null)
        {
            var sm = meshRenderer.sharedMaterial;
            sm.color = displayCol;
            sm.SetColor("_EmissionColor", mat.emissionColour * mat.emissionStrength); // Emission color
            sm.SetFloat("_Glossiness", mat.smoothness); // Smoothness (Glossiness in Standard Shader)
            sm.SetFloat("_Metallic", mat.specularProbability); // Metallic (maps specular probability for roughness/metallic workflow)

            // Set textures
            sm.SetTexture("_MainTex", mat.albedoTex); // Albedo texture
            sm.SetTexture("_BumpMap", mat.normalTex); // Normal map
            sm.SetTexture("_MetallicGlossMap", mat.roughnessTex); // Metallic/Glossiness map

        }
    }

    public void SetMaterialAsset(MaterialAsset mat)
    {
        materialAsset = mat;
        OnValidate();
    }
}
