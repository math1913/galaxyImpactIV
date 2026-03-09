using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Pathfinding;

[System.Serializable]
public class PickupEntry
{
    public GameObject prefab;

    [Range(0f, 1f)]
    [Tooltip("Probabilidad relativa de que este pickup sea elegido.")]
    public float spawnChance = 0.3f;

    [Tooltip("Número máximo de instancias de este pickup por oleada (solo para spawn al inicio/fin de ronda).")]
    public int maxPerWave = 2;

    [Tooltip("Ronda mínima a partir de la cual este pickup puede aparecer.")]
    public int minWave = 1;
}

[System.Serializable]
public class EnemyEntry
{
    [Tooltip("Prefab del enemigo.")]
    public GameObject prefab;

    [Range(0f, 1f)]
    [Tooltip("Probabilidad relativa de que este enemigo sea elegido al spawnear.")]
    public float spawnChance = 1f;

    [Tooltip("Ronda mínima a partir de la cual este enemigo puede aparecer.")]
    public int minWave = 1;

    [Tooltip("Máximo de instancias de este enemigo por oleada.")]
    public int maxPerWave = 10;

    [Header("Spawn radius por tipo")]
    [Tooltip("Multiplicador del radio MIN para este tipo.")]
    public float spawnRadiusMinMult = 1f;

    [Tooltip("Multiplicador del radio MAX para este tipo.")]
    public float spawnRadiusMaxMult = 1f;

    [Header("Coste para el sistema de puntos")]
    [Tooltip("Cuántos puntos cuesta spawnear UNA unidad de este enemigo.")]
    public int cost = 1;
    [Header("Spawn chance dinámico")]
    [Tooltip("Si está activado, esta probabilidad se interpolará por ronda.")]
    public bool useDynamicSpawnChance = false;

    [Tooltip("Spawn chance EN la ronda mínima (suele ser 0 o muy bajo).")]
    [Range(0f, 1f)] public float spawnChanceStart = 0.1f;

    [Tooltip("Spawn chance EN rondas altas (suele ser el máximo permitido).")]
    [Range(0f, 1f)] public float spawnChanceEnd = 1f;

    [Tooltip("Ronda en la que alcanza spawnChanceEnd.")]
    public int waveForMaxSpawnChance = 20;

    public float GetEffectiveSpawnChance(int currentWave)
    {
        if (!useDynamicSpawnChance)
            return spawnChance; // el valor fijo original

        if (currentWave <= minWave)
            return spawnChanceStart;

        float t = Mathf.InverseLerp(minWave, waveForMaxSpawnChance, currentWave);
        return Mathf.Lerp(spawnChanceStart, spawnChanceEnd, t);
    }

}


public class WaveManager : MonoBehaviour
{
    [Header("Configuración general")]
    [SerializeField] private GameObject enemyPrefab; // fallback si no se usan enemyTypes
    [SerializeField] private int totalWaves = 5;
    [SerializeField] private float timeBetweenWaves = 5f;

    [Header("Radio de spawn para enemigos (respecto a 0,0)")]
    [Tooltip("Distancia mínima desde el punto central (0,0) al generar enemigos.")]
    [SerializeField] private float enemySpawnRadiusMin = 8f;

    [Tooltip("Distancia máxima desde el punto central (0,0) al generar enemigos.")]
    [SerializeField] private float enemySpawnRadiusMax = 14f;
    [Header("Escalado del radio de spawn")]
    [Tooltip("Si está activado, el radio máximo de spawn aumentará con la ronda.")]
    [SerializeField] private bool scaleSpawnRadiusWithWave = true;

    [Tooltip("Cuánto aumenta el RADIO MÁXIMO de spawn por cada ronda.")]
    [SerializeField] private float spawnRadiusMaxIncreasePerWave = 5f;

    [Tooltip("Límite superior del radio máximo de spawn (por ejemplo 300, donde acaba el mapa del A*).")]
    [SerializeField] private float spawnRadiusMaxCap = 300f;


    [Header("Sistema de puntos por oleada")]
    [Tooltip("Puntos de presupuesto en la primera oleada.")]
    [SerializeField] private int pointsFirstWave = 10;

    [Tooltip("Cuántos puntos se añaden por cada oleada.")]
    [SerializeField] private int pointsIncreasePerWave = 5;

