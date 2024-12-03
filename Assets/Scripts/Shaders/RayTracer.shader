Shader"Custom/RayTracer"
{
	SubShader
	{
Cull Off
ZWrite Off
ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma require 2darray
#include "UnityCG.cginc"
			#pragma multi_compile _ DEBUG_VIS


struct appdata
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
};

struct v2f
{
    float2 uv : TEXCOORD0;
    float4 vertex : SV_POSITION;
};

v2f vert(appdata v)
{
    v2f o;
    o.vertex = UnityObjectToClipPos(v.vertex);
    o.uv = v.uv;
    return o;
}

			// --- Settings and constants ---
static const float PI = 3.1415;

			// Raytracing Settings
int MaxBounceCount;
int NumRaysPerPixel;
int Frame;

			// Camera settings
float DefocusStrength;
float DivergeStrength;
float3 ViewParams;
float4x4 CamLocalToWorldMatrix;

			// Sky settings
int UseSky;
float3 SunColour;
float SunFocus = 500;
float SunIntensity = 10;

			// Debug settings
int visMode;
float debugVisScale;

			// Textures
			//Texture2DArray AlbedoTextures;
			//Texture2DArray NormalTextures;
			//Texture2DArray RoughnessTextures;
			UNITY_DECLARE_TEX2DARRAY(AlbedoTextures);
			UNITY_DECLARE_TEX2DARRAY(NormalTextures);
			UNITY_DECLARE_TEX2DARRAY(RoughnessTextures);
			//SamplerState Sampler;

			// --- Structures ---
struct Ray
{
    float3 origin;
    float3 dir;
    float3 invDir;
};

struct Triangle
{
    float3 posA, posB, posC;
    float3 normA, normB, normC;
    float3 tanA, tanB, tanC;
    float2 uvA, uvB, uvC;
};

struct TriangleHitInfo
{
    bool didHit;
    float dst;
    float3 hitPoint;
    float3 normal;
    float3 tangent;
    float3 bitangent;
    int triIndex;
    bool hitFront;
    float2 uv;
};

struct RTMatData
{
    float4 colour;
    float4 emissionColour;
    float4 specularColour;
    float emissionStrength;
    float smoothness;
    float specularProbability;
    int flag;
				// extended (Be sure to match with structure that has been defined in C#)
    float transparency;
    float eta;
				// texture index
    int albedoTex;
    int normalTex;
    int roughnessTex;
};

struct Model
{
    int nodeOffset;
    int triOffset;
    float4x4 worldToLocalMatrix;
    float4x4 localToWorldMatrix;
    RTMatData material;
};

struct BVHNode
{
    float3 boundsMin;
    float3 boundsMax;
				// index refers to triangles if is leaf node (triangleCount > 0)
				// otherwise it is the index of the first child node
    int startIndex;
    int triangleCount;
};

struct ModelHitInfo
{
    bool didHit;
    float3 normal;
    float2 uv;
    float3 hitPoint;
    float dst;
    RTMatData material;
    bool hitFront;
};

			// --- Buffers (and their sizes) ---	
StructuredBuffer<Model> ModelInfo;
StructuredBuffer<Triangle> Triangles;
StructuredBuffer<BVHNode> Nodes;
int triangleCount;
int modelCount;

			// ---- RNG Functions ----

			// PCG (permuted congruential generator). Thanks to:
			// www.pcg-random.org and www.shadertoy.com/view/XlGcRh
uint NextRandom(inout uint state)
{
    state = state * 747796405 + 2891336453;
    uint result = ((state >> ((state >> 28) + 4)) ^ state) * 277803737;
    result = (result >> 22) ^ result;
    return result;
}

float RandomValue(inout uint state)
{
    return NextRandom(state) / 4294967295.0; // 2^32 - 1
}

			// Random value in normal distribution (with mean=0 and sd=1)
float RandomValueNormalDistribution(inout uint state)
{
				// Thanks to https://stackoverflow.com/a/6178290
    float theta = 2 * 3.1415926 * RandomValue(state);
    float rho = sqrt(-2 * log(RandomValue(state)));
    return rho * cos(theta);
}

			// Calculate a random direction
float3 RandomDirection(inout uint state)
{
				// Thanks to https://math.stackexchange.com/a/1585996
    float x = RandomValueNormalDistribution(state);
    float y = RandomValueNormalDistribution(state);
    float z = RandomValueNormalDistribution(state);
    return normalize(float3(x, y, z));
}

float2 RandomPointInCircle(inout uint rngState)
{
    float angle = RandomValue(rngState) * 2 * PI;
    float2 pointOnCircle = float2(cos(angle), sin(angle));
    return pointOnCircle * sqrt(RandomValue(rngState));
}

			// Crude sky colour function for background light
float3 GetEnvironmentLight(float3 dir)
{
    if (UseSky == 0)
        return 0;
    const float3 GroundColour = float3(0.35, 0.3, 0.35);
    const float3 SkyColourHorizon = float3(1, 1, 1);
    const float3 SkyColourZenith = float3(0.08, 0.37, 0.73);
				

    float skyGradientT = pow(smoothstep(0, 0.4, dir.y), 0.35);
    float groundToSkyT = smoothstep(-0.01, 0, dir.y);
    float3 skyGradient = lerp(SkyColourHorizon, SkyColourZenith, skyGradientT);
    float sun = pow(max(0, dot(dir, _WorldSpaceLightPos0.xyz)), SunFocus) * SunIntensity;
				// Combine ground, sky, and sun
    float3 composite = lerp(GroundColour, skyGradient, groundToSkyT) + sun * SunColour * (groundToSkyT >= 1);
    return composite;
}

float3 SampleAndTransformNormalMap(float3 uv3, float3 tangent, float3 bitangent, float3 normal)
{
				// Sample the normal map texture
    float3 tangentSpaceNormal = UNITY_SAMPLE_TEX2DARRAY(NormalTextures, uv3).rgb;
    tangentSpaceNormal = tangentSpaceNormal * 2.0f - 1.0f;
    float3x3 tbn = float3x3(tangent, bitangent, normal);
    
    return normalize(mul(tangentSpaceNormal,tbn));
}

// --- Ray Intersection Functions ---

// Calculate the intersection of a ray with a triangle using M?ler�Trumbore algorithm
// Thanks to https://stackoverflow.com/a/42752998
TriangleHitInfo RayTriangle(Ray ray, Triangle tri)
{
    float3 edgeAB = tri.posB - tri.posA;
    float3 edgeAC = tri.posC - tri.posA;
    float3 normalVector = cross(edgeAB, edgeAC);
    float3 ao = ray.origin - tri.posA;
    float3 dao = cross(ao, ray.dir);

    float determinant = -dot(ray.dir, normalVector);
    float invDet = 1 / determinant;

				// Calculate dst to triangle & barycentric coordinates of intersection point
    float dst = dot(ao, normalVector) * invDet;
    float u = dot(edgeAC, dao) * invDet;
    float v = -dot(edgeAB, dao) * invDet;
    float w = 1 - u - v;
	
    float2 uv = w * tri.uvA + u * tri.uvB + v * tri.uvC;

				// Initialize hit info
    TriangleHitInfo hitInfo;
    hitInfo.didHit = determinant >= 1E-8 && dst >= 0 && u >= 0 && v >= 0 && w >= 0;
    if (hitInfo.didHit)
    {
        hitInfo.hitPoint = ray.origin + ray.dir * dst;
        hitInfo.dst = dst;
        hitInfo.hitFront = determinant > 0;
        hitInfo.uv = uv;

        hitInfo.normal = normalize(tri.normA * w + tri.normB * u + tri.normC * v);
        // Calculate Tangent and Bitangent for each vertex (A, B, C)
        // local TBN
        hitInfo.tangent = normalize(tri.tanA * w + tri.tanB * u + tri.tanC * v);
        
        hitInfo.bitangent = normalize(cross(hitInfo.normal, hitInfo.tangent) * (u + v)); // Using cross product to compute the bitangent

    }
    return hitInfo;
}

			// Thanks to https://tavianator.com/2011/ray_box.html
float RayBoundingBoxDst(Ray ray, float3 boxMin, float3 boxMax)
{
    float3 tMin = (boxMin - ray.origin) * ray.invDir;
    float3 tMax = (boxMax - ray.origin) * ray.invDir;
    float3 t1 = min(tMin, tMax);
    float3 t2 = max(tMin, tMax);
    float tNear = max(max(t1.x, t1.y), t1.z);
    float tFar = min(min(t2.x, t2.y), t2.z);

    bool hit = tFar >= tNear && tFar > 0;
    float dst = hit ? tNear > 0 ? tNear : 0 : 1.#INF;
    return dst;
};


TriangleHitInfo RayTriangleBVH(inout Ray ray, float rayLength, int nodeOffset, int triOffset, inout int2 stats)
{
    TriangleHitInfo result;
    result.dst = rayLength;
    result.triIndex = -1;

    int stack[32];
    int stackIndex = 0;
    stack[stackIndex++] = nodeOffset + 0;

    while (stackIndex > 0)
    {
        BVHNode node = Nodes[stack[--stackIndex]];
        bool isLeaf = node.triangleCount > 0;

        if (isLeaf)
        {
            for (int i = 0; i < node.triangleCount; i++)
            {
                Triangle tri = Triangles[triOffset + node.startIndex + i];
                TriangleHitInfo triHitInfo = RayTriangle(ray, tri);
                stats[0]++; // count triangle intersection tests

                if (triHitInfo.didHit && triHitInfo.dst < result.dst)
                {
                    result = triHitInfo;
                    result.triIndex = node.startIndex + i;
                }
            }
        }
        else
        {
            int childIndexA = nodeOffset + node.startIndex + 0;
            int childIndexB = nodeOffset + node.startIndex + 1;
            BVHNode childA = Nodes[childIndexA];
            BVHNode childB = Nodes[childIndexB];

            float dstA = RayBoundingBoxDst(ray, childA.boundsMin, childA.boundsMax);
            float dstB = RayBoundingBoxDst(ray, childB.boundsMin, childB.boundsMax);
            stats[1] += 2; // count bounding box intersection tests
						
						// We want to look at closest child node first, so push it last
            bool isNearestA = dstA <= dstB;
            float dstNear = isNearestA ? dstA : dstB;
            float dstFar = isNearestA ? dstB : dstA;
            int childIndexNear = isNearestA ? childIndexA : childIndexB;
            int childIndexFar = isNearestA ? childIndexB : childIndexA;

            if (dstFar < result.dst)
                stack[stackIndex++] = childIndexFar;
            if (dstNear < result.dst)
                stack[stackIndex++] = childIndexNear;
        }
    }


    return result;
}



ModelHitInfo CalculateRayCollision(Ray worldRay, out int2 stats)
{
    ModelHitInfo result;
    result.dst = 1.#INF;
    Ray localRay;
    [loop]
    for (int i = 0; i < modelCount; i++)
    {
        Model model = ModelInfo[i];
		// Transform ray into model's local coordinate space
        localRay.origin = mul(model.worldToLocalMatrix, float4(worldRay.origin, 1));
        localRay.dir = mul(model.worldToLocalMatrix, float4(worldRay.dir, 0));
        localRay.invDir = 1 / localRay.dir;

		// Traverse bvh to find closest triangle intersection with current model
        TriangleHitInfo hit = RayTriangleBVH(localRay, result.dst, model.nodeOffset, model.triOffset, stats);

		// Record closest hit
        if (hit.dst < result.dst)
        {
            result.didHit = true;
            result.dst = hit.dst;
            result.uv = hit.uv;
            result.hitPoint = worldRay.origin + worldRay.dir * hit.dst;
            result.material = model.material;
            result.hitFront = hit.hitFront;
            
			// Apply normal map if present
            float3 worldNormal = normalize(mul(model.localToWorldMatrix, float4(hit.normal, 0)));
            if (model.material.normalTex > 0)
            {
                float3 worldTangent = normalize(mul(model.localToWorldMatrix, float4(hit.tangent, 0)));
                float3 worldBitangent = normalize(mul(model.localToWorldMatrix, float4(hit.bitangent, 0)));
                result.normal = SampleAndTransformNormalMap(float3(hit.uv, model.material.normalTex), worldTangent,worldBitangent,worldNormal);
                //result.normal = normalize(mul(model.localToWorldMatrix, float4(calculatedNormal, 0)));
                //result.normal = normalize(mul(model.localToWorldMatrix, float4(calculatedNormal, 0)));
                
            }
            else
                result.normal = worldNormal;
			
        }
    }

    return result;
}

float2 mod2(float2 x, float2 y)
{
    return x - y * floor(x / y);
}

float reflectance(float cosine, float eta)
{
    float r0 = (1 - eta) / (1 + eta);
    r0 = r0 * r0;
    return r0 + (1 - r0) * pow((1 - cosine), 5);
}

float3 Trace(float3 rayOrigin, float3 rayDir, inout uint rngState)
{
    float3 incomingLight = 0;
    float3 rayColour = 1;
				
    int2 stats;
    float dstSum = 0;
				
				[loop]
    for (int bounceIndex = 0; bounceIndex <= MaxBounceCount; bounceIndex++)
    {
        Ray ray;
        ray.origin = rayOrigin + rayDir * 1E-6;
        ray.dir = rayDir;
        ModelHitInfo hitInfo = CalculateRayCollision(ray, stats);

        if (hitInfo.didHit)
        {
            dstSum += hitInfo.dst;
            RTMatData material = hitInfo.material;
			
			
            if (material.flag == 1) // Checker pattern
            {
                float2 c = mod2(floor(hitInfo.hitPoint.xz), 2.0);
                material.colour = c.x == c.y ? material.colour : material.emissionColour;
            }
						
			// specular multiplier
            float s = material.roughnessTex > 0 ? (1 - UNITY_SAMPLE_TEX2DARRAY(RoughnessTextures, float3(hitInfo.uv, material.roughnessTex)).x) : 1;

			// Figure out new ray position and direction
            bool isSpecularBounce = s * material.specularProbability >= RandomValue(rngState);

            rayOrigin = hitInfo.hitPoint;
            float3 diffuseDir = normalize(hitInfo.normal + RandomDirection(rngState));
            float3 specularDir = reflect(rayDir, hitInfo.normal);
            float eta_div = hitInfo.hitFront ? 1.0 / material.eta : material.eta;
            float3 refractDir = refract(rayDir, hitInfo.normal, eta_div);
						
            if (material.transparency == 0)
            {
                rayDir = normalize(lerp(diffuseDir, specularDir, s * material.smoothness * isSpecularBounce));
            }
            else
            {
				// Check total internal reflection
                float cosTheta = min(1.0, dot(-rayDir, hitInfo.normal));
                float sinTheta = sqrt(1 - cosTheta * cosTheta);
                float sinPhi = sinTheta * eta_div;

				// Schlick's approximation
                float reflectProb = reflectance(cosTheta, eta_div);


                if (sinPhi > 1 || RandomValue(rngState) < reflectProb)
                {
                    rayDir = specularDir;
                }
                else
                {
                    if (hitInfo.hitFront)
                    {
						// Div with transparency
                        if (RandomValue(rngState) < material.transparency)
                            rayDir = refractDir;
                        else
                            rayDir = normalize(lerp(diffuseDir, specularDir, s * material.smoothness * isSpecularBounce));
                    }
                    else
                    {
						// Div without transparency
                        rayDir = refractDir;
                    }
                }

            }

						// Update light calculations
            float3 emittedLight = material.emissionColour * material.emissionStrength;
            incomingLight += emittedLight * rayColour;
			
			
            float4 albedoColor = material.albedoTex > 0 ? material.colour * UNITY_SAMPLE_TEX2DARRAY(AlbedoTextures, float3(hitInfo.uv, material.albedoTex)) : material.colour;
						//float4 albedoColor = float4(hitInfo.uv, 0, 1);
			
            rayColour *= lerp(albedoColor, material.specularColour, isSpecularBounce);

						// Random early exit if ray colour is nearly 0 (can't contribute much to final result)
            float p = max(rayColour.r, max(rayColour.g, rayColour.b));
            if (RandomValue(rngState) >= p)
            {
                break;
            }
            rayColour *= 1.0f / p;
        }
        else
        {
            incomingLight += GetEnvironmentLight(rayDir) * rayColour;
            break;
        }
    }

    return incomingLight;
}


float3 TraceDebugMode(float3 rayOrigin, float3 rayDir)
{
    int2 stats; // num triangle tests, num bounding box tests
    Ray ray;
    ray.origin = rayOrigin;
    ray.dir = rayDir;
    ModelHitInfo hitInfo = CalculateRayCollision(ray, stats);

				// Triangle test count vis
    if (visMode == 1)
    {
        float triVis = stats[0] / debugVisScale;
        return triVis < 1 ? triVis : float3(1, 0, 0);
    }
				// Box test count vis
    else if (visMode == 2)//
    {
        float boxVis = stats[1] / debugVisScale;
        return boxVis < 1 ? boxVis : float3(1, 0, 0);
    }
				// Distance
    else if (visMode == 3)
    {
        return length(rayOrigin - hitInfo.hitPoint) / debugVisScale;
    }
				// Normal
    else if (visMode == 4)
    {
        if (!hitInfo.didHit)
            return 0;
        return hitInfo.normal * 0.5 + 0.5;
    }

    return float3(1, 0, 1); // Invalid test mode
}


			// Run for every pixel in the display
float4 frag(v2f i) : SV_Target
{
				// Create seed for random number generator
    uint2 numPixels = _ScreenParams.xy;
    uint2 pixelCoord = i.uv * numPixels;
    uint pixelIndex = pixelCoord.y * numPixels.x + pixelCoord.x;
    uint rngState = pixelIndex + Frame * 719393; //

				// Calculate focus point
    float3 focusPointLocal = float3(i.uv - 0.5, 1) * ViewParams;
    float3 focusPoint = mul(CamLocalToWorldMatrix, float4(focusPointLocal, 1));
    float3 camRight = CamLocalToWorldMatrix._m00_m10_m20;
    float3 camUp = CamLocalToWorldMatrix._m01_m11_m21;

				// Debug Mode
#if DEBUG_VIS
					return float4(TraceDebugMode(_WorldSpaceCameraPos, normalize(focusPoint - _WorldSpaceCameraPos)), 1);
#endif
				
				// Trace multiple rays and average together
    float3 totalIncomingLight = 0;

    for (int rayIndex = 0; rayIndex < NumRaysPerPixel; rayIndex++)
    {
					// -- Calculate ray origin and direction --
					// Jitter the starting point of the ray. This allows for a depth of field effect.
        float2 defocusJitter = RandomPointInCircle(rngState) * DefocusStrength / numPixels.x;
        float3 rayOrigin = _WorldSpaceCameraPos + camRight * defocusJitter.x + camUp * defocusJitter.y;

					// Jitter the focus point when calculating the ray direction to allow for blurring the image
					// (at low strengths, this can be used for anti-aliasing)
        float2 jitter = RandomPointInCircle(rngState) * DivergeStrength / numPixels.x;
        float3 jitteredFocusPoint = focusPoint + camRight * jitter.x + camUp * jitter.y;
        float3 rayDir = normalize(jitteredFocusPoint - rayOrigin);

					// Trace
        totalIncomingLight += Trace(rayOrigin, rayDir, rngState);
    }


    float3 pixelCol = totalIncomingLight / NumRaysPerPixel;
    return float4(pixelCol, 1);
}

			ENDCG
		}
	}
}