using System.Collections.Generic;
using UnityEngine;

namespace BananaRun.Runner
{
    // 아이템의 프리팹과 등장 확률을 관리하는 클래스
    [System.Serializable]
    public class SpawnableItem
    {
        public GameObject prefab;
        [Range(0f, 100f)]
        [Tooltip("다른 아이템들과 비교했을 때의 상대적인 등장 확률 가중치")]
        public float probabilityWeight = 10f;
    }

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

        [Header("Item Settings")]
        [Tooltip("장애물 대신 아이템이 등장할 전체 확률 (0~1)")]
        [Range(0f, 1f)]
        public float itemSpawnChance = 0.2f;
        public List<SpawnableItem> itemsToSpawn;

        [Header("Obstacle Prefab Settings")]
        [Tooltip("원하는 장애물 프리팹들을 여기에 드래그하세요")]
        public GameObject[] obstaclePrefabs;
        
        [Tooltip("프리팹 사용 모드 (체크하면 프리팹만 사용, 해제하면 랜덤 큐브도 함께)")]
        public bool usePrefabsOnly = false;

        [Header("Procedural Obstacle Materials")]
        [Tooltip("점프해서 넘어야 하는 낮은 장애물 재질")]
        public Material lowObstacleMaterial;
        [Tooltip("피해야 하는 높은 장애물 재질")]
        public Material highObstacleMaterial;
        [Tooltip("슬라이드로 피해야 하는 장애물 재질")]
        public Material highSlidableObstacleMaterial;

        [Header("Fallback Appearance (프리팹 없을 때)")]
        public Vector2 obstacleHeightRange = new Vector2(0.8f, 2.5f);
        public Vector2 obstacleWidthRange = new Vector2(1.0f, 1.8f);
        
        [Header("Slide Mechanics")]
        [Tooltip("슬라이딩으로 피할 수 있는 최대 높이")]
        public float maxSlideableHeight = 1.3f;

        private readonly List<GameObject> _spawnedObstacles = new List<GameObject>();
        private readonly List<GameObject> _spawnedItems = new List<GameObject>();
        private float _nextSpawnZ;
        private float _gameStartTime;
        private float _virtualPlayerDistance = 0f;

        private void Start()
        {
            if (player == null)
            {
                var playerObj = GameObject.Find("Player");
                if (playerObj != null) player = playerObj.transform;
            }

            _nextSpawnZ = 100f; // 기존 10f에서 증가
            _gameStartTime = Time.time;
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.currentGameState != GameManager.GameState.Playing)
            {
                return;
            }

            if (player == null) return;

