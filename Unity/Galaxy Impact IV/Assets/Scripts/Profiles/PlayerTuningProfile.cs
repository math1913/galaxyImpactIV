using UnityEngine;

[CreateAssetMenu(fileName = "PlayerTuningProfile", menuName = "Galaxy Impact/Player Tuning Profile")]
public class PlayerTuningProfile : ScriptableObject
{
    [Header("Movement")]
    [Min(0f)] public float moveSpeed = 3.6f;
    [Min(0f)] public float acceleration = 40f;
    [Min(0f)] public float deceleration = 40f;
    [Min(0f)] public float movementMargin = 3f;

    [Header("Dash")]
    [Min(1)] public int maxDashCharges = 3;
    [Min(0)] public int startDashCharges = 0;
    [Min(0.01f)] public float dashDistanceUnits = 2f;
    [Min(0.02f)] public float dashDuration = 0.1f;
    [Min(0f)] public float dashCooldown = 0.05f;
    public Color dashActiveColor = Color.white;
    public Color dashEmptyColor = new Color(0.15f, 0.15f, 0.15f, 1f);

    [Header("Weapon")]
    [Min(0.01f)] public float fireRate = 3f;
    [Min(1)] public int magazineSize = 45;
    [Min(0f)] public float reloadTime = 1.6f;
    [Range(0f, 8f)] public float spreadDeg = 0.5f;
    [Min(0)] public int totalAmmo = int.MaxValue;

    [Header("Weapon Audio")]
    public AudioClip shootSfx;
    public AudioClip reloadStartSfx;
    public AudioClip reloadCompleteSfx;
    [Range(0f, 1f)] public float shootVolume = 0.6f;
    [Range(0f, 1f)] public float reloadVolume = 0.6f;

    [Header("Health")]
    [Min(1)] public int maxHealth = 100;
    public AudioClip playerDamageSfx;
    public AudioClip playerDeathSfx;
    [Range(0f, 1f)] public float damageVolume = 1f;
    [Range(0f, 1f)] public float deathVolume = 1f;
}
