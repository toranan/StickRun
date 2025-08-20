using System.Collections.Generic;
using UnityEngine;

namespace BananaRun.Runner
{
    public class ObstacleSpawner : MonoBehaviour
    {
        [Header("References")]
        public Transform player;

        [Header("Spawn Distances (Z)")]
        public float spawnAheadDistance = 500f; // 무한 생성을 위해 매우 멀리까지
        public float despawnBehindDistance = 50f;
        public float minGapZ = 5f; // 더 자주 생성
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
        public Vector2 obstacleHeightRange = new Vector2(0.8f, 2.5f); // 낮은 것~높은 것 다양하게
        public Vector2 obstacleWidthRange = new Vector2(1.0f, 1.8f);
        
        [Header("Slide Mechanics")]
        [Tooltip("슬라이딩으로 피할 수 있는 최대 높이")]
        public float maxSlideableHeight = 1.3f;
        
        [Tooltip("낮은 장애물 생성 확률 (0-1)")]
        [Range(0f, 1f)]
        public float lowObstacleChance = 0.4f; // 40% 확률로 낮은 장애물

        private readonly List<GameObject> _spawned = new List<GameObject>();
        private float _nextSpawnZ;
        private float _virtualPlayerDistance = 0f; // 가상 진행 거리

        private void Start()
        {
            if (player == null)
            {
                var playerObj = GameObject.Find("Player");
                if (playerObj != null) player = playerObj.transform;
            }

            _nextSpawnZ = 10f; // 플레이어 바로 앞부터 장애물 생성 시작
        }

        private void Update()
        {
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
                // 가상 진행 거리를 계속 증가시켜서 무한생성 기준점 제공
                _virtualPlayerDistance += runner.forwardSpeed * Time.deltaTime;
            }
        }

        private void MoveObstaclesTowardsPlayer()
        {
            var runner = player.GetComponent<RunnerPlayer>();
            if (runner == null || runner.isDead) return; // 게임오버 시 이동 정지

            // 모든 장애물을 플레이어 쪽으로 이동 (음의 Z 방향)
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
            // 진짜 무한 생성: 가상 진행 거리 기준으로 계속 생성
            float targetZ = _virtualPlayerDistance + spawnAheadDistance;
            int spawnCount = 0;
            while (_nextSpawnZ < targetZ)
            {
                SpawnRandomObstacle(_nextSpawnZ);
                _nextSpawnZ += Random.Range(minGapZ, maxGapZ);
                spawnCount++;
                
                // 무한 루프 방지 (프레임당 최대 50개)
                if (spawnCount > 50) 
                {
                    break;
                }
            }

            // 뒤쪽 정리 (플레이어 뒤로 지나간 장애물 제거)
            float minAllowedZ = -despawnBehindDistance;
            int removedCount = 0;
            for (int i = _spawned.Count - 1; i >= 0; i--)
            {
                if (_spawned[i] == null)
                {
                    _spawned.RemoveAt(i);
                    removedCount++;
                    continue;
                }
                if (_spawned[i].transform.position.z < minAllowedZ)
                {
                    Destroy(_spawned[i]);
                    _spawned.RemoveAt(i);
                    removedCount++;
                }
            }
            
            if (spawnCount > 0)
            {
                Debug.Log($"진짜 무한 생성: 가상거리 {_virtualPlayerDistance:F1}m, 장애물 {spawnCount}개 생성, 총 활성: {_spawned.Count}개");
            }
        }

        private void SpawnRandomObstacle(float z)
        {
            int laneIndex = Random.Range(0, laneCount);
            float half = (laneCount - 1) * 0.5f;
            float laneX = (laneIndex - half) * laneOffset;

            GameObject obj = null;
            bool shouldUsePrefab = HasValidPrefabs() && (usePrefabsOnly || Random.value < 0.7f); // 70% 확률로 프리팹 사용
            
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
            // 유효한 프리팹 중에서 랜덤 선택
            var validPrefabs = System.Array.FindAll(obstaclePrefabs, p => p != null);
            if (validPrefabs.Length == 0) return null;

            GameObject selectedPrefab = validPrefabs[Random.Range(0, validPrefabs.Length)];
            GameObject obj = Instantiate(selectedPrefab, transform);
            
            obj.name = $"Prefab_{selectedPrefab.name}_{z:F1}";
            
            // 위치 설정 (프리팹의 피벗을 고려해서 Y 위치 조정)
            Bounds bounds = GetObjectBounds(obj);
            float groundY = bounds.size.y * 0.5f - bounds.center.y;
            obj.transform.position = new Vector3(laneX, groundY, z);

            // Obstacle 컴포넌트가 없으면 추가
            if (obj.GetComponent<Obstacle>() == null)
            {
                var obs = obj.AddComponent<Obstacle>();
                obs.size = bounds.size;
            }

            Debug.Log($"🎲 프리팹 스폰: {selectedPrefab.name} (레인 {GetLaneNumber(laneX)})");
            return obj;
        }

        private GameObject SpawnProceduralObstacle(float laneX, float z)
        {
            float height;
            Color obstacleColor;
            string obstacleType;
            
            // 높이에 따른 장애물 타입 결정
            if (Random.value < lowObstacleChance)
            {
                // 낮은 장애물 (슬라이딩으로 피할 수 있음)
                height = Random.Range(0.6f, maxSlideableHeight);
                obstacleColor = new Color(0.2f, 0.8f, 0.3f); // 초록색 (슬라이딩 가능)
                obstacleType = "Low";
            }
            else
            {
                // 높은 장애물 (점프하거나 피해야 함)
                height = Random.Range(maxSlideableHeight + 0.2f, obstacleHeightRange.y);
                obstacleColor = new Color(0.8f, 0.2f, 0.2f); // 빨간색 (슬라이딩 불가)
                obstacleType = "High";
            }
            
            float width = Random.Range(obstacleWidthRange.x, obstacleWidthRange.y);

            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = $"{obstacleType}Obstacle_{height:F1}m_{z:F1}";
            obj.transform.SetParent(transform, false);
            obj.transform.localScale = new Vector3(width, height, width);
            obj.transform.position = new Vector3(laneX, height * 0.5f, z);

            var obs = obj.AddComponent<Obstacle>();
            obs.size = obj.transform.localScale;

            var renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = obstacleColor;
            }

            // 슬라이딩 가능 여부 로깅
            bool canSlide = height <= maxSlideableHeight;
            Debug.Log($"🎲 장애물 생성: {obstacleType} (높이 {height:F1}m) → {(canSlide ? "🟢 슬라이딩 가능" : "🔴 슬라이딩 불가")}");

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
            
            // 로컬 공간으로 변환
            bounds.center -= obj.transform.position;
            return bounds;
        }

        private int GetLaneNumber(float laneX)
        {
            float half = (laneCount - 1) * 0.5f;
            return Mathf.RoundToInt((laneX / laneOffset) + half) + 1; // 1, 2, 3으로 표시
        }
    }
}


