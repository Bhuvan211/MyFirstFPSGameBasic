using UnityEngine;    // Unity engine core types
using StarterAssets;  // StarterAssetsInputs lives in this namespace

/// <summary>
/// Feeds MobileJoystick + TouchLook input into StarterAssetsInputs each frame.
/// Attach this to the same GameObject as StarterAssetsInputs (your player).
/// Only activates on Android/iOS builds — PC/Editor keeps using mouse+keyboard untouched.
/// </summary>
public class MobileInputBridge : MonoBehaviour
{
    [SerializeField] private StarterAssetsInputs input;             // the shared input store (auto-found if left empty)
    [SerializeField] private bool forceEnableInEditor = false;      // check this to test touch UI in the Editor

    private bool wasJoystickPressed = false; // tracks the previous frame so we can clear movement exactly once on release
    private bool wasLookPressed = false;     // same, for the look area

    // true on real mobile builds; in Editor only when the preview checkbox is ticked
    private bool IsMobilePlatform =>
#if UNITY_ANDROID || UNITY_IOS
        true;                 // compiled into an Android/iOS build: always on
#else
        forceEnableInEditor;  // PC/Editor: only if forced for testing
#endif

    private void Awake()
    {
        if (input == null) input = GetComponent<StarterAssetsInputs>(); // auto-wire to the input store on this object
    }

    private void Update()
    {
        if (!IsMobilePlatform || input == null) return; // do nothing on PC, or if wiring failed

        // ---- Movement (left-side joystick) ----
        if (MobileJoystick.Instance != null)
        {
            if (MobileJoystick.Instance.IsPressed)
            {
                // finger is on the joystick: forward its direction+strength every frame
                input.MoveInput(new Vector2(MobileJoystick.Instance.Horizontal, MobileJoystick.Instance.Vertical));
                wasJoystickPressed = true; // remember it was active so we can clear on release
            }
            else if (wasJoystickPressed)
            {
                // finger just lifted: clear movement ONCE — don't stomp on keyboard
                // input every frame afterwards.
                input.MoveInput(Vector2.zero);
                wasJoystickPressed = false;
            }
        }

        // ---- Camera look (right-side touch area) ----
        if (TouchLook.Instance != null)
        {
            if (TouchLook.Instance.IsPressed)
            {
                Vector2 delta = TouchLook.Instance.ConsumeLookDelta(); // rotation accumulated this frame
                // write every frame while touching, even zero delta, so a still
                // finger stops the camera instead of leaving a stale value
                input.LookInput(delta);
                wasLookPressed = true; // remember it was active so we can clear on release
            }
            else if (wasLookPressed)
            {
                // finger just lifted: clear ONCE — otherwise the last delta persists
                // in StarterAssetsInputs.look and the camera keeps rotating forever.
                input.LookInput(Vector2.zero);
                wasLookPressed = false;
            }
        }
    }

    // ---- Button hooks: wire these to mobile UI buttons' pointer-down/up events ----
    public void OnJumpButtonDown() => input?.JumpInput(true);    // jump pressed
    public void OnJumpButtonUp() => input?.JumpInput(false);     // jump released
    public void OnSprintButtonDown() => input?.SprintInput(true);   // sprint held
    public void OnSprintButtonUp() => input?.SprintInput(false);    // sprint released
}