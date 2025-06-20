using UnityEngine;
using System.Collections.Generic;

public class MeshVisualizer : MonoBehaviour
{
    [SerializeField] private Material spritesheetMaterial;
    [SerializeField] private int spritesheetColumns = 2;
    [SerializeField] private int spritesheetRows = 3;
    
    // Body sprite settings
    [SerializeField] private int bodyColumnIndex = 0;
    [SerializeField] private int bodyRowIndex = 0;
    [SerializeField] private float bodyWidth = 1f;
    [SerializeField] private float bodyHeight = 1f;
    
    // Head sprite settings
    [SerializeField] private int headColumnIndex = 1;
    [SerializeField] private int headRowIndex = 0;
    [SerializeField] private float headWidth = 1f;
    [SerializeField] private float headHeight = 1f;
    
    // Head positioning variables - individual offsets for each direction
    [Header("Head Positioning Per Direction")]
    [SerializeField] private Vector3 headOffsetSouth = new Vector3(-0.11f, 0.8f, -0.1f);  // Down
    [SerializeField] private Vector3 headOffsetNorth = new Vector3(-0.11f, 0.8f, 0.1f);   // Up (behind body)
    [SerializeField] private Vector3 headOffsetEast = new Vector3(-0.11f, 0.8f, -0.1f);   // Right
    [SerializeField] private Vector3 headOffsetWest = new Vector3(0.0f, 0.8f, -0.1f);     // Left (flipped right)
      // Store the original values
    private Vector3 originalHeadOffsetSouth;
    private Vector3 originalHeadOffsetNorth;
    private Vector3 originalHeadOffsetEast;
    private Vector3 originalHeadOffsetWest;
      // References
    private MeshFilter bodyMeshFilter;
    private MeshFilter headMeshFilter;
    private GameObject headObject;
    private GameObject visualContainer; // Container for visual bouncing
    private bool isFlipped = false;

    // Direction constants
    public static readonly Vector2Int SOUTH = new Vector2Int(0, 0);
    public static readonly Vector2Int EAST = new Vector2Int(0, 1);
    public static readonly Vector2Int NORTH = new Vector2Int(0, 2);    void Awake()
    {
        Debug.Log("MeshVisualizer Awake - Creating visual container");
        
        // Create a visual container for bouncing effect
        if (visualContainer == null)
        {
            visualContainer = new GameObject("VisualContainer");
            visualContainer.transform.SetParent(transform);
            visualContainer.transform.localPosition = Vector3.zero;
            
            // Hide from Inspector to prevent serialization issues
            visualContainer.hideFlags = HideFlags.DontSaveInEditor;
            
            Debug.Log("Visual container created: " + visualContainer.name);
        }
    }
    
