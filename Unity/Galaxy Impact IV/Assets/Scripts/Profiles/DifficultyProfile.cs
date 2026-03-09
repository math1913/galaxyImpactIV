using UnityEngine;

[CreateAssetMenu(fileName = "DifficultyProfile", menuName = "Difficulty Profile")]
public class DifficultyProfile : ScriptableObject
{
    [Header("Enemigos")]
    public float enemySpeedMultiplier = 1f;  
    public float enemyHealthMultiplier = 1f;
    public float enemyDamageMultiplier = 1f;

    [Header("Sistema de Oleadas (Puntos)")]
    public float pointsMultiplier = 1f;

    [Header("Pickups")]
    public float pickupSpawnChanceMultiplier = 1f;
    public float pickupAmountMultiplier = 1f;

    [Header("Spawn Radius")]
    public float spawnRadiusMultiplier = 1f;
}
