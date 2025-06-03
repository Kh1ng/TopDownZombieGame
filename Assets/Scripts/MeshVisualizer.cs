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
    [SerializeField] private Vector3 headOffset = new Vector3(0f, 0.8f, 0f);
    
    // Animation variables
    [SerializeField] private float frameRate = 10f;
    private float frameTimer = 0f;
    private int currentFrame = 0;
    private bool isAnimating = false;
    private List<Vector2Int> bodyFrames = new List<Vector2Int>();
    private List<Vector2Int> headFrames = new List<Vector2Int>();
    
    // References
    private MeshFilter bodyMeshFilter;
    private MeshFilter headMeshFilter;
    private GameObject headObject;
    private bool isFlipped = false;

    // Direction constants
    public static readonly Vector2Int SOUTH = new Vector2Int(0, 0);
    public static readonly Vector2Int EAST = new Vector2Int(0, 1);
    public static readonly Vector2Int NORTH = new Vector2Int(0, 2);
    
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

    void Update()
    {
        // Handle animation if we're animating
        if (isAnimating && bodyFrames.Count > 0)
        {
            frameTimer += Time.deltaTime;
            if (frameTimer >= 1f / frameRate)
            {
                frameTimer = 0f;
                currentFrame = (currentFrame + 1) % bodyFrames.Count;
                
                // Update the sprite cells
                Vector2Int bodyFrame = bodyFrames[currentFrame];
                Vector2Int headFrame = headFrames[currentFrame];
                
                SetBodySpriteCell(bodyFrame.x, bodyFrame.y);
                SetHeadSpriteCell(headFrame.x, headFrame.y);
            }
        }
    }
    
    // Set up animation frames for a specific direction
    public void SetupDirectionalAnimation(Vector2 direction)
    {
        // Clear previous animation
        bodyFrames.Clear();
        headFrames.Clear();
        currentFrame = 0;
        frameTimer = 0f;
        
        // Determine which direction based on input
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            if (direction.x > 0)
            {
                // East/right animation
                SetupEastAnimation(false);
            }
            else
            {
                // West/left animation (flipped East)
                SetupEastAnimation(true);
            }
        }
        else
        {
            if (direction.y > 0)
            {
                // North/up animation
                SetupNorthAnimation();
            }
            else
            {
                // South/down animation
                SetupSouthAnimation();
            }
        }
        
        isAnimating = bodyFrames.Count > 0;
    }
    
    private void SetupSouthAnimation()
    {
        // For walking south animation
        FlipSprites(false); // Reset flip
        
        // If you have multiple frames for walking, add them here
        // For now, we'll just use the single South frame
        bodyFrames.Add(new Vector2Int(0, 0));  // Body south frame
        headFrames.Add(new Vector2Int(1, 0));  // Head south frame
        
        // Apply the first frame immediately
        SetBodySpriteCell(0, 0);
        SetHeadSpriteCell(1, 0);
    }
    
    private void SetupEastAnimation(bool flip)
    {
        // For walking east/west animation
        FlipSprites(flip);
        
        // Add animation frames
        bodyFrames.Add(new Vector2Int(0, 1));  // Body east frame
        headFrames.Add(new Vector2Int(1, 1));  // Head east frame
        
        // Apply the first frame immediately
        SetBodySpriteCell(0, 1);
        SetHeadSpriteCell(1, 1);
    }
    
    private void SetupNorthAnimation()
    {
        // For walking north animation
        FlipSprites(false); // Reset flip
        
        // Add animation frames
        bodyFrames.Add(new Vector2Int(0, 2));  // Body north frame
        headFrames.Add(new Vector2Int(1, 2));  // Head north frame
        
        // Apply the first frame immediately
        SetBodySpriteCell(0, 2);
        SetHeadSpriteCell(1, 2);
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

    public void FlipSprites(bool flip)
    {
        // Only proceed if the flip state is changing
        if (isFlipped == flip) return;
        
        isFlipped = flip;
        
        // Adjust the body mesh vertices instead of scaling
        if (bodyMeshFilter && bodyMeshFilter.mesh)
        {
            Mesh mesh = bodyMeshFilter.mesh;
            Vector3[] vertices = mesh.vertices;
            
            for (int i = 0; i < vertices.Length; i++)
            {
                // Flip around the center of the mesh
                vertices[i].x = bodyWidth - vertices[i].x;
            }
            
            mesh.vertices = vertices;
            mesh.RecalculateBounds();
        }
        
        // Do the same for the head mesh
        if (headMeshFilter && headMeshFilter.mesh)
        {
            Mesh mesh = headMeshFilter.mesh;
            Vector3[] vertices = mesh.vertices;
            
            for (int i = 0; i < vertices.Length; i++)
            {
                // Flip around the center of the mesh
                vertices[i].x = headWidth - vertices[i].x;
            }
            
            mesh.vertices = vertices;
            mesh.RecalculateBounds();
            
            // Adjust head position
            Vector3 headPos = headObject.transform.localPosition;
            headPos.x = flip ? -headOffset.x : headOffset.x;
            headObject.transform.localPosition = headPos;
        }
    }
    
    public bool IsFlipped()
    {
        return isFlipped;
    }
}
