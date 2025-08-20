using System.Collections.Generic;
using UnityEngine;

namespace BananaRun.Runner
{
    public class TrackSpawner : MonoBehaviour
    {
        [Header("References")]
        public Transform player;

        [Header("Segment Settings")]
        public float segmentLength = 5f; // 10f → 5f로 더 줄여서 점프 간격 절반으로
        public float segmentWidth = 8f;
        public float segmentHeight = 1f;
        public Color segmentColor = new Color(0.2f, 0.25f, 0.3f);

        [Header("Runtime Settings")]
        public float spawnAheadDistance = 600f; // 무한 생성을 위해 매우 멀리까지
        public float despawnBehindDistance = 50f;

        [Header("Gap Settings (바닥이 비는 구간)")]
        [Tooltip("점프로 건너야 하는 갭을 생성할지 여부")]
        public bool enableGaps = true;
        
        [Tooltip("갭 생성 확률 (0~1)")]
        [Range(0f, 1f)] public float gapChance = 0.12f; // 12% 확률로 간헐적 갭

        [Tooltip("갭 길이 범위(미터). 기존보다 절반 수준으로 짧게 유지")]
        public Vector2 gapLengthRange = new Vector2(0.6f, 1.2f);

        [Tooltip("연속 갭 방지: 최소 몇 개의 세그먼트 뒤에 갭을 허용할지")]
        public int minSegmentsBetweenGaps = 2;

        [Tooltip("플레이 시작부 안전 구간 길이(미터): 이 거리 안에서는 갭 미생성")]
        public float gapSafeZoneDistance = 40f;

        private int _segmentsSinceLastGap = 9999;

        private readonly List<GameObject> _segments = new List<GameObject>();
        private float _nextSpawnZ;
        private float _virtualPlayerDistance = 0f; // 가상 진행 거리

        private void Start()
        {
            if (player == null)
            {
                var playerObj = GameObject.FindWithTag("Player");
                if (playerObj != null) player = playerObj.transform;
            }

            // 초기 세그먼트 채우기 - 세그먼트가 짧아졌으니 더 많이 생성
            _nextSpawnZ = -segmentLength * 10; // 세그먼트 길이가 5f이므로 10배 = 50m 범위 채움
            MaintainSegments();
        }

        private void Update()
        {
            UpdateVirtualDistance();
            MaintainSegments();
            MoveSegmentsTowardsPlayer();
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

        private void MoveSegmentsTowardsPlayer()
        {
            if (player == null) return;

            var runner = player.GetComponent<RunnerPlayer>();
            if (runner == null || runner.isDead) return; // 게임오버 시 이동 정지

            // 모든 세그먼트를 플레이어 쪽으로 이동 (음의 Z 방향)
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

            // 진짜 무한 생성: 가상 진행 거리 기준으로 계속 생성
            float targetZ = _virtualPlayerDistance + spawnAheadDistance;
            int spawnCount = 0;
            while (_nextSpawnZ < targetZ)
            {
                // 짧은 갭을 삽입하여 점프 구간을 제어 (강화 규칙 적용)
                if (enableGaps
                    && _virtualPlayerDistance > gapSafeZoneDistance
                    && _segmentsSinceLastGap >= minSegmentsBetweenGaps
                    && Random.value < gapChance)
                {
                    float gapLen = Mathf.Clamp(Random.Range(gapLengthRange.x, gapLengthRange.y), 0.1f, 1.5f);
                    _nextSpawnZ += gapLen; // 바닥을 건너뛴 구간(갭)
                    _segmentsSinceLastGap = 0;
                }

                _nextSpawnZ += segmentLength;
                SpawnSegment(_nextSpawnZ - segmentLength * 0.5f);
                _segmentsSinceLastGap++;
                spawnCount++;
                
                // 무한 루프 방지 (프레임당 최대 20개)
                if (spawnCount > 20) 
                {
                    break;
                }
            }

            // 플레이어 뒤쪽 세그먼트 정리
            float minAllowedZ = -despawnBehindDistance;
            for (int i = _segments.Count - 1; i >= 0; i--)
            {
                if (_segments[i].transform.position.z + segmentLength * 0.5f < minAllowedZ)
                {
                    Destroy(_segments[i]);
                    _segments.RemoveAt(i);
                }
            }
            
            if (spawnCount > 0)
            {
                Debug.Log($"트랙 무한 생성: 가상거리 {_virtualPlayerDistance:F1}m, 세그먼트 {spawnCount}개 생성");
            }
        }

        private void SpawnSegment(float centerZ)
        {
            GameObject seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seg.name = $"Segment_{centerZ:F1}";
            seg.transform.SetParent(transform, false);
            seg.transform.position = new Vector3(0f, -segmentHeight * 0.5f, centerZ);
            seg.transform.localScale = new Vector3(segmentWidth, segmentHeight, segmentLength);

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
                // 레인 가이드는 시각적 표시만 — 이동을 막지 않도록 콜라이더 제거
                var c = lane.GetComponent<Collider>();
                if (c != null)
                {
                    Object.Destroy(c);
                }
            }
        }
    }
}


