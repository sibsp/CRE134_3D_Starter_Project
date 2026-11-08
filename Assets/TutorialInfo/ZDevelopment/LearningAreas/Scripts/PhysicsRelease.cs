using UnityEngine;

// =====================================================================
// PHYSICS RELEASE
// ---------------------------------------------------------------------
// What it does:
//   Holds some Rigidbodies perfectly still until Release() is called,
//   then lets physics take over (they fall, tumble, roll...).
//   ResetBodies() puts them all back where they started so you can try again.
//
// How to use it:
//   1. Drag the Rigidbodies into the "Bodies" list.
//   2. Call Release() and ResetBodies() from Trigger Zones or Interact Prompts.
//
// How it holds them still: "Is Kinematic" on a Rigidbody means
// "physics doesn't move this". We switch it on to freeze, off to release.
// =====================================================================
public class PhysicsRelease : MonoBehaviour
{
    // ---------- Settings you can change in the Inspector ----------

    // The objects to hold and release.
    public Rigidbody[] bodies;

    // ---------- Private variables ----------

    // Where each body started, and which way it was facing.
    // startPositions[0] belongs to bodies[0], and so on.
    private Vector3[] startPositions;
    private Quaternion[] startRotations;


    private void Awake()
    {
        // Make arrays the same length as the bodies list.
        startPositions = new Vector3[bodies.Length];
        startRotations = new Quaternion[bodies.Length];

        for (int i = 0; i < bodies.Length; i++)
        {
            // Remember where it started...
            startPositions[i] = bodies[i].position;
            startRotations[i] = bodies[i].rotation;

            // ...and freeze it.
            bodies[i].isKinematic = true;
        }
    }


    // Let physics take over.
    public void Release()
    {
        for (int i = 0; i < bodies.Length; i++)
        {
            bodies[i].isKinematic = false;

            // WakeUp makes sure the physics engine starts moving it straight away.
            bodies[i].WakeUp();
        }
    }


    // Freeze everything and put it back where it started.
    public void ResetBodies()
    {
        for (int i = 0; i < bodies.Length; i++)
        {
            Rigidbody body = bodies[i];

            // Stop any movement (only allowed while it is not kinematic).
            if (body.isKinematic == false)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            body.isKinematic = true;

            // Move it back to the start.
            body.position = startPositions[i];
            body.rotation = startRotations[i];
            body.transform.SetPositionAndRotation(startPositions[i], startRotations[i]);
        }
    }
}
