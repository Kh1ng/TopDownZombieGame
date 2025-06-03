using UnityEngine;

public class CharacterMovement : MonoBehaviour
{
    private Rigidbody2D rb;
    private Vector2 movement;
    private Vector2 lastDirection = Vector2.down;
    
    [SerializeField] private float moveSpeed = 5f;
    
    private MeshVisualizer meshVisualizer;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        meshVisualizer = GetComponent<MeshVisualizer>();
        
        if (meshVisualizer == null)
            meshVisualizer = GetComponentInChildren<MeshVisualizer>();
    }

    void Update()
    {
        HandleInput();
    }
    
    void FixedUpdate()
    {
        // Apply movement to rigidbody
        if (movement.sqrMagnitude > 0)
        {
            rb.MovePosition(rb.position + movement.normalized * moveSpeed * Time.fixedDeltaTime);
        }
    }

    private void HandleInput()
    {
        // Get input
        Vector2 previousMovement = movement;
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");
        
        if (movement.sqrMagnitude > 0.1f)
        {
            // We're moving - store direction and update animation
            lastDirection = movement.normalized;
            
            // If direction changed, update animation
            if (previousMovement != movement && meshVisualizer != null)
            {
                meshVisualizer.SetupDirectionalAnimation(movement);
            }
        }
        else
        {
            // Handle idle state - show static sprite based on last direction
            if (previousMovement.sqrMagnitude > 0.1f && meshVisualizer != null)
            {
                // We just stopped moving - set up static pose
                SetStaticDirection(lastDirection);
            }
        }
    }
    
    private void SetStaticDirection(Vector2 direction)
    {
        if (meshVisualizer == null) return;
        
        // Reset flip state
        meshVisualizer.FlipSprites(false);
        
        // Determine which direction based on vector
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            // Horizontal movement prioritized
            if (direction.x > 0)
            {
                // Face right
                meshVisualizer.SetBodySpriteCell(0, 1); // Body column 0, row 1 (East)
                meshVisualizer.SetHeadSpriteCell(1, 1); // Head column 1, row 1 (East)
            }
            else
            {
                // Face left (flipped East)
                meshVisualizer.SetBodySpriteCell(0, 1); // Body column 0, row 1 (East)
                meshVisualizer.SetHeadSpriteCell(1, 1); // Head column 1, row 1 (East)
                meshVisualizer.FlipSprites(true);       // Then flip
            }
        }
        else
        {
            // Vertical movement prioritized
            if (direction.y > 0)
            {
                // Face up
                meshVisualizer.SetBodySpriteCell(0, 2); // Body column 0, row 2 (North) 
                meshVisualizer.SetHeadSpriteCell(1, 2); // Head column 1, row 2 (North)
            }
            else
            {
                // Face down
                meshVisualizer.SetBodySpriteCell(0, 0); // Body column 0, row 0 (South)
                meshVisualizer.SetHeadSpriteCell(1, 0); // Head column 1, row 0 (South)
            }
        }
    }
}