    [Tooltip("Tope máximo de puntos por oleada (0 o negativo = sin tope).")]
    [SerializeField] private int maxPointsPerWaveCap = 0;

    [Tooltip("Multiplicador de dificultad por oleada (se puede usar para velocidad, vida, etc).")]
    [SerializeField] private float speedMultiplierPerWave = 1.1f;

    [Header("Tipos de enemigos")]
    [Tooltip("Lista de tipos de enemigos con probabilidad, ronda mínima, coste y máximo por oleada.")]
    [SerializeField] private List<EnemyEntry> enemyTypes = new List<EnemyEntry>();

    [Header("Referencias")]
    [SerializeField] private Transform player;

    [Header("Eventos")]
    public UnityEvent<int> OnWaveStarted;
    public UnityEvent<int> OnWaveCompleted;
    [Header("Audio")]
    [SerializeField] private AudioClip waveStartSfx;
    [SerializeField, Range(0f, 1f)] private float waveStartSfxVolume = 1f;
    [SerializeField] private bool playWaveStartSfx = true;


    [Header("Pickups - Configuración general")]
    [SerializeField] private LayerMask pickupObstructMask;
    [SerializeField] private bool includeTriggerObstacles = true;   // ahora mismo no se usa, pero lo dejamos por si amplías
    [SerializeField, Min(0f)] private float spawnPadding = 0.05f;
    [Tooltip("Lista de pickups con probabilidad, máximo por oleada y ronda mínima.")]
    [SerializeField] private List<PickupEntry> pickups = new List<PickupEntry>();

    [Header("Pickups por oleada (inicio/fin)")]
    [Tooltip("Si true, spawnea pickups al INICIAR cada oleada.")]
    [SerializeField] private bool spawnPickupsOnWaveStart = false;

    [Tooltip("Si true, spawnea pickups al TERMINAR cada oleada.")]
    [SerializeField] private bool spawnPickupsOnWaveEnd = true;

    [Tooltip("Distancia mínima desde el punto central (0,0) al generar pickups de oleada.")]
    [SerializeField] private float pickupSpawnRadiusMin = 4f;

    [Tooltip("Distancia máxima desde el punto central (0,0) al generar pickups de oleada.")]
    [SerializeField] private float pickupSpawnRadiusMax = 10f;

    [Header("Drops por muertes de enemigos")]
    [Tooltip("Mínimo de muertes antes de que pueda aparecer un pickup.")]
    [SerializeField, Min(1)] private int minKillsForPickup = 3;

    [Tooltip("Máximo de muertes antes de que pueda aparecer un pickup.")]
    [SerializeField, Min(1)] private int maxKillsForPickup = 10;

    [Header("Dificultad")]
    [SerializeField] private DifficultyProfile difficulty;
    [SerializeField] private MinimapController minimap;

    // Estado interno
    private int currentWave = 0;
    private int enemiesAlive = 0;
    private bool spawning = false;
    private Coroutine waveRoutine;

    // Contadores de sistema por muertes
    private int killsSinceLastPickup = 0;
    private int nextKillThreshold = 0;

    // Nº de spawns por tipo en la oleada actual
    private Dictionary<EnemyEntry, int> enemySpawnCount = new Dictionary<EnemyEntry, int>();


    private void Start()
    {
        if (difficulty == null)
            difficulty = Resources.Load<DifficultyProfile>("Difficulty/MediumProfile");

        // Cargar dificultad elegida por el jugador (si existe)
        string diffName = PlayerPrefs.GetString("Difficulty", "Medium");
        var loaded = Resources.Load<DifficultyProfile>($"Difficulty/{diffName}Profile");
        if (loaded != null)
            difficulty = loaded;
        Debug.Log("Difficulty loaded: " + difficulty);
        Debug.Log(AudioListener.volume);
        Debug.Log(AudioListener.pause);
        if (!player)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (!minimap)
            minimap = GameObject.FindGameObjectWithTag("Minimap")?.GetComponent<MinimapController>();


        if (minKillsForPickup < 1) minKillsForPickup = 1;
        if (maxKillsForPickup < minKillsForPickup) maxKillsForPickup = minKillsForPickup;
        if (GameStatsManager.Instance != null)
            OnWaveCompleted.AddListener(GameStatsManager.Instance.OnWaveCompleted);
        ResetPickupKillThreshold();

        waveRoutine = StartCoroutine(WaveLoop());
    }

