using UnityEngine;

namespace AE2
{
    // La torre nunca se elimina: solo cambia de LOD según la distancia horizontal al jugador.
    [RequireComponent(typeof(LODGroup))]
    public class DetalleTorre : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private float[] lodDistances = { 60f, 150f, 280f };
        [SerializeField] private float hysteresis = 5f;
        [SerializeField] private bool useDistance = true;
        [SerializeField] private float updateInterval = 0.1f;
        [SerializeField] private bool showStats = true;

        private LODGroup lodGroup;
        private int currentLod = -1;
        private float distance;
        private float timer;

        private void Awake()
        {
            lodGroup = GetComponent<LODGroup>();
        }

        private void Start()
        {
            if (player == null)
            {
                ControladorJugador jugador = FindFirstObjectByType<ControladorJugador>();
                if (jugador != null) player = jugador.transform;
                else if (Camera.main != null) player = Camera.main.transform;
            }

            if (!useDistance || player == null)
            {
                lodGroup.ForceLOD(-1);
                return;
            }

            UpdateLod();
        }

        private void Update()
        {
            if (!useDistance || player == null) return;

            timer += Time.deltaTime;
            if (timer < updateInterval) return;
            timer = 0f;

            UpdateLod();
        }

        private void UpdateLod()
        {
            // Se ignora la altura para que volar junto a la torre no baje su detalle
            Vector3 offset = player.position - transform.position;
            offset.y = 0f;
            distance = offset.magnitude;

            int target = 0;
            while (target < lodDistances.Length && distance > lodDistances[target]) target++;
            target = Mathf.Min(target, lodGroup.lodCount - 1);

            // Solo cambia de LOD si se pasa el límite con margen, para que no parpadee
            if (currentLod >= 0)
            {
                if (target > currentLod && distance < lodDistances[currentLod] + hysteresis)
                    target = currentLod;
                else if (target < currentLod && distance > lodDistances[currentLod - 1] - hysteresis)
                    target = currentLod;
            }

            if (target == currentLod) return;

            lodGroup.ForceLOD(target);
            currentLod = target;
        }

        private void OnDisable()
        {
            if (lodGroup != null) lodGroup.ForceLOD(-1);
        }

        
    }
}
