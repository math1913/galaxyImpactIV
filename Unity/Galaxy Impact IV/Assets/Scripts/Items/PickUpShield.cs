using UnityEngine;

public class PickUpShield : PickupBase
{
    [SerializeField] private int shieldAmount = 20;

    protected override void OnPickup(Collider2D player)
    {
        if (player.TryGetComponent<Shield>(out var shield))
        {
            shield.AddShield(shieldAmount);
        }
        else
        {
            Debug.LogWarning("PickUpShield: el jugador no tiene componente Shield.");
        }
    }
}
