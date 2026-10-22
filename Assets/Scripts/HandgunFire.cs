using System.Collections; // needed for IEnumerator coroutines
using UnityEngine;        // Unity engine core types

/// <summary>
/// Handgun shooting, reloading, and hit detection.
/// Attach to the gun's mechanics object. Enabled by HandGunPickup once the gun is picked up.
/// Fire: left mouse button (PC) or on-screen Fire button (mobile). Reload: R or auto at 0 bullets.
/// </summary>
public class HandgunFire : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] AudioSource gunFire;        // the bang sound
    [SerializeField] GameObject handgun;         // the gun model (for its Animator)
    [SerializeField] GameObject extraCross;      // crosshair flash shown while firing
    [SerializeField] AudioSource EmptyGunSound;  // the "click" when the magazine is empty

    [Header("Shooting")]
    [SerializeField] Camera fireCamera;          // camera the shots originate from (usually Main Camera)
    [SerializeField] float fireRange = 100f;     // max bullet travel distance in meters
    [SerializeField] float damage = 20f;         // damage dealt per hit
    [SerializeField] LayerMask hitMask = ~0;     // which layers bullets can hit (~0 = everything)

    [Header("Effects")]
    [SerializeField] ParticleSystem muzzleFlash; // muzzle flash particles (child of the gun, at the barrel)
    [SerializeField] ParticleSystem shellEject;  // optional brass casing particles
    [SerializeField] GameObject impactEffect;    // optional prefab spawned where a bullet hits

    [Header("Reload")]
    [SerializeField] AudioSource reloadSound;    // reload sound effect
    [SerializeField] float reloadTime = 1.5f;    // reload duration in seconds

    [Header("Debug")]
    [SerializeField] bool logMuzzleFlash = true; // print muzzle flash status to the Console

    [Header("Mobile")]
    [SerializeField] private bool forceEnableInEditor = false; // test touch input in the Editor

    private bool canFire = true;        // fire-rate gate: false while a shot/reload is in progress
    private bool isReloading = false;   // true during the reload coroutine
    private bool mobileFireHeld = false;// is the mobile Fire button currently held?

    private Animator gunAnimator;       // cached gun Animator (avoids GetComponent every shot)

    // true on real mobile builds; in Editor only when the preview checkbox is ticked
    private bool IsMobilePlatform =>
#if UNITY_ANDROID || UNITY_IOS
        true;
#else
        forceEnableInEditor;
