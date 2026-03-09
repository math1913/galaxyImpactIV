using UnityEngine;

public class PickupNuke : PickupBase
{
    [Header("Nuke")]
    [SerializeField] private string enemyTag = "Enemy";

    protected override void OnPickup(Collider2D player)
    {
        var enemies = GameObject.FindGameObjectsWithTag(enemyTag);

        foreach (var e in enemies)
        {
            var hp = e.GetComponent<Health>();
            if (hp != null)
            {
                // Usa tu sistema de muerte/eventos existente
                hp.TakeDamage(int.MaxValue);
            }
            else
            {
                Destroy(e);
            }
        }
    }
}
