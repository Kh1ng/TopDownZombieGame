using UnityEngine;
using System;

public class PlayerWeaponAim : MonoBehaviour
{
    // Event for shooting
    public event EventHandler<OnShootEventArgs> OnShoot;
    
    public class OnShootEventArgs : EventArgs 
    {
        public Vector3 gunEndPointPosition;
        public Vector3 shootPosition;
    }    [Header("Weapon Aim Settings")]
    [SerializeField] private bool smoothAiming = true;
    [SerializeField] private float aimSmoothing = 10f;
    [SerializeField] private bool showDebugLine = false;
    [SerializeField] private bool flipWeaponWhenAimingLeft = true;
    [SerializeField] private bool parentWeaponToVisualContainer = true;
    
    [Header("Weapon Positioning Per Direction")]
    [SerializeField] private Vector3 weaponOffsetSouth = new Vector3(0.3f, -0.2f, -0.1f);   // Down - in front
    [SerializeField] private Vector3 weaponOffsetNorth = new Vector3(0.3f, -0.2f, 0.1f);    // Up - behind character
    [SerializeField] private Vector3 weaponOffsetEast = new Vector3(0.3f, -0.2f, -0.1f);    // Right - side
    [SerializeField] private Vector3 weaponOffsetWest = new Vector3(-0.3f, -0.2f, -0.1f);   // Left - opposite side
    
    // Store the original values for runtime modification
    private Vector3 originalWeaponOffsetSouth;
    private Vector3 originalWeaponOffsetNorth;
    private Vector3 originalWeaponOffsetEast;
    private Vector3 originalWeaponOffsetWest;
    
    private Transform aimTransform;
    private Transform aimGunEndPointTransform;
    private Animator aimAnimator;
    private Camera playerCamera;
    private float targetAngle;    private void Awake() 
    {
        // Find camera - try main camera first, then find any camera
        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            playerCamera = FindObjectOfType<Camera>();
        }
        
        if (playerCamera == null)
        {
            Debug.LogError("No camera found! PlayerWeaponAim requires a camera to work.");
            enabled = false;
            return;
        }
          // Find the Visual Container from MeshVisualizer to parent the weapon to
        Transform visualContainer = null;
        if (parentWeaponToVisualContainer)
        {
            MeshVisualizer meshVisualizer = GetComponent<MeshVisualizer>();
            if (meshVisualizer != null)
            {
                GameObject visualContainerObj = meshVisualizer.GetVisualContainer();
                if (visualContainerObj != null)
                {
                    visualContainer = visualContainerObj.transform;
                    Debug.Log("Found Visual Container for weapon parenting: " + visualContainer.name);
                }
            }
        }
        
        // If no visual container found or disabled, use the player transform as fallback
        if (visualContainer == null)
        {
            if (parentWeaponToVisualContainer)
            {
                Debug.LogWarning("No Visual Container found! Weapon will not bounce with character. Using player transform as parent.");
            }
            visualContainer = transform;
        }
        
        // Find the Aim transform, first check if it's already under visual container
        aimTransform = visualContainer.Find("Aim");
        
        // If not found under visual container, check under player transform
        if (aimTransform == null)
        {
            aimTransform = transform.Find("Aim");
            
            // If found under player, move it to visual container
            if (aimTransform != null)
            {
                Debug.Log("Moving existing Aim object to Visual Container");
                aimTransform.SetParent(visualContainer);
            }
        }
          // Create Aim object if it doesn't exist
        if (aimTransform == null)
        {
            Debug.LogWarning("No 'Aim' child object found! Creating new aim transform under Visual Container.");
            GameObject aimObject = new GameObject("Aim");
            aimObject.transform.SetParent(visualContainer);
            aimObject.transform.localPosition = Vector3.zero;
            aimTransform = aimObject.transform;
        }
        
