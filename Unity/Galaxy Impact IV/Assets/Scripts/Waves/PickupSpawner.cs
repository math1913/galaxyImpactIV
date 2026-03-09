using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PickupSpawnData
{
    public GameObject prefab;
    [Range(0f, 1f)]
    [Tooltip("Probabilidad de que este pickup aparezca en una oleada (0 = nunca, 1 = siempre)")]
    public float spawnChance = 0.3f;

    [Tooltip("Número máximo de veces que puede aparecer por oleada.")]
    public int maxPerWave = 2;
}

public class PickupSpawner : MonoBehaviour
{
    [Header("Configuración general")]
    [Tooltip("Radio mínimo desde el jugador para aparecer el pickup.")]
    [SerializeField] private float minSpawnRadius = 3f;
    [Tooltip("Radio máximo desde el jugador para aparecer el pickup.")]
    [SerializeField] private float maxSpawnRadius = 10f;
    [Tooltip("Referencia al jugador (puede asignarse automáticamente si tiene el tag Player).")]
    [SerializeField] private Transform player;

    [Header("Tipos de Pickups y Frecuencias")]
    [SerializeField] private List<PickupSpawnData> pickups = new();

    private void Start()
    {
        if (!player)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    /// <summary>
    /// Llamar desde WaveManager cuando empieza o termina una oleada.
    /// </summary>
    public void SpawnPickups()
    {
        foreach (var pickup in pickups)
        {
            if (!pickup.prefab) continue;

            // Si el número aleatorio supera la probabilidad, no aparece este tipo
            if (Random.value > pickup.spawnChance) continue;

            int count = Random.Range(1, pickup.maxPerWave + 1);

            for (int i = 0; i < count; i++)
            {
                Vector2 dir = Random.insideUnitCircle.normalized;
                float dist = Random.Range(minSpawnRadius, maxSpawnRadius);
                Vector3 pos = player.position + new Vector3(dir.x, dir.y, 0) * dist;

                Instantiate(pickup.prefab, pos, Quaternion.identity);
            }
        }
    }
}
