using Unity.Netcode;
using UnityEngine;

/// Proyectil simple que avanza en +X local, daña y tiene TTL.
[RequireComponent(typeof(Collider2D))]
public class Bullet : MonoBehaviour
{
    [SerializeField] private float speed = 20f;
    [SerializeField] private int damage = 10;
    [SerializeField] private float lifeTime = 2f;
    [SerializeField] private LayerMask hitMask;

    private float elapsed;
    private float runtimeLifeTime;
    private ObjectPool pool;
    private bool hasRuntimeDamage;
    private int runtimeDamage;
    private bool cosmeticOnly;
    private bool shotEndReported;

    private bool igniteOnHit;
    private int igniteDamagePerTick = 1;
    private float igniteDuration = 2f;
    private float igniteTickInterval = 0.5f;

    private bool fanOnHit;
    private float fanAngleDeg = 60f;
    private float fanSpawnOffset = 0.15f;
    private bool allowFanSpawn = true;

    private Weapon ownerWeapon;
    private ulong shotOwnerClientId = ulong.MaxValue;
    private uint shotId;
    private bool notifyShotEnd;

    private Collider2D[] colliders;
    private bool[] colliderDefaults;

    public int DefaultDamage => damage;
    public float BaseLifeTime => lifeTime;

    private void Awake()
    {
        colliders = GetComponents<Collider2D>();
        colliderDefaults = new bool[colliders.Length];
        for (int i = 0; i < colliders.Length; i++)
            colliderDefaults[i] = colliders[i] != null && colliders[i].enabled;
    }

    public void Init(ObjectPool p)
    {
        pool = p;
    }

    private void OnEnable()
    {
        elapsed = 0f;
        runtimeLifeTime = lifeTime;
        hasRuntimeDamage = false;
        runtimeDamage = 0;
        cosmeticOnly = false;
        shotEndReported = false;
        shotOwnerClientId = ulong.MaxValue;
        shotId = 0;
        notifyShotEnd = false;

        igniteOnHit = false;
        fanOnHit = false;
        allowFanSpawn = true;
        ownerWeapon = null;

        RestoreColliderState();
    }

    private void Update()
    {
        if (!ShouldSimulateLocally())
            return;

        transform.position += transform.right * (speed * Time.deltaTime);
        elapsed += Time.deltaTime;

        if (elapsed >= runtimeLifeTime)
            Despawn(transform.position);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!ShouldProcessCollision(other))
            return;

        Vector3 impactPosition = other.ClosestPoint(transform.position);

        if (other.TryGetComponent<Health>(out var hp))
        {
            hp.TakeDamage(GetCurrentDamage(), shotOwnerClientId);

            if (igniteOnHit && igniteDamagePerTick > 0 && igniteDuration > 0f)
            {
                var ignite = other.GetComponent<IgniteStatus>();
                if (ignite == null)
                    ignite = other.gameObject.AddComponent<IgniteStatus>();

                ignite.Apply(hp, igniteDamagePerTick, igniteTickInterval, igniteDuration);
            }

            if (fanOnHit && ownerWeapon != null)
            {
                float baseAngle = transform.eulerAngles.z;
                Vector3 spawnPos = impactPosition + transform.right * fanSpawnOffset;

                ownerWeapon.SpawnExtraBullet(spawnPos, Quaternion.Euler(0f, 0f, baseAngle - fanAngleDeg), allowFanSpawn: false);
                ownerWeapon.SpawnExtraBullet(spawnPos, Quaternion.Euler(0f, 0f, baseAngle), allowFanSpawn: false);
                ownerWeapon.SpawnExtraBullet(spawnPos, Quaternion.Euler(0f, 0f, baseAngle + fanAngleDeg), allowFanSpawn: false);
            }
        }

        Despawn(impactPosition);
    }

    public void ConfigureCosmetic(float maxLifeTime)
    {
        cosmeticOnly = true;
        runtimeLifeTime = Mathf.Max(0.05f, maxLifeTime);
        SetCollidersEnabled(false);
    }

    public void SnapTo(Vector3 position)
    {
        transform.position = position;
    }

    public void ForceDespawn()
    {
        Despawn(transform.position, forced: true);
    }

    public void SetOwnerWeapon(Weapon weapon)
    {
        ownerWeapon = weapon;
    }

    public void SetDamage(int newDamage)
    {
        runtimeDamage = Mathf.Max(0, newDamage);
        hasRuntimeDamage = true;
    }

    public void SetIgnite(bool enabled, int dmgPerTick, float duration, float tickInterval)
    {
        igniteOnHit = enabled;
        igniteDamagePerTick = Mathf.Max(0, dmgPerTick);
        igniteDuration = Mathf.Max(0f, duration);
        igniteTickInterval = Mathf.Max(0.05f, tickInterval);
    }

    public void SetPiercingFan(bool enabled, float angleDeg, float spawnOffset, bool canSpawnFan)
    {
        allowFanSpawn = canSpawnFan;
        fanOnHit = enabled && canSpawnFan;
        fanAngleDeg = Mathf.Clamp(angleDeg, 1f, 179f);
        fanSpawnOffset = Mathf.Max(0f, spawnOffset);
    }

    public void SetShotContext(ulong ownerClientId, uint shotSequence, bool shouldNotifyShotEnd)
    {
        shotOwnerClientId = ownerClientId;
        shotId = shotSequence;
        notifyShotEnd = shouldNotifyShotEnd;
        shotEndReported = false;
    }

    private bool ShouldSimulateLocally()
    {
        if (cosmeticOnly)
            return true;

        if (!LanRuntime.IsActive)
            return true;

        if (!TryGetComponent<NetworkObject>(out var networkObject))
            return true;

        return !networkObject.IsSpawned || LanRuntime.IsServer;
    }

    private bool ShouldProcessCollision(Collider2D other)
    {
        if (cosmeticOnly)
            return false;

        if (LanRuntime.IsActive && !LanRuntime.IsServer)
            return false;

        return ((1 << other.gameObject.layer) & hitMask) != 0;
    }

    private void RestoreColliderState()
    {
        if (colliders == null)
            return;

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
                colliders[i].enabled = colliderDefaults[i];
        }
    }

    private void SetCollidersEnabled(bool enabled)
    {
        if (colliders == null)
            return;

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
                colliders[i].enabled = enabled;
        }
    }

    private void Despawn(Vector3 endPosition, bool forced = false)
    {
        ReportShotEnd(endPosition);

        if (TryGetComponent<NetworkObject>(out var networkObject) && networkObject.IsSpawned)
        {
            if (LanRuntime.IsServer)
                networkObject.Despawn(true);

            return;
        }

        if (pool != null && !cosmeticOnly)
        {
            pool.Return(gameObject);
            return;
        }

        if (forced || Application.isPlaying)
            Destroy(gameObject);
        else
            gameObject.SetActive(false);
    }

    private void ReportShotEnd(Vector3 endPosition)
    {
        if (shotEndReported || !notifyShotEnd || shotOwnerClientId == ulong.MaxValue || !LanRuntime.IsServer)
            return;

        shotEndReported = true;
        LanPlayerAvatar.ServerResolvePlayerShot(shotOwnerClientId, shotId, endPosition);
    }

    private int GetCurrentDamage()
    {
        return hasRuntimeDamage ? runtimeDamage : damage;
    }
}
