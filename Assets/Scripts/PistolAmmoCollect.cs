using UnityEngine; // Unity engine core types

/// <summary>
/// Ammo pickup box. When the player walks into its trigger, it plays a sound,
/// adds ammo to the reserve pool, and removes itself from the scene.
/// Requires a trigger collider on this object and a collider on the player.
/// </summary>
public class PistolAmmoCollect : MonoBehaviour
{
    [SerializeField] AudioSource ammoCollect;   // "pickup" sound effect
    [SerializeField] int ammoAmount = 30;       // how much reserve ammo this box gives

    // Unity calls this automatically when something enters the trigger collider
    void OnTriggerEnter(Collider other)
    {
        GetComponent<BoxCollider>().enabled = false;   // disable the trigger so it can't fire twice
        ammoCollect.Play();                            // play the pickup sound
        GlobalAmmo.reserveAmmoCount += ammoAmount;     // add ammo to the global reserve pool
        Destroy(gameObject);                           // remove the box from the scene
    }
}