using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameOverUI : MonoBehaviour
{
    [Header("Referencias a textos")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI roundsText;
    public TextMeshProUGUI timeText;

    public TextMeshProUGUI killsNormalText;
    public TextMeshProUGUI killsFastText;
    public TextMeshProUGUI killsTankText;
    public TextMeshProUGUI killsShooterText;

    [Header("Config")]
    public string mainMenuScene = "MainMenu";
    public string gameScene = "GameScene";


    private void Start()
    {
        RefreshStatsUI();
        StartCoroutine(RefreshStatsRoutine());
    }

    public void OnBackToMenu()
    {
        if (GameStatsManager.Instance != null)
            GameStatsManager.Instance.ResetRunStats();

        if (LanRuntime.IsActive || LanSessionLifecycle.LastClosedSessionWasLan)
        {
            LanSessionLifecycle.ExitToLobby();
            return;
        }

        SceneManager.LoadScene(mainMenuScene);
    }
    public void PlayAgain()
    {
        if (GameStatsManager.Instance != null)
            GameStatsManager.Instance.ResetRunStats();

        if (LanRuntime.IsActive || LanSessionLifecycle.LastClosedSessionWasLan)
        {
            LanSessionLifecycle.ExitToLobby();
            return;
        }

        SceneManager.LoadScene(gameScene);
    }

    private IEnumerator RefreshStatsRoutine()
    {
        for (int i = 0; i < 90; i++)
        {
            RefreshStatsUI();
            yield return null;
        }
    }

    private void RefreshStatsUI()
    {
        GameRunStatsSnapshot stats = ResolveStatsSnapshot();
        int waveReached = stats.currentWaveReached > 0 ? stats.currentWaveReached : stats.wavesCompleted;

        SetText(scoreText, stats.score + " pts");
        SetText(roundsText, waveReached + " waves");
        SetText(timeText, FormatTimeAlive(stats));
        SetText(killsNormalText, stats.killsNormal + " kills");
        SetText(killsFastText, stats.killsFast + " kills");
        SetText(killsTankText, stats.killsTank + " kills");
        SetText(killsShooterText, stats.killsShooter + " kills");
    }

    private static GameRunStatsSnapshot ResolveStatsSnapshot()
    {
        if (GameStatsManager.Instance != null)
            return GameStatsManager.Instance.GetSnapshot();

        if (GameStatsManager.HasLastRunSnapshot)
            return GameStatsManager.LastRunSnapshot;

        return default;
    }

    private static void SetText(TextMeshProUGUI text, string value)
    {
        if (text != null)
            text.text = value;
    }

    private static string FormatTimeAlive(GameRunStatsSnapshot stats)
    {
        int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(stats.timePlayed));
        if (totalSeconds == 0 && stats.minutesPlayed > 0)
            totalSeconds = stats.minutesPlayed * 60;

        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        if (minutes > 0)
            return minutes + "m " + seconds.ToString("00") + "s";

        return seconds + "s";
    }
}
