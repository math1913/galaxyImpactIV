using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStatsManager : MonoBehaviour
{
    public static GameStatsManager Instance { get; private set; }

    private static bool s_hasPendingLanSnapshot;
    private static LanRunStatsSnapshot s_pendingLanSnapshot;
    private static bool s_pendingLanSnapshotShouldSend;

    [Header("Referencias")]
    public AuthService authService;

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

    [Header("Config XP por ronda")]
    [Tooltip("XP base que se da al completar cada ronda.")]
    public int xpPerWave = 20;
    [Tooltip("XP extra cada X rondas (por ejemplo cada 5 oleadas).")]
    public int xpBonusEveryXWaves = 5;
    public int xpBonusAmount = 50;

    private bool runFinalized;
    private bool lanSnapshotApplied;
    private bool lanStatsSent;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (s_hasPendingLanSnapshot)
            ApplyLanFinalSnapshot(s_pendingLanSnapshot);

        if (s_pendingLanSnapshotShouldSend && !lanStatsSent)
        {
            lanStatsSent = true;
            _ = SendCurrentStatsToApi();
        }
    }

    public void RegisterKill(EnemyController.EnemyType tipo, int xpGained)
    {
        if (LanRuntime.IsActive)
            return;

        switch (tipo)
        {
            case EnemyController.EnemyType.Normal:
                killsNormal++;
                break;
            case EnemyController.EnemyType.Fast:
                killsFast++;
                break;
            case EnemyController.EnemyType.Tank:
                killsTank++;
                break;
            case EnemyController.EnemyType.Shooter:
                killsShooter++;
                break;
        }

        killsThisRun++;
        xpThisRun += xpGained;
        scoreThisRun += xpGained;
    }

    public void AddXP(int amount)
    {
        if (LanRuntime.IsActive)
            return;

        xpThisRun += amount;
        scoreThisRun += amount;
    }

    public void OnWaveCompleted(int waveNumber)
    {
        if (LanRuntime.IsActive)
            return;

        wavesCompleted = waveNumber;
        xpThisRun += GetWaveXpReward(waveNumber);
    }

    public int GetWaveXpReward(int waveNumber)
    {
        int xpReward = xpPerWave;
        if (xpBonusEveryXWaves > 0 && waveNumber % xpBonusEveryXWaves == 0)
            xpReward += xpBonusAmount;

        return xpReward;
    }

    public async Task EndRunAndSendToApi()
    {
        SceneManager.LoadScene("GameOver");
        await SendCurrentStatsToApi();
    }

    public async Task SendCurrentStatsToApi()
    {
        int userId = PlayerPrefs.GetInt("userId", -1);
        if (userId == -1)
        {
            Debug.LogWarning("No hay userId. Stats no enviados.");
            return;
        }

        int kills = killsThisRun;
        int xp = xpThisRun;
        int score = scoreThisRun;
        int minutes = minutesPlayed;

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

        try
        {
            if (authService != null)
                await authService.UpdateStats(userId, kills, xp);
            else
                Debug.LogWarning("authService es null");

            if (AchievementAPIClient.Instance != null)
                await AchievementAPIClient.Instance.SendBatch(batch);
            else
                Debug.LogWarning("AchievementAPIClient.Instance es null");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error enviando stats: " + e);
        }
    }

    public void ApplyLanFinalSnapshot(LanRunStatsSnapshot snapshot)
    {
        killsNormal = snapshot.killsNormal;
        killsFast = snapshot.killsFast;
        killsTank = snapshot.killsTank;
        killsShooter = snapshot.killsShooter;
        minutesPlayed = snapshot.minutesPlayed;
        pickupHealth = snapshot.pickupHealth;
        pickupShield = snapshot.pickupShield;
        pickupAmmo = snapshot.pickupAmmo;
        pickupExp = snapshot.pickupExp;
        scoreThisRun = snapshot.score;
        killsThisRun = snapshot.killsThisRun;
        xpThisRun = snapshot.xpThisRun;
        wavesCompleted = snapshot.wavesCompleted;
        timePlayed = snapshot.minutesPlayed * 60f;

        runFinalized = true;
        lanSnapshotApplied = true;
        s_pendingLanSnapshot = snapshot;
        s_hasPendingLanSnapshot = true;
    }

    public async Task ApplyLanFinalSnapshotAndSend(LanRunStatsSnapshot snapshot)
    {
        ApplyLanFinalSnapshot(snapshot);
        if (lanStatsSent)
            return;

        lanStatsSent = true;
        s_pendingLanSnapshotShouldSend = false;
        await SendCurrentStatsToApi();
    }

    public static void CachePendingLanSnapshot(LanRunStatsSnapshot snapshot, bool sendToApi = false)
    {
        s_pendingLanSnapshot = snapshot;
        s_hasPendingLanSnapshot = true;
        s_pendingLanSnapshotShouldSend = sendToApi;

        if (Instance != null)
        {
            Instance.ApplyLanFinalSnapshot(snapshot);
            if (sendToApi && !Instance.lanStatsSent)
            {
                Instance.lanStatsSent = true;
                _ = Instance.SendCurrentStatsToApi();
            }
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

        runFinalized = false;
        lanSnapshotApplied = false;
        lanStatsSent = false;
        s_hasPendingLanSnapshot = false;
        s_pendingLanSnapshot = default;
        s_pendingLanSnapshotShouldSend = false;
    }

    private void Update()
    {
        if (runFinalized || lanSnapshotApplied)
            return;

        timePlayed += Time.deltaTime;
        minutesPlayed = Mathf.FloorToInt(timePlayed / 60f);
    }
}
