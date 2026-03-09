using UnityEngine;
using Pathfinding;
using System.Collections;

[RequireComponent(typeof(Seeker), typeof(AILerp), typeof(AIDestinationSetter))]
public class EnemySetup : MonoBehaviour
{
    IEnumerator Start()
    {
        // Esperar a que el A* haya terminado de escanear
        yield return new WaitUntil(() => AstarPath.active != null && !AstarPath.active.isScanning);

        var player = GameObject.FindGameObjectWithTag("Player")?.transform;
        var setter = GetComponent<AIDestinationSetter>();
        var ai = GetComponent<AILerp>();

        if (player != null)
        {
            setter.target = player;

            // Configurar movimiento y búsqueda
            ai.canMove = true;
            ai.canSearch = true;
            ai.isStopped = false;
            ai.repathRate = 0.5f; // recalcular cada medio segundo
            ai.SearchPath();       // forzar primera ruta

            Debug.Log($"{name}: Ruta inicializada hacia {player.name}");
        }
        else
        {
            Debug.LogWarning($"{name}: No se encontró un objeto con tag 'Player'.");
        }
    }
}
