using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class LabPlayModeSmokeTest
{
    private static bool started;
    private static bool finished;
    private static bool sawError;
    private static int playFrames;
    private static Vector2 initialGoombaPosition;
    private static bool previousOptionsEnabled;
    private static EnterPlayModeOptions previousOptions;

    public static void Run()
    {
        previousOptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
        previousOptions = EditorSettings.enterPlayModeOptions;
        EditorSettings.enterPlayModeOptionsEnabled = true;
        EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;

        EditorSceneManager.OpenScene("Assets/Scenes/MarioLab.unity");
        Application.logMessageReceived += OnLog;
        EditorApplication.update += Tick;
        started = true;
        EditorApplication.EnterPlaymode();
    }

    private static void Tick()
    {
        if (!started)
        {
            return;
        }

        if (EditorApplication.isPlaying)
        {
            playFrames++;
            GameObject goomba = GameObject.Find("Goomba");
            if (playFrames == 1 && goomba != null)
            {
                initialGoombaPosition = goomba.GetComponent<Rigidbody2D>().position;
            }

            if (playFrames >= 45 && !finished)
            {
                finished = true;
                try
                {
                    ValidatePlayMode();
                    Debug.Log("MARIO_LAB_PLAYMODE_TEST_PASSED");
                }
                catch (Exception exception)
                {
                    sawError = true;
                    Debug.LogException(exception);
                }
                EditorApplication.ExitPlaymode();
            }
            return;
        }

        if (!finished)
        {
            return;
        }

        Cleanup();
        EditorApplication.Exit(sawError ? 1 : 0);
    }

    private static void ValidatePlayMode()
    {
        GameObject mario = GameObject.Find("Mario");
        GameObject ground = GameObject.Find("Ground");
        GameObject goomba = GameObject.Find("Goomba");
        GameObject canvas = GameObject.Find("HUD Canvas");
        GameObject restartButton = GameObject.Find("RestartButton");
        Require(mario != null, "Mario is missing in Play Mode.");
        Require(ground != null, "Ground is missing in Play Mode.");
        Require(goomba != null, "Goomba is missing in Play Mode.");
        Require(canvas != null && restartButton != null, "HUD Canvas or Restart button is missing.");

        PlayerMovement movement = mario.GetComponent<PlayerMovement>();
        Rigidbody2D marioBody = mario.GetComponent<Rigidbody2D>();
        Rigidbody2D goombaBody = goomba.GetComponent<Rigidbody2D>();
        JumpOverGoomba jump = mario.GetComponent<JumpOverGoomba>();
        TextMeshProUGUI scoreText = movement.scoreText;
        Require(movement != null && marioBody != null, "Mario movement or Rigidbody2D is missing.");
        Require(jump != null && scoreText != null && jump.enemyLocation != null, "Scoring references are incomplete.");
        Require(jump.OnGroundCheck(), "Mario's BoxCast did not detect the Ground layer.");
        Require(restartButton.GetComponent<Button>().navigation.mode == Navigation.Mode.None, "Restart button navigation must be None.");
        Require(mario.transform.position.y > -3.1f, "Mario fell through the ground.");
        Require(Vector2.Distance(initialGoombaPosition, goombaBody.position) > 0.01f, "Goomba patrol did not move.");

        Require(Mathf.Approximately(movement.speed, 10f), "Mario speed must match the lab value of 10.");
        Require(Mathf.Approximately(movement.maxSpeed, 20f), "Mario maxSpeed must match the lab value of 20.");
        Require(Mathf.Approximately(movement.upSpeed, 10f), "Mario upSpeed must match the lab value of 10.");
        Require(Mathf.Approximately(marioBody.gravityScale, 1f), "Mario gravity scale must match the lab value of 1.");

        MethodInfo stopHorizontal = typeof(PlayerMovement).GetMethod("StopHorizontalMovement", BindingFlags.Instance | BindingFlags.NonPublic);
        Require(stopHorizontal != null, "Horizontal stop helper is missing.");
        marioBody.linearVelocity = new Vector2(4f, 6f);
        stopHorizontal.Invoke(movement, null);
        Require(Mathf.Approximately(marioBody.linearVelocity.x, 0f), "Releasing horizontal input did not stop horizontal motion.");
        Require(Mathf.Approximately(marioBody.linearVelocity.y, 6f), "Releasing horizontal input incorrectly changed vertical motion.");

        jump.score = 3;
        scoreText.text = "Score: 3";
        var trigger = typeof(PlayerMovement).GetMethod("OnTriggerEnter2D", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Require(trigger != null, "Mario enemy trigger callback is unavailable.");
        trigger.Invoke(movement, new object[] { goomba.GetComponent<Collider2D>() });
        Require(Mathf.Approximately(Time.timeScale, 0f), "Enemy collision did not freeze time.");
        Require(movement.gameOverPanel != null && movement.gameOverPanel.activeSelf, "Game Over panel was not shown.");
        TextMeshProUGUI gameOverText = movement.gameOverPanel.GetComponentInChildren<TextMeshProUGUI>(true);
        Require(gameOverText != null && gameOverText.text.Contains("Score: 3"), "Game Over did not display the final score.");

        movement.RestartButtonCallback(0);
        Require(Mathf.Approximately(Time.timeScale, 1f), "Restart did not resume time.");
        Require(jump.score == 0 && scoreText.text == "Score: 0", "Restart did not reset the score value and text.");
        Require(!movement.gameOverPanel.activeSelf, "Restart did not hide the Game Over panel.");
        Require(Vector3.Distance(goomba.transform.localPosition, goomba.GetComponent<EnemyMovement>().startPosition) < 0.001f, "Restart did not reset Goomba.");
        Require(!sawError, "An unexpected runtime error was logged.");
    }

    private static void OnLog(string condition, string stackTrace, LogType type)
    {
        // Unity 6 can emit an unrelated Quick Search indexing exception when a
        // freshly copied project starts in batch mode. It is not a game error.
        if (stackTrace.Contains("UnityEditor.Search.SearchDatabase"))
        {
            return;
        }

        if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert)
        {
            sawError = true;
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void Cleanup()
    {
        Application.logMessageReceived -= OnLog;
        EditorApplication.update -= Tick;
        EditorSettings.enterPlayModeOptionsEnabled = previousOptionsEnabled;
        EditorSettings.enterPlayModeOptions = previousOptions;
        started = false;
    }
}
