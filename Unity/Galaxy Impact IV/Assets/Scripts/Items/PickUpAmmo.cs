using UnityEngine;

public class PickupAmmo : PickupBase
{
    [SerializeField] private int ammoAmount = 30;

    protected override void OnPickup(Collider2D player)
    {
        // Ejemplo: accede a la clase Weapon del jugador
        Transform muzzle = player.transform.Find("Muzzle");
        if (muzzle && muzzle.TryGetComponent<Weapon>(out var weapon))
        {
            Debug.Log($"Recogió munición + {ammoAmount}");
            weapon.AddAmmo(ammoAmount);
        }
    }
}
