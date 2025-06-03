using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LayerAndTagDumper : MonoBehaviour
{
    [ContextMenu("Dump Layer & Tag Info")]
    void DumpLayersAndTags()
    {
        LayerTagDumpData dumpData = new LayerTagDumpData();
        
        // Collect all layers used in the scene
        var allObjects = FindObjectsOfType<GameObject>();
        var layerCounts = new Dictionary<int, int>();
        var tagCounts = new Dictionary<string, int>();
        
        foreach (var obj in allObjects)
        {
            // Count layer usage
            if (!layerCounts.ContainsKey(obj.layer))
                layerCounts[obj.layer] = 0;
            layerCounts[obj.layer]++;
            
            // Count tag usage
            if (!tagCounts.ContainsKey(obj.tag))
                tagCounts[obj.tag] = 0;
            tagCounts[obj.tag]++;
        }
        
        // Fill layer info
        foreach (var layerPair in layerCounts)
        {
            dumpData.layerUsage.Add(new LayerInfo {
                layerId = layerPair.Key,
                layerName = LayerMask.LayerToName(layerPair.Key),
                objectCount = layerPair.Value
            });
        }
        
        // Fill tag info
        foreach (var tagPair in tagCounts)
        {
            dumpData.tagUsage.Add(new TagInfo {
                tagName = tagPair.Key,
                objectCount = tagPair.Value
            });
        }
        
        // Analyze physics layer collisions
        dumpData.collisionMatrix = new bool[32, 32];
        for (int i = 0; i < 32; i++)
        {
            for (int j = 0; j < 32; j++)
            {
                dumpData.collisionMatrix[i, j] = Physics2D.GetIgnoreLayerCollision(i, j) == false;
            }
        }
        
        // Get sorting layers
        dumpData.sortingLayers = new List<SortingLayerInfo>();
        var sortingLayers = SortingLayer.layers;
        foreach (var layer in sortingLayers)
        {
            dumpData.sortingLayers.Add(new SortingLayerInfo {
                id = layer.id,
                name = layer.name,
                value = layer.value
            });
        }
        
        string json = JsonUtility.ToJson(dumpData, true);
        string path = Path.Combine(Application.dataPath, "layer_tag_dump.json");
        File.WriteAllText(path, json);
        Debug.Log("Layer and tag dump saved to: " + path);
    }
    
    [System.Serializable]
    public class LayerTagDumpData
    {
        public List<LayerInfo> layerUsage = new List<LayerInfo>();
        public List<TagInfo> tagUsage = new List<TagInfo>();
        public bool[,] collisionMatrix;
        public List<SortingLayerInfo> sortingLayers = new List<SortingLayerInfo>();
    }
    
    [System.Serializable]
    public class LayerInfo
    {
        public int layerId;
        public string layerName;
        public int objectCount;
    }
    
    [System.Serializable]
    public class TagInfo
    {
        public string tagName;
        public int objectCount;
    }
    
    [System.Serializable]
    public class SortingLayerInfo
    {
        public int id;
        public string name;
        public int value;
    }
}