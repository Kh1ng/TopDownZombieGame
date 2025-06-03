using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Linq;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SceneVisualDumper : MonoBehaviour
{
    [ContextMenu("Dump Scene Visual Info To JSON")]
    void DumpSceneVisuals()
    {
        VisualDump visualDump = new VisualDump();
        visualDump.renderPipelineInfo = GetRenderPipelineInfo();
        visualDump.cameraInfo = GetCamerasInfo();
        visualDump.materialInfo = GetMaterialsInfo();
        visualDump.lightingInfo = GetLightingInfo();
        visualDump.rendererInfo = GetRendererInfo();
        visualDump.spriteInfo = GetSpriteInfo();
        visualDump.meshInfo = GetMeshInfo();
        visualDump.missingComponents = GetMissingComponentsInfo();
        visualDump.visualIssues = AnalyzeVisualIssues();

        string json = JsonUtility.ToJson(visualDump, true);
        string path = Path.Combine(Application.dataPath, "visual_dump.json");
        File.WriteAllText(path, json);
        Debug.Log("Visual scene dump saved to: " + path);
    }

    private RenderPipelineInfo GetRenderPipelineInfo()
    {
        RenderPipelineInfo info = new RenderPipelineInfo();
        
        var currentPipeline = GraphicsSettings.currentRenderPipeline;
        info.currentRenderPipelineType = currentPipeline != null ? currentPipeline.GetType().Name : "Built-in Render Pipeline";
        info.pipelineAssetName = currentPipeline != null ? currentPipeline.name : "None";
        
        if (currentPipeline is UniversalRenderPipelineAsset urpAsset)
        {
            info.mainLightRenderingMode = urpAsset.mainLightRenderingMode.ToString();
            info.supportsHDR = urpAsset.supportsHDR;
            info.msaaSampleCount = urpAsset.msaaSampleCount;
            info.renderScale = urpAsset.renderScale;
        }

        info.colorSpace = QualitySettings.activeColorSpace.ToString();
        info.anisotropicFiltering = QualitySettings.anisotropicFiltering.ToString();
        info.antiAliasing = QualitySettings.antiAliasing;
        info.realtimeReflectionProbes = QualitySettings.realtimeReflectionProbes;

        return info;
    }

    private List<CameraInfo> GetCamerasInfo()
    {
        var cameras = FindObjectsOfType<Camera>();
        var cameraInfos = new List<CameraInfo>();

        foreach (var camera in cameras)
        {
            CameraInfo info = new CameraInfo();
            info.name = camera.gameObject.name;
            info.path = GetFullPath(camera.transform);
            info.enabled = camera.enabled;
            info.orthographic = camera.orthographic;
            info.orthographicSize = camera.orthographicSize;
            info.fieldOfView = camera.fieldOfView;
            info.depth = camera.depth;
            info.clearFlags = camera.clearFlags.ToString();
            info.backgroundColor = ColorToString(camera.backgroundColor);
            info.cullingMask = LayerMaskToString(camera.cullingMask);
            info.targetTexture = camera.targetTexture != null ? camera.targetTexture.name : "null";
            
            // Check for URP camera data
            var additionalCameraData = camera.GetComponent<UniversalAdditionalCameraData>();
            if (additionalCameraData != null)
            {
                info.renderType = additionalCameraData.renderType.ToString();
                info.renderPostProcessing = additionalCameraData.renderPostProcessing;
                info.antialiasing = additionalCameraData.antialiasing.ToString();
                info.renderShadows = additionalCameraData.renderShadows;
                info.volumeLayerMask = LayerMaskToString(additionalCameraData.volumeLayerMask);
            }
            
            cameraInfos.Add(info);
        }

        return cameraInfos;
    }

    private List<MaterialInfo> GetMaterialsInfo()
    {
        var renderers = FindObjectsOfType<Renderer>();
        var materialInfos = new List<MaterialInfo>();
        var processedMaterials = new HashSet<Material>();

        foreach (var renderer in renderers)
        {
            foreach (var material in renderer.sharedMaterials)
            {
                if (material != null && !processedMaterials.Contains(material))
                {
                    MaterialInfo info = new MaterialInfo();
                    info.name = material.name;
                    info.shader = material.shader.name;
                    info.renderQueue = material.renderQueue;
                    info.isUsingBuiltInShader = material.shader.name.StartsWith("Legacy") || 
                                                material.shader.name.StartsWith("Standard") ||
                                                material.shader.name.StartsWith("Unlit") ||
                                                material.shader.name.StartsWith("Sprites") ||
                                                material.shader.name.StartsWith("Particles");
                    
                    info.isUsingURPShader = material.shader.name.StartsWith("Universal") || 
                                            material.shader.name.StartsWith("URP") ||
                                            material.shader.name.StartsWith("Shader Graphs");
                    
                    info.isTransparent = material.HasProperty("_Mode") && material.GetFloat("_Mode") > 0 ||
                                         material.HasProperty("_Surface") && material.GetFloat("_Surface") > 0;
                    
                    info.color = material.HasProperty("_Color") ? ColorToString(material.GetColor("_Color")) : "N/A";
                    info.mainTexture = material.mainTexture != null ? material.mainTexture.name : "null";
                    
                    processedMaterials.Add(material);
                    materialInfos.Add(info);
                }
            }
        }

        return materialInfos;
    }

    private LightingInfo GetLightingInfo()
    {
        LightingInfo info = new LightingInfo();
        info.ambientMode = RenderSettings.ambientMode.ToString();
        info.ambientColor = ColorToString(RenderSettings.ambientLight);
        info.ambientIntensity = RenderSettings.ambientIntensity;
        info.fogEnabled = RenderSettings.fog;
        info.fogColor = ColorToString(RenderSettings.fogColor);
        
        var lights = FindObjectsOfType<Light>();
        info.lightCount = lights.Length;
        
        var light2Ds = FindObjectsOfType<Light2D>();
        info.light2DCount = light2Ds.Length;
        
        if (light2Ds.Length > 0)
        {
            var globalLights = light2Ds.Where(l => l.lightType == Light2D.LightType.Global).ToList();
            info.hasGlobalLight = globalLights.Count > 0;
            
            if (globalLights.Count > 0)
            {
                info.globalLightColor = ColorToString(globalLights[0].color);
                info.globalLightIntensity = globalLights[0].intensity;
            }
        }
        
        return info;
    }

    private List<RendererInfo> GetRendererInfo()
    {
        var renderers = FindObjectsOfType<Renderer>();
        var rendererInfos = new List<RendererInfo>();

        foreach (var renderer in renderers)
        {
            RendererInfo info = new RendererInfo();
            info.name = renderer.gameObject.name;
            info.path = GetFullPath(renderer.transform);
            info.enabled = renderer.enabled;
            info.isVisible = renderer.isVisible;
            info.sortingLayerName = renderer.sortingLayerName;
            info.sortingOrder = renderer.sortingOrder;
            info.materialCount = renderer.sharedMaterials.Length;
            info.receiveShadows = renderer.receiveShadows;
            info.lightmapIndex = renderer.lightmapIndex;
            
            // Check for missing materials
            bool hasMissingMaterial = false;
            for (int i = 0; i < renderer.sharedMaterials.Length; i++)
            {
                if (renderer.sharedMaterials[i] == null)
                {
                    hasMissingMaterial = true;
                    break;
                }
            }
            info.hasMissingMaterial = hasMissingMaterial;
            
            rendererInfos.Add(info);
        }

        return rendererInfos;
    }

    private List<SpriteInfo> GetSpriteInfo()
    {
        var spriteRenderers = FindObjectsOfType<SpriteRenderer>();
        var spriteInfos = new List<SpriteInfo>();

        foreach (var spriteRenderer in spriteRenderers)
        {
            SpriteInfo info = new SpriteInfo();
            info.name = spriteRenderer.gameObject.name;
            info.path = GetFullPath(spriteRenderer.transform);
            info.enabled = spriteRenderer.enabled;
            info.spriteName = spriteRenderer.sprite != null ? spriteRenderer.sprite.name : "null";
            info.color = ColorToString(spriteRenderer.color);
            info.sortingLayerName = spriteRenderer.sortingLayerName;
            info.sortingOrder = spriteRenderer.sortingOrder;
            info.flipX = spriteRenderer.flipX;
            info.flipY = spriteRenderer.flipY;
            info.drawMode = spriteRenderer.drawMode.ToString();
            info.maskInteraction = spriteRenderer.maskInteraction.ToString();
            info.spriteSortPoint = spriteRenderer.spriteSortPoint.ToString();
            
            spriteInfos.Add(info);
        }

        return spriteInfos;
    }

    private List<MeshInfo> GetMeshInfo()
    {
        var meshFilters = FindObjectsOfType<MeshFilter>();
        var meshInfos = new List<MeshInfo>();

        foreach (var meshFilter in meshFilters)
        {
            MeshInfo info = new MeshInfo();
            info.name = meshFilter.gameObject.name;
            info.path = GetFullPath(meshFilter.transform);
            
            if (meshFilter.sharedMesh != null)
            {
                info.meshName = meshFilter.sharedMesh.name;
                info.vertexCount = meshFilter.sharedMesh.vertexCount;
                info.subMeshCount = meshFilter.sharedMesh.subMeshCount;
                info.triangleCount = meshFilter.sharedMesh.triangles.Length / 3;
                info.hasNormals = meshFilter.sharedMesh.normals.Length > 0;
                info.hasTangents = meshFilter.sharedMesh.tangents.Length > 0;
                info.hasUVs = meshFilter.sharedMesh.uv.Length > 0;
                info.hasColors = meshFilter.sharedMesh.colors.Length > 0;
            }
            else
            {
                info.meshName = "null";
                info.vertexCount = 0;
            }
            
            meshInfos.Add(info);
        }

        return meshInfos;
    }

    private List<MissingComponentInfo> GetMissingComponentsInfo()
    {
        var missingComponentInfos = new List<MissingComponentInfo>();
        var allGameObjects = FindObjectsOfType<GameObject>();

        foreach (var go in allGameObjects)
        {
            var components = go.GetComponents<Component>();
            bool hasMissingComponent = false;
            
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == null)
                {
                    hasMissingComponent = true;
                    break;
                }
            }
            
            if (hasMissingComponent)
            {
                MissingComponentInfo info = new MissingComponentInfo();
                info.name = go.name;
                info.path = GetFullPath(go.transform);
                missingComponentInfos.Add(info);
            }
        }

        return missingComponentInfos;
    }

    private List<string> AnalyzeVisualIssues()
    {
        List<string> issues = new List<string>();
        
        // Check for render pipeline mismatch
        var materials = FindObjectsOfType<Renderer>()
            .SelectMany(r => r.sharedMaterials)
            .Where(m => m != null)
            .ToList();
            
        bool hasBuiltInMaterials = materials.Any(m => 
            m.shader.name.StartsWith("Legacy") || 
            m.shader.name.StartsWith("Standard") ||
            m.shader.name.StartsWith("Unlit/") ||
            m.shader.name.StartsWith("Sprites/"));
            
        bool hasURPMaterials = materials.Any(m => 
            m.shader.name.StartsWith("Universal") || 
            m.shader.name.StartsWith("URP/") ||
            m.shader.name.StartsWith("Shader Graphs/"));
            
        bool isUsingURP = GraphicsSettings.currentRenderPipeline != null && 
                         GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset;
        
        if (isUsingURP && hasBuiltInMaterials)
        {
            issues.Add("Project is using URP but has materials with built-in render pipeline shaders");
        }
        
        if (!isUsingURP && hasURPMaterials)
        {
            issues.Add("Project is using built-in render pipeline but has URP shaders");
        }
        
        // Check for missing sprites
        var spriteRenderers = FindObjectsOfType<SpriteRenderer>();
        if (spriteRenderers.Any(sr => sr.sprite == null))
        {
            issues.Add("Scene has SpriteRenderer components with missing sprites");
        }
        
        // Check for missing meshes
        var meshFilters = FindObjectsOfType<MeshFilter>();
        if (meshFilters.Any(mf => mf.sharedMesh == null))
        {
            issues.Add("Scene has MeshFilter components with missing meshes");
        }
        
        // Check for missing materials
        var renderers = FindObjectsOfType<Renderer>();
        if (renderers.Any(r => r.sharedMaterials.Any(m => m == null)))
        {
            issues.Add("Scene has Renderer components with missing materials");
        }
        
        // Check for 2D lighting issues
        var light2Ds = FindObjectsOfType<Light2D>();
        if (light2Ds.Length > 0)
        {
            bool hasGlobalLight = light2Ds.Any(l => l.lightType == Light2D.LightType.Global);
            if (!hasGlobalLight)
            {
                issues.Add("Scene uses 2D lights but has no global 2D light");
            }
        }
        
        // Check for camera issues
        var cameras = FindObjectsOfType<Camera>();
        if (cameras.Length == 0)
        {
            issues.Add("Scene has no cameras");
        }
        else if (cameras.Count(c => c.gameObject.activeInHierarchy && c.enabled) == 0)
        {
            issues.Add("Scene has no active cameras");
        }
        else if (cameras.Count(c => c.gameObject.activeInHierarchy && c.enabled) > 1)
        {
            issues.Add("Scene has multiple active cameras - this can cause rendering issues");
        }
        
        // Check for HDR settings in URP
        if (isUsingURP)
        {
            var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            var additionalCameraDatas = FindObjectsOfType<UniversalAdditionalCameraData>();
            
            if (!urpAsset.supportsHDR && additionalCameraDatas.Any(cd => cd.requiresColorOption == CameraOverrideOption.On && cd.requiresColorTexture))
            {
                issues.Add("URP asset has HDR disabled but cameras are requesting HDR color textures");
            }
        }

        // Check for static batching issues
        var staticRenderers = FindObjectsOfType<Renderer>().Where(r => r.gameObject.isStatic).ToList();
        
        #if UNITY_EDITOR
        // Different versions of Unity have different APIs for accessing batching settings
        bool staticBatchingEnabled = false;
        
        // Try to access the batching settings through reflection to be compatible with multiple Unity versions
        try {
            var playerSettingsType = typeof(UnityEditor.PlayerSettings);
            var batchingProperty = playerSettingsType.GetProperty("staticBatching", 
                BindingFlags.Public | BindingFlags.Static);
            
            if (batchingProperty != null) {
                staticBatchingEnabled = (bool)batchingProperty.GetValue(null);
            }
            else {
                // For newer Unity versions that use batchingSettings
                var batchingSettingsProperty = playerSettingsType.GetProperty("batchingSettings",
                    BindingFlags.Public | BindingFlags.Static);
                
                if (batchingSettingsProperty != null) {
                    var batchingSettings = batchingSettingsProperty.GetValue(null);
                    var staticBatchingField = batchingSettings.GetType().GetField("staticBatching");
                    staticBatchingEnabled = (bool)staticBatchingField.GetValue(batchingSettings);
                }
            }
        }
        catch (System.Exception e) {
            Debug.LogWarning($"Could not determine static batching settings: {e.Message}");
            // If we can't determine, assume it's enabled as that's the default
            staticBatchingEnabled = true;
        }
        
        if (staticRenderers.Count > 0 && !staticBatchingEnabled)
        {
            issues.Add("Scene has static renderers but static batching is disabled");
        }
#else
        if (staticRenderers.Count > 0)
        {
            issues.Add("Scene has static renderers (static batching status can only be checked in editor)");
        }
        #endif
        
        return issues;
    }

    private string GetFullPath(Transform t)
    {
        if (t.parent == null) return t.name;
        return GetFullPath(t.parent) + "/" + t.name;
    }

    private string ColorToString(Color color)
    {
        return $"RGBA({color.r:F3}, {color.g:F3}, {color.b:F3}, {color.a:F3})";
    }

    private string LayerMaskToString(LayerMask mask)
    {
        return $"Mask: {mask.value} ({string.Join(", ", GetLayerMaskNames(mask))})";
    }

    private string[] GetLayerMaskNames(LayerMask mask)
    {
        var layers = new List<string>();
        for (int i = 0; i < 32; i++)
        {
            if ((mask & (1 << i)) != 0)
            {
                string layerName = LayerMask.LayerToName(i);
                if (!string.IsNullOrEmpty(layerName))
                {
                    layers.Add(layerName);
                }
                else
                {
                    layers.Add($"Layer {i}");
                }
            }
        }
        return layers.ToArray();
    }

    // Define data classes
    [System.Serializable]
    public class VisualDump
    {
        public RenderPipelineInfo renderPipelineInfo;
        public List<CameraInfo> cameraInfo = new List<CameraInfo>();
        public List<MaterialInfo> materialInfo = new List<MaterialInfo>();
        public LightingInfo lightingInfo;
        public List<RendererInfo> rendererInfo = new List<RendererInfo>();
        public List<SpriteInfo> spriteInfo = new List<SpriteInfo>();
        public List<MeshInfo> meshInfo = new List<MeshInfo>();
        public List<MissingComponentInfo> missingComponents = new List<MissingComponentInfo>();
        public List<string> visualIssues = new List<string>();
    }

    [System.Serializable]
    public class RenderPipelineInfo
    {
        public string currentRenderPipelineType;
        public string pipelineAssetName;
        public string mainLightRenderingMode;
        public bool supportsHDR;
        public int msaaSampleCount;
        public float renderScale;
        public string colorSpace;
        public string anisotropicFiltering;
        public int antiAliasing;
        public bool realtimeReflectionProbes;
    }

    [System.Serializable]
    public class CameraInfo
    {
        public string name;
        public string path;
        public bool enabled;
        public bool orthographic;
        public float orthographicSize;
        public float fieldOfView;
        public float depth;
        public string clearFlags;
        public string backgroundColor;
        public string cullingMask;
        public string targetTexture;
        public string renderType;
        public bool renderPostProcessing;
        public string antialiasing;
        public bool renderShadows;
        public string volumeLayerMask;
    }

    [System.Serializable]
    public class MaterialInfo
    {
        public string name;
        public string shader;
        public int renderQueue;
        public bool isUsingBuiltInShader;
        public bool isUsingURPShader;
        public bool isTransparent;
        public string color;
        public string mainTexture;
    }

    [System.Serializable]
    public class LightingInfo
    {
        public string ambientMode;
        public string ambientColor;
        public float ambientIntensity;
        public bool fogEnabled;
        public string fogColor;
        public int lightCount;
        public int light2DCount;
        public bool hasGlobalLight;
        public string globalLightColor;
        public float globalLightIntensity;
    }

    [System.Serializable]
    public class RendererInfo
    {
        public string name;
        public string path;
        public bool enabled;
        public bool isVisible;
        public string sortingLayerName;
        public int sortingOrder;
        public int materialCount;
        public bool receiveShadows;
        public int lightmapIndex;
        public bool hasMissingMaterial;
    }

    [System.Serializable]
    public class SpriteInfo
    {
        public string name;
        public string path;
        public bool enabled;
        public string spriteName;
        public string color;
        public string sortingLayerName;
        public int sortingOrder;
        public bool flipX;
        public bool flipY;
        public string drawMode;
        public string maskInteraction;
        public string spriteSortPoint;
    }

    [System.Serializable]
    public class MeshInfo
    {
        public string name;
        public string path;
        public string meshName;
        public int vertexCount;
        public int subMeshCount;
        public int triangleCount;
        public bool hasNormals;
        public bool hasTangents;
        public bool hasUVs;
        public bool hasColors;
    }

    [System.Serializable]
    public class MissingComponentInfo
    {
        public string name;
        public string path;
    }
}