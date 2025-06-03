using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneComponentDumperPosition : MonoBehaviour
{
    [ContextMenu("Dump Position Info To JSON")]
    void DumpPositions()
    {
        var allObjects = FindObjectsOfType<GameObject>();
        var positionList = new List<PositionData>();

        foreach (var obj in allObjects)
        {
            var posData = new PositionData
            {
                name = obj.name,
                path = GetFullPath(obj.transform),
                position = FormatVector(obj.transform.position),
                rotation = FormatVector(obj.transform.eulerAngles),
                localScale = FormatVector(obj.transform.localScale),
                worldToLocalMatrix = MatrixToString(obj.transform.worldToLocalMatrix),
                localToWorldMatrix = MatrixToString(obj.transform.localToWorldMatrix),
                hasRigidbody = false,
                hasCollider = false,
                boundingBox = GetBoundingBox(obj),
                centerOfMass = "(0, 0, 0)"
            };
            
            // Get detailed physics data if available
            var rigidbody2D = obj.GetComponent<Rigidbody2D>();
            if (rigidbody2D != null)
            {
                posData.hasRigidbody = true;
                posData.rigidbodyData = new RigidbodyData
                {
                    position = FormatVector2(rigidbody2D.position),
                    rotation = rigidbody2D.rotation.ToString("F2"),
                    linearVelocity = FormatVector2(rigidbody2D.linearVelocity),
                    angularVelocity = rigidbody2D.angularVelocity.ToString("F2"),
                    mass = rigidbody2D.mass.ToString("F2"),
                    centerOfMass = FormatVector2(rigidbody2D.centerOfMass),
                    worldCenterOfMass = FormatVector2(rigidbody2D.worldCenterOfMass),
                    inertia = rigidbody2D.inertia.ToString("F6"),
                    bodyType = rigidbody2D.bodyType.ToString()
                };
                
                posData.centerOfMass = FormatVector2(rigidbody2D.worldCenterOfMass);
            }

            // Add collider data if available
            var colliders2D = obj.GetComponents<Collider2D>();
            if (colliders2D.Length > 0)
            {
                posData.hasCollider = true;
                posData.colliderData = new List<ColliderData>();
                
                foreach (var collider in colliders2D)
                {
                    var colliderData = new ColliderData
                    {
                        type = collider.GetType().Name,
                        bounds = FormatBounds(collider.bounds),
                        offset = "(0, 0)"
                    };
                    
                    // Get specific collider type data
                    if (collider is BoxCollider2D boxCollider)
                    {
                        colliderData.size = FormatVector2(boxCollider.size);
                        colliderData.offset = FormatVector2(boxCollider.offset);
                    }
                    else if (collider is CircleCollider2D circleCollider)
                    {
                        colliderData.radius = circleCollider.radius.ToString("F2");
                        colliderData.offset = FormatVector2(circleCollider.offset);
                    }
                    else if (collider is CapsuleCollider2D capsuleCollider)
                    {
                        colliderData.size = FormatVector2(capsuleCollider.size);
                        colliderData.offset = FormatVector2(capsuleCollider.offset);
                    }
                    else if (collider is EdgeCollider2D edgeCollider)
                    {
                        colliderData.pointCount = edgeCollider.pointCount.ToString();
                        colliderData.offset = FormatVector2(edgeCollider.offset);
                    }
                    else if (collider is PolygonCollider2D polygonCollider)
                    {
                        colliderData.pointCount = polygonCollider.points.Length.ToString();
                        colliderData.offset = FormatVector2(polygonCollider.offset);
                    }
                    
                    posData.colliderData.Add(colliderData);
                }
            }
            
            // Get renderer bounds if available
            var renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                posData.rendererBounds = FormatBounds(renderer.bounds);
            }
            
            positionList.Add(posData);
        }

        var dumpData = new PositionDumpData
        {
            sceneObjects = positionList,
            sceneName = SceneManager.GetActiveScene().name,
            sceneCount = SceneManager.sceneCount,
            physicsSettings = new PhysicsSettingsData
            {
                gravity = FormatVector2(Physics2D.gravity),
                velocityIterations = Physics2D.velocityIterations,
                positionIterations = Physics2D.positionIterations,
                defaultContactOffset = Physics2D.defaultContactOffset.ToString("F4")
            }
        };

        // Add spatial analysis
        dumpData.spatialAnalysis = AnalyzeSceneSpatialData(positionList);

        string json = JsonUtility.ToJson(dumpData, true);
        string path = Path.Combine(Application.dataPath, "position_dump.json");
        File.WriteAllText(path, json);
        Debug.Log("Position dump saved to: " + path);
    }

    private SpatialAnalysisData AnalyzeSceneSpatialData(List<PositionData> positions)
    {
        var analysis = new SpatialAnalysisData();
        
        if (positions.Count == 0)
            return analysis;

        // Find scene bounds
        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;
        
        foreach (var pos in positions)
        {
            string[] parts = pos.position.Replace("(", "").Replace(")", "").Split(',');
            if (parts.Length >= 2)
            {
                if (float.TryParse(parts[0], out float x) && float.TryParse(parts[1], out float y))
                {
                    minX = Mathf.Min(minX, x);
                    minY = Mathf.Min(minY, y);
                    maxX = Mathf.Max(maxX, x);
                    maxY = Mathf.Max(maxY, y);
                }
            }
        }
        
        analysis.sceneBounds = $"({minX:F2}, {minY:F2}) to ({maxX:F2}, {maxY:F2})";
        analysis.sceneWidth = (maxX - minX).ToString("F2");
        analysis.sceneHeight = (maxY - minY).ToString("F2");
        analysis.sceneCenter = $"({(minX + maxX) / 2:F2}, {(minY + maxY) / 2:F2})";
        
        // Find potential issues
        foreach (var pos in positions)
        {
            if (pos.hasRigidbody && pos.hasCollider)
            {
                // Check for very large or very small objects
                string[] parts = pos.localScale.Replace("(", "").Replace(")", "").Split(',');
                if (parts.Length >= 2)
                {
                    if (float.TryParse(parts[0], out float x) && float.TryParse(parts[1], out float y))
                    {
                        if (x > 10f || y > 10f)
                        {
                            analysis.largeObjects.Add($"{pos.name}: scale {pos.localScale}");
                        }
                        if (x < 0.1f || y < 0.1f)
                        {
                            analysis.verySmallObjects.Add($"{pos.name}: scale {pos.localScale}");
                        }
                    }
                }
                
                // Check for objects far from origin
                string[] posParts = pos.position.Replace("(", "").Replace(")", "").Split(',');
                if (posParts.Length >= 2)
                {
                    if (float.TryParse(posParts[0], out float x) && float.TryParse(posParts[1], out float y))
                    {
                        float distanceFromOrigin = Mathf.Sqrt(x*x + y*y);
                        if (distanceFromOrigin > 1000f)
                        {
                            analysis.farFromOriginObjects.Add($"{pos.name}: distance {distanceFromOrigin:F2}");
                        }
                    }
                }
            }
        }
        
        return analysis;
    }

    private string GetBoundingBox(GameObject obj)
    {
        Bounds bounds = new Bounds();
        bool initialized = false;
        
        // Combine bounds from all renderers and colliders
        Renderer[] renderers = obj.GetComponents<Renderer>();
        foreach (var renderer in renderers)
        {
            if (!initialized)
            {
                bounds = renderer.bounds;
                initialized = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }
        
        Collider2D[] colliders = obj.GetComponents<Collider2D>();
        foreach (var collider in colliders)
        {
            if (!initialized)
            {
                bounds = collider.bounds;
                initialized = true;
            }
            else
            {
                bounds.Encapsulate(collider.bounds);
            }
        }
        
        if (!initialized)
            return "No bounds";
            
        return FormatBounds(bounds);
    }

    private string FormatVector(Vector3 vector)
    {
        return $"({vector.x:F2}, {vector.y:F2}, {vector.z:F2})";
    }
    
    private string FormatVector2(Vector2 vector)
    {
        return $"({vector.x:F2}, {vector.y:F2})";
    }
    
    private string FormatBounds(Bounds bounds)
    {
        return $"Center: {FormatVector(bounds.center)}, Extents: {FormatVector(bounds.extents)}";
    }
    
    private string MatrixToString(Matrix4x4 matrix)
    {
        return $"[{matrix.m00:F2},{matrix.m01:F2},{matrix.m02:F2},{matrix.m03:F2}]" +
               $"[{matrix.m10:F2},{matrix.m11:F2},{matrix.m12:F2},{matrix.m13:F2}]" +
               $"[{matrix.m20:F2},{matrix.m21:F2},{matrix.m22:F2},{matrix.m23:F2}]" +
               $"[{matrix.m30:F2},{matrix.m31:F2},{matrix.m32:F2},{matrix.m33:F2}]";
    }

    string GetFullPath(Transform t)
    {
        if (t.parent == null) return t.name;
        return GetFullPath(t.parent) + "/" + t.name;
    }

    [System.Serializable]
    public class PositionDumpData
    {
        public List<PositionData> sceneObjects;
        public string sceneName;
        public int sceneCount;
        public PhysicsSettingsData physicsSettings;
        public SpatialAnalysisData spatialAnalysis;
    }

    [System.Serializable]
    public class PositionData
    {
        public string name;
        public string path;
        public string position;
        public string rotation;
        public string localScale;
        public string worldToLocalMatrix;
        public string localToWorldMatrix;
        public bool hasRigidbody;
        public bool hasCollider;
        public string boundingBox;
        public string centerOfMass;
        public string rendererBounds;
        public RigidbodyData rigidbodyData;
        public List<ColliderData> colliderData;
    }

    [System.Serializable]
    public class RigidbodyData
    {
        public string position;
        public string rotation;
        public string linearVelocity;
        public string angularVelocity;
        public string mass;
        public string centerOfMass;
        public string worldCenterOfMass;
        public string inertia;
        public string bodyType;
    }

    [System.Serializable]
    public class ColliderData
    {
        public string type;
        public string bounds;
        public string offset;
        public string size;
        public string radius;
        public string pointCount;
    }

    [System.Serializable]
    public class PhysicsSettingsData
    {
        public string gravity;
        public int velocityIterations;
        public int positionIterations;
        public string defaultContactOffset;
    }

    [System.Serializable]
    public class SpatialAnalysisData
    {
        public string sceneBounds;
        public string sceneWidth;
        public string sceneHeight;
        public string sceneCenter;
        public List<string> largeObjects = new List<string>();
        public List<string> verySmallObjects = new List<string>();
        public List<string> farFromOriginObjects = new List<string>();
    }
}