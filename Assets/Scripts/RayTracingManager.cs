using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.Linq;

public class RayTracingManager : MonoBehaviour
{
    public enum VisMode
    {
        Default = 0,
        TriangleTestCount = 1,
        BoxTestCount = 2,
        Distance = 3,
        Normal = 4
    }

    [Header("Main Settings")]
    [SerializeField] bool rayTracingEnabled = true;
    public bool accumulate = true;
    public bool useSky;
    [SerializeField] float sunFocus = 500;
    [SerializeField] float sunIntensity = 10;
    [SerializeField] Color sunColor = Color.white;

    [SerializeField, Range(0, 32)] int maxBounceCount = 4;
    [SerializeField, Range(0, 64)] int numRaysPerPixel = 2;
    [SerializeField, Min(0)] float defocusStrength = 0;
    [SerializeField, Min(0)] float divergeStrength = 0.3f;
    [SerializeField, Min(0)] float focusDistance = 1;

    [Header("Debug Settings")]
    [SerializeField] VisMode visMode;
    [SerializeField] float triTestVisScale;
    [SerializeField] float boxTestVisScale;
    [SerializeField] float distanceTestVisScale;
    [SerializeField] bool useSceneView;

    [Header("References")]
    [SerializeField] Shader rayTracingShader;
    [SerializeField] Shader accumulateShader;

    [Header("Info")]
    [SerializeField] int numAccumulatedFrames;

    // Materials and render textures
    Material rayTracingMaterial;
    Material accumulateMaterial;
    RenderTexture resultTexture;

    // Buffers
    ComputeBuffer triangleBuffer;
    ComputeBuffer nodeBuffer;
    ComputeBuffer modelBuffer;
    Texture2DArray albedoTexArray;
    Texture2DArray normalTexArray;
    Texture2DArray roughnessTexArray;

    MeshInfo[] meshInfo;
    Model[] models;


    bool hasBVH;
    LocalKeyword debugVisShaderKeyword;

    public float FocusDistance { get=>focusDistance; set => focusDistance = value; }
    public bool Accumulated { get => accumulate; set { accumulate = value; numAccumulatedFrames = 1; } }