    private IEnumerator WaveLoop()
    {
        for (int i = 1; i <= totalWaves; i++)
        {
            currentWave = i;
            OnWaveStarted?.Invoke(currentWave);
            if (playWaveStartSfx && waveStartSfx != null)
            {
                // Preferir AudioManager si existe (mezcla con música + superposición)
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlaySFX(waveStartSfx, waveStartSfxVolume);
                else
                    AudioSource.PlayClipAtPoint(waveStartSfx, Vector3.zero, waveStartSfxVolume);
            }
            // Reset dash charges al inicio de cada oleada
            if (player != null)
            {
                var dash = player.GetComponent<DashChargesEffect>();
                if (dash != null)
                    dash.ResetChargesToFull();
            }
            // Reset de límites por tipo de enemigo
            enemySpawnCount.Clear();
            foreach (var e in enemyTypes)
            {
                if (e != null)
                    enemySpawnCount[e] = 0;
            }

            // Reset de sistema por muertes por ronda
            killsSinceLastPickup = 0;
            ResetPickupKillThreshold();

            if (spawnPickupsOnWaveStart)
                SpawnPickupsForWave();

            // ==== NUEVO: presupuesto de puntos por oleada ====
            int pointsThisWave = pointsFirstWave + (i - 1) * pointsIncreasePerWave;
            if (maxPointsPerWaveCap > 0)
                pointsThisWave = Mathf.Min(pointsThisWave, maxPointsPerWaveCap);
            //se ajusta segun la dificultad
            pointsThisWave = Mathf.RoundToInt(pointsThisWave * difficulty.pointsMultiplier);

            yield return StartCoroutine(SpawnWaveWithPoints(pointsThisWave));
            yield return new WaitUntil(() => enemiesAlive <= 0);

            OnWaveCompleted?.Invoke(currentWave);

            if (spawnPickupsOnWaveEnd)
                SpawnPickupsForWave();

            yield return new WaitForSeconds(timeBetweenWaves);
        }

    }

    /// <summary>
    /// Spawnea enemigos gastando un presupuesto de puntos.
    /// Mientras queden puntos suficientes para al menos un enemigo válido, sigue spawneando.
    /// </summary>
    private IEnumerator SpawnWaveWithPoints(int pointsBudget)
    {
        spawning = true;
        enemiesAlive = 0;

        int remainingPoints = Mathf.Max(0, pointsBudget);
        Debug.Log($"--- Oleada {currentWave} iniciada. Presupuesto: {remainingPoints} puntos ---");

        // Seguridad para que nunca se quede en bucle infinito.
        int safetyCounter = 1000;

        while (remainingPoints > 0 && safetyCounter-- > 0)
        {
            EnemyEntry chosen = ChooseEnemyTypeForBudget(remainingPoints);
            if (chosen == null)
            {
                // No hay ningún enemigo que podamos pagar o que no haya llegado a su máximo.
                break;
            }

            // Posición de spawn
            Vector2 spawnDir = Random.insideUnitCircle.normalized;

            // Calculamos el radio máximo actual según la ronda
            float currentMaxRadius = enemySpawnRadiusMax;
            //ajustamos segun la dificultad
            currentMaxRadius *= difficulty.spawnRadiusMultiplier;

            if (scaleSpawnRadiusWithWave)
            {
                currentMaxRadius = enemySpawnRadiusMax + spawnRadiusMaxIncreasePerWave * (currentWave - 1);
                currentMaxRadius = Mathf.Min(currentMaxRadius, spawnRadiusMaxCap);
            }

            // Seguridad: que el min nunca supere al max
            float minRadius = enemySpawnRadiusMin;
            if (minRadius > currentMaxRadius)
            {
                minRadius = currentMaxRadius * 0.5f;
            }

            float typeMin = minRadius * Mathf.Max(0.01f, chosen.spawnRadiusMinMult);
            float typeMax = currentMaxRadius * Mathf.Max(0.01f, chosen.spawnRadiusMaxMult);
            typeMax = Mathf.Min(typeMax, spawnRadiusMaxCap);

            // Seguridad: que min no supere max
            if (typeMin > typeMax)
                typeMin = typeMax * 0.9f;

            float distance = Random.Range(typeMin, typeMax);

            Vector3 spawnPos = Vector3.zero + new Vector3(spawnDir.x, spawnDir.y, 0) * distance;

            GameObject prefabToSpawn = chosen.prefab != null ? chosen.prefab : enemyPrefab;
            if (prefabToSpawn == null)
            {
                Debug.LogWarning("WaveManager: EnemyEntry sin prefab y sin fallback asignado.");
                break;
            }

            GameObject enemy = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
            minimap?.RegisterEnemy(enemy.transform);

            // Contador por tipo
            if (!enemySpawnCount.ContainsKey(chosen))
                enemySpawnCount[chosen] = 0;
            enemySpawnCount[chosen]++;

            enemiesAlive++;

            // Cobrar puntos
            int cost = Mathf.Max(1, chosen.cost);
            remainingPoints -= cost;

            // Enemigo apunta al jugador
            var setter = enemy.GetComponent<AIDestinationSetter>();
            if (setter != null)
                setter.target = player;

            // Ajuste de dificultad y suscripción a la muerte
            if (enemy.TryGetComponent<EnemyController>(out var ec))
            {
                float baseMultiplier = Mathf.Pow(speedMultiplierPerWave, currentWave - 1);
                baseMultiplier *= difficulty.enemySpeedMultiplier;
                
                ec.SetDifficultyMultiplier(baseMultiplier);

                Transform enemyTransform = enemy.transform;

                ec.OnDeath.AddListener(() =>
                {
                    minimap?.UnregisterEnemy(enemyTransform);
                    HandleEnemyDeath(enemyTransform.position);
                });
            }
            // Pequeño delay entre spawns
            yield return new WaitForSeconds(0.3f);
        }

        spawning = false;
        Debug.Log($"Oleada {currentWave} terminada de spawnear. Puntos restantes sin usar: {Mathf.Max(0, remainingPoints)}");
    }

