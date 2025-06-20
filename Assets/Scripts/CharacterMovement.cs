using UnityEngine;

public class CharacterMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float teleportDistance = 3f;
    [SerializeField] private float teleportCooldown = 1f;
    
    [Header("Movement Mode")]
    [SerializeField] private MovementSettings movementSettings = new MovementSettings();
    
    private Rigidbody2D rb;
    private Vector2 movement;
    private Vector2 lastDirection = Vector2.down;
    private Vector2 currentLookDirection = Vector2.down;
    private Camera playerCamera;
    
    private MeshVisualizer meshVisualizer;
    private CharacterAnimator characterAnimator;    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        meshVisualizer = GetComponent<MeshVisualizer>();
        characterAnimator = GetComponent<CharacterAnimator>();
        
        // Find camera for mouse world position
        playerCamera = Camera.main;
        if (playerCamera == null)
            playerCamera = FindObjectOfType<Camera>();
        
        if (meshVisualizer == null)
            meshVisualizer = GetComponentInChildren<MeshVisualizer>();
        if (characterAnimator == null)
            characterAnimator = GetComponentInChildren<CharacterAnimator>();
    }

    void Update()
    {
        HandleInput();
        HandleLookDirection();
    }
    
    void FixedUpdate()
    {
        // Apply movement to rigidbody
        if (movement.sqrMagnitude > 0)
        {
            rb.MovePosition(rb.position + movement.normalized * moveSpeed * Time.fixedDeltaTime);
        }
    }    private void HandleInput()
    {
        // Get input
        Vector2 previousMovement = movement;
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");
        
        // Store movement direction for potential use
        if (movement.sqrMagnitude > 0.1f)
        {
            lastDirection = movement.normalized;
        }
        
        // Handle teleport input
        if (Input.GetKeyDown(KeyCode.Space))
        {
            HandleTeleport();
        }
    }
    
    private void HandleTeleport()
    {
        // Use current look direction for teleport instead of movement direction
        Vector2 teleportDirection = currentLookDirection.normalized;
        Vector2 teleportOffset = teleportDirection * teleportDistance;
        Vector2 newPosition = rb.position + teleportOffset;
        
        // Apply the teleport
        rb.MovePosition(newPosition);
        
        Debug.Log($"Teleported {teleportDistance} units in look direction {teleportDirection}");
    }
    
    private void HandleLookDirection()
    {
        Vector2 newLookDirection = currentLookDirection;
        
        switch (movementSettings.lookMode)
        {
            case LookMode.FaceMovementDirection:
                // Current behavior - face movement direction
                if (movement.sqrMagnitude > 0.1f)
                {
                    newLookDirection = movement.normalized;
                }
                else
                {
                    newLookDirection = lastDirection;
                }
                break;
                
            case LookMode.FaceMousePosition:
                // New behavior - always face mouse
                if (playerCamera != null)
                {
                    Vector3 mouseWorldPos = GetMouseWorldPosition();
                    Vector3 lookDir = (mouseWorldPos - transform.position).normalized;
                    newLookDirection = new Vector2(lookDir.x, lookDir.y);
                }
                break;
                
            case LookMode.FaceFixedDirection:
                // Face a fixed direction
                newLookDirection = movementSettings.fixedLookDirection.normalized;
                break;
                
            case LookMode.FaceTargetObject:
                // Face a specific target
                if (movementSettings.targetToFace != null)
                {
                    Vector3 targetDir = (movementSettings.targetToFace.position - transform.position).normalized;
                    newLookDirection = new Vector2(targetDir.x, targetDir.y);
                }
                break;
        }
        
        // Apply look direction with optional smoothing
        if (movementSettings.smoothLooking)
        {
            currentLookDirection = Vector2.Lerp(currentLookDirection, newLookDirection, 
                                               movementSettings.lookSmoothingSpeed * Time.deltaTime);
        }
        else
        {
            currentLookDirection = newLookDirection;
        }
        
        // Update character animation based on look direction
        if (characterAnimator != null && currentLookDirection.sqrMagnitude > 0.1f)
        {
            // Only update animation if we're moving OR if we're in mouse look mode
            bool shouldUpdateAnimation = movement.sqrMagnitude > 0.1f || 
                                       movementSettings.lookMode == LookMode.FaceMousePosition;
                                       
            if (shouldUpdateAnimation)
            {
                characterAnimator.SetupDirectionalAnimation(currentLookDirection);
                if (movement.sqrMagnitude <= 0.1f)
                {
                    // We're not moving but looking around - stop walking animation
                    characterAnimator.StopWalking();
                }
            }
        }
    }
    
    private Vector3 GetMouseWorldPosition()
    {
        if (playerCamera == null) return transform.position;
        
        Vector3 mouseScreenPosition = Input.mousePosition;
        mouseScreenPosition.z = Mathf.Abs(playerCamera.transform.position.z);
        return playerCamera.ScreenToWorldPoint(mouseScreenPosition);
    }
    
    // Public methods to change movement modes at runtime
    public void SetMovementMode(MovementMode mode)
    {
        movementSettings.movementMode = mode;
        Debug.Log($"Movement mode changed to: {mode}");
    }
    
    public void SetLookMode(LookMode mode)
    {
        movementSettings.lookMode = mode;
        Debug.Log($"Look mode changed to: {mode}");
    }
    
    // Quick preset methods
    [ContextMenu("Use Directional Movement (Original)")]
    public void UseDirectionalMovement()
    {
        SetMovementMode(MovementMode.DirectionalMovement);
        SetLookMode(LookMode.FaceMovementDirection);
    }
    
    [ContextMenu("Use Mouse Look Movement")]
    public void UseMouseLookMovement()
    {
        SetMovementMode(MovementMode.DirectionalMovement); // Still move with WASD
        SetLookMode(LookMode.FaceMousePosition);           // But look at mouse
    }
    
    // Getter for current look direction (useful for other scripts)
    public Vector2 GetCurrentLookDirection()
    {
        return currentLookDirection;
    }
    
    public MovementSettings GetMovementSettings()
    {
        return movementSettings;
    }
}
