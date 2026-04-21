using System;
using Pathfinding;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class EnemyController : NetworkBehaviour
{
    public static event Action<Transform> OnEnemySpawned;
    public static event Action<Transform> OnEnemyDespawned;

    public enum EnemyType
    {
        Normal,
        Fast,
        Tank,
        Shooter
    }

    [SerializeField] private EnemyType enemyType;
    public EnemyType Type => enemyType;

    [Header("Daño por contacto")]
    [SerializeField] private int contactDamage = 10;
    [SerializeField] private float touchCooldown = 0.5f;
    [SerializeField] private float moveSpeed = 3.5f;
    private float touchTimer;
    private float baseMoveSpeed;
    private IAstarAI ai;

    [Header("Referencias")]
    [SerializeField] private Health health;
    public UnityEngine.Events.UnityEvent OnDeath = new UnityEngine.Events.UnityEvent();

    [Header("Rotación hacia el jugador")]
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private Transform visualToRotate;

    [SerializeField] private int xpOnDeath = 5;

    private readonly NetworkVariable<int> syncedHealth = new NetworkVariable<int>(0);
    private readonly NetworkVariable<int> syncedMaxHealth = new NetworkVariable<int>(0);
    private readonly NetworkVariable<bool> syncedDead = new NetworkVariable<bool>(false);
    private readonly NetworkVariable<int> syncedHitPulse = new NetworkVariable<int>(0);

    private AIDestinationSetter destSetter;
    private Transform target;

    public static event Action<EnemyType> OnAnyEnemyKilled;

    private void Awake()
    {
        if (!health)
            health = GetComponent<Health>();

        if (health)
        {
            health.OnDamage.AddListener(HandleDamaged);
            health.OnDeath.AddListener(HandleKilled);
        }

        destSetter = GetComponent<AIDestinationSetter>();

        if (visualToRotate == null)
            visualToRotate = transform;

        baseMoveSpeed = moveSpeed;
        ai = GetComponent<IAstarAI>();
        if (ai != null)
            ai.maxSpeed = moveSpeed;
    }

    public override void OnNetworkSpawn()
    {
        if (!LanRuntime.IsActive)
            return;

        if (IsServer)
            SyncNetworkHealth();
        else
            SubscribeReplication();
    }

    public override void OnNetworkDespawn()
    {
        if (!LanRuntime.IsActive || IsServer)
            return;

        syncedHealth.OnValueChanged -= HandleReplicatedHealthChanged;
        syncedMaxHealth.OnValueChanged -= HandleReplicatedMaxHealthChanged;
        syncedDead.OnValueChanged -= HandleReplicatedDeadChanged;
        syncedHitPulse.OnValueChanged -= HandleReplicatedHitPulseChanged;
    }

    private void OnEnable()
    {
        if (!Application.isPlaying)
            return;

        OnEnemySpawned?.Invoke(transform);
    }

    private void OnDisable()
    {
        if (!Application.isPlaying)
            return;

        OnEnemyDespawned?.Invoke(transform);
    }

    private void Update()
    {
        if (LanRuntime.IsActive && !LanRuntime.IsServer)
            return;

        if (touchTimer > 0f)
            touchTimer -= Time.deltaTime;

        RefreshLanTargetIfNeeded();

        if (destSetter != null)
            target = destSetter.target;

        RotateTowardsTarget();
        ApplyGlobalSlow();

        if (LanRuntime.IsServer)
            SyncNetworkHealth();
    }

    private void RefreshLanTargetIfNeeded()
    {
        if (!LanRuntime.IsServer || destSetter == null)
            return;

        if (destSetter.target != null && (!destSetter.target.TryGetComponent(out LanPlayerAvatar targetLanPlayer) || targetLanPlayer.IsAlive))
            return;

        destSetter.target = LanPlayerAvatar.GetClosestPlayerTransform(transform.position);
    }

    private void RotateTowardsTarget()
    {
        if (target == null || visualToRotate == null)
            return;

        Vector3 dir = target.position - visualToRotate.position;
        dir.z = 0f;

        if (dir.sqrMagnitude < 0.0001f)
            return;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Quaternion desiredRot = Quaternion.AngleAxis(angle, Vector3.forward);
        visualToRotate.rotation = Quaternion.Lerp(
            visualToRotate.rotation,
            desiredRot,
            rotationSpeed * Time.deltaTime
        );
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (LanRuntime.IsActive && !LanRuntime.IsServer)
            return;

        if (touchTimer > 0f)
            return;

        if (collision.collider.CompareTag("Player"))
        {
            if (collision.collider.TryGetComponent(out Health hp))
                hp.TakeDamage(contactDamage);

            touchTimer = touchCooldown;
        }
    }

    public void SetDifficultyMultiplier(float multiplier)
    {
        moveSpeed *= multiplier;
        baseMoveSpeed = moveSpeed;

        if (ai == null)
            ai = GetComponent<IAstarAI>();

        if (ai != null)
            ai.maxSpeed = baseMoveSpeed * EnemyGlobalSlow.CurrentMultiplier;
    }

    private void ApplyGlobalSlow()
    {
        if (ai == null)
            return;

        ai.maxSpeed = baseMoveSpeed * EnemyGlobalSlow.CurrentMultiplier;
    }

    private void HandleDamaged(int _)
    {
        if (LanRuntime.IsActive && IsServer)
        {
            syncedHitPulse.Value++;
            SyncNetworkHealth();
        }
    }

    private void HandleKilled()
    {
        OnDeath.Invoke();
        OnAnyEnemyKilled?.Invoke(enemyType);

        if (LanRuntime.IsActive)
        {
            if (LanRuntime.IsServer)
            {
                SyncNetworkHealth();
                LanPlayerAvatar.ServerRecordEnemyKill(health.LastDamageSourceClientId, enemyType, xpOnDeath);
            }

            return;
        }

        if (GameStatsManager.Instance != null)
            GameStatsManager.Instance.RegisterKill(enemyType, xpOnDeath);
    }

    private void SyncNetworkHealth()
    {
        if (health == null)
            return;

        syncedHealth.Value = health.CurrentHealth;
        syncedMaxHealth.Value = health.MaxHealth;
        syncedDead.Value = health.CurrentHealth <= 0;
    }

    private void SubscribeReplication()
    {
        syncedHealth.OnValueChanged += HandleReplicatedHealthChanged;
        syncedMaxHealth.OnValueChanged += HandleReplicatedMaxHealthChanged;
        syncedDead.OnValueChanged += HandleReplicatedDeadChanged;
        syncedHitPulse.OnValueChanged += HandleReplicatedHitPulseChanged;
        ApplyReplicatedHealth(triggerDamageFeedback: false);
    }

    private void ApplyReplicatedHealth(bool triggerDamageFeedback)
    {
        if (health == null)
            return;

        health.ApplyStateFromNetwork(
            syncedHealth.Value,
            syncedMaxHealth.Value,
            syncedDead.Value,
            triggerDamageFeedback
        );
    }

    private void HandleReplicatedHealthChanged(int previousValue, int newValue)
    {
        ApplyReplicatedHealth(triggerDamageFeedback: false);
    }

    private void HandleReplicatedMaxHealthChanged(int previousValue, int newValue)
    {
        ApplyReplicatedHealth(triggerDamageFeedback: false);
    }

    private void HandleReplicatedDeadChanged(bool previousValue, bool newValue)
    {
        ApplyReplicatedHealth(triggerDamageFeedback: false);
    }

    private void HandleReplicatedHitPulseChanged(int previousValue, int newValue)
    {
        if (newValue == previousValue || health == null)
            return;

        health.PlayDamageFeedback();
        ApplyReplicatedHealth(triggerDamageFeedback: false);
    }
}
