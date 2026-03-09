using UnityEngine;
using Pathfinding;
using System;

[RequireComponent(typeof(Collider2D))]
public class EnemyController : MonoBehaviour
{
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
    [SerializeField] private Health health; // tu script de vida del enemigo
    public UnityEngine.Events.UnityEvent OnDeath = new UnityEngine.Events.UnityEvent();

    [Header("Rotación hacia el jugador")]
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private Transform visualToRotate; // el sprite / hijo que rota (puede ser el propio transform)

    private AIDestinationSetter destSetter;
    private Transform target;
    [SerializeField] private int xpOnDeath = 5; // set en inspector por tipo


    public static event Action<EnemyType> OnAnyEnemyKilled;
    private void Awake()
    {
        if (!health) health = GetComponent<Health>();

        if (health)
            health.OnDeath.AddListener(() => 
            {
                OnDeath.Invoke();
                OnAnyEnemyKilled?.Invoke(enemyType);

                if (GameStatsManager.Instance != null){
                    GameStatsManager.Instance.RegisterKill(enemyType, xpOnDeath);
                }
            });
        destSetter = GetComponent<AIDestinationSetter>();

        // Si no asignas nada en el inspector, rotaremos este mismo objeto
        if (visualToRotate == null)
            visualToRotate = transform;

        baseMoveSpeed = moveSpeed;
        ai = GetComponent<IAstarAI>();
        if (ai != null)
        {
            ai.maxSpeed = moveSpeed;
        }

    }

    private void Update()
    {
        if (touchTimer > 0f)
            touchTimer -= Time.deltaTime;

        // coger el target aunque se asigne más tarde (EnemySetup lo pone en Start)
        if (target == null && destSetter != null)
        {
            target = destSetter.target;
        }

        RotateTowardsTarget();
        ApplyGlobalSlow();
    }

    private void RotateTowardsTarget()
    {
        if (target == null || visualToRotate == null) return;

        Vector3 dir = target.position - visualToRotate.position;
        dir.z = 0f;

        if (dir.sqrMagnitude < 0.0001f) return;

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
        if (touchTimer > 0f) return;

        if (collision.collider.CompareTag("Player"))
        {
            if (collision.collider.TryGetComponent<Health>(out var hp))
                hp.TakeDamage(contactDamage);

            touchTimer = touchCooldown;
        }
    }

    public void SetDifficultyMultiplier(float multiplier)
    {
        moveSpeed *= multiplier;
        baseMoveSpeed = moveSpeed;

        if (ai == null) ai = GetComponent<IAstarAI>();
        if (ai != null)
        {
            ai.maxSpeed = baseMoveSpeed * EnemyGlobalSlow.CurrentMultiplier;
        }
    }


    private void ApplyGlobalSlow()
    {
        if (ai == null) return;

        // baseMoveSpeed contiene tu velocidad ya escalada por dificultad si llamaste SetDifficultyMultiplier
        ai.maxSpeed = baseMoveSpeed * EnemyGlobalSlow.CurrentMultiplier;
    }

}
