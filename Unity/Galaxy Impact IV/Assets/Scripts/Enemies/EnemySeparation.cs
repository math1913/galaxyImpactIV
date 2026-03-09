using UnityEngine;
using Pathfinding;

[RequireComponent(typeof(AILerp))]
public class EnemySeparation : MonoBehaviour
{
    [Header("Configuración")]
    public float separationRadius = 1.2f;     // distancia mínima entre enemigos
    public float separationForce = 2f;        // intensidad del empuje
    public LayerMask enemyMask;               // capa donde están tus enemigos
    public float updateRate = 0.2f;           // cada cuánto recalcular separación

    AILerp ai;
    Vector2 smoothOffset;
    float timer;

    void Start()
    {
        ai = GetComponent<AILerp>();
        ai.canMove = true;
        ai.canSearch = true;

        // Si no se asigna en el inspector, busca la capa "Enemy"
        if (enemyMask == 0) enemyMask = LayerMask.GetMask("Enemy");
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= updateRate)
        {
            timer = 0f;
            ApplySeparation();
        }
    }

    void ApplySeparation()
    {
        Collider2D[] neighbors = Physics2D.OverlapCircleAll(transform.position, separationRadius, enemyMask);

        Vector2 push = Vector2.zero;
        int count = 0;

        foreach (var n in neighbors)
        {
            if (n.gameObject == gameObject) continue;

            Vector2 dir = (Vector2)(transform.position - n.transform.position);
            float dist = dir.magnitude;

            if (dist > 0.01f)
            {
                float strength = Mathf.Clamp01(1f - (dist / separationRadius));
                push += dir.normalized * strength;
                count++;
            }
        }

        if (count > 0)
            push /= count;

        // Aplica el empuje suavemente
        smoothOffset = Vector2.Lerp(smoothOffset, push * separationForce, 0.3f);

        // Actualiza el destino del AILerp ligeramente desplazado
        var setter = GetComponent<AIDestinationSetter>();
        if (setter != null && setter.target != null)
        {
            Vector2 targetPos = setter.target.position;
            ai.destination = targetPos + smoothOffset;
            ai.SearchPath();
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0.5f, 0, 0.2f);
        Gizmos.DrawSphere(transform.position, separationRadius);
    }
}
