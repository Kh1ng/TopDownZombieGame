using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Reflection;
using System.Linq;

public class SceneComponentDumper : MonoBehaviour
{
    // Move this outside the method to class level
    private static readonly HashSet<string> PropertiesToSkip = new HashSet<string>
    {
        "transform", "gameObject", "tag", "name", "hideFlags", 
        "hierarchyCapacity", "hierarchyCount", "hasChanged",
        "worldToLocalMatrix", "localToWorldMatrix",
        "position", "rotation", "localScale", "localPosition", "localRotation",
        "eulerAngles", "localEulerAngles", "right", "up", "forward", "lossyScale"
    };

    [ContextMenu("Dump Scene Info To JSON")]
    void DumpScene()
    {
        var allObjects = FindObjectsOfType<GameObject>();
        var dumpList = new List<DumpedObject>();

        foreach (var obj in allObjects)
        {
            var dump = new DumpedObject
            {
                name = obj.name,
                path = GetFullPath(obj.transform),
                active = obj.activeInHierarchy,
                parent = obj.transform.parent ? obj.transform.parent.name : null,
                tag = obj.tag,
                layer = obj.layer,
                isStatic = obj.isStatic,
                components = new List<ComponentInfo>(),
                childCount = obj.transform.childCount,
                siblingIndex = obj.transform.GetSiblingIndex(),
            };

            foreach (var comp in obj.GetComponents<Component>())
            {
                if (comp == null)
                {
                    dump.components.Add(new ComponentInfo { type = "MissingScript", fields = new List<ComponentField>() });
                    continue;
                }

                var compInfo = new ComponentInfo
                {
                    type = comp.GetType().ToString(),
                    fields = new List<ComponentField>()
                };

                foreach (var field in comp.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    object value = field.GetValue(comp);
                    compInfo.fields.Add(new ComponentField { key = field.Name, value = value != null ? value.ToString() : "null" });

                    // For each serialized field that references another object, add to references list
                    if (value is UnityEngine.Object objRef && objRef != null)
                    {
                        string refPath = objRef is GameObject go
                            ? GetFullPath(go.transform)
                            : objRef.name;
                        compInfo.references.Add($"{field.Name}: {refPath}");
                    }
                }

                // Add a list of property names to skip (common noise properties)
                foreach (var prop in comp.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (prop.CanRead && prop.GetIndexParameters().Length == 0 && !PropertiesToSkip.Contains(prop.Name))
                    {
                        // Only add properties not in the skip list
                        try
                        {
                            object value = prop.GetValue(comp, null);
                            compInfo.fields.Add(new ComponentField { key = prop.Name, value = value != null ? value.ToString() : "null" });
                        }
                        catch { /* Ignore properties that throw */ }
                    }
                }

                // Enhance the DumpScene method to capture values for key components:

                // For EventSystem components - capture the input system type
                if (comp is UnityEngine.EventSystems.EventSystem ||
                    comp is UnityEngine.EventSystems.StandaloneInputModule ||
                    comp is UnityEngine.InputSystem.UI.InputSystemUIInputModule)
                {
                    // Add special flag fields for quick identification
                    compInfo.fields.Add(new ComponentField { key = "_inputSystemType", value = "Legacy Input Module" });
                    if (comp is UnityEngine.InputSystem.UI.InputSystemUIInputModule)
                        compInfo.fields.Add(new ComponentField { key = "_inputSystemType", value = "New Input System" });
                }

                // For input-related scripts - capture key settings
                if (comp.GetType().Name.Contains("Controller") ||
                    comp.GetType().Name.Contains("Input"))
                {
                    compInfo.fields.Add(new ComponentField { key = "_usesInputSystem", value = comp.GetType().GetMethods()
                        .Any(m => m.Name.Contains("Keyboard") || m.Name.Contains("Gamepad"))
                        ? "New Input System" : "Legacy Input" });
                }

                // Special handling for common components to capture numerical values
                else if (comp is Rigidbody2D rb2d)
                {
                    // Keep only essential information, not position-related data
                    compInfo.fields.Add(new ComponentField { key = "bodyType", value = rb2d.bodyType.ToString() });
                    compInfo.fields.Add(new ComponentField { key = "simulated", value = rb2d.simulated.ToString() });
                    compInfo.fields.Add(new ComponentField { key = "useAutoMass", value = rb2d.useAutoMass.ToString() });
                    compInfo.fields.Add(new ComponentField { key = "isKinematic", value = rb2d.isKinematic.ToString() });
                    // Skip position, velocity, rotation, etc.
                }
                else if (comp is Collider2D collider2d)
                {
                    compInfo.fields.Add(new ComponentField { key = "isTrigger", value = collider2d.isTrigger.ToString() });
                    compInfo.fields.Add(new ComponentField { key = "enabled", value = collider2d.enabled.ToString() });
                    compInfo.fields.Add(new ComponentField { key = "density", value = collider2d.density.ToString("F2") });

                    if (collider2d is BoxCollider2D boxCollider)
                    {
                        compInfo.fields.Add(new ComponentField { key = "size", value = boxCollider.size.ToString("F2") });
                        compInfo.fields.Add(new ComponentField { key = "offset", value = boxCollider.offset.ToString("F2") });
                    }
                    else if (collider2d is CircleCollider2D circleCollider)
                    {
                        compInfo.fields.Add(new ComponentField { key = "radius", value = circleCollider.radius.ToString("F2") });
                        compInfo.fields.Add(new ComponentField { key = "offset", value = circleCollider.offset.ToString("F2") });
                    }
                }
                else if (comp is SpriteRenderer spriteRenderer)
                {
                    // For SpriteRenderer - only capture the most relevant properties
                    compInfo.fields.Add(new ComponentField { key = "sprite", value = spriteRenderer.sprite ? spriteRenderer.sprite.name : "null" });
                    compInfo.fields.Add(new ComponentField { key = "color", value = $"RGBA({spriteRenderer.color.r:F3}, {spriteRenderer.color.g:F3}, {spriteRenderer.color.b:F3}, {spriteRenderer.color.a:F3})" });
                    compInfo.fields.Add(new ComponentField { key = "sortingLayer", value = $"{spriteRenderer.sortingLayerName}:{spriteRenderer.sortingOrder}" });
                    continue; // Skip the reflection-based property dumping
                }
                else if (comp is Camera camera)
                {
                    compInfo.fields.Add(new ComponentField { key = "orthographic", value = camera.orthographic.ToString() });
                    compInfo.fields.Add(new ComponentField { key = "orthographicSize", value = camera.orthographicSize.ToString("F2") });
                    compInfo.fields.Add(new ComponentField { key = "fieldOfView", value = camera.fieldOfView.ToString("F2") });
                    compInfo.fields.Add(new ComponentField { key = "nearClipPlane", value = camera.nearClipPlane.ToString("F2") });
                    compInfo.fields.Add(new ComponentField { key = "farClipPlane", value = camera.farClipPlane.ToString("F2") });
                    compInfo.fields.Add(new ComponentField { key = "depth", value = camera.depth.ToString("F2") });
                    compInfo.fields.Add(new ComponentField { key = "cullingMask", value = camera.cullingMask.ToString() });
                }

                dump.components.Add(compInfo);
            }

            // Add these fields:
            dump.potentialIssues = new List<string>();

            // Then populate during analysis:
            // Check for input system conflicts
            if (obj.GetComponentsInChildren<UnityEngine.EventSystems.StandaloneInputModule>(true).Length > 0 &&
                obj.GetComponentsInChildren<UnityEngine.InputSystem.UI.InputSystemUIInputModule>(true).Length > 0)
            {
                dump.potentialIssues.Add("Mixed input systems: Both legacy and new Input System modules found");
            }

            dumpList.Add(dump);
        }

        var dumpedList = new DumpedObjectList { objects = dumpList };

        // Fill in the scene summary and project settings
        dumpedList.summary.activeScene = SceneManager.GetActiveScene().name;

#if UNITY_EDITOR
        // Input settings
        dumpedList.summary.projectSettings.unityVersion = Application.unityVersion;

        // Get active input handling setting (old vs new Input System)
        var inputManagerAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("ProjectSettings/InputManager.asset");
        if (inputManagerAsset != null)
        {
            var serializedObject = new UnityEditor.SerializedObject(inputManagerAsset);

            // Try different property names for different Unity versions
            var activeInputHandlingProperty = serializedObject.FindProperty("m_ActiveInputHandler");

            if (activeInputHandlingProperty != null)
            {
                dumpedList.summary.projectSettings.input.activeInputHandlingNewSystem = (activeInputHandlingProperty.intValue == 1);
                dumpedList.summary.usingNewInputSystem = (activeInputHandlingProperty.intValue == 1);
            }
            else
            {
                // Alternative detection method for older Unity versions
                bool hasNewInputSystem = System.Type.GetType("UnityEngine.InputSystem.InputSystem, Unity.InputSystem") != null;
                bool legacyInputExists = System.Type.GetType("UnityEngine.Input, UnityEngine") != null;

                // Check if assemblies are loaded that indicate new input system
                var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
                bool inputSystemAssemblyLoaded = assemblies.Any(a => a.GetName().Name == "Unity.InputSystem");

                dumpedList.summary.projectSettings.input.activeInputHandlingNewSystem = inputSystemAssemblyLoaded;
                dumpedList.summary.usingNewInputSystem = inputSystemAssemblyLoaded;

                Debug.LogWarning("Could not find m_ActiveInputHandler property - using assembly detection instead");
            }
        }
        else
        {
            Debug.LogWarning("Could not load InputManager.asset");
        }

        // Physics 2D settings
        var physics2DSettings = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("ProjectSettings/Physics2DSettings.asset");
        if (physics2DSettings != null)
        {
            var physics2DObject = new UnityEditor.SerializedObject(physics2DSettings);
            var velocityIterations = physics2DObject.FindProperty("m_VelocityIterations");
            if (velocityIterations != null)
            {
                dumpedList.summary.projectSettings.physics.velocityIterations = velocityIterations.intValue;
            }
            var positionIterations = physics2DObject.FindProperty("m_PositionIterations");
            if (positionIterations != null)
            {
                dumpedList.summary.projectSettings.physics.positionIterations = positionIterations.intValue;
            }
            var queriesHitTriggers = physics2DObject.FindProperty("m_QueriesHitTriggers");
            if (queriesHitTriggers != null)
            {
                dumpedList.summary.projectSettings.physics.queriesHitTriggers = queriesHitTriggers.boolValue;
            }
        }
        else
        {
            Debug.LogWarning("Could not load Physics2DSettings.asset");
        }

        // Rendering settings
        var graphicsSettings = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("ProjectSettings/GraphicsSettings.asset");
        var graphicsObject = new UnityEditor.SerializedObject(graphicsSettings);
        var pipelineAsset = graphicsObject.FindProperty("m_CustomRenderPipeline").objectReferenceValue;
        dumpedList.summary.projectSettings.rendering.pipelineAsset = pipelineAsset ? pipelineAsset.name : "Built-in Render Pipeline";
        dumpedList.summary.projectSettings.rendering.useSRP = pipelineAsset != null;
        dumpedList.summary.projectSettings.rendering.colorSpace = UnityEditor.PlayerSettings.colorSpace.ToString();

        // Build settings
        dumpedList.summary.projectSettings.build.activeBuildTarget = UnityEditor.EditorUserBuildSettings.activeBuildTarget.ToString();
        dumpedList.summary.projectSettings.build.scenes = UnityEditor.EditorBuildSettings.scenes.Select(s => s.path).ToArray();

        // Get script execution order (most relevant for input handling)
        var scriptSettings = UnityEditor.MonoImporter.GetAllRuntimeMonoScripts()
            .Where(s => UnityEditor.MonoImporter.GetExecutionOrder(s) != 0)
            .ToDictionary(s => s.name, s => UnityEditor.MonoImporter.GetExecutionOrder(s));

        dumpedList.scriptExecutionOrder = scriptSettings;
#else
        dumpedList.summary.projectSettings.unityVersion = Application.unityVersion;
#endif

        // Add analysis of potential issues based on settings
        if (dumpedList.summary.usingNewInputSystem)
        {
            bool foundLegacyInputUsage = dumpList.Any(obj =>
                obj.components.Any(comp =>
                    comp.type.Contains("StandaloneInputModule") && !comp.type.Contains("InputSystemUIInputModule")));

            if (foundLegacyInputUsage)
            {
                dumpedList.summary.potentialIssues.Add("Project is using new Input System but legacy input modules are still present");
            }
        }

        string json = JsonUtility.ToJson(dumpedList, true);
        string path = Path.Combine(Application.dataPath, "scene_dump.json");
        File.WriteAllText(path, json);
        Debug.Log("Scene dump saved to: " + path);

        // Add to DumpScene method at the end
        Debug.Log("For detailed position/transform data, use SceneComponentDumperPosition");
    }

