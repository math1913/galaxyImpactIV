using UnityEngine;

public class PickupFireRateUp : PickupBase
{
    [Header("Fire Rate Up")]
    [SerializeField] private float duration = 8f;
    [SerializeField] private float multiplier = 1.35f;
    [SerializeField] private Sprite icon;

    protected override void OnPickup(Collider2D player)
    {
        // BuffManager (si todavía no lo agregaste, lo crea)
        var bm = player.GetComponent<BuffManager>();
        if (bm == null) bm = player.gameObject.AddComponent<BuffManager>();

        // Weapon puede estar en el mismo GO del player o en un hijo
        var weapon = player.GetComponentInChildren<Weapon>();
        if (weapon == null) return;

        bm.AddOrRefresh(
            id: "FireRateUp",
            duration: duration,
            onApply: () => weapon.MultiplyFireRate(multiplier),
            onRemove: () => weapon.DivideFireRate(multiplier),
            icon: icon
        );
    }
}
