using UnityEngine;

// =====================================================================
// ROTATOR
// ---------------------------------------------------------------------
// What it does:
//   Spins an object at a steady speed.
//
// How to use it:
//   Add it to any object and set "Degrees Per Second".
//   (0, 30, 0) spins it slowly around its up (Y) axis.
// =====================================================================
public class Rotator : MonoBehaviour
{
    // ---------- Settings you can change in the Inspector ----------

    // How many degrees to turn each second, around X, Y and Z.
    public Vector3 degreesPerSecond = new Vector3(0f, 30f, 0f);

    // Self = spin around the object's own axes.
    // World = spin around the world's axes.
    public Space space = Space.Self;


    // Update runs every frame. Turn a little bit each frame.
    private void Update()
    {
        // Multiplying by Time.deltaTime turns "per second" into "this frame".
        Vector3 turnThisFrame = degreesPerSecond * Time.deltaTime;
        transform.Rotate(turnThisFrame, space);
    }
}