    private void OnEnable()
    {
        numAccumulatedFrames = 0;
        hasBVH = false;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            numAccumulatedFrames = 1;
            Debug.Log("Reset render");
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            string path = System.IO.Path.Combine(Application.persistentDataPath, "screencap_ray.png");
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log("Screenshot: " + path);
        }
    }

    public void ResetRender()
    {
        numAccumulatedFrames = 0;
    }

    // Called after any camera (e.g. game or scene camera) has finished rendering into the src texture
    void OnRenderImage(RenderTexture src, RenderTexture target)
    {
        if (!Application.isPlaying)
        {
            Graphics.Blit(src, target); // Draw the unaltered camera render to the screen
            return;
        }

        bool isSceneCam = Camera.current.name == "SceneCamera";
        // Debug.Log("Rendering... isscenecam = " + isSceneCam + "  " + Camera.current.name);
        if (isSceneCam)
        {
            if (rayTracingEnabled && useSceneView)
            {
                InitFrame();
                Graphics.Blit(null, target, rayTracingMaterial);
            }
            else
            {
                Graphics.Blit(src, target); // Draw the unaltered camera render to the screen
            }
        }
        else
        {
            Camera.current.cullingMask = rayTracingEnabled ? 1<<31 : 2147483647;
            if (rayTracingEnabled && !useSceneView)
            {
                InitFrame();

                if (accumulate && visMode == VisMode.Default)
                {
                    // Create copy of prev frame
                    RenderTexture prevFrameCopy = RenderTexture.GetTemporary(src.width, src.height, 0, ShaderHelper.RGBA_SFloat);
                    Graphics.Blit(resultTexture, prevFrameCopy);

                    // Run the ray tracing shader and draw the result to a temp texture
                    rayTracingMaterial.SetInt("Frame", numAccumulatedFrames);
                    RenderTexture currentFrame = RenderTexture.GetTemporary(src.width, src.height, 0, ShaderHelper.RGBA_SFloat);
                    Graphics.Blit(null, currentFrame, rayTracingMaterial);

                    // Accumulate
                    accumulateMaterial.SetInt("_Frame", numAccumulatedFrames);
                    accumulateMaterial.SetTexture("_PrevFrame", prevFrameCopy);
                    Graphics.Blit(currentFrame, resultTexture, accumulateMaterial);

                    // Draw result to screen
                    Graphics.Blit(resultTexture, target);

                    // Release temps
                    RenderTexture.ReleaseTemporary(prevFrameCopy);
                    RenderTexture.ReleaseTemporary(currentFrame);
                    numAccumulatedFrames += Application.isPlaying ? 1 : 0;
                }
                else
                {
                    numAccumulatedFrames = 0;
                    Graphics.Blit(null, target, rayTracingMaterial);
                }
            }
            else
            {
                Graphics.Blit(src, target); // Draw the unaltered camera render to the screen
            }
        }
    }

    void InitFrame()
    {
        // Create materials used in blits
        if (rayTracingMaterial == null || rayTracingMaterial.shader != rayTracingShader)
        {
            ShaderHelper.InitMaterial(rayTracingShader, ref rayTracingMaterial);
            debugVisShaderKeyword = new LocalKeyword(rayTracingShader, "DEBUG_VIS");
        }
        ShaderHelper.InitMaterial(accumulateShader, ref accumulateMaterial);
        ShaderHelper.CreateRenderTexture(ref resultTexture, Screen.width, Screen.height, FilterMode.Bilinear, ShaderHelper.RGBA_SFloat, "Result");
        models = FindObjectsOfType<Model>();

        if (!hasBVH)
        {
            var data = CreateAllMeshData(models);
            hasBVH = true;

            meshInfo = data.meshInfo.ToArray();
            ShaderHelper.CreateStructuredBuffer(ref modelBuffer, meshInfo);

            // Triangles buffer
            ShaderHelper.CreateStructuredBuffer(ref triangleBuffer, data.triangles);
            rayTracingMaterial.SetBuffer("Triangles", triangleBuffer);
            rayTracingMaterial.SetInt("triangleCount", triangleBuffer.count);

            // Node buffer
            ShaderHelper.CreateStructuredBuffer(ref nodeBuffer, data.nodes);
            rayTracingMaterial.SetBuffer("Nodes", nodeBuffer);

            // Texture buffer
            ShaderHelper.CreateTextureArrayBuffer(ref albedoTexArray, data.albedoTextures);
            ShaderHelper.CreateTextureArrayBuffer(ref normalTexArray, data.normalTextures);
            ShaderHelper.CreateTextureArrayBuffer(ref roughnessTexArray, data.roughnessTextures);
            rayTracingMaterial.SetTexture("AlbedoTextures", albedoTexArray);
            rayTracingMaterial.SetTexture("NormalTextures", normalTexArray);
            rayTracingMaterial.SetTexture("RoughnessTextures", roughnessTexArray);

        }
        UpdateModels();
        // Update data
        UpdateCameraParams(Camera.current);
        SetShaderParams();
    }

    void SetShaderParams()
    {
        rayTracingMaterial.SetKeyword(debugVisShaderKeyword, visMode != VisMode.Default);
        rayTracingMaterial.SetInt("visMode", (int)visMode);
        float debugVisScale = visMode switch
        {
            VisMode.TriangleTestCount => triTestVisScale,
            VisMode.BoxTestCount => boxTestVisScale,
            VisMode.Distance => distanceTestVisScale,
            _ => triTestVisScale
        };
        rayTracingMaterial.SetFloat("debugVisScale", debugVisScale);
        rayTracingMaterial.SetInt("Frame", numAccumulatedFrames);
        rayTracingMaterial.SetInt("UseSky", useSky ? 1 : 0);

        rayTracingMaterial.SetInt("MaxBounceCount", maxBounceCount);
        rayTracingMaterial.SetInt("NumRaysPerPixel", numRaysPerPixel);
        rayTracingMaterial.SetFloat("DefocusStrength", defocusStrength);
        rayTracingMaterial.SetFloat("DivergeStrength", divergeStrength);

        rayTracingMaterial.SetFloat("SunFocus", sunFocus);
        rayTracingMaterial.SetFloat("SunIntensity", sunIntensity);
        rayTracingMaterial.SetColor("SunColour", sunColor);
    }

    void UpdateCameraParams(Camera cam)
    {
        float planeHeight = focusDistance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 2;
        float planeWidth = planeHeight * cam.aspect;
        // Send data to shader
        rayTracingMaterial.SetVector("ViewParams", new Vector3(planeWidth, planeHeight, focusDistance));
        rayTracingMaterial.SetMatrix("CamLocalToWorldMatrix", cam.transform.localToWorldMatrix);
    }

    void UpdateModels()
    {
        for (int i = 0; i < models.Length; i++)
        {
            meshInfo[i].WorldToLocalMatrix = models[i].transform.worldToLocalMatrix;
            meshInfo[i].LocalToWorldMatrix = models[i].transform.localToWorldMatrix;
            meshInfo[i].Material = new RTMatData(models[i].Material);
        }
        modelBuffer.SetData(meshInfo);
        rayTracingMaterial.SetBuffer("ModelInfo", modelBuffer);
        rayTracingMaterial.SetInt("modelCount", models.Length);
    }

    MeshDataLists CreateAllMeshData(Model[] models)
    {
        MeshDataLists allData = new();
        Dictionary<Mesh, (int nodeOffset, int triOffset)> meshLookup = new();
        Dictionary<Texture2D, int> albedoTexLookup = new() { { Texture2D.whiteTexture, 0 }};
        Dictionary<Texture2D, int> normalTexLookup = new() { { Texture2D.whiteTexture, 0 } };
        Dictionary<Texture2D, int> roughnessTexLookup = new() { { Texture2D.whiteTexture, 0 } };

        foreach (Model model in models)
        {
            // Construct BVH if this is the first time seeing the current mesh (otherwise reuse)
            if (!meshLookup.ContainsKey(model.Mesh))
            {
                meshLookup.Add(model.Mesh, (allData.nodes.Count, allData.triangles.Count));

                BVH bvh = new(model.Mesh.vertices, model.Mesh.triangles, model.Mesh.normals, model.Mesh.tangents.Select(t=>new Vector3(t.x,t.y,t.z)).ToArray(), model.Mesh.uv);
                if (model.logBVHStats) Debug.Log($"BVH Stats: {model.gameObject.name}\n{bvh.stats}");

                allData.triangles.AddRange(bvh.GetTriangles());
                allData.nodes.AddRange(bvh.GetNodes());
            }

            var mat = new RTMatData(model.Material);
            int idx = 0;

            if (model.Material.albedoTex == null)
            { idx = 0; }
            else if (albedoTexLookup.TryGetValue(model.Material.albedoTex, out idx))
            { }
            else
            {
                idx = albedoTexLookup.Count;
                albedoTexLookup.Add(model.Material.albedoTex, albedoTexLookup.Count);
            }

            mat.albetoTex = idx;
            model.Material.albedoIdx = idx;

            if (model.Material.normalTex == null)
            { idx = 0; }
            else if (normalTexLookup.TryGetValue(model.Material.normalTex, out idx))
            { }
            else
            {
                idx = normalTexLookup.Count;
                normalTexLookup.Add(model.Material.normalTex, normalTexLookup.Count);
            }

            mat.normalTex = idx;
            model.Material.normalIdx = idx;

            if (model.Material.roughnessTex == null)
            { idx = 0; }
            else if (roughnessTexLookup.TryGetValue(model.Material.roughnessTex, out idx))
            { }
            else
            {
                idx = roughnessTexLookup.Count;
                roughnessTexLookup.Add(model.Material.roughnessTex, roughnessTexLookup.Count);
            }

            mat.roughnessTex = idx;
            model.Material.roughnessIdx = idx;

            // Create the mesh info
            allData.meshInfo.Add(new MeshInfo()
            {
                NodeOffset = meshLookup[model.Mesh].nodeOffset,
                TriangleOffset = meshLookup[model.Mesh].triOffset,
                WorldToLocalMatrix = model.transform.worldToLocalMatrix,
                Material = mat
            }) ;
        }

        allData.albedoTextures = albedoTexLookup.Select(kv=>kv.Key).ToArray();
        allData.albedoTextures[0] = Texture2D.whiteTexture; // null to white texture
        allData.normalTextures = normalTexLookup.Select(kv=>kv.Key).ToArray();
        allData.normalTextures[0] = Texture2D.normalTexture; 
        allData.roughnessTextures = roughnessTexLookup.Select(kv=>kv.Key).ToArray();
        allData.roughnessTextures[0] = Texture2D.blackTexture;


        return allData;
    }

    class MeshDataLists
    {

        public List<Triangle> triangles = new();
        public List<BVH.Node> nodes = new();
        public List<MeshInfo> meshInfo = new();

        public Texture2D[] albedoTextures;
        public Texture2D[] normalTextures;
        public Texture2D[] roughnessTextures;
    }

    void OnDestroy()
    {
        if (Application.isPlaying)
        {
            ShaderHelper.Release(triangleBuffer, nodeBuffer, modelBuffer);
            ShaderHelper.Release(resultTexture);
            Destroy(rayTracingMaterial);
        }
    }

    void OnValidate()
    {
    }

    struct MeshInfo
    {
        public int NodeOffset;
        public int TriangleOffset;
        public Matrix4x4 WorldToLocalMatrix;
        public Matrix4x4 LocalToWorldMatrix;
        public RTMatData Material;
    }
}
