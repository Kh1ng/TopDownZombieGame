using UnityEngine;

public class MeshVisualizer : MonoBehaviour
{
    [SerializeField] private Material spritesheetMaterial;
    [SerializeField] private int spritesheetColumns = 2;
    [SerializeField] private int spritesheetRows = 3;
    
    // Body sprite (first column, first row)
    [SerializeField] private int bodyColumnIndex = 0;
    [SerializeField] private int bodyRowIndex = 0;
    [SerializeField] private float bodyWidth = 1f;
    [SerializeField] private float bodyHeight = 1f;
    
    // Head sprite (second column, first row)
    [SerializeField] private int headColumnIndex = 1;
    [SerializeField] private int headRowIndex = 0;
    [SerializeField] private float headWidth = 1f;
    [SerializeField] private float headHeight = 1f;
    [SerializeField] private Vector3 headOffset = new Vector3(0f, 0.8f, 0f); // Adjust this to position the head
    
    // Reference to store created meshes
    private MeshFilter bodyMeshFilter;
    private MeshFilter headMeshFilter;
    private GameObject headObject;

    void Start()
    {
        Debug.Log("MeshRenderer Start");

        // Get or create MeshFilter for body
        bodyMeshFilter = GetComponent<MeshFilter>();
        if (bodyMeshFilter == null)
            bodyMeshFilter = gameObject.AddComponent<MeshFilter>();
            
        // Set up body mesh
        Mesh bodyMesh = CreateMesh(bodyWidth, bodyHeight, bodyColumnIndex, bodyRowIndex);
        bodyMeshFilter.mesh = bodyMesh;
        
        // Get or add MeshRenderer for body
        MeshRenderer bodyRenderer = GetComponent<MeshRenderer>();
        if (bodyRenderer == null)
            bodyRenderer = gameObject.AddComponent<MeshRenderer>();
        
        // Create a child GameObject for the head
        headObject = new GameObject("Head");
        headObject.transform.SetParent(transform);
        headObject.transform.localPosition = new Vector3(headOffset.x, headOffset.y, -0.1f); // To make head appear in front of body
        
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
            headRenderer.material = spritesheetMaterial;
        }
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
    }

    // Method to change body sprite
    public void SetBodySpriteCell(int column, int row)
    {
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
        headColumnIndex = column;
        headRowIndex = row;
        
        // Recalculate UVs
        Vector2[] uvs = CalculateUVsForSpritesheetCell(headColumnIndex, headRowIndex);
        
        // Update the mesh
        if (headMeshFilter && headMeshFilter.mesh)
            headMeshFilter.mesh.uv = uvs;
    }
    
    // Method to adjust head position at runtime
    public void SetHeadOffset(Vector3 offset)
    {
        headOffset = offset;
        if (headObject)
            headObject.transform.localPosition = headOffset;
    }
}