    void Start()
    {
        Debug.Log("MeshVisualizer Start");
        
        // Store the original head offset values from inspector
        originalHeadOffsetSouth = headOffsetSouth;
        originalHeadOffsetNorth = headOffsetNorth;
        originalHeadOffsetEast = headOffsetEast;
        originalHeadOffsetWest = headOffsetWest;

        // Get or create MeshFilter for body (attach to visual container)
        bodyMeshFilter = visualContainer.GetComponent<MeshFilter>();
        if (bodyMeshFilter == null)
            bodyMeshFilter = visualContainer.AddComponent<MeshFilter>();
              // Set up body mesh
        Mesh bodyMesh = CreateMesh(bodyWidth, bodyHeight, bodyColumnIndex, bodyRowIndex);
        bodyMeshFilter.mesh = bodyMesh;
        
        // Get or add MeshRenderer for body (attach to visual container)
        MeshRenderer bodyRenderer = visualContainer.GetComponent<MeshRenderer>();
        if (bodyRenderer == null)
            bodyRenderer = visualContainer.AddComponent<MeshRenderer>();
          // Create a child GameObject for the head (child of visual container)
        headObject = new GameObject("Head");
        headObject.transform.SetParent(visualContainer.transform);
        headObject.transform.localPosition = headOffsetSouth;
        
        // Hide from Inspector to prevent serialization issues
        headObject.hideFlags = HideFlags.DontSaveInEditor;
        
        // Add components to head
        headMeshFilter = headObject.AddComponent<MeshFilter>();
        MeshRenderer headRenderer = headObject.AddComponent<MeshRenderer>();
        
        // Set up head mesh
        Mesh headMesh = CreateMesh(headWidth, headHeight, headColumnIndex, headRowIndex);
        headMeshFilter.mesh = headMesh;
          // Assign material to both renderers
        if (spritesheetMaterial != null)
        {
            bodyRenderer.material = spritesheetMaterial;
            headRenderer.material = spritesheetMaterial;        }
        
        // Store original position for bounce animation (visual container position)
        // Note: CharacterAnimator will handle position animations now
    }    void Update()
    {
        // Validate references in case something got destroyed
        ValidateReferences();
        
        // Update head position if inspector values changed
        #if UNITY_EDITOR
        if (headObject != null)
        {
            // Check if any offset values changed and update accordingly
            // This allows real-time tweaking in the inspector during play mode
            if (originalHeadOffsetSouth != headOffsetSouth ||
                originalHeadOffsetNorth != headOffsetNorth ||
                originalHeadOffsetEast != headOffsetEast ||
                originalHeadOffsetWest != headOffsetWest)
            {
                // Update stored values                originalHeadOffsetSouth = headOffsetSouth;
                originalHeadOffsetNorth = headOffsetNorth;
                originalHeadOffsetEast = headOffsetEast;
                originalHeadOffsetWest = headOffsetWest;
            }
        }
        #endif

        // Validate references to ensure nothing is null
        ValidateReferences();
    }    // Public getter for the visual container (used by CharacterAnimator)
    public GameObject GetVisualContainer()
    {
        // If visual container doesn't exist yet, create it
        if (visualContainer == null)
        {
            Debug.LogWarning("Visual container was null, creating it now...");
            visualContainer = new GameObject("VisualContainer");
            visualContainer.transform.SetParent(transform);
            visualContainer.transform.localPosition = Vector3.zero;
            
            // Hide from Inspector to prevent serialization issues
            visualContainer.hideFlags = HideFlags.DontSaveInEditor;
        }
        
        return visualContainer;
    }
      // New method to set head position based on direction
    public void SetHeadPositionForDirection(string direction)
    {
        ValidateReferences();
        
        if (headObject == null) return;
        
        Vector3 targetPosition;
        
        switch (direction.ToLower())
        {
            case "north":
            case "up":
                targetPosition = originalHeadOffsetNorth;
                break;
            case "south":
            case "down":
                targetPosition = originalHeadOffsetSouth;
                break;
            case "east":
            case "right":
                targetPosition = originalHeadOffsetEast;
                break;
            case "west":
            case "left":
                targetPosition = originalHeadOffsetWest;
                break;
            default:
                targetPosition = originalHeadOffsetSouth; // Default fallback
                break;
        }
        
        headObject.transform.localPosition = targetPosition;
    }

    private Mesh CreateMesh(float width, float height, int columnIndex, int rowIndex)
    {
        Mesh mesh = new Mesh();

        // Define vertices for a quad (rectangle)
        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(0, 0, 0);
        vertices[1] = new Vector3(0, height, 0);
        vertices[2] = new Vector3(width, height, 0);
        vertices[3] = new Vector3(width, 0, 0);

        // Calculate UV coordinates for the specific section of the spritesheet
        Vector2[] uvs = CalculateUVsForSpritesheetCell(columnIndex, rowIndex);
        
        // Define triangles (two triangles to form a quad)
        int[] triangles = new int[6];
        triangles[0] = 0;
        triangles[1] = 1;
        triangles[2] = 2;
        
        triangles[3] = 0;
        triangles[4] = 2;
        triangles[5] = 3;

        // Assign data to mesh
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        
        return mesh;
    }

