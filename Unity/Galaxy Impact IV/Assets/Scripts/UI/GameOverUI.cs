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
        var stats = GameStatsManager.Instance;
        if (stats == null)
            return;

        scoreText.text = stats.scoreThisRun.ToString() + " pts";
        roundsText.text = stats.wavesCompleted.ToString() + " waves";
        timeText.text = stats.minutesPlayed.ToString() + " min";
        killsNormalText.text = stats.killsNormal.ToString() + " kills";
        killsFastText.text = stats.killsFast.ToString() + " kills";
        killsTankText.text = stats.killsTank.ToString() + " kills";
        killsShooterText.text = stats.killsShooter.ToString() + " kills";
    }
}
