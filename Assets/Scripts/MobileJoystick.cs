using UnityEngine;               // Unity engine core types
using UnityEngine.EventSystems;  // touch/pointer event interfaces (IDragHandler, etc.)

/// <summary>
/// On-screen virtual joystick for mobile movement input.
/// Attach to a UI Image (the "background" circle). Assign a child UI Image as the "handle".
/// MobileInputBridge reads Horizontal/Vertical from this every frame and forwards it
/// into StarterAssetsInputs so the player moves.
/// </summary>
public class MobileJoystick : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    public static MobileJoystick Instance { get; private set; } // scene-wide access point

    [Header("References")]
    [SerializeField] private RectTransform background;  // the outer circle image
    [SerializeField] private RectTransform handle;      // the draggable inner knob image

    [Header("Settings")]
    [SerializeField] private float handleRange = 100f;  // max distance (pixels) the handle can move from center
    [SerializeField] private float deadZone = 0.1f;     // ignore tiny inputs below this (0..1) to prevent drift

    private Vector2 input = Vector2.zero;               // current direction+strength, each axis in -1..1

    // read by MobileInputBridge; dead-zone filters out finger jitter near the center
    public float Horizontal => Mathf.Abs(input.x) < deadZone ? 0f : input.x; // left/right (-1..1)
    public float Vertical => Mathf.Abs(input.y) < deadZone ? 0f : input.y;   // down/up (-1..1)
    public bool IsPressed { get; private set; }                              // is a finger on the joystick?

    private void Awake()
    {
        if (Instance == null) Instance = this;                  // register the singleton
        if (background == null) background = GetComponent<RectTransform>(); // default: use our own RectTransform as the background
    }

    // finger touched the joystick area
    public void OnPointerDown(PointerEventData eventData)
    {
        IsPressed = true;   // mark as active
        OnDrag(eventData);  // jump the handle straight to the touch point
    }

    // finger moved while touching
    public void OnDrag(PointerEventData eventData)
    {
        // convert the screen touch position into a position relative to the joystick center
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background, eventData.position, eventData.pressEventCamera, out Vector2 position);

        position = Vector2.ClampMagnitude(position, handleRange); // keep the handle inside the circle
        input = position / handleRange;                           // normalize pixels into -1..1 range

        if (handle != null)
            handle.anchoredPosition = position;                   // move the visible knob to match
    }

    // finger lifted off the screen
    public void OnPointerUp(PointerEventData eventData)
    {
        IsPressed = false;                    // no longer active
        input = Vector2.zero;                 // stop all movement
        if (handle != null)
            handle.anchoredPosition = Vector2.zero; // snap the knob back to center
    }
}
