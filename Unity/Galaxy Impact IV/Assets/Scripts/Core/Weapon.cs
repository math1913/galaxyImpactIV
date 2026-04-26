using UnityEngine;
using System.Collections;
using UnityEngine.Events;
using Unity.Netcode;

/// Control básico de disparo con recarga y dispersión opcional.
public class Weapon : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform muzzle;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private ObjectPool bulletPool;

    [Header("Stats")]
    [SerializeField] private float fireRate = 10f;
    [SerializeField] private int magazineSize = 45;
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

    public int TotalAmmo { get; private set; }
    public int CurrentAmmo { get; private set; }
    public bool IsReloading { get; private set; }
    public float SecondsPerShot => 1f / Mathf.Max(0.01f, fireRate * fireRateMultiplier);
    public float ProjectileLifeTime => TryGetBulletTemplate(out var bullet) ? bullet.BaseLifeTime : 0.25f;
    public Transform Muzzle => muzzle != null ? muzzle : transform;

    public UnityEvent<int> OnAmmoChanged;
    public UnityEvent<int> OnTotalAmmoChanged;

    private float cooldown;
    private float fireRateMultiplier = 1f;
    private float damageMultiplier = 1f;
    private bool useLocalInput = true;

    private bool igniteEnabled = false;
    private int igniteDamagePerTick = 2;
    private float igniteDotDuration = 2.5f;
    private float igniteTickInterval = 0.5f;

    private bool piercingFanEnabled = false;
    [SerializeField] private float fanAngleDeg = 60f;
    [SerializeField] private float fanSpawnOffset = 0.15f;

    private void Awake()
    {
        CurrentAmmo = magazineSize;
        TotalAmmo = totalAmmo;

        OnAmmoChanged?.Invoke(CurrentAmmo);
        OnTotalAmmoChanged?.Invoke(TotalAmmo);
    }

    private void Update()
    {
        if (LanRuntime.IsClientReplica(gameObject))
            return;

        if (cooldown > 0f)
            cooldown -= Time.deltaTime;

        if (!useLocalInput)
            return;

        if (!automaticFire)
        {
            if (Input.GetButton("Fire1"))
                TryFire();

            if (Input.GetKeyDown(KeyCode.R))
                Reload();
        }
    }

    public bool CanAttemptShot()
    {
        return !IsReloading && cooldown <= 0f && CurrentAmmo > 0;
    }

    public bool TryGetMuzzleSnapshot(out Vector3 position, out Quaternion rotation)
    {
        Transform source = Muzzle;
        position = source.position;
        rotation = source.rotation;
        return source != null;
    }

    public bool TryFire()
    {
        if (!TryGetMuzzleSnapshot(out var position, out var rotation))
            return false;

        float spreadOffset = Random.Range(-spreadDeg, spreadDeg);
        return TryFireInternal(position, ApplySpread(rotation, spreadOffset), 0u, false);
    }

    public bool TryFireWithShotId(uint shotId, Vector3 position, Quaternion rotation)
    {
        return TryFireInternal(position, ApplySpread(rotation, ComputeSpreadOffset(shotId)), shotId, true);
    }

    public Bullet SpawnCosmeticShot(uint shotId, Vector3 position, Quaternion rotation, float maxLifeTime)
    {
        if (bulletPrefab == null)
            return null;

        Quaternion finalRotation = ApplySpread(rotation, ComputeSpreadOffset(shotId));
        GameObject go = Instantiate(bulletPrefab, position, finalRotation);
        go.SetActive(true);

        if (!go.TryGetComponent<Bullet>(out var bullet))
            return null;

        ConfigureBullet(bullet, allowFanSpawn: false);
        bullet.ConfigureCosmetic(maxLifeTime);
        bullet.SetShotContext(ulong.MaxValue, shotId, shouldNotifyShotEnd: false);
        return bullet;
    }

    public void ApplyPredictedShot()
    {
        if (CurrentAmmo <= 0)
            return;

        CurrentAmmo--;
        OnAmmoChanged?.Invoke(CurrentAmmo);
    }

    public void RevertPredictedShot()
    {
        CurrentAmmo = Mathf.Clamp(CurrentAmmo + 1, 0, magazineSize);
        OnAmmoChanged?.Invoke(CurrentAmmo);
    }

    public void Reload()
    {
        if (IsReloading || CurrentAmmo == magazineSize || TotalAmmo <= 0)
            return;

        IsReloading = true;
        PlayReloadStartSfxLocal();

        OnAmmoChanged?.Invoke(CurrentAmmo);
        OnTotalAmmoChanged?.Invoke(TotalAmmo);
        StartCoroutine(ReloadRoutine());
    }

    public bool CanReload()
    {
        return !IsReloading && CurrentAmmo < magazineSize && TotalAmmo > 0;
    }

    public void AddAmmo(int amount)
    {
        TotalAmmo += amount;
        OnTotalAmmoChanged?.Invoke(TotalAmmo);
    }

    public void MultiplyFireRate(float multiplier)
    {
        fireRateMultiplier *= multiplier;
        fireRateMultiplier = Mathf.Clamp(fireRateMultiplier, 0.05f, 100f);
    }

    public void DivideFireRate(float multiplier)
    {
        if (Mathf.Approximately(multiplier, 0f))
            return;

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
        if (Mathf.Approximately(multiplier, 0f))
            return;

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

    public void SpawnExtraBullet(Vector3 position, Quaternion rotation, bool allowFanSpawn)
    {
        SpawnBullet(position, rotation, allowFanSpawn);
    }

    public void SetUseLocalInput(bool value)
    {
        useLocalInput = value;
    }

    public void ApplyTuning(
        float tunedFireRate,
        int tunedMagazineSize,
        float tunedReloadTime,
        float tunedSpreadDeg,
        int tunedTotalAmmo,
        AudioClip tunedShootSfx,
        AudioClip tunedReloadStartSfx,
        AudioClip tunedReloadCompleteSfx,
        float tunedShootVolume,
        float tunedReloadVolume)
    {
        fireRate = Mathf.Max(0.01f, tunedFireRate);
        magazineSize = Mathf.Max(1, tunedMagazineSize);
        reloadTime = Mathf.Max(0f, tunedReloadTime);
        spreadDeg = Mathf.Clamp(tunedSpreadDeg, 0f, 8f);
        totalAmmo = Mathf.Max(0, tunedTotalAmmo);
        shootSfx = tunedShootSfx;
        reloadStartSfx = tunedReloadStartSfx;
        reloadCompleteSfx = tunedReloadCompleteSfx;
        shootVolume = Mathf.Clamp01(tunedShootVolume);
        reloadVolume = Mathf.Clamp01(tunedReloadVolume);
    }

    public void ApplyStateFromNetwork(int currentAmmo, int totalAmmo, bool reloading)
    {
        bool wasReloading = IsReloading;
        CurrentAmmo = Mathf.Max(0, currentAmmo);
        TotalAmmo = Mathf.Max(0, totalAmmo);
        IsReloading = reloading;
        OnAmmoChanged?.Invoke(CurrentAmmo);
        OnTotalAmmoChanged?.Invoke(TotalAmmo);

        if (wasReloading && !IsReloading)
            PlayReloadCompleteSfxLocal();

    }

    public void PlayShootSfxLocal(Vector3 position)
    {
        PlayShootSfx(position);
    }

    public void PlayReloadStartSfxLocal()
    {
        if (reloadStartSfx != null)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(reloadStartSfx, reloadVolume);
            else
                AudioSource.PlayClipAtPoint(reloadStartSfx, transform.position, reloadVolume);
        }
    }

    private bool TryFireInternal(Vector3 position, Quaternion rotation, uint shotId, bool hasShotId)
    {
        if (!CanAttemptShot())
            return false;

        PlayShootSfx(position);

        cooldown = SecondsPerShot;
        CurrentAmmo--;
        OnAmmoChanged?.Invoke(CurrentAmmo);

        SpawnBullet(position, rotation, allowFanSpawn: true, shotId, hasShotId);
        return true;
    }

    private void SpawnBullet(Vector3 position, Quaternion rotation, bool allowFanSpawn, uint shotId = 0u, bool hasShotId = false)
    {
        bool allowPooling = !LanRuntime.IsActive && bulletPool != null;
        GameObject go = allowPooling ? bulletPool.Get() : Instantiate(bulletPrefab);
        go.transform.position = position;
        go.transform.rotation = rotation;
        go.SetActive(true);

        if (LanRuntime.IsServer && go.TryGetComponent<NetworkObject>(out var networkObject) && !networkObject.IsSpawned)
            networkObject.Spawn(true);

        if (!go.TryGetComponent<Bullet>(out var bullet))
            return;

        if (allowPooling)
            bullet.Init(bulletPool);

        ConfigureBullet(bullet, allowFanSpawn);

        ulong shooterClientId = ResolveShotOwnerClientId();
        bool shouldNotifyShotEnd = LanRuntime.IsServer && hasShotId && shooterClientId != ulong.MaxValue;
        bullet.SetShotContext(shooterClientId, shotId, shouldNotifyShotEnd);
    }

    private void ConfigureBullet(Bullet bullet, bool allowFanSpawn)
    {
        int baseDmg = bullet.DefaultDamage;
        int finalDmg = Mathf.RoundToInt(baseDmg * damageMultiplier);

        bullet.SetOwnerWeapon(this);
        bullet.SetDamage(finalDmg);
        bullet.SetIgnite(igniteEnabled, igniteDamagePerTick, igniteDotDuration, igniteTickInterval);
        bullet.SetPiercingFan(piercingFanEnabled, fanAngleDeg, fanSpawnOffset, allowFanSpawn);
    }

    private IEnumerator ReloadRoutine()
    {
        yield return new WaitForSeconds(reloadTime);

        int needed = magazineSize - CurrentAmmo;
        int takeFromReserve = Mathf.Min(needed, TotalAmmo);

        CurrentAmmo += takeFromReserve;
        TotalAmmo -= takeFromReserve;
        IsReloading = false;

        OnAmmoChanged?.Invoke(CurrentAmmo);
        OnTotalAmmoChanged?.Invoke(TotalAmmo);

        PlayReloadCompleteSfxLocal();
    }

    private void PlayReloadCompleteSfxLocal()
    {
        if (reloadCompleteSfx != null)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(reloadCompleteSfx, reloadVolume);
            else
                AudioSource.PlayClipAtPoint(reloadCompleteSfx, transform.position, reloadVolume);
        }
    }

    private void PlayShootSfx(Vector3 position)
    {
        if (shootSfx == null)
            return;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(shootSfx, shootVolume);
        else
            AudioSource.PlayClipAtPoint(shootSfx, position, shootVolume);
    }

    private Quaternion ApplySpread(Quaternion rotation, float spreadOffset)
    {
        return rotation * Quaternion.Euler(0f, 0f, spreadOffset);
    }

    private float ComputeSpreadOffset(uint shotId)
    {
        if (spreadDeg <= 0f)
            return 0f;

        uint hash = shotId;
        hash ^= 2747636419u;
        hash *= 2654435769u;
        hash ^= hash >> 16;
        float normalized = (hash & 0x00FFFFFFu) / 16777215f;
        return Mathf.Lerp(-spreadDeg, spreadDeg, normalized);
    }

    private bool TryGetBulletTemplate(out Bullet bullet)
    {
        bullet = null;
        return bulletPrefab != null && bulletPrefab.TryGetComponent(out bullet);
    }

    private ulong ResolveShotOwnerClientId()
    {
        if (!LanRuntime.IsActive)
            return ulong.MaxValue;

        LanPlayerAvatar lanPlayer = GetComponentInParent<LanPlayerAvatar>();
        return lanPlayer != null ? lanPlayer.OwnerClientId : ulong.MaxValue;
    }
}
