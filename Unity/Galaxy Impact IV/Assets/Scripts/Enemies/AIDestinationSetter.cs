using UnityEngine;
using Pathfinding;

namespace Pathfinding
{
    [HelpURL("https://arongranberg.com/astar/docs/aidesinationsetter.html")]
    public class AIDestinationSetter : VersionedMonoBehaviour
    {
        public Transform target;
        IAstarAI ai;

        void OnEnable()
        {
            ai = GetComponent<IAstarAI>();
            if (ai != null)
            {
                // asegurarse de escuchar cada frame
                ai.canSearch = true;
                ai.canMove = true;
                ai.isStopped = false;
                ai.SearchPath();
            }
        }

        void Update()
        {
            if (target != null && ai != null)
            {
                // 🔧 actualizar la posición cada frame (sin depender de eventos)
                ai.destination = target.position;
            }
        }
    }
}
