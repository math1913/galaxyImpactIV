using UnityEngine;

/// Proyectil simple que avanza en +X local, daña y tiene TTL.
[RequireComponent(typeof(Collider2D))]
public class Bullet : MonoBehaviour
{
    [SerializeField] private float speed = 18f;
    [SerializeField] private int damage = 10;
    [SerializeField] private float lifeTime = 2f;
    [SerializeField] private LayerMask hitMask; // Enemigos / Obstáculos

    private float t;
    private ObjectPool pool;
    private bool hasRuntimeDamage = false;
    private int runtimeDamage = 0;

    // Ignite
    private bool igniteOnHit = false;
    private int igniteDamagePerTick = 1;
    private float igniteDuration = 2f;
    private float igniteTickInterval = 0.5f;

    // Piercing fan
    private bool fanOnHit = false;
    private float fanAngleDeg = 60f;
    private float fanSpawnOffset = 0.15f;
    private bool allowFanSpawn = true;

    // Owner (para spawnear balas extra sin consumir ammo)
    private Weapon ownerWeapon = null;

    // Exponer el damage “base” del prefab (para que Weapon calcule el final)
    public int DefaultDamage => damage;


    public void Init(ObjectPool p) => pool = p;

    private void OnEnable()
    {
        t = 0f;

        hasRuntimeDamage = false;
        runtimeDamage = 0;

        igniteOnHit = false;

        fanOnHit = false;
        allowFanSpawn = true;

        ownerWeapon = null;
    }


    private void Update()
    {
        transform.position += transform.right * (speed * Time.deltaTime);
        t += Time.deltaTime;
        if (t >= lifeTime) Despawn();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Bala chocó con: " + other.name + " Layer: " + LayerMask.LayerToName(other.gameObject.layer));

        // Filtra por máscara
        if (((1 << other.gameObject.layer) & hitMask) == 0) return;

        if (other.TryGetComponent<Health>(out var hp))
        {
            hp.TakeDamage(GetCurrentDamage());

            // Ignite: agrega/refresh un DOT en el enemigo
            if (igniteOnHit && igniteDamagePerTick > 0 && igniteDuration > 0f)
            {
                var ignite = other.GetComponent<IgniteStatus>();
                if (ignite == null) ignite = other.gameObject.AddComponent<IgniteStatus>();
                ignite.Apply(hp, igniteDamagePerTick, igniteTickInterval, igniteDuration);
            }

            // Piercing fan: 3 balas a -60, 0, +60 (spread 60°) en dirección del disparo
            if (fanOnHit && ownerWeapon != null)
            {
                // Punto de impacto aproximado
                Vector3 hitPoint = other.ClosestPoint(transform.position);

                float baseAngle = transform.eulerAngles.z;
                Vector3 spawnPos = hitPoint + transform.right * fanSpawnOffset;

                ownerWeapon.SpawnExtraBullet(spawnPos, Quaternion.Euler(0f, 0f, baseAngle - fanAngleDeg), allowFanSpawn: false);
                ownerWeapon.SpawnExtraBullet(spawnPos, Quaternion.Euler(0f, 0f, baseAngle),             allowFanSpawn: false);
                ownerWeapon.SpawnExtraBullet(spawnPos, Quaternion.Euler(0f, 0f, baseAngle + fanAngleDeg), allowFanSpawn: false);
            }
        }
        Despawn();
    }

    private void Despawn()
    {
        if (pool) pool.Return(gameObject);
        else gameObject.SetActive(false);
    }
    public void SetOwnerWeapon(Weapon w) => ownerWeapon = w;

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

    private int GetCurrentDamage() => hasRuntimeDamage ? runtimeDamage : damage;

}
