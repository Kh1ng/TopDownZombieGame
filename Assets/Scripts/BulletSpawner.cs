using UnityEngine;

public class BulletSpawner : MonoBehaviour
{
    [Header("Bullet Settings")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float bulletSpeed = 20f;
    
    private PlayerWeaponAim weaponAim;
    
    private void Awake()
    {
        weaponAim = GetComponent<PlayerWeaponAim>();
        
        if (weaponAim == null)
        {
            Debug.LogError("BulletSpawner requires PlayerWeaponAim component!");
            enabled = false;
            return;
        }
    }
    
    private void Start()
    {
        // Subscribe to the shooting event
        weaponAim.OnShoot += WeaponAim_OnShoot;
    }
    
    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (weaponAim != null)
        {
            weaponAim.OnShoot -= WeaponAim_OnShoot;
        }
    }
    
    private void WeaponAim_OnShoot(object sender, PlayerWeaponAim.OnShootEventArgs e)
    {
        if (bulletPrefab == null)
        {
            Debug.LogWarning("No bullet prefab assigned!");
            return;
        }
        
        // Calculate direction from gun to target
        Vector3 shootDirection = (e.shootPosition - e.gunEndPointPosition).normalized;
        
        // Spawn bullet at gun end point
        GameObject bullet = Instantiate(bulletPrefab, e.gunEndPointPosition, Quaternion.identity);
        
        // Add velocity to bullet
        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        if (bulletRb != null)
        {
            bulletRb.linearVelocity = shootDirection * bulletSpeed;
        }
        
        // Rotate bullet to face direction
        float angle = Mathf.Atan2(shootDirection.y, shootDirection.x) * Mathf.Rad2Deg;
        bullet.transform.eulerAngles = new Vector3(0, 0, angle);
        
        Debug.Log($"Bullet spawned from {e.gunEndPointPosition} towards {e.shootPosition}");
    }
}
