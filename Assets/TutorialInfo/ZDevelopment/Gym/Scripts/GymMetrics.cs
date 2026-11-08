using UnityEngine;
using StarterAssets;

// =====================================================================
// GYM METRICS
// ---------------------------------------------------------------------
// What it does:
//   Reads the REAL movement numbers from the player in the scene
//   (walk speed, jump height, step height...), so the gym's signs are
//   never out of date. Change the player's Jump Height and every sign
//   updates itself.
//
// This is NOT a component: you don't add it to an object. It is a plain
// class that holds some numbers. MetricSign.cs uses it like this:
//
//     GymMetrics numbers = GymMetrics.ReadFromPlayer();
//     float jump = numbers.jumpHeight;
//
// "using StarterAssets;" at the top lets us use the FirstPersonController
// script that comes with Unity's Starter Assets.
// =====================================================================
public class GymMetrics
{
    // ---------- The numbers ----------
    // These start with sensible default values, in case there is no player.

    public float walkSpeed = 4f;       // metres per second
    public float sprintSpeed = 6f;     // metres per second
    public float jumpHeight = 1.2f;    // metres
    public float gravity = -15f;       // metres per second, per second (negative = down)
    public float stepOffset = 0.25f;   // tallest step the player can walk up, in metres
    public float slopeLimit = 45f;     // steepest slope the player can walk up, in degrees
    public float height = 2f;          // player height in metres
    public float width = 1f;           // player width in metres
    public float eyeHeight = 1.375f;   // camera height in metres


    // How long the player is in the air during a jump, in seconds.
    // (Physics: time to go up and come back down again.)
    public float GetAirTime()
    {
        float timeGoingUp = Mathf.Sqrt(2f * jumpHeight / Mathf.Abs(gravity));
        return timeGoingUp * 2f;
    }


    // How far the player travels during a jump while walking.
    public float GetJumpDistanceWalking()
    {
        return walkSpeed * GetAirTime();
    }


    // How far the player travels during a jump while sprinting.
    public float GetJumpDistanceSprinting()
    {
        return sprintSpeed * GetAirTime();
    }


    // =================================================================
    // ReadFromPlayer() finds the player and copies its real settings.
    //
    // "static" means you call it on the CLASS (GymMetrics.ReadFromPlayer())
    // rather than on an object, because there is no object yet: this
    // method is the thing that makes one.
    // =================================================================
    public static GymMetrics ReadFromPlayer()
    {
        // Make a new set of numbers with the default values.
        GymMetrics numbers = new GymMetrics();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            // No player in the scene: just use the defaults.
            return numbers;
        }

        // The Character Controller knows the player's size and climbing limits.
        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller != null)
        {
            numbers.stepOffset = controller.stepOffset;
            numbers.slopeLimit = controller.slopeLimit;
            numbers.height = controller.height;
            numbers.width = controller.radius * 2f;
        }

        // The camera sits on a child object called "PlayerCameraRoot".
        Transform cameraRoot = player.transform.Find("PlayerCameraRoot");
        if (cameraRoot != null)
        {
            numbers.eyeHeight = cameraRoot.localPosition.y;
        }

        // The First Person Controller knows the speeds, jump and gravity.
        FirstPersonController movement = player.GetComponent<FirstPersonController>();
        if (movement != null)
        {
            numbers.walkSpeed = movement.MoveSpeed;
            numbers.sprintSpeed = movement.SprintSpeed;
            numbers.jumpHeight = movement.JumpHeight;
            numbers.gravity = movement.Gravity;
        }

        return numbers;
    }
}
