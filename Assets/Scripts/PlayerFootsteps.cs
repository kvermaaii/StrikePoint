using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Audio controller for player footsteps.
/// Plays surface step sounds timed with player movement velocity and grounded state.
/// </summary>
public class PlayerFootsteps : MonoBehaviour
{
    private AudioSource source;
    public AudioClip[] footstepsounds;
    public float timebetweenSteps = 0.45f;

    private bool isMoving;
    private float timer;
    private PlayerMovement pm;
    private Sliding sliding;

    private void Start()
    {
        source = GetComponent<AudioSource>();
        pm = GetComponentInParent<PlayerMovement>();
        sliding = GetComponentInParent<Sliding>();
    }

    private void Update()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        isMoving = (horizontal != 0f || vertical != 0f);

        if (isMoving)
        {
            timer -= Time.deltaTime;

            if (timer <= 0f)
            {
                timer = timebetweenSteps;
                if (footstepsounds != null && footstepsounds.Length > 0 && source != null)
                {
                    source.clip = footstepsounds[Random.Range(0, footstepsounds.Length)];
                    if (pm != null && pm.isGrounded && (sliding == null || !sliding.sliding))
                    {
                        source.Play();
                    }
                }
            }
        }
        else
        {
            timer = timebetweenSteps;
        }
    }
}

/// <summary>
/// Legacy alias to preserve serialized references from previous project builds.
/// </summary>
[System.Obsolete("Renamed to PlayerFootsteps")]
public class ayaksesi : PlayerFootsteps
{
}