    /// <summary>
    /// Elige un tipo de enemigo respetando:
    /// - minWave
    /// - maxPerWave
    /// - coste <= remainingPoints
    /// usando spawnChance como peso.
    /// </summary>
    private EnemyEntry ChooseEnemyTypeForBudget(int remainingPoints)
    {
        float totalChance = 0f;

        // Primero calculamos la suma de pesos de todos los candidatos válidos
        foreach (var e in enemyTypes)
        {
            if (e == null || e.prefab == null) continue;
            if (e.minWave > currentWave) continue;
            float chance = e.GetEffectiveSpawnChance(currentWave);
            if (chance <= 0f) continue;
            if (e.cost <= 0) continue; // evitamos cosas raras

            if (e.cost > remainingPoints) continue; // no lo puedo pagar

            if (enemySpawnCount.TryGetValue(e, out int spawned))
            {
                if (spawned >= e.maxPerWave)
                    continue; // ya llegó a su límite por oleada
            }

            totalChance += chance;
        }

        if (totalChance <= 0f)
            return null;

        float roll = Random.value * totalChance;
        float accum = 0f;

        foreach (var e in enemyTypes)
        {
            if (e == null || e.prefab == null) continue;
            if (e.minWave > currentWave) continue;
            if (e.spawnChance <= 0f) continue;
            if (e.cost <= 0) continue;
            if (e.cost > remainingPoints) continue;

            if (enemySpawnCount.TryGetValue(e, out int spawned))
            {
                if (spawned >= e.maxPerWave)
                    continue;
            }
            float chance = e.GetEffectiveSpawnChance(currentWave);
            accum += chance;
            if (roll <= accum)
            {
                return e;
            }
        }

        return null;
    }

    //   SISTEMA 2: PICKUPS POR MUERTES
    private void HandleEnemyDeath(Vector3 deathPosition)
    {
        enemiesAlive--;

        if (pickups == null || pickups.Count == 0)
            return;

        killsSinceLastPickup++;

        if (killsSinceLastPickup >= nextKillThreshold)
        {
            killsSinceLastPickup = 0;
            ResetPickupKillThreshold();
            SpawnPickupAtPosition(deathPosition);
        }
    }

