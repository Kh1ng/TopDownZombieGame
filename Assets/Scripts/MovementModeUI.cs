using UnityEngine;
using UnityEngine.UI;

public class MovementModeUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button directionalButton;
    [SerializeField] private Button mouseLookButton;
    [SerializeField] private Button fixedDirectionButton;
    
    [Header("Character Reference")]
    [SerializeField] private CharacterMovement characterMovement;
    
    private void Start()
    {
        // Find character movement if not assigned
        if (characterMovement == null)
        {
            characterMovement = FindObjectOfType<CharacterMovement>();
        }
        
        // Setup button events
        if (directionalButton != null)
        {
            directionalButton.onClick.AddListener(() => {
                characterMovement?.UseDirectionalMovement();
                Debug.Log("Switched to Directional Movement");
            });
        }
        
        if (mouseLookButton != null)
        {
            mouseLookButton.onClick.AddListener(() => {
                characterMovement?.UseMouseLookMovement();
                Debug.Log("Switched to Mouse Look Movement");
            });
        }
        
        if (fixedDirectionButton != null)
        {
            fixedDirectionButton.onClick.AddListener(() => {
                characterMovement?.SetLookMode(LookMode.FaceFixedDirection);
                Debug.Log("Switched to Fixed Direction");
            });
        }
    }
    
    private void Update()
    {
        // Allow keyboard shortcuts for testing
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            characterMovement?.UseDirectionalMovement();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            characterMovement?.UseMouseLookMovement();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            characterMovement?.SetLookMode(LookMode.FaceFixedDirection);
        }
    }
}
