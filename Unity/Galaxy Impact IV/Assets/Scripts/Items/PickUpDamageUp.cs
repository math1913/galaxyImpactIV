using UnityEngine;

public class PickupDamageUp : PickupBase
{
    [Header("Damage Up")]
    [SerializeField] private float duration = 10f;
    [SerializeField] private float multiplier = 1.5f;
    [SerializeField] private Sprite icon;

    protected override void OnPickup(Collider2D player)
    {
        var bm = player.GetComponent<BuffManager>() ?? player.gameObject.AddComponent<BuffManager>();
        var weapon = player.GetComponentInChildren<Weapon>();
        if (weapon == null) return;

        bm.AddOrRefresh(
            id: "DamageUp",
            duration: duration,
            onApply: () => weapon.MultiplyDamage(multiplier),
            onRemove: () => weapon.DivideDamage(multiplier),
            icon: icon
        );
    }
}