    private void ResetPickupKillThreshold()
    {
        if (minKillsForPickup < 1)
            minKillsForPickup = 1;
        if (maxKillsForPickup < minKillsForPickup)
            maxKillsForPickup = minKillsForPickup;

        // int: Random.Range(min, maxExclusive)
        nextKillThreshold = Random.Range(minKillsForPickup, maxKillsForPickup + 1);
        // Debug.Log($"Siguiente pickup tras {nextKillThreshold} muertes.");
    }

    private void SpawnPickupAtPosition(Vector3 deathPosition)
    {
        var chosen = ChoosePickupWeighted();
        if (chosen == null || chosen.prefab == null)
            return;

        float radius = 0.4f + spawnPadding;
        Vector3 goodPos = GetValidSpawn2D(deathPosition, radius);

        Instantiate(chosen.prefab, goodPos, Quaternion.identity);
    }

    /// Elige un pickup usando spawnChance como peso, filtrando por minWave y prefab válido.
    private PickupEntry ChoosePickupWeighted()
    {
        float totalChance = 0f;

        foreach (var e in pickups)
        {
            if (e == null || e.prefab == null) continue;
            if (e.minWave > currentWave) continue;
            if (e.spawnChance <= 0f) continue;

            totalChance += e.spawnChance * difficulty.pickupSpawnChanceMultiplier;
        }

        if (totalChance <= 0f)
            return null;

        float roll = Random.value * totalChance;
        float accum = 0f;

        foreach (var e in pickups)
        {
            if (e == null || e.prefab == null) continue;
            if (e.minWave > currentWave) continue;
            if (e.spawnChance <= 0f) continue;

            accum += e.spawnChance * difficulty.pickupSpawnChanceMultiplier;
            if (roll <= accum)
                return e;
        }

        return null;
    }

    //   SISTEMA 1: PICKUPS POR OLEADA
    private void SpawnPickupsForWave()
    {
        foreach (var entry in pickups)
        {
            if (entry?.prefab == null) continue;
            if (entry.minWave > currentWave) continue;         // solo si la ronda >= minWave
            if (Random.value > entry.spawnChance * difficulty.pickupSpawnChanceMultiplier) continue;
            if (entry.maxPerWave <= 0) continue;

            int count = Random.Range(
                1,
                Mathf.RoundToInt(entry.maxPerWave * difficulty.pickupAmountMultiplier) + 1
            );


            for (int i = 0; i < count; i++)
            {
                Vector2 dir = Random.insideUnitCircle.normalized;
                float dist = Random.Range(pickupSpawnRadiusMin, pickupSpawnRadiusMax);
                Vector3 pos = Vector3.zero + new Vector3(dir.x, dir.y, 0) * dist;

                Vector3 goodPos = GetValidSpawn2D(pos, 0.4f);
                Instantiate(entry.prefab, goodPos, Quaternion.identity);
            }
        }
    }

    //   UTILIDADES / GESTIÓN
    private Vector3 GetValidSpawn2D(Vector3 candidate, float radius, int tries = 10000)
    {
        while (tries-- > 0)
        {
            if (!Physics2D.OverlapCircle(candidate, radius, pickupObstructMask))
                return candidate;

            // si está bloqueado → muevo un poco al costado
            candidate += (Vector3)(Random.insideUnitCircle * radius);
        }

        return candidate; // fallback
    }

    public void StopWaves()
    {
        if (waveRoutine != null)
        {
            StopCoroutine(waveRoutine);
            waveRoutine = null;
        }
    }

#if UNITY_EDITOR
    // Gizmos de depuración visual en el Editor
    private void OnDrawGizmosSelected()
    {
        Vector3 center = Vector3.zero;

        // Enemigos (rojo)
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.4f);
        Gizmos.DrawWireSphere(center, enemySpawnRadiusMin);
        Gizmos.DrawWireSphere(center, enemySpawnRadiusMax);

        // Pickups (verde)
        Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.4f);
        Gizmos.DrawWireSphere(center, pickupSpawnRadiusMin);
        Gizmos.DrawWireSphere(center, pickupSpawnRadiusMax);

        // Centro (blanco)
        Gizmos.color = Color.white;
        Gizmos.DrawSphere(center, 0.2f);
    }
#endif
}