        // Get animator from the aim transform
        aimAnimator = aimTransform.GetComponent<Animator>();
        if (aimAnimator == null)
        {
            Debug.LogWarning("No Animator found on Aim object. Shooting animations will not work.");
        }
          // Find the gun end point for bullet spawn position
        aimGunEndPointTransform = aimTransform.Find("GunEndPointPosition");
        if (aimGunEndPointTransform == null)
        {
            Debug.LogWarning("No 'GunEndPointPosition' found! Creating a default gun end point.");
            GameObject gunEndPoint = new GameObject("GunEndPointPosition");
            gunEndPoint.transform.SetParent(aimTransform);
            gunEndPoint.transform.localPosition = new Vector3(1f, 0f, 0f); // 1 unit forward from gun
            aimGunEndPointTransform = gunEndPoint.transform;
        }
        
        // Store the original weapon offset values from inspector
        originalWeaponOffsetSouth = weaponOffsetSouth;
        originalWeaponOffsetNorth = weaponOffsetNorth;
        originalWeaponOffsetEast = weaponOffsetEast;
        originalWeaponOffsetWest = weaponOffsetWest;
        
        // Set initial weapon position
        SetWeaponPositionForDirection(Vector2.down); // Start facing down
    }
    
    private void Update() 
    {
        HandleAiming();
        HandleShooting();
        
        // Update weapon position if inspector values changed (for real-time tweaking)
        #if UNITY_EDITOR
        if (aimTransform != null)
        {
            // Check if any offset values changed and update accordingly
            if (originalWeaponOffsetSouth != weaponOffsetSouth ||
                originalWeaponOffsetNorth != weaponOffsetNorth ||
                originalWeaponOffsetEast != weaponOffsetEast ||
                originalWeaponOffsetWest != weaponOffsetWest)
            {
                // Update stored values
                originalWeaponOffsetSouth = weaponOffsetSouth;
                originalWeaponOffsetNorth = weaponOffsetNorth;
                originalWeaponOffsetEast = weaponOffsetEast;
                originalWeaponOffsetWest = weaponOffsetWest;
            }        }
        #endif
    }
    
    private void HandleAiming()
    {
        if (aimTransform == null || playerCamera == null) return;
        
        Vector3 mousePosition = GetMouseWorldPosition();
        Vector3 aimDirection = (mousePosition - aimTransform.position).normalized;
          // Only aim if we have a valid direction
        if (aimDirection.magnitude > 0.01f)
        {
            float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
            targetAngle = angle;
            
            // Set weapon position based on aim direction (similar to head positioning)
            Vector2 aimDir2D = new Vector2(aimDirection.x, aimDirection.y);
            SetWeaponPositionForDirection(aimDir2D);
            
            // Handle weapon flipping to prevent upside-down appearance
            if (flipWeaponWhenAimingLeft)
            {
                Vector3 localScale = aimTransform.localScale;
                if (angle > 90f || angle < -90f)
                {
                    // Pointing left - flip the weapon vertically
                    localScale.y = -Mathf.Abs(localScale.y);
                }
                else
                {
                    // Pointing right - normal orientation
                    localScale.y = Mathf.Abs(localScale.y);
                }
                aimTransform.localScale = localScale;
            }
            
            if (smoothAiming)
            {
                // Smoothly interpolate the angle
                float smoothAngle = Mathf.LerpAngle(aimTransform.eulerAngles.z, targetAngle, Time.deltaTime * aimSmoothing);
                aimTransform.eulerAngles = new Vector3(0, 0, smoothAngle);
            }
            else
            {
                aimTransform.eulerAngles = new Vector3(0, 0, targetAngle);
            }
        }
        
        // Debug line to visualize aim direction
        if (showDebugLine)
        {
            Debug.DrawLine(aimTransform.position, mousePosition, Color.red);
        }
    }
    
    private Vector3 GetMouseWorldPosition()
    {
        if (playerCamera == null) return Vector3.zero;
        
        Vector3 mouseScreenPosition = Input.mousePosition;
        mouseScreenPosition.z = Mathf.Abs(playerCamera.transform.position.z); // Distance from camera
        return playerCamera.ScreenToWorldPoint(mouseScreenPosition);
    }
      private void HandleShooting() 
    {
        if (Input.GetMouseButtonDown(0)) 
        {
            Vector3 mousePosition = GetMouseWorldPosition();
            
            // Trigger shoot animation if animator exists
            if (aimAnimator != null)
            {
                aimAnimator.SetTrigger("Shoot");
            }
            
            // Fire the shoot event with gun end point and target position
            OnShoot?.Invoke(this, new OnShootEventArgs 
            {
                gunEndPointPosition = aimGunEndPointTransform != null ? aimGunEndPointTransform.position : aimTransform.position,
                shootPosition = mousePosition
            });
            
            Debug.Log($"Shot fired from {aimGunEndPointTransform?.position} towards {mousePosition}");
        }
    }
    
    // Method to set weapon position based on direction (similar to head positioning)
    public void SetWeaponPositionForDirection(Vector2 direction)
    {
        if (aimTransform == null) return;
        
        Vector3 targetPosition;
        
        // Determine direction based on input vector
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            if (direction.x > 0)
            {
                // East/right
                targetPosition = originalWeaponOffsetEast;
            }
            else
            {
                // West/left
                targetPosition = originalWeaponOffsetWest;
            }
        }
        else
        {
            if (direction.y > 0)
            {
                // North/up - weapon behind character
                targetPosition = originalWeaponOffsetNorth;
            }
            else
            {
                // South/down - weapon in front of character
                targetPosition = originalWeaponOffsetSouth;
            }
        }
        
        aimTransform.localPosition = targetPosition;
    }
    
    // Overload for string direction (for consistency with head positioning)
    public void SetWeaponPositionForDirection(string direction)
    {
        if (aimTransform == null) return;
        
        Vector3 targetPosition;
        
        switch (direction.ToLower())
        {
            case "north":
            case "up":
                targetPosition = originalWeaponOffsetNorth;
                break;
            case "south":
            case "down":
                targetPosition = originalWeaponOffsetSouth;
                break;
            case "east":
            case "right":
                targetPosition = originalWeaponOffsetEast;
                break;
            case "west":
            case "left":
                targetPosition = originalWeaponOffsetWest;
                break;
            default:
                targetPosition = originalWeaponOffsetSouth; // Default fallback
                break;
        }
        
        aimTransform.localPosition = targetPosition;
    }

    // Helper method to manually reparent weapon to visual container (useful for existing setups)
    [ContextMenu("Reparent Weapon to Visual Container")]
    public void ReparentWeaponToVisualContainer()
    {
        if (!parentWeaponToVisualContainer) return;
        
        MeshVisualizer meshVisualizer = GetComponent<MeshVisualizer>();
        if (meshVisualizer == null)
        {
            Debug.LogWarning("No MeshVisualizer found! Cannot reparent weapon.");
            return;
        }
        
        GameObject visualContainerObj = meshVisualizer.GetVisualContainer();
        if (visualContainerObj == null)
        {
            Debug.LogWarning("No Visual Container found! Cannot reparent weapon.");
            return;
        }
        
        if (aimTransform != null && aimTransform.parent != visualContainerObj.transform)
        {
            Vector3 worldPos = aimTransform.position;
            Quaternion worldRot = aimTransform.rotation;
            Vector3 worldScale = aimTransform.lossyScale;
            
            aimTransform.SetParent(visualContainerObj.transform);
            
            // Restore world position/rotation/scale
            aimTransform.position = worldPos;
            aimTransform.rotation = worldRot;
            aimTransform.localScale = Vector3.one; // Reset scale since parent might have scale
            
            Debug.Log("Successfully reparented weapon to Visual Container!");
        }
        else
        {
            Debug.Log("Weapon is already parented to Visual Container or aimTransform is null.");
        }
    }
    
    // Context menu helpers for testing weapon positions
    [ContextMenu("Test Weapon Position - North")]
    public void TestWeaponPositionNorth()
    {
        SetWeaponPositionForDirection("north");
        Debug.Log("Weapon positioned for North direction");
    }
    
    [ContextMenu("Test Weapon Position - South")]
    public void TestWeaponPositionSouth()
    {
        SetWeaponPositionForDirection("south");
        Debug.Log("Weapon positioned for South direction");
    }
    
    [ContextMenu("Test Weapon Position - East")]
    public void TestWeaponPositionEast()
    {
        SetWeaponPositionForDirection("east");
        Debug.Log("Weapon positioned for East direction");
    }
    
    [ContextMenu("Test Weapon Position - West")]
    public void TestWeaponPositionWest()
    {
        SetWeaponPositionForDirection("west");
        Debug.Log("Weapon positioned for West direction");
    }
}