    string GetFullPath(Transform t)
    {
        if (t.parent == null) return t.name;
        return GetFullPath(t.parent) + "/" + t.name;
    }

    [System.Serializable]
    public class DumpedObject
    {
        public string name;
        public string path;
        public bool active;
        public string parent;
        public string tag;
        public int layer;
        public bool isStatic;
        public int childCount;
        public int siblingIndex;
        public List<ComponentInfo> components;

        // Add these fields:
        public List<string> potentialIssues = new List<string>();
    }

    [System.Serializable]
    public class ComponentInfo
    {
        public string type;
        public List<ComponentField> fields = new List<ComponentField>();
        public List<string> references = new List<string>();
    }

    [System.Serializable]
    public class ComponentField
    {
        public string key;
        public string value;
    }

    [System.Serializable]
    public class DumpedObjectList
    {
        public List<DumpedObject> objects;
        public SceneSummary summary = new SceneSummary();

        // Add to DumpedObjectList:
        public Dictionary<string, int> scriptExecutionOrder = new Dictionary<string, int>();
    }

    [System.Serializable]
    public class SceneSummary
    {
        public string activeScene;
        public bool usingNewInputSystem;
        public List<string> potentialIssues = new List<string>();

        // Add these Project Settings fields
        public ProjectSettings projectSettings = new ProjectSettings();
    }

    [System.Serializable]
    public class ProjectSettings
    {
        public InputSettings input = new InputSettings();
        public PhysicsSettings physics = new PhysicsSettings();
        public RenderSettings rendering = new RenderSettings();
        public string unityVersion;
        public BuildSettings build = new BuildSettings();
    }

    [System.Serializable]
    public class InputSettings
    {
        public bool activeInputHandlingNewSystem;
        public bool backButtonLeavesApp;
        public bool enableIMEComposition;
    }

    [System.Serializable]
    public class PhysicsSettings
    {
        public bool auto2DSyncTransforms;
        public float defaultContactOffset;
        public int velocityIterations;
        public int positionIterations;
        public bool queriesHitTriggers;
    }

    [System.Serializable]
    public class RenderSettings
    {
        public string pipelineAsset;
        public bool useSRP;
        public string colorSpace;
    }

    [System.Serializable]
    public class BuildSettings
    {
        public string activeBuildTarget;
        public string[] scenes;
    }
}
