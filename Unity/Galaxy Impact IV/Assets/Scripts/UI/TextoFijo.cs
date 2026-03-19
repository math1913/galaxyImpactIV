using UnityEngine;

public class TextoFijo : MonoBehaviour
{
    private Transform objetivo;
    private Vector3 offsetInicial;

    void Start()
    {
        // 1. Guardamos quién es nuestro padre (el jugador) antes de separarnos
        objetivo = transform.parent;

        if (objetivo != null)
        {
            // 2. Calculamos la distancia exacta a la que pusiste el texto en el editor de Unity
            // Esto respeta perfectamente dónde lo colocaste visualmente.
            offsetInicial = transform.position - objetivo.position;

            // 3. ¡El truco! Rompemos la relación padre-hijo.
            // Ahora el texto es libre y ninguna rotación del jugador le afectará.
            transform.SetParent(null);
        }
    }

    void LateUpdate()
    {
        // Si el jugador todavía existe, lo seguimos
        if (objetivo != null)
        {
            // Mantenemos la posición a la distancia exacta que calculamos al inicio
            transform.position = objetivo.position + offsetInicial;
            
            // Forzamos a que esté siempre horizontal
            transform.rotation = Quaternion.identity;
        }
        else
        {
            // Si el jugador se desconecta o es destruido, destruimos este texto también
            Destroy(gameObject);
        }
    }
}