using UnityEngine;
using System.Collections;
using UnityEngine.Events;

/// Control básico de disparo con recarga y dispersión opcional.
public class Weapon : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform muzzle;         // Punto de salida
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private ObjectPool bulletPool;

    [Header("Stats")]
    [SerializeField] private float fireRate = 10f;     // disparos/seg
    [SerializeField] private int magazineSize = 20;
    [SerializeField] private float reloadTime = 1.2f;
    [SerializeField, Range(0f, 8f)] private float spreadDeg = 2f;

    [Header("AI Settings")]
    public bool automaticFire = false;
    public bool autoReload = false;

    [Header("Audio")]
    [SerializeField] private AudioClip shootSfx;
    [SerializeField] private AudioClip reloadStartSfx;
    [SerializeField] private AudioClip reloadCompleteSfx;
    [SerializeField, Range(0f, 1f)] private float shootVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float reloadVolume = 1f;



    [Header("Ammo (Total)")]
    [Tooltip("Balas en reserva")]
    [SerializeField] private int totalAmmo = 99999999;
    public int TotalAmmo { get; private set; }   // reserva actual

    public int CurrentAmmo { get; private set; }
    public bool IsReloading { get; private set; }
    public UnityEvent<int> OnAmmoChanged; // evento (valor actual del cargador)
    public UnityEvent<int> OnTotalAmmoChanged; // evento (valor actual de la reserva)
    float cooldown;
    private float fireRateMultiplier = 1f;
    private float damageMultiplier = 1f;

    // Ignite (config de DOT)
    private bool igniteEnabled = false;
    private int igniteDamagePerTick = 2;
    private float igniteDotDuration = 2.5f;
    private float igniteTickInterval = 0.5f;

    // Piercing fan
    private bool piercingFanEnabled = false;
    [SerializeField] private float fanAngleDeg = 60f;
    [SerializeField] private float fanSpawnOffset = 0.15f;


    private void Awake()
    {
        CurrentAmmo = magazineSize;
        TotalAmmo = totalAmmo; // inicializa la reserva con el valor del inspector

        // Actualizar UI si hay listeners
        OnAmmoChanged?.Invoke(CurrentAmmo);
        OnTotalAmmoChanged?.Invoke(TotalAmmo);
    }

    private void Update()
    {
        if (cooldown > 0f) cooldown -= Time.deltaTime;

        if (!automaticFire)
        {
            if (Input.GetButton("Fire1")) TryFire();
            if (Input.GetKeyDown(KeyCode.R)) Reload();
        }
        else
        {
            // Modo enemigo: dispara solo si alguien externo lo ordena (TryFire llamado desde otro script)
        }
    }


    public void TryFire()
    {
        if (IsReloading || cooldown > 0f) return;

        if (CurrentAmmo <= 0)
        {
            // SFX de vacío si quieres
            return;
        }

        Fire();
    }

    public void Reload()
    {
        // Si ya está recargando, o cargador lleno, o no hay reserva, no hace nada.
        if (IsReloading || CurrentAmmo == magazineSize || TotalAmmo <= 0) return;
        IsReloading = true;
        if (reloadStartSfx != null)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(reloadStartSfx, reloadVolume);
            else
                AudioSource.PlayClipAtPoint(reloadStartSfx, transform.position, reloadVolume);
        }
        OnAmmoChanged?.Invoke(CurrentAmmo); // Actualizar UI del cargador (opcional)
        OnTotalAmmoChanged?.Invoke(TotalAmmo); // Actualizar UI de la reserva (opcional)
        StartCoroutine(ReloadRoutine());
    }
    public void AddAmmo(int amount)
    {
        TotalAmmo += amount;
        OnTotalAmmoChanged?.Invoke(TotalAmmo);
    }

    void Fire()
    {
        
        if (shootSfx != null)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(shootSfx, shootVolume);
            else
                AudioSource.PlayClipAtPoint(shootSfx, muzzle ? muzzle.position : transform.position, shootVolume);
        }
        cooldown = 1f / Mathf.Max(0.01f, fireRate * fireRateMultiplier);
        CurrentAmmo--;

        // Instanciar / Pool
        GameObject go = bulletPool ? bulletPool.Get() : Instantiate(bulletPrefab);
        go.transform.position = muzzle.position;
        go.transform.rotation = muzzle.rotation * Quaternion.Euler(0, 0, Random.Range(-spreadDeg, spreadDeg));
        go.SetActive(true);

        if (go.TryGetComponent<Bullet>(out var b))
        {
            if (bulletPool) b.Init(bulletPool);
            ConfigureBullet(b, allowFanSpawn: true);
        }

        // aualizar UI
        OnAmmoChanged?.Invoke(CurrentAmmo);


    }

    IEnumerator ReloadRoutine()
    {
        
        yield return new WaitForSeconds(reloadTime);

        // Cuántas balas necesita el cargador
        int needed = magazineSize - CurrentAmmo;
        // Cuántas balas podemos tomar de la reserva
        int takeFromReserve = Mathf.Min(needed, TotalAmmo);

        // Rellenar cargador con lo tomado
        CurrentAmmo += takeFromReserve;
        TotalAmmo -= takeFromReserve;

        IsReloading = false;

        // Actualizar UI
        OnAmmoChanged?.Invoke(CurrentAmmo);
        OnTotalAmmoChanged?.Invoke(TotalAmmo);
        if (reloadCompleteSfx != null)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(reloadCompleteSfx, reloadVolume);
            else
                AudioSource.PlayClipAtPoint(reloadCompleteSfx, transform.position, reloadVolume);
        }
 
    }
    public void MultiplyFireRate(float multiplier)
    {
        fireRateMultiplier *= multiplier;
        fireRateMultiplier = Mathf.Clamp(fireRateMultiplier, 0.05f, 100f);
    }

    public void DivideFireRate(float multiplier)
    {
        if (Mathf.Approximately(multiplier, 0f)) return;
        fireRateMultiplier /= multiplier;
        fireRateMultiplier = Mathf.Clamp(fireRateMultiplier, 0.05f, 100f);
    }
    public void MultiplyDamage(float multiplier)
    {
        damageMultiplier *= multiplier;
        damageMultiplier = Mathf.Clamp(damageMultiplier, 0.05f, 100f);
    }

    public void DivideDamage(float multiplier)
    {
        if (Mathf.Approximately(multiplier, 0f)) return;
        damageMultiplier /= multiplier;
        damageMultiplier = Mathf.Clamp(damageMultiplier, 0.05f, 100f);
    }

    public void SetIgnite(bool enabled, int dmgPerTick, float dotDuration, float tickInterval)
    {
        igniteEnabled = enabled;
        igniteDamagePerTick = Mathf.Max(0, dmgPerTick);
        igniteDotDuration = Mathf.Max(0f, dotDuration);
        igniteTickInterval = Mathf.Max(0.05f, tickInterval);
    }

    public void SetPiercingFan(bool enabled)
    {
        piercingFanEnabled = enabled;
    }
    private void ConfigureBullet(Bullet b, bool allowFanSpawn)
    {
        // Daño final: baseDamage del prefab * multiplier
        int baseDmg = b.DefaultDamage;
        int finalDmg = Mathf.RoundToInt(baseDmg * damageMultiplier);

        b.SetOwnerWeapon(this);
        b.SetDamage(finalDmg);
        b.SetIgnite(igniteEnabled, igniteDamagePerTick, igniteDotDuration, igniteTickInterval);
        b.SetPiercingFan(piercingFanEnabled, fanAngleDeg, fanSpawnOffset, allowFanSpawn);
    }

    // Usado por el fan: NO consume ammo, NO toca cooldown
    public void SpawnExtraBullet(Vector3 position, Quaternion rotation, bool allowFanSpawn)
    {
        GameObject go = bulletPool ? bulletPool.Get() : Instantiate(bulletPrefab);
        go.transform.position = position;
        go.transform.rotation = rotation;
        go.SetActive(true);

        if (go.TryGetComponent<Bullet>(out var b))
        {
            if (bulletPool) b.Init(bulletPool);
            ConfigureBullet(b, allowFanSpawn);
        }
    }

}
