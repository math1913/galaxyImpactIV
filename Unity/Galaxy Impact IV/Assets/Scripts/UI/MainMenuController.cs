using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Player Stats UI")]
    public TMP_Text usernameText;
    public TMP_Text levelText;
    public TMP_Text aliensKilledText;   // Aliens Totales = suma de kills en puntuaciones[]
    public TMP_Text highScoreText;

    [Header("Opcional: título u otros textos")]
    public TMP_Text titleText;

    [Header("Servicios")]
    public AuthService authService;

    private User currentUser;

    private async void Start()
    {
        // Recuperamos el id guardado en el login
        int userId = PlayerPrefs.GetInt("userId", -1);

        if (userId == -1)
        {
            Debug.LogWarning("No hay userId en PlayerPrefs. Volviendo al Login.");
            SceneManager.LoadScene("Login");
            return;
        }

        currentUser = await authService.GetUserById(userId);

        if (currentUser == null)
        {
            Debug.LogError("No se pudo cargar el usuario desde la API");
            return;
        }

        // Username
        if (usernameText != null)
            usernameText.text = currentUser.username;

        // Nivel actual
        if (levelText != null)
            levelText.text = ("Level: " + currentUser.nivelActual.ToString());

        int aliensTotales = 0;
        if (currentUser.puntuaciones != null)
        {
            foreach (int killCount in currentUser.puntuaciones)
                aliensTotales += killCount;
        }

        if (aliensKilledText != null)
            aliensKilledText.text = ("Aliens Killed: " + aliensTotales.ToString());

        int highScore = 0;
        if (currentUser.puntuaciones != null && currentUser.puntuaciones.Length > 0)
        {
            foreach (int s in currentUser.puntuaciones)
                if (s > highScore)
                    highScore = s;
        }

        if (highScoreText != null)
            highScoreText.text = ("High Score: " + highScore.ToString());

        // Título
        if (titleText != null)
            titleText.text = "MAIN MENU";
    }

    // ===== Botones =====

    public void OnStartGame()
    {
        SceneManager.LoadScene("GameScene"); 
    }

    public void OnSettings()
    {
        SceneManager.LoadScene("Settings");
    }

    public void OnLogros()
    {
        SceneManager.LoadScene("Achievements");
    }

    public void OnLogout()
    {
        PlayerPrefs.DeleteKey("userId");
        PlayerPrefs.Save();
        SceneManager.LoadScene("LoginScene");
    }
}
