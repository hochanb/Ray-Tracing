using UnityEngine;

[System.Serializable]
public struct RayTracingMaterial
{
	public enum MaterialFlag
	{
		None,
		CheckerPattern,
		InvisibleLight
	}

	[Header("Basics")]
	public Color colour;
	public Color emissionColour;
	public Color specularColour;
	public float emissionStrength;
	[Range(0, 1)] public float smoothness;
	[Range(0, 1)] public float specularProbability;
	public MaterialFlag flag;

	// extended (Be sure to match with structure that has been defined in RayTracer.shader)

	[Header("Extended")]
	// Add other properties
	[Range(0, 1)] public float transparency;    // 1 is full opaque



	public void SetDefaultValues()
	{
		colour = Color.white;
		emissionColour = Color.white;
		emissionStrength = 0;
		specularColour = Color.white;
		smoothness = 0;
		specularProbability = 1;
		//transparency = 1;
	}
}