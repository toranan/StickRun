using System.Collections.Generic;
using UnityEngine;

namespace BananaRun.Runner
{
    public class ObstacleSpawner : MonoBehaviour
    {
        [Header("References")]
        public Transform player;

        [Header("Spawn Distances (Z)")]
        public float spawnAheadDistance = 500f;
        public float despawnBehindDistance = 50f;
        public float minGapZ = 5f;
        public float maxGapZ = 10f;

        [Header("Lanes")]
        public int laneCount = 3;
        public float laneOffset = 2f;

        [Header("Prefab Settings")]
        [Tooltip("원하는 장애물 프리팹들을 여기에 드래그하세요")]
        public GameObject[] obstaclePrefabs;
        
        [Tooltip("프리팹 사용 모드 (체크하면 프리팹만 사용, 해제하면 랜덤 큐브도 함께)")]
        public bool usePrefabsOnly = false;

        [Header("Fallback Appearance (프리팹 없을 때)")]
        public Vector2 obstacleHeightRange = new Vector2(0.8f, 2.5f);
        public Vector2 obstacleWidthRange = new Vector2(1.0f, 1.8f);
        
        [Header("Slide Mechanics")]
        [Tooltip("슬라이딩으로 피할 수 있는 최대 높이")]
        public float maxSlideableHeight = 1.3f;

        private readonly List<GameObject> _spawned = new List<GameObject>();
        private float _nextSpawnZ;
        private float _virtualPlayerDistance = 0f;

        private void Start()
        {
            if (player == null)
            {
                var playerObj = GameObject.Find("Player");
                if (playerObj != null) player = playerObj.transform;
            }

            _nextSpawnZ = 10f;
        }

        private void Update()
        {
            // 게임이 플레이 중이 아니면 아무것도 하지 않음
            if (GameManager.Instance != null && GameManager.Instance.currentGameState != GameManager.GameState.Playing)
            {
                return;
            }

            if (player == null) return;

            UpdateVirtualDistance();
            MoveObstaclesTowardsPlayer();
            MaintainObstacles();
        }

        private void UpdateVirtualDistance()
        {
            var runner = player.GetComponent<RunnerPlayer>();
            if (runner != null && !runner.isDead)
            {
                _virtualPlayerDistance += runner.forwardSpeed * Time.deltaTime;
            }
        }

        private void MoveObstaclesTowardsPlayer()
        {
            var runner = player.GetComponent<RunnerPlayer>();
            if (runner == null || runner.isDead) return;

            float moveSpeed = runner.forwardSpeed;
            Vector3 movement = Vector3.back * moveSpeed * Time.deltaTime;

            foreach (var obstacle in _spawned)
            {
                if (obstacle != null)
                {
                    obstacle.transform.position += movement;
                }
            }
        }

        private void MaintainObstacles()
        {
            float targetZ = _virtualPlayerDistance + spawnAheadDistance;
            int spawnCount = 0;
            while (_nextSpawnZ < targetZ)
            {
                SpawnRandomObstacle(_nextSpawnZ);
                _nextSpawnZ += Random.Range(minGapZ, maxGapZ);
                spawnCount++;
                
                if (spawnCount > 50) 
                {
                    break;
                }
            }

            float minAllowedZ = -despawnBehindDistance;
            for (int i = _spawned.Count - 1; i >= 0; i--)
            {
                if (_spawned[i] == null)
                {
                    _spawned.RemoveAt(i);
                    continue;
                }
                if (_spawned[i].transform.position.z < minAllowedZ)
                {
                    Destroy(_spawned[i]);
                    _spawned.RemoveAt(i);
                }
            }
        }

        private void SpawnRandomObstacle(float z)
        {
            int laneIndex = Random.Range(0, laneCount);
            float half = (laneCount - 1) * 0.5f;
            float laneX = (laneIndex - half) * laneOffset;

            GameObject obj = null;
            bool shouldUsePrefab = HasValidPrefabs() && (usePrefabsOnly || Random.value < 0.7f);
            
            if (shouldUsePrefab)
            {
                obj = SpawnPrefabObstacle(laneX, z);
            }
            else
            {
                obj = SpawnProceduralObstacle(laneX, z);
            }

            if (obj != null)
            {
                _spawned.Add(obj);
            }
        }

        private bool HasValidPrefabs()
        {
            return obstaclePrefabs != null && obstaclePrefabs.Length > 0 && System.Array.Exists(obstaclePrefabs, p => p != null);
        }

