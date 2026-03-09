using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

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
        var stats = GameStatsManager.Instance;

        // Score total
        scoreText.text = stats.scoreThisRun.ToString() + " pts";

        // Rondas completadas
        roundsText.text = stats.wavesCompleted.ToString() + " waves";

        timeText.text = stats.minutesPlayed.ToString() + " min";

        // Kills por tipo
        killsNormalText.text = stats.killsNormal.ToString() + " kills";
        killsFastText.text = stats.killsFast.ToString() + " kills";
        killsTankText.text = stats.killsTank.ToString() + " kills";
        killsShooterText.text = stats.killsShooter.ToString() + " kills";

        stats.ResetRunStats();
    }

    public void OnBackToMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
    public void PlayAgain()
    {
        SceneManager.LoadScene("GameScene");
    }
}