            UpdateVirtualDistance();
            MoveSpawnedObjectsTowardsPlayer();
            MaintainObjects();
        }

        private void UpdateVirtualDistance()
        {
            var runner = player.GetComponent<RunnerPlayer>();
            if (runner != null && !runner.isDead)
            {
                _virtualPlayerDistance += runner.forwardSpeed * Time.deltaTime;
            }
        }

        private void MoveSpawnedObjectsTowardsPlayer()
        {
            var runner = player.GetComponent<RunnerPlayer>();
            if (runner == null || runner.isDead) return;

            float moveSpeed = runner.forwardSpeed;
            Vector3 movement = Vector3.back * moveSpeed * Time.deltaTime;

            // 장애물과 아이템 모두 플레이어 쪽으로 이동
            foreach (var obj in _spawnedObstacles)
            {
                if (obj != null)
                {
                    obj.transform.position += movement;
                }
            }

            foreach (var obj in _spawnedItems)
            {
                if (obj != null)
                {
                    obj.transform.position += movement;
                }
            }
        }

        private void MaintainObjects()
        {
            // 게임 시작 후 3초 동안은 오브젝트를 생성하지 않음
            if (Time.time - _gameStartTime < 6f)
            {
                return;
            }

            float targetZ = _virtualPlayerDistance + spawnAheadDistance;
            int spawnCount = 0;
            while (_nextSpawnZ < targetZ)
            {
                SpawnObject(_nextSpawnZ);
                _nextSpawnZ += Random.Range(minGapZ, maxGapZ);
                spawnCount++;
                
                if (spawnCount > 50) 
                {
                    break;
                }
            }

            float minAllowedZ = -despawnBehindDistance;
            
            // 장애물 정리
            for (int i = _spawnedObstacles.Count - 1; i >= 0; i--)
            {
                if (_spawnedObstacles[i] == null)
                {
                    _spawnedObstacles.RemoveAt(i);
                    continue;
                }
                if (_spawnedObstacles[i].transform.position.z < minAllowedZ)
                {
                    Destroy(_spawnedObstacles[i]);
                    _spawnedObstacles.RemoveAt(i);
                }
            }
            
            // 아이템 정리
            for (int i = _spawnedItems.Count - 1; i >= 0; i--)
            {
                if (_spawnedItems[i] == null)
                {
                    _spawnedItems.RemoveAt(i);
                    continue;
                }
                if (_spawnedItems[i].transform.position.z < minAllowedZ)
                {
                    Destroy(_spawnedItems[i]);
                    _spawnedItems.RemoveAt(i);
                }
            }
        }

        private void SpawnObject(float z)
        {
            // 아이템을 스폰할지, 장애물을 스폰할지 결정
            if (itemsToSpawn.Count > 0 && Random.value < itemSpawnChance)
            {
                SpawnRandomItem(z);
            }
            else
            {
                SpawnRandomObstacle(z);
            }
        }

        private void SpawnRandomItem(float z)
        {
            float totalWeight = 0f;
            foreach (var item in itemsToSpawn)
            {
                totalWeight += item.probabilityWeight;
            }

            float randomValue = Random.Range(0, totalWeight);
            GameObject selectedPrefab = null;
            float cumulativeWeight = 0f;
            foreach (var item in itemsToSpawn)
            {
                cumulativeWeight += item.probabilityWeight;
                if (randomValue < cumulativeWeight)
                {
                    selectedPrefab = item.prefab;
                    break;
                }
            }

            if (selectedPrefab != null)
            {
                int laneIndex = Random.Range(0, laneCount);
                float half = (laneCount - 1) * 0.5f;
                float laneX = (laneIndex - half) * laneOffset;

                GameObject obj = Instantiate(selectedPrefab, transform);
                obj.transform.position = new Vector3(laneX, 1f, z); // 아이템은 보통 공중에 살짝 띄움
                _spawnedItems.Add(obj);
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
                _spawnedObstacles.Add(obj);
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
            Renderer renderer;

            if (obstacleTypeRoll < 0.4f) // 40% chance for low obstacles
            {
                float height = Random.Range(0.6f, maxSlideableHeight);
                obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obj.name = $"LowObstacle_{height:F1}m_{z:F1}";
                obj.transform.SetParent(transform, false);
                float width = Random.Range(obstacleWidthRange.x, obstacleWidthRange.y);
                obj.transform.localScale = new Vector3(width, height, width);
                obj.transform.position = new Vector3(laneX, height * 0.5f, z);

                var obs = obj.AddComponent<Obstacle>();
                obs.size = obj.transform.localScale;

                renderer = obj.GetComponent<Renderer>();
                if (renderer != null && lowObstacleMaterial != null) 
                {
                    renderer.material = lowObstacleMaterial;
                }
            }
            else if (obstacleTypeRoll < 0.7f) // 30% chance for high obstacles
            {
                float height = Random.Range(maxSlideableHeight + 0.2f, obstacleHeightRange.y);
                obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obj.name = $"HighObstacle_{height:F1}m_{z:F1}";
                obj.transform.SetParent(transform, false);
                float width = Random.Range(obstacleWidthRange.x, obstacleWidthRange.y);
                obj.transform.localScale = new Vector3(width, height, width);
                obj.transform.position = new Vector3(laneX, height * 0.5f, z);

                var obs = obj.AddComponent<Obstacle>();
                obs.size = obj.transform.localScale;

                renderer = obj.GetComponent<Renderer>();
                if (renderer != null && highObstacleMaterial != null)
                {
                    renderer.material = highObstacleMaterial;
                }
            }
            else // 30% chance for high, slidable obstacles
            {
                obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obj.name = $"HighSlidableObstacle_{z:F1}";
                obj.transform.SetParent(transform, false);
                
                float barHeight = 2f;
                float barWidth = laneOffset * 1.2f;
                float yPos = 2.5f;

                obj.transform.localScale = new Vector3(barWidth, barHeight, 0.5f);
                obj.transform.position = new Vector3(laneX, yPos, z);

                var obs = obj.AddComponent<Obstacle>();
                obs.size = obj.transform.localScale;

                renderer = obj.GetComponent<Renderer>();
                if (renderer != null && highSlidableObstacleMaterial != null)
                {
                    renderer.material = highSlidableObstacleMaterial;
                }
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