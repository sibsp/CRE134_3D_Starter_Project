using UnityEngine;

// =====================================================================
// PATROL
// ---------------------------------------------------------------------
// What it does:
//   Walks an animal from waypoint to waypoint in a loop, waiting a little
//   at each one, and tells its Animator when it is walking so the right
//   animation plays. If "Is Enemy" is ticked, it stops and attacks when
//   the player comes close.
//
// How to use it:
//   1. Put the animal model inside an empty "mover" object.
//   2. Add this script to the mover.
//   3. Make some empty GameObjects as waypoints and drag them into the
//      "Waypoints" list.
//   The animal's Animator Controller needs two Bool parameters called
//   "Walking" and "Attacking".
// =====================================================================
public class Patrol : MonoBehaviour
{
    // ---------- Settings you can change in the Inspector ----------

    // The points to walk between, in order.
    public Transform[] waypoints;

    // Walking speed in metres per second.
    public float speed = 1.5f;

    // How fast it turns, in degrees per second.
    public float turnSpeed = 180f;

    // How long to wait at each waypoint, in seconds.
    public float waitAtPoint = 2f;

    [Header("Enemy behaviour")]
    // Tick this to make the animal attack the player.
    public bool isEnemy = false;

    // How close (in metres) the player must be before an enemy attacks.
    public float alertRadius = 5f;

    // ---------- Private variables ----------

    // The Animator on the animal model (a child of this object).
    private Animator animator;

    // The player, so an enemy knows where to look.
    private Transform player;

    // Which waypoint we are walking towards (0 = the first one).
    private int waypointIndex = 0;

    // Counts down while waiting at a waypoint.
    private float waitTimer = 0f;


    private void Start()
    {
        // The Animator is on the model, which is a CHILD of this object,
        // so we use GetComponentInChildren.
        animator = GetComponentInChildren<Animator>();

        // Find the object tagged "Player".
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
    }


    // Update runs every frame and decides what the animal does next.
    private void Update()
    {
        // 1. Enemies attack if the player is close enough.
        if (isEnemy == true && player != null)
        {
            float distanceToPlayer = Vector3.Distance(Flatten(player.position), Flatten(transform.position));

            if (distanceToPlayer < alertRadius)
            {
                TurnTowards(player.position);
                SetAnimation(false, true);
                return;   // stop here: skip the rest of Update this frame
            }
        }

        // 2. No waypoints? Just stand still.
        if (waypoints == null || waypoints.Length == 0)
        {
            SetAnimation(false, false);
            return;
        }

        // 3. Waiting at a waypoint? Count down and stand still.
        if (waitTimer > 0f)
        {
            waitTimer = waitTimer - Time.deltaTime;
            SetAnimation(false, false);
            return;
        }

        // 4. Walk towards the current waypoint.
        Vector3 target = waypoints[waypointIndex].position;

        // Keep our own height, so the animal doesn't tilt up or down.
        target.y = transform.position.y;

        TurnTowards(target);

        // MoveTowards steps towards the target, but never further than
        // speed x time, so we never overshoot it.
        float stepThisFrame = speed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, target, stepThisFrame);

        SetAnimation(true, false);

        // 5. Close enough to the waypoint? Head for the next one and wait.
        float distanceToTarget = Vector3.Distance(transform.position, target);
        if (distanceToTarget < 0.1f)
        {
            waypointIndex = waypointIndex + 1;

            // After the last waypoint, go back to the first one.
            if (waypointIndex >= waypoints.Length)
            {
                waypointIndex = 0;
            }

            waitTimer = waitAtPoint;
        }
    }


    // Turn smoothly to face a point, a few degrees each frame.
    private void TurnTowards(Vector3 point)
    {
        Vector3 direction = Flatten(point) - Flatten(transform.position);

        // If the point is right on top of us there is no direction to face.
        if (direction.magnitude < 0.01f)
        {
            return;
        }

        // LookRotation makes a rotation that faces along "direction".
        Quaternion lookRotation = Quaternion.LookRotation(direction);

        // RotateTowards turns from our current rotation towards lookRotation,
        // but only by turnSpeed degrees per second.
        float degreesThisFrame = turnSpeed * Time.deltaTime;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRotation, degreesThisFrame);
    }


    // Tell the Animator what we are doing. This is the ONLY place this
    // script touches animation: it sets two Bool parameters, and the
    // Animator Controller decides which animation to play.
    private void SetAnimation(bool walking, bool attacking)
    {
        if (animator == null)
        {
            return;
        }

        animator.SetBool("Walking", walking);
        animator.SetBool("Attacking", attacking);
    }


    // Return the same position but with the height (y) set to 0,
    // so that distances and directions ignore height.
    private Vector3 Flatten(Vector3 position)
    {
        position.y = 0f;
        return position;
    }
}
