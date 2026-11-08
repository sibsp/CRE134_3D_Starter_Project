using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

// =====================================================================
// ZOO CLIP PLAYER
// ---------------------------------------------------------------------
// What it does:
//   Plays ONE animation clip on a loop, without needing an Animator
//   Controller. The Animation Zoo uses it so you can see every clip of
//   a model side by side.
//
// How to use it:
//   Add it to a model that has an Animator, and drag a clip into "Clip".
//
// Advanced note (you don't need to understand this to use it):
//   Normally an Animator Controller decides which clip plays. Here we use
//   Unity's "Playables" system instead: we build a tiny graph in code
//   that connects one clip straight to the Animator. You won't need
//   Playables for your own projects; an Animator Controller is easier.
// =====================================================================
[RequireComponent(typeof(Animator))]
public class ZooClipPlayer : MonoBehaviour
{
    // ---------- Settings you can change in the Inspector ----------

    // The animation to play.
    public AnimationClip clip;

    // 1 = normal speed, 0.5 = half speed, 2 = double speed.
    [Range(0f, 2f)]
    public float speed = 1f;

    // ---------- Private variables ----------

    // The little graph that connects the clip to the Animator.
    private PlayableGraph graph;

    // The part of the graph that plays the clip.
    private AnimationClipPlayable clipPlayable;


    // OnEnable runs when the object is switched on: build the graph and start playing.
    private void OnEnable()
    {
        if (clip == null)
        {
            return;
        }

        Animator animator = GetComponent<Animator>();

        // 1. Make an empty graph that runs on normal game time.
        graph = PlayableGraph.Create(name + "_ZooGraph");
        graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

        // 2. Add a "playable" that plays our clip.
        clipPlayable = AnimationClipPlayable.Create(graph, clip);

        // 3. Connect the graph's output to the Animator on this model.
        AnimationPlayableOutput output = AnimationPlayableOutput.Create(graph, "Zoo", animator);
        output.SetSourcePlayable(clipPlayable);

        // Start at a random point in the clip, so neighbours aren't in perfect sync.
        float randomStartTime = Random.value * clip.length;
        clipPlayable.SetTime(randomStartTime);

        // 4. Start playing.
        graph.Play();
    }


    private void Update()
    {
        if (clip == null)
        {
            return;
        }

        if (graph.IsValid() == false)
        {
            return;
        }

        // Use the speed from the Inspector (so you can change it while playing).
        clipPlayable.SetSpeed(speed);

        // If we've gone past the end of the clip, go back to the start.
        // This makes it loop even if the clip itself isn't set to loop.
        double currentTime = clipPlayable.GetTime();
        if (currentTime > clip.length)
        {
            clipPlayable.SetTime(currentTime % clip.length);
        }
    }


    // OnDisable runs when the object is switched off. We must destroy the
    // graph ourselves, otherwise it would carry on using memory.
    private void OnDisable()
    {
        if (graph.IsValid())
        {
            graph.Destroy();
        }
    }
}
