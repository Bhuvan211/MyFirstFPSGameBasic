using UnityEngine; // Unity engine core types

/// <summary>
/// The handgun lying in the world, waiting to be picked up.
/// Shows "press E" text when the player looks at it from close range,
/// and swaps the world gun for the in-hand gun on pickup.
/// Attach to the world gun object (needs a collider).
/// </summary>
public class HandGunPickup : MonoBehaviour
{
    private const float PickupRange = 2f;        // how close (meters) the player must be to pick up

    private float theDistance;                   // current distance from the player's look-ray to a target

    [SerializeField] GameObject actionText;      // "Press E to pick up" UI prompt
    [SerializeField] GameObject theGun;          // the gun model lying in the world (this pickup)
    [SerializeField] GameObject gunInHand;       // the gun model attached to the player's camera/hand
    [SerializeField] GameObject gunMechanics;    // object holding the HandgunFire script

    void Update()
    {
        theDistance = PlayerCasting.distanceFromTarget; // refresh how far the player is looking each frame
    }

    // Unity calls this every frame while the mouse/look ray is over this object's collider
    void OnMouseOver()
    {
        // show the prompt only when close enough to actually pick up
        actionText.SetActive(theDistance <= PickupRange);

        // pressed E while close? pick it up (GetKeyDown so it fires once per press)
        if (theDistance <= PickupRange && Input.GetKeyDown(KeyCode.E))
            DoPickup();
    }

    // Unity calls this when the look ray stops pointing at this object
    void OnMouseExit()
    {
        actionText.SetActive(false); // hide the prompt
    }

    private void DoPickup()
    {
        GetComponent<BoxCollider>().enabled = false;          // stop this object from being looked at again
        actionText.SetActive(false);                          // hide the prompt
        gunInHand.SetActive(true);                            // show the gun in the player's hands
        theGun.SetActive(false);                              // hide the gun on the ground
        gunMechanics.GetComponent<HandgunFire>().enabled = true; // turn on shooting

        // ammo counter only appears once the player actually has a gun in hand
        GlobalAmmo.Instance?.SetAmmoDisplayVisible(true);
    }

    /// <summary>Called by the mobile "Pick Up" UI button.</summary>
    public void TryMobilePickup()
    {
        if (theDistance <= PickupRange) // same range check as the E key
            DoPickup();
    }
}