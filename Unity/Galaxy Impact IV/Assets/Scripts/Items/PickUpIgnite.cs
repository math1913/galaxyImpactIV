using UnityEngine;

public class PickupIgnite : PickupBase
{
    [Header("Ignite")]
    [SerializeField] private float duration = 10f;         // tiempo en que el player tiene ignite activo
    [SerializeField] private int damagePerTick = 2;
    [SerializeField] private float dotDuration = 2.5f;
    [SerializeField] private float tickInterval = 0.5f;
    [SerializeField] private Sprite icon;

    protected override void OnPickup(Collider2D player)
    {
        var bm = player.GetComponent<BuffManager>() ?? player.gameObject.AddComponent<BuffManager>();
        var weapon = player.GetComponentInChildren<Weapon>();
        if (weapon == null) return;

        bm.AddOrRefresh(
            id: "Ignite",
            duration: duration,
            onApply: () => weapon.SetIgnite(true, damagePerTick, dotDuration, tickInterval),
            onRemove: () => weapon.SetIgnite(false, 0, 0f, 0.5f),
            icon: icon
        );
    }
}