#endif

    private void Awake()
    {
        if (handgun != null)
            gunAnimator = handgun.GetComponent<Animator>(); // cache the Animator once

        // make sure the flash never starts on its own, only when we shoot
        if (muzzleFlash != null)
        {
            muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    void Update()
    {
        // Manual reload — R on PC. Works any time (even with bullets left).
        if (!IsMobilePlatform && Input.GetKeyDown(KeyCode.R))
            TryReload();

        // pick the correct fire input for the current platform
        bool fireInput = IsMobilePlatform ? mobileFireHeld : Input.GetMouseButton(0);

        // holding fire, ready to shoot, and not mid-reload?
        if (fireInput && canFire && !isReloading)
        {
            canFire = false; // block further shots until the current one finishes
            if (GlobalAmmo.handgunAmmoCount == 0)
                StartCoroutine(EmptyGun());  // out of bullets: click, then auto-reload
            else
                StartCoroutine(FiringGun()); // bullets left: shoot
        }
    }

    // One full shot: effects + damage + fire-rate cooldown
    IEnumerator FiringGun()
    {
        if (gunFire != null) gunFire.Play();              // bang
        if (extraCross != null) extraCross.SetActive(true); // flash the crosshair
        GlobalAmmo.handgunAmmoCount -= 1;                 // consume one bullet

        if (gunAnimator != null) gunAnimator.Play("HandgunFire"); // recoil animation

        PlayMuzzleFlash();                                // muzzle flash
        if (shellEject != null) shellEject.Play();        // brass casing

        DoHitDetection();                                 // raycast and apply damage

        yield return new WaitForSeconds(0.5f);            // fire-rate pause
        if (gunAnimator != null) gunAnimator.Play("New State");   // back to idle pose
        if (extraCross != null) extraCross.SetActive(false);      // hide the crosshair flash
        yield return new WaitForSeconds(0.1f);            // tiny extra cooldown
        canFire = true;                                   // ready for the next shot
    }

    // Plays the muzzle flash and logs its state so you can see why it may not show
    private void PlayMuzzleFlash()
    {
        if (muzzleFlash == null)
        {
            Debug.LogWarning("Muzzle flash is NOT assigned on HandgunFire");
            return;
        }

        // restart from the beginning every shot, even if the last one is still going
        muzzleFlash.Play(true);

        if (logMuzzleFlash)
        {
            Debug.Log($"Flash: active={muzzleFlash.gameObject.activeInHierarchy} " +
                      $"playing={muzzleFlash.isPlaying} " +
                      $"layer={LayerMask.LayerToName(muzzleFlash.gameObject.layer)} " +
                      $"scale={muzzleFlash.transform.lossyScale}");
        }
    }

    // Trigger pulled with an empty magazine: click sound, then auto-reload
    IEnumerator EmptyGun()
    {
        if (EmptyGunSound != null) EmptyGunSound.Play(); // empty "click"
        yield return new WaitForSeconds(0.6f);            // brief lockout so the click can be heard
        canFire = true;

        // auto-reload only when the magazine actually hit 0 AND we have spare ammo
        if (GlobalAmmo.handgunAmmoCount == 0 && GlobalAmmo.reserveAmmoCount > 0)
            TryReload();
    }

    // Raycast from the camera; if we hit something Damageable, hurt it and spawn impact sparks
    private void DoHitDetection()
    {
        if (fireCamera == null) return; // no camera assigned: can't aim, skip

        Ray ray = new Ray(fireCamera.transform.position, fireCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, fireRange, hitMask))
        {
            // spawn the impact effect where the bullet landed, facing away from the surface
            if (impactEffect != null)
                Instantiate(impactEffect, hit.point, Quaternion.LookRotation(hit.normal));

            // search the hit object (and its parents) for something that can take damage
            Damageable target = hit.collider.GetComponentInParent<Damageable>();
            if (target != null)
                target.TakeDamage(damage); // apply this gun's damage
        }
    }

    /// <summary>
    /// Manual reload (R key / mobile Reload button) OR auto-triggered at 0 bullets.
    /// Safe to call any time — silently does nothing if the mag is full,
    /// a reload is already running, or there's no reserve ammo.
    /// </summary>
    public void TryReload()
    {
        if (isReloading) return;                                            // already reloading
        if (GlobalAmmo.handgunAmmoCount >= GlobalAmmo.MagazineSize) return; // mag already full
        if (GlobalAmmo.reserveAmmoCount <= 0) return;                       // nothing left in reserve

        StartCoroutine(ReloadRoutine()); // all checks passed: reload
    }

    // Plays reload sound/animation, waits, then moves bullets from reserve into the mag
    private IEnumerator ReloadRoutine()
    {
        isReloading = true;  // block overlapping reloads
        canFire = false;     // can't shoot mid-reload

        if (reloadSound != null) reloadSound.Play();          // reload sound (optional)
        if (gunAnimator != null) gunAnimator.Play("Reload");  // reload animation

        yield return new WaitForSeconds(reloadTime); // wait for the reload to finish

        if (gunAnimator != null) gunAnimator.Play("New State"); // back to idle pose

        // move as many bullets as needed (or as many as we have) from reserve to magazine
        int needed = GlobalAmmo.MagazineSize - GlobalAmmo.handgunAmmoCount;  // bullets missing from the mag
        int amountToReload = Mathf.Min(needed, GlobalAmmo.reserveAmmoCount); // capped by what's in reserve

        GlobalAmmo.handgunAmmoCount += amountToReload;  // load bullets into the magazine
        GlobalAmmo.reserveAmmoCount -= amountToReload;  // remove them from the reserve pool

        isReloading = false; // reload done
        canFire = true;      // ready to shoot again
    }

    // ---- Mobile touch input: wire to the on-screen Fire button's down/up events ----
    public void OnFireButtonDown() => mobileFireHeld = true;  // fire button pressed
    public void OnFireButtonUp() => mobileFireHeld = false;   // fire button released
}