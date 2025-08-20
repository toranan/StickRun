using System.Collections.Generic;
using UnityEngine;

namespace BananaRun.Runner
{
    public class TrackSpawner : MonoBehaviour
    {
        [Header("References")]
        public Transform player;

        [Header("Segment Settings")]
        public float segmentLength = 5f;
        public float segmentWidth = 8f;
        public float segmentHeight = 1f;
        public Color segmentColor = new Color(0.2f, 0.25f, 0.3f);

        [Header("Runtime Settings")]
        public float spawnAheadDistance = 1000f; // 더 멀리까지 생성
        
        [Header("Gap Settings")]
        public bool enableGaps = false;
        public float gapChance = 0.12f;
        public Vector2 gapLengthRange = new Vector2(0.8f, 1.6f);
        public int minSegmentsBetweenGaps = 2;
        public float gapSafeZoneDistance = 40f;

        private readonly List<GameObject> _segments = new List<GameObject>();
        private float _nextSpawnZ;
        private float _virtualPlayerDistance = 0f;
        private int _segmentsSinceLastGap = 0;
        private bool _inGap = false;
        private float _currentGapEnd = 0f;

        private void Start()
        {
            if (player == null)
            {
                var playerObj = GameObject.FindWithTag("Player");
                if (playerObj != null) player = playerObj.transform;
            }

            _nextSpawnZ = -segmentLength * 10;
            MaintainSegments();
        }

        private void Update()
        {
            UpdateVirtualDistance();
            MaintainSegments();
        }

        private void LateUpdate()
        {
            MoveSegmentsTowardsPlayer();
        }

        private void UpdateVirtualDistance()
        {
            var runner = player.GetComponent<RunnerPlayer>();
            if (runner != null && !runner.isDead)
            {
                _virtualPlayerDistance += runner.forwardSpeed * Time.deltaTime;
            }
        }

        private void MoveSegmentsTowardsPlayer()
        {
            if (player == null) return;

            var runner = player.GetComponent<RunnerPlayer>();
            if (runner == null || runner.isDead) return;

            float moveSpeed = runner.forwardSpeed;
            Vector3 movement = Vector3.back * moveSpeed * Time.deltaTime;

            foreach (var segment in _segments)
            {
                if (segment != null)
                {
                    segment.transform.position += movement;
                }
            }
        }

        private void MaintainSegments()
        {
            if (player == null) return;

            float targetZ = _virtualPlayerDistance + spawnAheadDistance;
            int spawnCount = 0;
            while (_nextSpawnZ < targetZ)
            {
                // Gap 로직 처리
                if (enableGaps && ShouldCreateGap())
                {
                    CreateGap();
                }
                else if (_inGap && _nextSpawnZ >= _currentGapEnd)
                {
                    EndGap();
                }

                if (!_inGap)
                {
                    SpawnSegment(_nextSpawnZ - segmentLength * 0.5f);
                    _segmentsSinceLastGap++;
                }

                _nextSpawnZ += segmentLength;
                spawnCount++;
                
                if (spawnCount > 20) 
                {
                    break;
                }
            }
        }

        private void SpawnSegment(float centerZ)
        {
            GameObject seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seg.name = $"Segment_{centerZ:F1}";
            seg.transform.SetParent(transform, false);
            seg.transform.position = new Vector3(0f, -segmentHeight * 0.5f, centerZ);
            // 세그먼트를 약간 겹치게 하여 틈새 방지
            seg.transform.localScale = new Vector3(segmentWidth, segmentHeight, segmentLength + 0.1f);

            var renderer = seg.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial.color = segmentColor;
            }

            _segments.Add(seg);

            // 3 레인 가이드 라인(옵션)
            for (int i = -1; i <= 1; i++)
            {
                GameObject lane = GameObject.CreatePrimitive(PrimitiveType.Cube);
                lane.name = $"LaneGuide_{centerZ:F1}_{i + 1}";
                lane.transform.SetParent(seg.transform, false);
                lane.transform.localScale = new Vector3(0.05f, 1.02f, 1.001f);
                lane.transform.localPosition = new Vector3(i * 2f, 0.005f, 0f);
                var r = lane.GetComponent<Renderer>();
                if (r != null) r.sharedMaterial.color = new Color(0.35f, 0.4f, 0.5f);
                var c = lane.GetComponent<Collider>();
                if (c != null)
                {
                    Object.Destroy(c);
                }
            }
        }

        private bool ShouldCreateGap()
        {
            // 플레이어가 안전 거리 내에 있으면 gap 생성하지 않음
            if (_virtualPlayerDistance + gapSafeZoneDistance > _nextSpawnZ)
            {
                return false;
            }

            // 최소 세그먼트 수를 충족하지 않으면 gap 생성하지 않음
            if (_segmentsSinceLastGap < minSegmentsBetweenGaps)
            {
                return false;
            }

            // 이미 gap 중이면 생성하지 않음
            if (_inGap)
            {
                return false;
            }

            // 확률적으로 gap 생성 결정
            return Random.value < gapChance;
        }

        private void CreateGap()
        {
            _inGap = true;
            float gapLength = Random.Range(gapLengthRange.x, gapLengthRange.y);
            _currentGapEnd = _nextSpawnZ + gapLength;
            _segmentsSinceLastGap = 0;
            
            Debug.Log($"Gap 생성: {_nextSpawnZ:F1} ~ {_currentGapEnd:F1} (길이: {gapLength:F1})");
        }

        private void EndGap()
        {
            _inGap = false;
            Debug.Log($"Gap 종료: {_nextSpawnZ:F1}");
        }
    }
}
