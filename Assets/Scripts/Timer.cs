using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using UnityEngine;
using System;

public class Timer : MonoBehaviour
{
    [SerializeField]
    GameObject startwall;
    [SerializeField]
    GameObject endwall;
    [SerializeField]
    float distance = 20f;

    [SerializeField]
    TextMeshProUGUI TimerText;

    private float startTime;
    public bool started = false;
    public float CurrentTime;

    public GameObject StartWall
    {
        get => startwall;
        set => startwall = value;
    }

    public GameObject EndWall
    {
        get => endwall;
        set => endwall = value;
    }

    void Start()
    {
    }

    void Update()
    {
        updateTimer();

        // 1. Check startwall trigger ray
        if (startwall != null && !started)
        {
            Ray raystart = new Ray(new Vector3(startwall.transform.position.x, startwall.transform.position.y + 1.0f, startwall.transform.position.z), startwall.transform.right);
            Debug.DrawRay(raystart.origin, raystart.direction * distance);

            RaycastHit hitInfoStart;
            if (Physics.Raycast(raystart, out hitInfoStart, distance))
            {
                if (hitInfoStart.collider.gameObject.CompareTag("Player"))
                {
                    started = true;
                    startTime = Time.time;
                }
            }
        }

        // 2. Check endwall trigger ray
        if (endwall != null && started)
        {
            Ray rayend = new Ray(new Vector3(endwall.transform.position.x - 6.0f, endwall.transform.position.y + 1.5f, endwall.transform.position.z + 3.0f), endwall.transform.right);
            Debug.DrawRay(rayend.origin, rayend.direction * distance);

            RaycastHit hitInfoEnd;
            if (Physics.Raycast(rayend, out hitInfoEnd, distance))
            {
                if (hitInfoEnd.collider.gameObject.CompareTag("Player"))
                {
                    TriggerFinish();
                }
            }
        }
    }

    /// <summary>
    /// Called when the player crosses either the physical Finish Gate trigger
    /// or the raycast finish wall. Stops the timer and records score.
    /// </summary>
    public void TriggerFinish()
    {
        if (started)
        {
            started = false;
            GameManager.Instance.GameFinished(CurrentTime);
        }
        else if (!GameManager.gameFinished)
        {
            float recordedTime = CurrentTime > 0.05f ? CurrentTime : GameManager.Instance.currentTime;
            GameManager.Instance.GameFinished(recordedTime);
        }
    }

    void updateTimer()
    {
        if (started)
        {
            CurrentTime += Time.deltaTime;
            TimeSpan timeSpan = TimeSpan.FromSeconds(CurrentTime);
            if (TimerText != null)
            {
                TimerText.text = string.Format("{0:D2}:{1:D2}:{2:D2}", timeSpan.Minutes, timeSpan.Seconds, timeSpan.Milliseconds / 10);
            }
        }
    }
}
