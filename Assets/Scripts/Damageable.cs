using UnityEngine; // Unity engine core types (MonoBehaviour, GameObject, etc.)

/// <summary>
/// Attach to anything that can be shot/destroyed (barrels, targets, enemies).
/// Tracks health, spawns an optional effect, and destroys the object at 0 HP.
/// </summary>
public class Damageable : MonoBehaviour
{
    [SerializeField] private float maxHealth = 50f;          // total health, editable in the Inspector
    [SerializeField] private GameObject destroyEffect;       // optional particle/prefab spawned on death

    private float currentHealth;                             // health remaining right now

    private void Awake()
    {
        currentHealth = maxHealth;                           // start with full health when the object loads
    }

    /// <summary>Called by weapons (e.g. HandgunFire) when this object is hit.</summary>
    public void TakeDamage(float amount)
    {
        currentHealth -= amount;                             // subtract the incoming damage
        if (currentHealth <= 0f)                             // died?
        {
            if (destroyEffect != null)                       // only spawn an effect if one was assigned
                Instantiate(destroyEffect, transform.position, Quaternion.identity); // boom effect at our position, no rotation
            Destroy(gameObject);                             // remove this object from the scene
        }
    }
}