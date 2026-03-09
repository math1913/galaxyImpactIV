using UnityEngine;
using Pathfinding;

public class EnemyShooterController : MonoBehaviour
{
    [Header("Arma")]
    public Weapon weapon;

    [Header("Disparo")]
    public float fireInterval = 0.5f;
    public float shootRange = 7f;

    private AIDestinationSetter destSetter;
    private float timer;

    private void Awake()
    {
        destSetter = GetComponent<AIDestinationSetter>();

        // Si estás usando los flags en Weapon:
        if (weapon != null)
        {
            weapon.automaticFire = true; // si añadiste este campo
            weapon.autoReload   = true;  // idem
        }
    }

    private void Update()
    {
        if (weapon == null || destSetter == null) return;
        if (destSetter.target == null) return; // aún no lo puso EnemySetup

        timer -= Time.deltaTime;

        // Distancia al jugador (el target del AIDestinationSetter)
        Vector3 targetPos = destSetter.target.position;
        float dist = Vector2.Distance(transform.position, targetPos);

        if (dist <= shootRange && timer <= 0f)
        {
            weapon.TryFire();
            timer = fireInterval;
            // Debug opcional:
            // Debug.Log($"{name} dispara (distancia {dist})");
        }

        // Recarga automática (si la tienes en Weapon)
        if (weapon.autoReload && weapon.CurrentAmmo <= 0 && !weapon.IsReloading)
        {
            weapon.Reload();
        }
    }
}
