using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Central game manager handling time-attack flow, shooting accuracy,
/// live score points, auto-level target population, and JSON leaderboard integration.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public static bool gameFinished { get; private set; }
    public static int numOfTargetsHit { get; private set; }
    public static int shotsFired { get; private set; }
    public static int shotsHit { get; private set; }
    public static float accuracy { get; private set; }
    public static int totalLevelScore { get; private set; }

    public float currentTime;
    private bool showLeaderboardToggle = false;

    [Header("UI Score Display References")]
    [SerializeField] private TextMeshProUGUI InitialTimerText;
    [SerializeField] private TextMeshProUGUI TargetHitsText;
    [SerializeField] private TextMeshProUGUI FinalTimerText;

    public PauseManager PauseManager;

    private void Awake()
    {
        Instance = this;

        gameFinished = false;
        numOfTargetsHit = 0;
        shotsFired = 0;
        shotsHit = 0;
        accuracy = 0f;
        totalLevelScore = 0;
        currentTime = 0f;
        showLeaderboardToggle = false;
    }

    private void Start()
    {
        // 1. Guaranteed target population for the active level
        string currentSceneName = SceneManager.GetActiveScene().name;
        if (!currentSceneName.Contains("Menu") && !currentSceneName.Contains("Tech Demo"))
        {
            AutoLevelPopulator.PopulateScene(currentSceneName);
        }

        // 2. Ensure LeaderboardManager instance is active
        if (LeaderboardManager.Instance == null)
        {
            GameObject lbObj = new GameObject("LeaderboardManager");
            lbObj.AddComponent<LeaderboardManager>();
        }
        else
        {
            LeaderboardManager.Instance.ResetRunRecordState();
        }
    }

    private void Update()
    {
        if (!gameFinished)
        {
            currentTime += Time.deltaTime;
        }

        accuracy = shotsFired > 0 ? ((float)shotsHit / shotsFired) * 100f : 0f;

        // Clean scoring: exactly 50 points per target hit
        totalLevelScore = numOfTargetsHit * 50;

        // Toggle leaderboard view with TAB key anytime
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            showLeaderboardToggle = !showLeaderboardToggle;
        }

        // Handle end-of-stage scoring
        if (gameFinished)
        {
            TimeSpan initialTimeSpan = TimeSpan.FromSeconds(Math.Max(0, currentTime));

            if (InitialTimerText != null)
            {
                InitialTimerText.text = "Time: " + string.Format("{0:D2}:{1:D2}:{2:D2}", initialTimeSpan.Minutes, initialTimeSpan.Seconds, initialTimeSpan.Milliseconds / 10);
            }

            if (TargetHitsText != null)
            {
                TargetHitsText.text = $"Targets Hit: {numOfTargetsHit}";
            }

            if (FinalTimerText != null)
            {
                FinalTimerText.text = $"FINAL SCORE: {totalLevelScore} PTS (+50 pts / target)";
            }

            if (PauseManager != null)
            {
                PauseManager.ScorePanel();
            }
        }
    }

    public void TargetHit()
    {
        numOfTargetsHit++;
        totalLevelScore = numOfTargetsHit * 50;
    }

    public void RegisterShotFired()
    {
        shotsFired++;
    }

    public void RegisterShotHit()
    {
        shotsHit++;
    }

    public void GameFinished(float runTime)
    {
        this.currentTime = runTime;
        gameFinished = true;
    }

    private void OnGUI()
    {
        // 1. LIVE HUD SCORING BANNER (Top-Right of Screen)
        DrawLiveScoreHUD();

        // 2. LEADERBOARD PANEL (Shown when finished OR when pressing TAB)
        if ((gameFinished || showLeaderboardToggle) && LeaderboardManager.Instance != null)
        {
            string levelName = SceneManager.GetActiveScene().name;
            LeaderboardManager.Instance.DrawLeaderboardGUI(levelName, currentTime, numOfTargetsHit, totalLevelScore);
        }
    }

    private void DrawLiveScoreHUD()
    {
        float hudWidth = 240f;
        float hudHeight = 82f;
        float x = Screen.width - hudWidth - 20f;
        float y = 20f;

        // Dark translucent backing
        GUI.Box(new Rect(x, y, hudWidth, hudHeight), "");

        GUILayout.BeginArea(new Rect(x + 10, y + 8, hudWidth - 20, hudHeight - 16));

        // Score display
        GUIStyle scoreStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 17,
            fontStyle = FontStyle.Bold
        };
        scoreStyle.normal.textColor = new Color(1f, 0.85f, 0.2f); // Gold
        GUILayout.Label($"SCORE: {totalLevelScore:N0} PTS", scoreStyle);

        // Stats line (Accuracy removed)
        GUIStyle statsStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold
        };
        statsStyle.normal.textColor = Color.white;
        GUILayout.Label($"TARGETS: {numOfTargetsHit}", statsStyle);

        // Leaderboard hint
        GUIStyle hintStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11
        };
        hintStyle.normal.textColor = showLeaderboardToggle ? Color.green : Color.cyan;
        GUILayout.Label(showLeaderboardToggle ? "[TAB] Hide Leaderboard" : "[TAB] View Leaderboard", hintStyle);

        GUILayout.EndArea();
    }
}
