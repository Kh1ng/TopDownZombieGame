using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class InputSystemDumper : MonoBehaviour
{
    [ContextMenu("Dump Input System Info")]
    void DumpInputInfo()
    {
        var inputDump = new InputDumpData();
        
        // Check for Input System package presence
        inputDump.hasInputSystemPackage = System.Type.GetType("UnityEngine.InputSystem.InputSystem, Unity.InputSystem") != null;
        inputDump.hasLegacyInput = System.Type.GetType("UnityEngine.Input, UnityEngine") != null;
        
        // Detect input modules in the scene
        var standaloneModules = FindObjectsOfType<UnityEngine.EventSystems.StandaloneInputModule>();
        var inputSystemModules = FindObjectsOfType<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        
        inputDump.hasStandaloneInputModule = standaloneModules.Length > 0;
        inputDump.hasInputSystemUIModule = inputSystemModules.Length > 0;
        
        // Report all input-related components
        foreach (var module in standaloneModules)
        {
            inputDump.legacyInputModules.Add(new InputModuleInfo {
                gameObjectName = module.gameObject.name,
                path = GetFullPath(module.transform),
                horizontalAxis = module.horizontalAxis,
                verticalAxis = module.verticalAxis,
                submitButton = module.submitButton,
                cancelButton = module.cancelButton
            });
        }
        
        foreach (var module in inputSystemModules)
        {
            inputDump.newInputModules.Add(new InputSystemModuleInfo {
                gameObjectName = module.gameObject.name,
                path = GetFullPath(module.transform),
                moveRepeatDelay = module.moveRepeatDelay,
                moveRepeatRate = module.moveRepeatRate,
                trackedDeviceDragThresholdMultiplier = module.trackedDeviceDragThresholdMultiplier
            });
        }
        
        // Find scripts that likely use input
        var allScripts = FindObjectsOfType<MonoBehaviour>();
        foreach (var script in allScripts)
        {
            if (script == null) continue;
            
            var scriptType = script.GetType();
            string scriptText = "";
            
            #if UNITY_EDITOR
            var monoScript = MonoScript.FromMonoBehaviour(script);
            if (monoScript != null) {
                scriptText = monoScript.text;
            }
            #endif
            
            bool usesLegacyInput = scriptText.Contains("Input.Get") || 
                                 scriptText.Contains("Input.mouse") ||
                                 scriptText.Contains("Input.GetAxis");
                                 
            bool usesNewInputSystem = scriptText.Contains("UnityEngine.InputSystem") ||
                                    scriptText.Contains("Keyboard.current") ||
                                    scriptText.Contains("Gamepad.current") ||
                                    scriptText.Contains("InputAction");
            
            if (usesLegacyInput || usesNewInputSystem)
            {
                inputDump.inputScripts.Add(new InputScriptInfo {
                    name = script.GetType().Name,
                    gameObjectName = script.gameObject.name,
                    path = GetFullPath(script.transform),
                    usesLegacyInput = usesLegacyInput,
                    usesNewInputSystem = usesNewInputSystem
                });
            }
        }
        
        string json = JsonUtility.ToJson(inputDump, true);
        string path = Path.Combine(Application.dataPath, "input_dump.json");
        File.WriteAllText(path, json);
        Debug.Log("Input system dump saved to: " + path);
    }
    
    string GetFullPath(Transform t)
    {
        if (t.parent == null) return t.name;
        return GetFullPath(t.parent) + "/" + t.name;
    }
    
    [System.Serializable]
    public class InputDumpData
    {
        public bool hasInputSystemPackage;
        public bool hasLegacyInput;
        public bool hasStandaloneInputModule;
        public bool hasInputSystemUIModule;
        public List<InputModuleInfo> legacyInputModules = new List<InputModuleInfo>();
        public List<InputSystemModuleInfo> newInputModules = new List<InputSystemModuleInfo>();
        public List<InputScriptInfo> inputScripts = new List<InputScriptInfo>();
    }
    
    [System.Serializable]
    public class InputModuleInfo
    {
        public string gameObjectName;
        public string path;
        public string horizontalAxis;
        public string verticalAxis;
        public string submitButton;
        public string cancelButton;
    }
    
    [System.Serializable]
    public class InputSystemModuleInfo
    {
        public string gameObjectName;
        public string path;
        public float moveRepeatDelay;
        public float moveRepeatRate;
        public float trackedDeviceDragThresholdMultiplier;
    }
    
    [System.Serializable]
    public class InputScriptInfo
    {
        public string name;
        public string gameObjectName;
        public string path;
        public bool usesLegacyInput;
        public bool usesNewInputSystem;
    }
}