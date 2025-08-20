using System.Collections.Generic;
using UnityEngine;

namespace BananaRun.Runner
{
    public class AdvancedObstacleSpawner : MonoBehaviour
    {
        [Header("References")]
        public Transform player;

        [Header("Spawn Distances (Z)")]
        public float spawnAheadDistance = 500f;
        public float despawnBehindDistance = 50f;
        public float minGapZ = 3f;
        public float maxGapZ = 8f;

        [Header("Lanes")]
        public int laneCount = 3;
        public float laneOffset = 2f;

        [Header("Spawn Mode")]
        public SpawnMode spawnMode = SpawnMode.Random;
        
        public enum SpawnMode { Random, TypeBased, PatternBased, Mixed }

        [Header("Legacy Random Settings")]
        public Vector2 obstacleHeightRange = new Vector2(1.0f, 2.2f);
        public Vector2 obstacleWidthRange = new Vector2(1.0f, 1.8f);

        [Header("Type-Based Settings")]
        public ObstacleType[] obstacleTypes;

        [Header("Pattern-Based Settings")]
        public ObstaclePattern[] obstaclePatterns;
        
        [Header("Mixed Mode Settings")]
        [Range(0f, 1f)]
        public float patternSpawnChance = 0.3f; // 30% 확률로 패턴, 70% 확률로 타입 기반

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
            Debug.Log($"고급 장애물 스포너 시작 - 모드: {spawnMode}");
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
                SpawnObstacleByMode(_nextSpawnZ);
                _nextSpawnZ += Random.Range(minGapZ, maxGapZ);
                spawnCount++;
                
                if (spawnCount > 50) break;
            }

            // 뒤쪽 정리
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

        private void SpawnObstacleByMode(float z)
        {
            switch (spawnMode)
            {
                case SpawnMode.Random:
                    SpawnRandomObstacle(z);
                    break;
                case SpawnMode.TypeBased:
                    SpawnTypedObstacle(z);
                    break;
                case SpawnMode.PatternBased:
                    SpawnPatternObstacle(z);
                    break;
                case SpawnMode.Mixed:
                    if (Random.value < patternSpawnChance)
                        SpawnPatternObstacle(z);
                    else
                        SpawnTypedObstacle(z);
                    break;
            }
        }

        private void SpawnRandomObstacle(float z)
        {
            int laneIndex = Random.Range(0, laneCount);
            float half = (laneCount - 1) * 0.5f;
            float laneX = (laneIndex - half) * laneOffset;

            float height = Random.Range(obstacleHeightRange.x, obstacleHeightRange.y);
            float width = Random.Range(obstacleWidthRange.x, obstacleWidthRange.y);

            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = $"RandomObstacle_{z:F1}";
            obj.transform.SetParent(transform, false);
            obj.transform.localScale = new Vector3(width, height, width);
            obj.transform.position = new Vector3(laneX, height * 0.5f, z);

            var obs = obj.AddComponent<Obstacle>();
            obs.size = obj.transform.localScale;

            var renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                Color col = Color.Lerp(new Color(0.8f, 0.2f, 0.2f), new Color(0.9f, 0.55f, 0.15f), Random.value);
                renderer.sharedMaterial.color = col;
            }

