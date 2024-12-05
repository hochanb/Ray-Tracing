using UnityEngine;


/// <summary>
/// User asset data structure
/// </summary>
[System.Serializable]
public class RayTracingMaterial
{

	[Header("Basics")]
	public Color colour;
	public Color emissionColour;
	public Color specularColour;
	public float emissionStrength;
	[Range(0, 1)] public float smoothness;
	[Range(0, 1)] public float specularProbability;
	public MaterialFlag flag;

	// extended (Be sure to match with structure that has been defined in RayTracer.shader)

	//[Header("Extended")]
	// Add other properties
	[Range(0, 1)] public float transparency;    // 0 is full opaque

	[Range(0, 3)] public float eta;

	// texture indices
	public Texture2D albedoTex;
	public Texture2D normalTex;
	public Texture2D roughnessTex;
	[Range(0, 10)] public float density; // 1 is fully dense


	public int albedoIdx { get; set; }
	public int normalIdx { get; set; }
	public int roughnessIdx { get; set; }

	public void SetDefaultValues()
	{
		colour = Color.white;
		emissionColour = Color.white;
		emissionStrength = 0;
		specularColour = Color.white;
		smoothness = 0;
		specularProbability = 0;
		transparency = 0;
		eta = 1;
		density = 0.5f;
	}

}
public enum MaterialFlag
{
    None,
    CheckerPattern,
    InvisibleLight,
    Fog
}

/// <summary>
/// Render data structure to pass into pipeline
/// Must match with 
/// </summary>
public struct RTMatData
{

    public readonly Color colour;
    public readonly Color emissionColour;
    public readonly Color specularColour;
    public readonly float emissionStrength;
    public readonly  float smoothness;
    public readonly float specularProbability;
	public readonly MaterialFlag flag;
    public readonly float transparency;    
    public readonly float eta;
	public int albetoTex;
	public int normalTex;
	public int roughnessTex;

	public RTMatData(RayTracingMaterial rtMat)
	{
		colour = rtMat.colour;
		emissionColour = rtMat.emissionColour;
		specularColour = rtMat.specularColour;
		emissionColour = rtMat.emissionColour;
		emissionStrength = rtMat.emissionStrength;
		smoothness = rtMat.smoothness;
		specularProbability = rtMat.specularProbability;
		flag = rtMat.flag;
		transparency = rtMat.transparency;
		eta= rtMat.eta;
		// texture indices should be mapped by raytracing manager
		albetoTex = rtMat.albedoIdx;
		normalTex = rtMat.normalIdx;
		roughnessTex = rtMat.roughnessIdx;
	}
}