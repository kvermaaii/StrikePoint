using UnityEngine;

/// <summary>
/// Physical trigger collider placed at the course halfway finish line.
/// Halves map length for fast-paced college target shooting demos.
/// Triggers stage completion, stops timer, and brings up the local JSON Leaderboard.
/// </summary>
public class CourseFinishTrigger : MonoBehaviour
{
    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;

        // Verify it is the player
        if (other.CompareTag("Player") || other.GetComponent<PlayerMovement>() != null || other.GetComponentInParent<PlayerMovement>() != null)
        {
            hasTriggered = true;
            Debug.Log("[CourseFinishTrigger] >> FINISH LINE CROSSED! Stage Complete! <<");

            Timer timer = Object.FindAnyObjectByType<Timer>();
            if (timer != null)
            {
                timer.TriggerFinish();
            }
            else if (GameManager.Instance != null)
            {
                GameManager.Instance.GameFinished(GameManager.Instance.currentTime);
            }
        }
    }
}

