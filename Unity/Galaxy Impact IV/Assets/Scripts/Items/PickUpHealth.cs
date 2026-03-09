using UnityEngine;

public class PickupHealth : PickupBase
{
    [SerializeField] private int healAmount = 25;

    protected override void OnPickup(Collider2D player)
    {
        if (player.TryGetComponent<Health>(out var hp))
            hp.Heal(healAmount);
    }
}
