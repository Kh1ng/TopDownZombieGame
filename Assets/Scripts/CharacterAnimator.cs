using UnityEngine;
using System.Collections.Generic;

public class CharacterAnimator : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float frameRate = 10f;
    [SerializeField] private bool enableWalkAnimation = true;
    
    // Bounce animation variables
    [Header("Bounce Animation")]
    [SerializeField] private float bounceHeight = 0.1f;
    [SerializeField] private float bounceSpeed = 10f;
    
    // Breathing animation variables
    [Header("Breathing Animation")]
    [SerializeField] private float breathingHeight = 0.02f;
    [SerializeField] private float breathingSpeed = 2f;
    [SerializeField] private bool enableBreathingAnimation = true;
    
    // Animation state
    private float frameTimer = 0f;
    private int currentFrame = 0;
    private bool isAnimating = false;
    private bool isWalking = false;
    private List<Vector2Int> bodyFrames = new List<Vector2Int>();
    private List<Vector2Int> headFrames = new List<Vector2Int>();
    
    // References
    private MeshVisualizer meshVisualizer;
    private GameObject visualContainer;
    private Vector3 originalBodyPosition;    void Awake()
    {
        // Find MeshVisualizer component
        meshVisualizer = GetComponent<MeshVisualizer>();
        if (meshVisualizer == null)
        {
            meshVisualizer = GetComponentInChildren<MeshVisualizer>();
        }
        
        if (meshVisualizer == null)
        {
            Debug.LogError("CharacterAnimator requires a MeshVisualizer component on this GameObject or its children!");
            enabled = false;
            return;
        }
        
        Debug.Log("CharacterAnimator found MeshVisualizer: " + meshVisualizer.name);
    }
    
    void Start()
    {
        // Get the visual container from MeshVisualizer
        visualContainer = meshVisualizer.GetVisualContainer();
        if (visualContainer == null)
        {
            Debug.LogError("CharacterAnimator could not get visual container from MeshVisualizer!");
            enabled = false;
            return;
        }
        
        // Store the original position for animations
        originalBodyPosition = visualContainer.transform.localPosition;
        
        Debug.Log("CharacterAnimator initialized successfully with visual container: " + visualContainer.name);
    }
    
    void Update()
    {
        HandlePositionAnimation();
        HandleFrameAnimation();
    }
    
    private void HandlePositionAnimation()
    {
        if (visualContainer == null) return;
        
        if (isWalking && enableWalkAnimation)
        {
            // Walking bounce animation
            float bounce = Mathf.Sin(Time.time * bounceSpeed) * bounceHeight;
            
            Vector3 visualPos = originalBodyPosition;
            visualPos.y += bounce;
            visualContainer.transform.localPosition = visualPos;
        }
        else if (enableBreathingAnimation)
        {
            // Idle breathing animation
            float breathing = Mathf.Sin(Time.time * breathingSpeed) * breathingHeight;
            
            Vector3 visualPos = originalBodyPosition;
            visualPos.y += breathing;
            visualContainer.transform.localPosition = visualPos;
        }
        else
        {
            // No animation - return to original position
            visualContainer.transform.localPosition = originalBodyPosition;
        }
    }
    
    private void HandleFrameAnimation()
    {
        // Handle sprite frame animation if we're animating
        if (isAnimating && bodyFrames.Count > 0 && meshVisualizer != null)
        {
            frameTimer += Time.deltaTime;
            if (frameTimer >= 1f / frameRate)
            {
                frameTimer = 0f;
                currentFrame = (currentFrame + 1) % bodyFrames.Count;
                
                // Update the sprite cells through MeshVisualizer
                Vector2Int bodyFrame = bodyFrames[currentFrame];
                Vector2Int headFrame = headFrames[currentFrame];
                
                meshVisualizer.SetBodySpriteCell(bodyFrame.x, bodyFrame.y);
                meshVisualizer.SetHeadSpriteCell(headFrame.x, headFrame.y);
            }
        }
    }    // Set up animation frames for a specific direction
    public void SetupDirectionalAnimation(Vector2 direction)
    {
        if (meshVisualizer == null)
        {
            Debug.LogWarning("CharacterAnimator: meshVisualizer is null!");
            return;
        }
        
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
        
        // Start walking animation
        isWalking = true;
        isAnimating = enableWalkAnimation && bodyFrames.Count > 1;
    }
    
    private void SetupSouthAnimation()
    {
        // For walking south animation
        meshVisualizer.FlipSprites(false); // Reset flip
        
        // Set head position for south direction
        meshVisualizer.SetHeadPositionForDirection("south");
        
        // Add animation frames
        bodyFrames.Add(new Vector2Int(0, 0));  // Body south frame
        headFrames.Add(new Vector2Int(1, 0));  // Head south frame
        
        // Apply the first frame immediately
        meshVisualizer.SetBodySpriteCell(0, 0);
        meshVisualizer.SetHeadSpriteCell(1, 0);
    }
    
    private void SetupEastAnimation(bool flip)
    {
        // For walking east/west animation
        meshVisualizer.FlipSprites(flip);
        
        // Set head position for east or west direction
        if (flip)
        {
            meshVisualizer.SetHeadPositionForDirection("west");
        }
        else
        {
            meshVisualizer.SetHeadPositionForDirection("east");
        }
        
        // Add animation frames
        bodyFrames.Add(new Vector2Int(0, 1));  // Body east frame
        headFrames.Add(new Vector2Int(1, 1));  // Head east frame
        
        // Apply the first frame immediately
        meshVisualizer.SetBodySpriteCell(0, 1);
        meshVisualizer.SetHeadSpriteCell(1, 1);
    }
    
    private void SetupNorthAnimation()
    {
        // For walking north animation
        meshVisualizer.FlipSprites(false); // Reset flip
        
        // Set head position for north direction
        meshVisualizer.SetHeadPositionForDirection("north");
        
        // Add animation frames
        bodyFrames.Add(new Vector2Int(0, 2));  // Body north frame
        headFrames.Add(new Vector2Int(1, 2));  // Head north frame
        
        // Apply the first frame immediately
        meshVisualizer.SetBodySpriteCell(0, 2);
        meshVisualizer.SetHeadSpriteCell(1, 2);
    }    // Call this when character stops moving
    public void StopWalking()
    {
        isWalking = false;
        isAnimating = false;
        
        // Set to idle frame (first frame of current direction)
        if (bodyFrames.Count > 0 && meshVisualizer != null)
        {
            meshVisualizer.SetBodySpriteCell(bodyFrames[0].x, bodyFrames[0].y);
            meshVisualizer.SetHeadSpriteCell(headFrames[0].x, headFrames[0].y);
        }
    }
}
