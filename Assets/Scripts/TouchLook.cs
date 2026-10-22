using UnityEngine;               // Unity engine core types
using UnityEngine.EventSystems;  // touch/pointer event interfaces

/// <summary>
/// Drag-anywhere-on-screen camera look for mobile.
/// Turns are measured relative to screen height, so the feel is the same on every phone.
/// </summary>
public class TouchLook : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    public static TouchLook Instance { get; private set; } // scene-wide access point

    [Tooltip("1 = a full-screen-height swipe turns about 90 degrees. Lower = slower.")]
    [SerializeField] private float sensitivity = 0.5f;     // look speed multiplier

    [Tooltip("Invert the Y axis (drag up = look down)")]
    [SerializeField] private bool invertY = false;         // optional inverted-look mode

    [Tooltip("Degrees turned by a full-screen-height swipe at sensitivity 1")]
    [SerializeField] private float degreesPerScreenHeight = 90f;

    private Vector2 lastPos;                               // finger position on the previous drag event
    private Vector2 lookDelta;                             // rotation (degrees) accumulated since last read

    public bool IsPressed { get; private set; }            // is a finger touching the look area?

    private void Awake()
    {
        if (Instance == null) Instance = this; // register the singleton
    }

    /// <summary>Returns the rotation accumulated since the last call, then resets it.</summary>
    public Vector2 ConsumeLookDelta()
    {
        Vector2 result = lookDelta;  // grab the accumulated rotation
        lookDelta = Vector2.zero;    // reset so it isn't applied twice
        return result;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        IsPressed = true;                // mark as active
        lastPos = eventData.position;    // remember where the touch started
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 delta = eventData.position - lastPos; // pixels moved since last event
        lastPos = eventData.position;

        // Screen Y points up, Unity look Y points down: negate so dragging up looks up
        delta.y = -delta.y;
        if (invertY) delta.y = -delta.y;

        // Convert pixels to degrees relative to screen height (resolution-independent)
        float degreesPerPixel = sensitivity * degreesPerScreenHeight / Screen.height;
        lookDelta += delta * degreesPerPixel;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        IsPressed = false; // no longer looking
    }
}