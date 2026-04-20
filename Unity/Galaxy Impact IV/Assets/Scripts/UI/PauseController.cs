using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseController : MonoBehaviour
{
    [Header("Panel de Pausa")]
    public GameObject pausePanel;

    private bool isPaused = false;

    private void Start()
    {
        if (pausePanel != null)
            pausePanel.SetActive(false);
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape))
            return;

        if (LanRuntime.IsActive && !LanRuntime.IsServer)
            return;

        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    public void PauseGame()
    {
        if (LanRuntime.IsActive && !LanRuntime.IsServer)
            return;

        isPaused = true;

        if (!LanRuntime.IsActive)
            Time.timeScale = 0f;

        if (pausePanel != null)
            pausePanel.SetActive(true);
    }

    public void ResumeGame()
    {
        if (LanRuntime.IsActive && !LanRuntime.IsServer)
            return;

        isPaused = false;
        Time.timeScale = 1f;

        if (pausePanel != null)
            pausePanel.SetActive(false);
    }

    public void GoToMainMenu(string mainMenuSceneName)
    {
        if (LanRuntime.IsActive && !LanRuntime.IsServer)
            return;

        ResumeGame();

        if (LanRuntime.IsActive)
        {
            LanSessionLifecycle.ExitToLobby();
            return;
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }
}
