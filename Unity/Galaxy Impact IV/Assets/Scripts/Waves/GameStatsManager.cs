using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public class GameStatsManager : MonoBehaviour
{
    public static GameStatsManager Instance { get; private set; }

    [Header("Referencias")]
    public AuthService authService;   // Asignar en el inspector

    [Header("Stats de esta partida")]
    public int killsNormal = 0;
    public int killsFast = 0;
    public int killsTank = 0;
    public int killsShooter = 0;

    public int minutesPlayed = 0;
    public float timePlayed = 0f;

    public int pickupHealth = 0;
    public int pickupShield = 0;
    public int pickupAmmo = 0;
    public int pickupExp = 0;

    public int scoreThisRun = 0;
    public int killsThisRun = 0;
    public int xpThisRun = 0;
    public int wavesCompleted = 0;
    private bool runActive = false;
    private bool isSubscribedToWaveManager = false;



    [Header("Config XP por ronda")]
    [Tooltip("XP base que se da al completar cada ronda.")]
    public int xpPerWave = 20;
    [Tooltip("XP extra cada X rondas (por ejemplo cada 5 oleadas).")]
    public int xpBonusEveryXWaves = 5;
    public int xpBonusAmount = 50;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // persiste entre escenas

    }

    // ======= KILLS & XP =======
    public void RegisterKill(EnemyController.EnemyType tipo, int xpGained)
    {
        switch(tipo)
        {
            case EnemyController.EnemyType.Normal: killsNormal++; break;
            case EnemyController.EnemyType.Fast: killsFast++; break;
            case EnemyController.EnemyType.Tank: killsTank++; break;
            case EnemyController.EnemyType.Shooter: killsShooter++; break;
        }
        killsThisRun++;
        xpThisRun += xpGained;
        scoreThisRun += xpGained;
        // Debug.Log($"Kill registrada. XP +{xpGained}. Totales: Kills={killsThisRun}, XP={xpThisRun}");
        Debug.Log($"[KILL] Tipo: {tipo} | " +
              $"Normal={killsNormal}, Speed={killsFast}, Tank={killsTank}, Shooter={killsShooter}");
    }

    public void AddXP(int amount)
    {
        xpThisRun += amount;
        scoreThisRun += amount;
        // Debug.Log($"XP +{amount}. XP total run = {xpThisRun}");
    }

    // ======= XP por ronda =======
    public void OnWaveCompleted(int waveNumber)
    {
        wavesCompleted = waveNumber;
        // XP fija por oleada
        xpThisRun += xpPerWave;

        // Bonus cada X rondas
        if (xpBonusEveryXWaves > 0 && waveNumber % xpBonusEveryXWaves == 0)
        {
            xpThisRun += xpBonusAmount;
        }

        // Debug.Log($"Wave {waveNumber} completada. XP total run = {xpThisRun}");
    }

    public async Task EndRunAndSendToApi()
    {
        // ===== SNAPSHOT DE DATOS (ya no dependemos de la escena) =====
        int userId = PlayerPrefs.GetInt("userId", -1);
        int kills = killsThisRun;
        int xp = xpThisRun;
        int score = scoreThisRun;
        int minutes = Mathf.FloorToInt(timePlayed / 60f);

        var batch = new AchievementAPIClient.AchievementBatchRequest
        {
            userId = userId,
            killsNormal = killsNormal,
            killsFast = killsFast,
            killsTank = killsTank,
            killsShooter = killsShooter,
            minutesPlayed = minutes,
            score = score,
            pickupHealth = pickupHealth,
            pickupShield = pickupShield,
            pickupAmmo = pickupAmmo,
            pickupExp = pickupExp,
            wavesCompleted = wavesCompleted
        };

        // ===== CAMBIO DE ESCENA INMEDIATO =====
        SceneManager.LoadScene("GameOver");

        // ===== VALIDACIONES =====
        if (userId == -1)
        {
            Debug.LogWarning("No hay userId. Stats no enviados.");
            return;
        }

        try
        {
            // Actualizar stats básicos
            if (authService != null)
            {
                await authService.UpdateStats(userId, kills, xp);
            }
            else
            {
                Debug.LogWarning("authService es null");
            }

            // Enviar logros
            if (AchievementAPIClient.Instance != null)
            {
                await AchievementAPIClient.Instance.SendBatch(batch);
                Debug.Log("Logros enviados correctamente.");
            }
            else
            {
                Debug.LogWarning("AchievementAPIClient.Instance es null");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error enviando stats: " + e);
        }
    }

    public void ResetRunStats()
    {
        killsThisRun = 0;
        xpThisRun = 0;

        killsNormal = 0;
        killsFast = 0;
        killsTank = 0;
        killsShooter = 0;

        pickupHealth = 0;
        pickupShield = 0;
        pickupAmmo = 0;
        pickupExp = 0;

        scoreThisRun = 0;

        wavesCompleted = 0;
        timePlayed = 0f;   
        minutesPlayed = 0;
    }

    private void Update()
    {
        timePlayed += Time.deltaTime;
        minutesPlayed = Mathf.FloorToInt(timePlayed / 60f);
    }


}
