using System.Collections.Generic;
using UnityEngine;

namespace BananaRun.Runner
{
    public class TrackSpawner : MonoBehaviour
    {
        [Header("References")]
        public Transform player;                 // 플레이어(속도만 읽음)

        [Header("Segment Settings")]
        public float segmentLength = 5f;         // 하나의 바닥 Z 길이
        public float segmentWidth  = 8f;         // X 폭
        public float segmentHeight = 1f;         // Y 높이
        public Color segmentColor  = new Color(0.2f, 0.25f, 0.3f);

        [Header("Runtime Settings")]
        public float spawnAheadDistance = 100f;  // 플레이어 앞(카메라 방향)으로 얼마나 미리 생성할지

        [Header("Gap Settings")]
        public bool enableGaps = false;          // 틈 생성 on/off
        [Range(0f, 1f)]
        public float gapChance = 0.12f;          // 틈 발생 확률
        public Vector2 gapLengthRange = new Vector2(0.8f, 1.6f); // 틈 길이(월드 Z 기준)
        public int minSegmentsBetweenGaps = 2;   // 틈 사이 최소 세그먼트 수
        public float gapSafeZoneDistance = 40f;  // 플레이어 근처 안전 구간(이 안에서는 틈 금지)

        // 내부 상태
        private readonly List<GameObject> _segments = new List<GameObject>();
        private float _nextSpawnZ;               // 다음 스폰 위치의 "앞쪽 경계" (로컬 Z, 그리드 단위)
        private int _segmentsSinceLastGap = 0;
        private bool _inGap = false;
        private float _currentGapEnd = 0f;       // 현재 진행 중인 gap의 끝 지점(로컬 Z)

        private void Start()
        {
            if (player == null)
            {
                var playerObj = GameObject.FindWithTag("Player");
                if (playerObj != null) player = playerObj.transform;
            }

            // 시작 시, 카메라 뒤쪽까지 충분히 깔아두기
            _nextSpawnZ = -segmentLength * 10f;  // 로컬 Z 기준. 음수로 멀리 뒤부터 깔기.
            MaintainSegments();
        }

        private void Update()
        {
            MaintainSegments();
        }

        private void LateUpdate()
        {
            MoveWorldBackward();
        }

        /// <summary>
        /// 부모(스포너)만 뒤로 이동시켜 월드가 플레이어 쪽으로 다가오게 함.
        /// </summary>
        private void MoveWorldBackward()
        {
            if (player == null) return;

            var runner = player.GetComponent<RunnerPlayer>();
            if (runner == null || runner.isDead) return;

            float moveSpeed = runner.forwardSpeed;
            transform.position += Vector3.back * moveSpeed * Time.deltaTime; // 부모만 이동
        }

        /// <summary>
        /// 필요한 만큼 세그먼트를 유지/생성
        /// </summary>
        private void MaintainSegments()
        {
            if (player == null) return;

            // 부모가 뒤로 이동하므로, "이동한 거리"는 = -transform.position.z
            float traveled = -transform.position.z;
            float targetZ  = traveled + spawnAheadDistance; // 여기까지는 바닥이 존재해야 함

            int guard = 0;
            while (_nextSpawnZ < targetZ)
            {
                // Gap 판단 (로컬 Z 기준)
                if (enableGaps && ShouldCreateGap(traveled))
                {
                    CreateGap();
                }
                else if (_inGap && _nextSpawnZ >= _currentGapEnd)
                {
                    EndGap();
                }

                if (!_inGap)
                {
                    // _nextSpawnZ는 "앞쪽 경계"이므로 중앙은 -segmentLength*0.5 만큼 뒤
                    SpawnSegment(_nextSpawnZ - segmentLength * 0.5f);
                    _segmentsSinceLastGap++;
                }

                _nextSpawnZ += segmentLength;
                guard++;
                if (guard > 50) break; // 안전 장치
            }
        }

        /// <summary>
        /// 세그먼트 하나를 로컬 좌표 그리드에 정확히 배치
        /// </summary>
        private void SpawnSegment(float centerZLocal)
        {
            GameObject seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seg.name = $"Segment_{centerZLocal:F1}";
            seg.transform.SetParent(transform, false); // 반드시 false (로컬 유지)
            seg.transform.localPosition = new Vector3(0f, -segmentHeight * 0.5f, centerZLocal);

            // 살짝 겹치게 해서 미세한 틈 방지(이제는 부모만 이동하므로 사실상 필요 적음)
            seg.transform.localScale = new Vector3(segmentWidth, segmentHeight, segmentLength + 0.1f);

            var renderer = seg.GetComponent<Renderer>();
            if (renderer != null)
            {
                // 머티리얼 인스턴스 생성 방지: sharedMaterial 사용
                renderer.sharedMaterial.color = segmentColor;
            }

            _segments.Add(seg);

            // 3레인 가이드 (옵션) — 콜라이더 제거
            for (int i = -1; i <= 1; i++)
            {
                GameObject lane = GameObject.CreatePrimitive(PrimitiveType.Cube);
                lane.name = $"LaneGuide_{centerZLocal:F1}_{i + 1}";
                lane.transform.SetParent(seg.transform, false);
                lane.transform.localScale = new Vector3(0.05f, 1.02f, 1.001f);
                lane.transform.localPosition = new Vector3(i * 2f, 0.005f, 0f);

                var r = lane.GetComponent<Renderer>();
                if (r != null) r.sharedMaterial.color = new Color(0.35f, 0.4f, 0.5f);

                var c = lane.GetComponent<Collider>();
                if (c != null) Destroy(c);
            }
        }

        /// <summary>
        /// Safe zone/최소 간격/중복 gap 상태 등을 고려해 틈 생성 여부 결정
        /// </summary>
        private bool ShouldCreateGap(float traveled)
        {
            // 플레이어 근처 안전 구간: 현재 생성 예정 z가 안전거리 이내면 금지
            if ((traveled + gapSafeZoneDistance) > _nextSpawnZ)
                return false;

            // gap 사이 최소 세그먼트 규칙
            if (_segmentsSinceLastGap < minSegmentsBetweenGaps)
                return false;

            if (_inGap)
                return false;

            return Random.value < gapChance;
        }

        private void CreateGap()
        {
            _inGap = true;

            // gapLength는 연속적인 '월드 Z' 길이. 로컬에서도 동일하게 사용.
            float gapLength = Random.Range(gapLengthRange.x, gapLengthRange.y);
            _currentGapEnd = _nextSpawnZ + gapLength;
            _segmentsSinceLastGap = 0;

            Debug.Log($"[Track] Gap start: {_nextSpawnZ:F2}  end: {_currentGapEnd:F2}  len: {gapLength:F2}");
        }

        private void EndGap()
        {
            _inGap = false;
            Debug.Log($"[Track] Gap end at Z={_nextSpawnZ:F2}");
        }
    }
}
