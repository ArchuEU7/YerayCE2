using System;
using System.Collections.Generic;
using UnityEngine;

namespace AE2
{
    // Los árboles solo existen cerca del jugador:
    //   más lejos que spawnDistance                  
    //   entre fullDetailDistance y spawnDistance     
    //   más cerca que fullDetailDistance            
    public class CargaArboles : MonoBehaviour
    {
        [Serializable]
        public struct TreeRecord
        {
            public int prefabIndex;
            public Vector3 localPosition;
            public Quaternion localRotation;
            public Vector3 localScale;
        }

        public enum SpawnMode
        {
            InstantiateDestroy,
            Pool
        }

        [SerializeField] private Transform player;
        [SerializeField] private float spawnDistance = 120f;
        [SerializeField] private float fullDetailDistance = 40f;
        [SerializeField] private float hysteresis = 10f;
        [SerializeField] private int lowDetailMeshLod = 1;
        [SerializeField] private SpawnMode mode = SpawnMode.InstantiateDestroy;
        [SerializeField] private float updateInterval = 0.1f;

        [SerializeField] private List<GameObject> prefabs = new List<GameObject>();
        [SerializeField] private List<TreeRecord> records = new List<TreeRecord>();

        private class ActiveTree
        {
            public GameObject instance;
            public Renderer[] renderers;
            public int meshLod;
        }

        private readonly Dictionary<int, ActiveTree> active = new Dictionary<int, ActiveTree>();
        private readonly List<int> toRemove = new List<int>();
        private Dictionary<Vector2Int, List<int>> grid;
        private Vector3[] worldPositions;
        private Stack<GameObject>[] pools;
        private float cellSize;
        private float timer;

        private void Start()
        {
            if (player == null)
            {
                ControladorJugador jugador = FindFirstObjectByType<ControladorJugador>();
                if (jugador != null) player = jugador.transform;
                else if (Camera.main != null) player = Camera.main.transform;
            }

            if (player == null)
            {
                Debug.LogError("CargaArboles: no se ha encontrado el jugador.");
                enabled = false;
                return;
            }

            if (records.Count == 0)
            {
                Debug.LogWarning("CargaArboles: no hay árboles guardados.");
                enabled = false;
                return;
            }

            BuildGrid();

            pools = new Stack<GameObject>[prefabs.Count];
            for (int i = 0; i < pools.Length; i++) pools[i] = new Stack<GameObject>();

            UpdateTrees();
        }

        private void Update()
        {
            timer += Time.deltaTime;
            if (timer < updateInterval) return;
            timer = 0f;

            UpdateTrees();
        }

        // El mapa se divide en celdas para revisar solo los árboles que rodean al jugador
        private void BuildGrid()
        {
            cellSize = Mathf.Max(1f, spawnDistance + hysteresis);
            grid = new Dictionary<Vector2Int, List<int>>();
            worldPositions = new Vector3[records.Count];

            for (int i = 0; i < records.Count; i++)
            {
                Vector3 world = transform.TransformPoint(records[i].localPosition);
                worldPositions[i] = world;

                Vector2Int cell = CellOf(world);
                if (!grid.TryGetValue(cell, out List<int> list))
                {
                    list = new List<int>();
                    grid.Add(cell, list);
                }
                list.Add(i);
            }
        }

        private Vector2Int CellOf(Vector3 position)
        {
            return new Vector2Int(Mathf.FloorToInt(position.x / cellSize), Mathf.FloorToInt(position.z / cellSize));
        }

        private void UpdateTrees()
        {
            Vector3 p = player.position;
            float despawnDistance = spawnDistance + hysteresis;

            toRemove.Clear();
            foreach (KeyValuePair<int, ActiveTree> pair in active)
            {
                float distance = Vector3.Distance(p, worldPositions[pair.Key]);
                if (distance > despawnDistance) toRemove.Add(pair.Key);
                else SetDetail(pair.Value, distance);
            }

            foreach (int id in toRemove) Despawn(id);

            Vector2Int center = CellOf(p);
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    if (!grid.TryGetValue(new Vector2Int(center.x + dx, center.y + dz), out List<int> list)) continue;

                    foreach (int id in list)
                    {
                        if (active.ContainsKey(id)) continue;

                        float distance = Vector3.Distance(p, worldPositions[id]);
                        if (distance <= spawnDistance) Spawn(id, distance);
                    }
                }
            }
        }

        private void Spawn(int id, float distance)
        {
            TreeRecord record = records[id];
            GameObject instance;

            if (mode == SpawnMode.Pool && pools[record.prefabIndex].Count > 0)
            {
                instance = pools[record.prefabIndex].Pop();
                instance.SetActive(true);
            }
            else
            {
                instance = Instantiate(prefabs[record.prefabIndex], transform);
            }

            instance.transform.localPosition = record.localPosition;
            instance.transform.localRotation = record.localRotation;
            instance.transform.localScale = record.localScale;

            ActiveTree tree = new ActiveTree
            {
                instance = instance,
                renderers = instance.GetComponentsInChildren<Renderer>(),
                meshLod = int.MinValue
            };

            SetDetail(tree, distance);
            active.Add(id, tree);
        }

        private void Despawn(int id)
        {
            ActiveTree tree = active[id];
            active.Remove(id);

            if (mode == SpawnMode.Pool)
            {
                tree.instance.SetActive(false);
                pools[records[id].prefabIndex].Push(tree.instance);
            }
            else
            {
                Destroy(tree.instance);
            }
        }

        private void SetDetail(ActiveTree tree, float distance)
        {
            int meshLod = distance < fullDetailDistance ? 0 : lowDetailMeshLod;
            if (meshLod == tree.meshLod) return;

            foreach (Renderer r in tree.renderers)
            {
                r.forceMeshLod = (short)meshLod;
            }

            tree.meshLod = meshLod;
        }

        private void OnDrawGizmosSelected()
        {
            Transform target = player;
            if (target == null && Camera.main != null) target = Camera.main.transform;
            if (target == null) return;

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(target.position, fullDetailDistance);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(target.position, spawnDistance);
        }
    }
}