            _spawned.Add(obj);
        }

        private void SpawnTypedObstacle(float z)
        {
            if (obstacleTypes == null || obstacleTypes.Length == 0)
            {
                SpawnRandomObstacle(z);
                return;
            }

            // 가중치 기반 선택
            ObstacleType selectedType = SelectObstacleTypeByWeight();
            if (selectedType == null)
            {
                SpawnRandomObstacle(z);
                return;
            }

            // 허용된 레인에서 선택
            int laneIndex = selectedType.allowedLanes[Random.Range(0, selectedType.allowedLanes.Length)];
            float half = (laneCount - 1) * 0.5f;
            float laneX = (laneIndex - half) * laneOffset;

            GameObject obj;
            if (selectedType.prefab != null)
            {
                obj = Instantiate(selectedType.prefab, transform);
            }
            else if (selectedType.useProcedural)
            {
                obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obj.transform.localScale = selectedType.size;
                
                var renderer = obj.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial.color = selectedType.color;
                }
            }
            else
            {
                SpawnRandomObstacle(z);
                return;
            }

            obj.name = $"{selectedType.name}_{z:F1}";
            obj.transform.SetParent(transform, false);
            obj.transform.position = new Vector3(laneX, selectedType.size.y * 0.5f, z);

            if (obj.GetComponent<Obstacle>() == null)
            {
                var obs = obj.AddComponent<Obstacle>();
                obs.size = selectedType.size;
            }

            _spawned.Add(obj);
        }

        private void SpawnPatternObstacle(float z)
        {
            if (obstaclePatterns == null || obstaclePatterns.Length == 0)
            {
                SpawnTypedObstacle(z);
                return;
            }

            // 난이도에 맞는 패턴 필터링
            var validPatterns = new List<ObstaclePattern>();
            foreach (var pattern in obstaclePatterns)
            {
                if (_virtualPlayerDistance >= pattern.minDifficulty && _virtualPlayerDistance <= pattern.maxDifficulty)
                {
                    validPatterns.Add(pattern);
                }
            }

            if (validPatterns.Count == 0)
            {
                SpawnTypedObstacle(z);
                return;
            }

            // 가중치 기반 패턴 선택
            ObstaclePattern selectedPattern = SelectPatternByWeight(validPatterns);
            SpawnPattern(selectedPattern, z);
        }

        private void SpawnPattern(ObstaclePattern pattern, float startZ)
        {
            foreach (var obstacleData in pattern.obstacles)
            {
                float obstacleZ = startZ + obstacleData.relativeZ;
                
                if (obstacleTypes != null && obstacleData.obstacleTypeIndex < obstacleTypes.Length)
                {
                    SpawnSpecificObstacle(obstacleTypes[obstacleData.obstacleTypeIndex], obstacleData.laneIndex, obstacleZ);
                }
                else
                {
                    // 타입이 없으면 기본 장애물
                    SpawnDefaultObstacle(obstacleData.laneIndex, obstacleZ);
                }
            }

            // 패턴 전체 길이만큼 다음 스폰 위치 이동
            _nextSpawnZ = startZ + pattern.patternLength;
        }

        private void SpawnSpecificObstacle(ObstacleType type, int laneIndex, float z)
        {
            float half = (laneCount - 1) * 0.5f;
            float laneX = (laneIndex - half) * laneOffset;

            GameObject obj;
            if (type.prefab != null)
            {
                obj = Instantiate(type.prefab, transform);
            }
            else
            {
                obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obj.transform.localScale = type.size;
                
                var renderer = obj.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial.color = type.color;
                }
            }

            obj.name = $"Pattern_{type.name}_{z:F1}";
            obj.transform.SetParent(transform, false);
            obj.transform.position = new Vector3(laneX, type.size.y * 0.5f, z);

            if (obj.GetComponent<Obstacle>() == null)
            {
                var obs = obj.AddComponent<Obstacle>();
                obs.size = type.size;
            }

            _spawned.Add(obj);
        }

        private void SpawnDefaultObstacle(int laneIndex, float z)
        {
            float half = (laneCount - 1) * 0.5f;
            float laneX = (laneIndex - half) * laneOffset;

            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = $"DefaultObstacle_{z:F1}";
            obj.transform.SetParent(transform, false);
            obj.transform.localScale = Vector3.one;
            obj.transform.position = new Vector3(laneX, 0.5f, z);

            var obs = obj.AddComponent<Obstacle>();
            obs.size = Vector3.one;

            _spawned.Add(obj);
        }

        private ObstacleType SelectObstacleTypeByWeight()
        {
            float totalWeight = 0f;
            foreach (var type in obstacleTypes)
            {
                totalWeight += type.spawnWeight;
            }

            float randomValue = Random.Range(0f, totalWeight);
            float currentWeight = 0f;

            foreach (var type in obstacleTypes)
            {
                currentWeight += type.spawnWeight;
                if (randomValue <= currentWeight)
                {
                    return type;
                }
            }

            return obstacleTypes[0];
        }

        private ObstaclePattern SelectPatternByWeight(List<ObstaclePattern> patterns)
        {
            float totalWeight = 0f;
            foreach (var pattern in patterns)
            {
                totalWeight += pattern.spawnWeight;
            }

            float randomValue = Random.Range(0f, totalWeight);
            float currentWeight = 0f;

            foreach (var pattern in patterns)
            {
                currentWeight += pattern.spawnWeight;
                if (randomValue <= currentWeight)
                {
                    return pattern;
                }
            }

            return patterns[0];
        }
    }
}
