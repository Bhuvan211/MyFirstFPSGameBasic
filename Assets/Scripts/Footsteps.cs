using System.Collections; // needed for IEnumerator coroutines
using UnityEngine;        // Unity engine core types

/// <summary>
/// Plays random footstep sounds while the player holds a movement key.
/// Steps come faster while sprinting (Shift). Attach to the player.
/// </summary>
public class Footsteps : MonoBehaviour
{
    [Header("Step Sounds (assign 4 AudioSources)")]
    [SerializeField] AudioSource f1;                 // footstep sound variant 1
    [SerializeField] AudioSource f2;                 // footstep sound variant 2
    [SerializeField] AudioSource f3;                 // footstep sound variant 3
    [SerializeField] AudioSource f4;                 // footstep sound variant 4

    [Header("Timing (seconds between steps)")]
    [SerializeField] float walkDelay = 0.6f;         // step interval while walking
    [SerializeField] float sprintDelay = 0.3f;       // step interval while sprinting

    private bool isStepping;                         // true while a step coroutine is running (prevents overlap)

    void Update()
    {
        // true while any WASD movement key is held
        bool isMoving = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S)
                     || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D);

        // start a new step only when moving AND the previous step finished
        if (isMoving && !isStepping)
            StartCoroutine(Footstep());
    }

    IEnumerator Footstep()
    {
        isStepping = true;                       // block new steps until this one is done

        int soundNumber = Random.Range(1, 5);    // pick a random variant: 1, 2, 3, or 4 (max is exclusive)

        if (soundNumber == 1) f1.Play();         // play the matching AudioSource
        if (soundNumber == 2) f2.Play();
        if (soundNumber == 3) f3.Play();
        if (soundNumber == 4) f4.Play();

        // sprint = quicker steps; walk = slower steps.
        // (Captured BEFORE the wait so holding/releasing Shift mid-step doesn't change this step's timing.)
        float delay = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                      ? sprintDelay : walkDelay;

        yield return new WaitForSeconds(delay);  // wait before allowing the next step

        isStepping = false;                      // allow the next footstep
    }
}
