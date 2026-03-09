using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class AchievementAPIClient : MonoBehaviour
{
    public static AchievementAPIClient Instance; // ← Singleton opcional

    public string baseUrl = "http://localhost:8080/api/achievements";

    private void Awake()
    {
        // Inicializar Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ---------- MODELO REQUEST PARA updateBatch ----------
    [Serializable]
    public class AchievementBatchRequest
    {
        public long userId;

        public int killsNormal;
        public int killsFast;
        public int killsTank;
        public int killsShooter;
        public int minutesPlayed;

        public int score;

        public int pickupHealth;
        public int pickupShield;
        public int pickupAmmo;
        public int pickupExp;
        public int wavesCompleted;
        public int levelReached;
    }

    // ---------- MODELO RESPUESTA LOGROS ----------
    [Serializable]
    public class AchievementDTO
    {
        public string codigo;
        public string nombre;
        public string descripcion;
        
        public int progresoActual;
        public int objetivo;

        public bool completado;

        public int puntosRecompensa;
        public string fechaDesbloqueo;
        public string categoria;
        public string tipo;
    }

    // ============================================================
    // POST /updateBatch  (Método correcto)
    // ============================================================
    public async Task<bool> SendBatch(AchievementBatchRequest batch)
    {
        string url = $"{baseUrl}/updateBatch";

        string json = JsonUtility.ToJson(batch);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        using UnityWebRequest req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        var op = req.SendWebRequest();

        while (!op.isDone)
            await Task.Yield();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error enviando batch: " + req.error);
            return false;
        }

        Debug.Log("Batch enviado correctamente");
        return true;
    }

    // ============================================================
    // GET /user/{id}
    // ============================================================
    public async Task<List<AchievementDTO>> GetAchievements(long userId)
    {
        string url = $"{baseUrl}/user/{userId}";

        using UnityWebRequest req = UnityWebRequest.Get(url);

        var op = req.SendWebRequest();
        while (!op.isDone)
            await Task.Yield();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error obteniendo logros: " + req.error);
            return null;
        }

        string rawJson = req.downloadHandler.text;

        // JsonUtility no soporta arrays directos → se envuelve
        string wrapped = "{\"items\":" + rawJson + "}";
        AchievementListWrapper wrapper = JsonUtility.FromJson<AchievementListWrapper>(wrapped);

        return wrapper.items;
    }

    [Serializable]
    public class AchievementListWrapper
    {
        public List<AchievementDTO> items;
    }
}
