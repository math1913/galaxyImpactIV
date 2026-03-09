using UnityEngine;

public class PickupSlower : PickupBase
{
    [Header("Slower")]
    [SerializeField] private float duration = 6f;
    [SerializeField, Range(0.05f, 1f)] private float slowMultiplier = 0.6f;
    [SerializeField] private Sprite icon;

    protected override void OnPickup(Collider2D player)
    {
        var bm = player.GetComponent<BuffManager>() ?? player.gameObject.AddComponent<BuffManager>();

        bm.AddOrRefresh(
            id: "Slower",
            duration: duration,
            onApply: () => EnemyGlobalSlow.Activate(slowMultiplier, duration),
            onRemove: () => EnemyGlobalSlow.Clear(),
            icon: icon
        );
    }
}
