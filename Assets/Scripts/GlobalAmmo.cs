using UnityEngine; // Unity engine core types

/// <summary>
/// Global ammo store for the handgun plus the on-screen ammo counter.
/// One instance in the scene (singleton). Ammo values are static so any
/// script (gun, pickups) can read/modify them directly.
/// </summary>
public class GlobalAmmo : MonoBehaviour
{
    public static GlobalAmmo Instance { get; private set; } // scene-wide access point

    [Header("Ammo Pools")]
    public static int handgunAmmoCount = 12;    // bullets currently loaded in the magazine
    public static int reserveAmmoCount = 48;    // spare bullets carried, used to refill on reload
    public const int MagazineSize = 12;         // magazine capacity (cannot be exceeded)

    [SerializeField] GameObject ammoDisplay;    // the UI object showing the ammo count

    private TMPro.TMP_Text ammoText;            // cached text component (avoids GetComponent every frame)

    private void Awake()
    {
        Instance = this;                                    // register this as the singleton
        if (ammoDisplay != null)
            ammoText = ammoDisplay.GetComponent<TMPro.TMP_Text>(); // cache the TMP text once
    }

    void Start()
    {
        // Hidden by default — only shown once the player actually has the gun in hand.
        SetAmmoDisplayVisible(false);
    }

    void Update()
    {
        // refresh the on-screen counter, e.g. "12 / 48"
        if (ammoText != null)
            ammoText.text = handgunAmmoCount + " / " + reserveAmmoCount;
    }

    /// <summary>Show or hide the ammo counter UI (called when the gun is picked up).</summary>
    public void SetAmmoDisplayVisible(bool visible)
    {
        if (ammoDisplay != null)
            ammoDisplay.SetActive(visible); // enable/disable the whole UI object
    }
}