using UnityEngine;

public class CharacterMovement : MonoBehaviour
{
    private Rigidbody2D rb;
    private Vector2 movement;
    [SerializeField] private float moveSpeed = 5f;
    
    // Add Animator reference alongside MeshVisualizer
    private Animator animator;
    private MeshVisualizer meshVisualizer;
    
    // Add variables to track the current visual direction
    private Vector2 lastDirection = Vector2.down; // Default facing down
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        meshVisualizer = GetComponent<MeshVisualizer>();
        
        // If we couldn't find it on this GameObject, try looking for it in children
        if (meshVisualizer == null)
        {
            meshVisualizer = GetComponentInChildren<MeshVisualizer>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Get input
        movement.x = Input.GetAxisRaw("Horizontal"); // A/D or Left/Right
        movement.y = Input.GetAxisRaw("Vertical");   // W/S or Up/Down
        
        // Update animator parameters
        if (animator != null)
        {
            animator.SetFloat("Horizontal", movement.x);
            animator.SetFloat("Vertical", movement.y);
            
            // Optional: Speed parameter for distinguishing idle vs. moving
            float speed = movement.sqrMagnitude;
            animator.SetFloat("Speed", speed);
        }
        else
        {
            // Fallback to direct method if no animator
            UpdateVisuals();
        }
    }

    // FixedUpdate is called at a fixed time interval and is used for physics calculations
    void FixedUpdate()
    {
        HandleMovement();
    }        
    
    private void HandleMovement()
    {
        rb.MovePosition(rb.position + movement.normalized * moveSpeed * Time.fixedDeltaTime);
    }
    
    // Keep this method as a fallback or for direct control
    private void UpdateVisuals()
    {
        if (meshVisualizer != null)
        {
            // Store last non-zero direction for keeping the facing direction when idle
            if (movement.x != 0 || movement.y != 0)
            {
                lastDirection = new Vector2(movement.x, movement.y).normalized;
            }
            
            // Change the sprite based on direction
            // Assuming your spritesheet is organized with:
            // Column 0, Row 0: Body facing left
            // Column 1, Row 0: Body facing right
            // Column 0, Row 1: Body facing up
            // Column 1, Row 1: Body facing down
            
            // Check horizontal direction first (takes priority)
            if (lastDirection.x > 0.1f)
            {
                // Facing right
                meshVisualizer.SetBodySpriteCell(1, 0);
                meshVisualizer.SetHeadSpriteCell(1, 0);
            }
            else if (lastDirection.x < -0.1f)
            {
                // Facing left
                meshVisualizer.SetBodySpriteCell(0, 0);
                meshVisualizer.SetHeadSpriteCell(0, 0);
            }
            // If not moving horizontally, check vertical
            else if (lastDirection.y > 0.1f)
            {
                // Facing up
                meshVisualizer.SetBodySpriteCell(0, 1);
                meshVisualizer.SetHeadSpriteCell(0, 1);
            }
            else if (lastDirection.y < -0.1f)
            {
                // Facing down
                meshVisualizer.SetBodySpriteCell(1, 1);
                meshVisualizer.SetHeadSpriteCell(1, 1);
            }
        }
    }

    // Add these public methods to your CharacterMovement.cs
    public void FaceDown()
    {
        if (meshVisualizer != null)
        {
            meshVisualizer.SetBodySpriteCell(1, 1);
            meshVisualizer.SetHeadSpriteCell(1, 1);
        }
    }

    public void FaceUp()
    {
        if (meshVisualizer != null)
        {
            meshVisualizer.SetBodySpriteCell(0, 1);
            meshVisualizer.SetHeadSpriteCell(0, 1);
        }
    }

    public void FaceLeft()
    {
        if (meshVisualizer != null)
        {
            meshVisualizer.SetBodySpriteCell(0, 0);
            meshVisualizer.SetHeadSpriteCell(0, 0);
        }
    }

    public void FaceRight()
    {
        if (meshVisualizer != null)
        {
            meshVisualizer.SetBodySpriteCell(1, 0);
            meshVisualizer.SetHeadSpriteCell(1, 0);
        }
    }
}