        private GameObject SpawnPrefabObstacle(float laneX, float z)
        {
            var validPrefabs = System.Array.FindAll(obstaclePrefabs, p => p != null);
            if (validPrefabs.Length == 0) return null;

            GameObject selectedPrefab = validPrefabs[Random.Range(0, validPrefabs.Length)];
            GameObject obj = Instantiate(selectedPrefab, transform);
            
            obj.name = $"Prefab_{selectedPrefab.name}_{z:F1}";
            
            Bounds bounds = GetObjectBounds(obj);
            float groundY = bounds.size.y * 0.5f - bounds.center.y;
            obj.transform.position = new Vector3(laneX, groundY, z);

            if (obj.GetComponent<Obstacle>() == null)
            {
                var obs = obj.AddComponent<Obstacle>();
                obs.size = bounds.size;
            }

            return obj;
        }

        private GameObject SpawnProceduralObstacle(float laneX, float z)
        {
            GameObject obj;
            float obstacleTypeRoll = Random.value;

            if (obstacleTypeRoll < 0.4f) // 40% chance for low obstacles
            {
                // Low obstacle (can be jumped over or slid under)
                float height = Random.Range(0.6f, maxSlideableHeight);
                obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obj.name = $"LowObstacle_{height:F1}m_{z:F1}";
                obj.transform.SetParent(transform, false);
                float width = Random.Range(obstacleWidthRange.x, obstacleWidthRange.y);
                obj.transform.localScale = new Vector3(width, height, width);
                obj.transform.position = new Vector3(laneX, height * 0.5f, z);

                var obs = obj.AddComponent<Obstacle>();
                obs.size = obj.transform.localScale;

                var renderer = obj.GetComponent<Renderer>();
                if (renderer != null) renderer.material.color = new Color(0.2f, 0.8f, 0.3f); // Green

                Debug.Log($"🎲 장애물 생성: Low (높이 {height:F1}m)");
            }
            else if (obstacleTypeRoll < 0.7f) // 30% chance for high obstacles
            {
                // High obstacle (must be jumped over or avoided)
                float height = Random.Range(maxSlideableHeight + 0.2f, obstacleHeightRange.y);
                obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obj.name = $"HighObstacle_{height:F1}m_{z:F1}";
                obj.transform.SetParent(transform, false);
                float width = Random.Range(obstacleWidthRange.x, obstacleWidthRange.y);
                obj.transform.localScale = new Vector3(width, height, width);
                obj.transform.position = new Vector3(laneX, height * 0.5f, z);

                var obs = obj.AddComponent<Obstacle>();
                obs.size = obj.transform.localScale;

                var renderer = obj.GetComponent<Renderer>();
                if (renderer != null) renderer.material.color = new Color(0.8f, 0.2f, 0.2f); // Red

                Debug.Log($"🎲 장애물 생성: High (높이 {height:F1}m)");
            }
            else // 30% chance for high, slidable obstacles
            {
                // High slidable obstacle (must be slid under)
                obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obj.name = $"HighSlidableObstacle_{z:F1}";
                obj.transform.SetParent(transform, false);
                
                float barHeight = 2f; // Taller bar
                float barWidth = laneOffset * 1.2f; // Slightly wider than a lane
                float yPos = 2.5f; // Positioned higher

                obj.transform.localScale = new Vector3(barWidth, barHeight, 0.5f);
                obj.transform.position = new Vector3(laneX, yPos, z);

                var obs = obj.AddComponent<Obstacle>();
                obs.size = obj.transform.localScale;

                var renderer = obj.GetComponent<Renderer>();
                if (renderer != null) renderer.material.color = new Color(0.8f, 0.8f, 0.2f); // Yellow

                Debug.Log($"🎲 장애물 생성: HighSlidable (Y위치 {yPos:F1}m, 높이 {barHeight:F1}m)");
            }

            return obj;
        }

        private Bounds GetObjectBounds(GameObject obj)
        {
            var renderers = obj.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return new Bounds(obj.transform.position, Vector3.one);
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
            
            bounds.center -= obj.transform.position;
            return bounds;
        }

        private int GetLaneNumber(float laneX)
        {
            float half = (laneCount - 1) * 0.5f;
            return Mathf.RoundToInt((laneX / laneOffset) + half) + 1;
        }
    }
}