    private Vector2[] CalculateUVsForSpritesheetCell(int column, int row)
    {
        Vector2[] uvs = new Vector2[4];
        
        // Calculate the width and height of each cell in UV space (0-1)
        float cellWidth = 1.0f / spritesheetColumns;
        float cellHeight = 1.0f / spritesheetRows;
        
        // Calculate the starting UV coordinates for the specified cell
        float startU = column * cellWidth;
        float startV = 1.0f - ((row + 1) * cellHeight); // Invert V because textures are flipped vertically
        
        // Define UVs for the quad (in counter-clockwise order)
        uvs[0] = new Vector2(startU, startV);               // Bottom-left
        uvs[1] = new Vector2(startU, startV + cellHeight);  // Top-left
        uvs[2] = new Vector2(startU + cellWidth, startV + cellHeight); // Top-right
        uvs[3] = new Vector2(startU + cellWidth, startV);   // Bottom-right
        
        return uvs;
    }    // Method to change body sprite
    public void SetBodySpriteCell(int column, int row)
    {
        ValidateReferences();
        
        bodyColumnIndex = column;
        bodyRowIndex = row;
        
        // Recalculate UVs
        Vector2[] uvs = CalculateUVsForSpritesheetCell(bodyColumnIndex, bodyRowIndex);
        
        // Update the mesh
        if (bodyMeshFilter && bodyMeshFilter.mesh)
            bodyMeshFilter.mesh.uv = uvs;
    }
      // Method to change head sprite
    public void SetHeadSpriteCell(int column, int row)
    {
        ValidateReferences();
        
        headColumnIndex = column;
        headRowIndex = row;
        
        // Recalculate UVs
        Vector2[] uvs = CalculateUVsForSpritesheetCell(headColumnIndex, headRowIndex);
        
        // Update the mesh
        if (headMeshFilter && headMeshFilter.mesh)
            headMeshFilter.mesh.uv = uvs;
    }
      public void FlipSprites(bool flip)
    {
        ValidateReferences();
        
        // Only proceed if the flip state is changing
        if (isFlipped == flip) return;
        
        isFlipped = flip;
        
        // Adjust the body mesh vertices
        if (bodyMeshFilter && bodyMeshFilter.mesh)
        {
            Mesh mesh = bodyMeshFilter.mesh;
            Vector3[] vertices = mesh.vertices;
            
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i].x = bodyWidth - vertices[i].x;
            }
            
            mesh.vertices = vertices;
            mesh.RecalculateBounds();
        }
        
        // Adjust the head mesh vertices
        if (headMeshFilter && headMeshFilter.mesh)
        {
            Mesh mesh = headMeshFilter.mesh;
            Vector3[] vertices = mesh.vertices;
              for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i].x = headWidth - vertices[i].x;
            }
            
            mesh.vertices = vertices;
            mesh.RecalculateBounds();
        }
        
        // Note: Head position is now handled by SetHeadPositionForDirection()
        // so we don't need to adjust position here anymore
    }

    void OnDestroy()
    {
        // Clean up dynamically created objects to prevent Inspector errors
        if (headObject != null)
        {
            DestroyImmediate(headObject);
            headObject = null;
        }
        
        if (visualContainer != null)
        {
            DestroyImmediate(visualContainer);
            visualContainer = null;
        }
    }
    
    // Validation method to ensure all references are valid
    private void ValidateReferences()
    {
        if (visualContainer == null)
        {
            Debug.LogWarning("VisualContainer became null, recreating...");
            visualContainer = new GameObject("VisualContainer");
            visualContainer.transform.SetParent(transform);
            visualContainer.transform.localPosition = Vector3.zero;
            visualContainer.hideFlags = HideFlags.DontSaveInEditor;
        }
        
        if (headObject == null && visualContainer != null)
        {
            Debug.LogWarning("HeadObject became null, recreating...");
            headObject = new GameObject("Head");
            headObject.transform.SetParent(visualContainer.transform);
            headObject.transform.localPosition = headOffsetSouth;
            headObject.hideFlags = HideFlags.DontSaveInEditor;
            
            // Re-add components
            headMeshFilter = headObject.AddComponent<MeshFilter>();
            MeshRenderer headRenderer = headObject.AddComponent<MeshRenderer>();
            
            // Re-setup head mesh and material
            if (spritesheetMaterial != null)
            {
                Mesh headMesh = CreateMesh(headWidth, headHeight, headColumnIndex, headRowIndex);
                headMeshFilter.mesh = headMesh;
                headRenderer.material = spritesheetMaterial;
            }
        }
    }
}
