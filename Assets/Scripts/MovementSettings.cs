using UnityEngine;

public enum MovementMode
{
    DirectionalMovement,    // Current system - character faces movement direction
    MouseLookMovement,      // New system - character always faces mouse
    LockedDirection,        // Character faces a fixed direction
    TankControls           // Alternative - forward/back relative to current facing
}

public enum LookMode
{
    FaceMovementDirection,  // Look where you're moving (current)
    FaceMousePosition,      // Always look at mouse
    FaceFixedDirection,     // Look in a specific direction
    FaceTargetObject       // Look at a specific transform/target
}

[System.Serializable]
public class MovementSettings
{
    [Header("Movement Behavior")]
    public MovementMode movementMode = MovementMode.DirectionalMovement;
    public LookMode lookMode = LookMode.FaceMovementDirection;
    
    [Header("Look Settings")]
    public bool smoothLooking = true;
    public float lookSmoothingSpeed = 10f;
    
    [Header("Fixed Direction (when using LockedDirection)")]
    public Vector2 fixedLookDirection = Vector2.down;
    
    [Header("Target Object (when using FaceTargetObject)")]
    public Transform targetToFace;
}
