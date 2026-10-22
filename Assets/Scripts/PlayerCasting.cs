using UnityEngine; // Unity engine core types

/// <summary>
/// Casts a ray forward from the player every frame and stores how far away
/// the first hit object is. Other scripts (e.g. HandGunPickup) read
/// distanceFromTarget to know if the player is close enough to interact.
/// Attach to the player (or the camera/eye position).
/// </summary>
public class PlayerCasting : MonoBehaviour
{
    public static float distanceFromTarget;  // distance to whatever we're looking at (read by other scripts)

    [SerializeField] float toTarget;         // same value, exposed in the Inspector for debugging

    void Update()
    {
        // shoot an invisible ray straight forward from this object's position
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit))
        {
            distanceFromTarget = hit.distance; // record how far away the hit surface is
            toTarget = hit.distance;           // mirror it so it's visible in the Inspector
        }
        else
        {
            // nothing in front — report "infinitely far" so pickups don't trigger by accident
            distanceFromTarget = float.MaxValue;
            toTarget = float.MaxValue;
        }
    }
}
