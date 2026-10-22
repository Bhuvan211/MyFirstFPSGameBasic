using UnityEngine; // Unity engine core types

/// <summary>
/// Attach to any UI element (button, panel) that should only appear on Android/iOS.
/// Hides itself automatically on PC/Editor unless forceEnableInEditor is checked for testing.
/// </summary>
public class MobileOnlyUI : MonoBehaviour
{
    [Tooltip("Check to preview this UI in the Editor (on real PC builds it still hides)")]
    [SerializeField] private bool forceEnableInEditor = false; // editor-only preview switch

    private void Awake()
    {
#if UNITY_ANDROID || UNITY_IOS
        // real mobile build — always show the touch UI
        gameObject.SetActive(true);
#else
        // PC/Editor — hidden unless the preview checkbox is ticked
        gameObject.SetActive(forceEnableInEditor);
#endif
    }
}
