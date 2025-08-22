using System.Collections.Generic;
using UnityEngine;

namespace BananaRun.Runner
{
    public class TrackSpawner : MonoBehaviour
    {
        // ── Materials ──────────────────────────────────────────────────────────────
        [Header("Materials")]
        [Tooltip("바닥에 적용할 머티리얼. 예: Assets/Scenes/main/Ground.mat (URP/Lit 권장)")]
        [SerializeField] private Material groundMat;     // ← 여기에 Ground.mat 드롭
        [Tooltip("레일 가이드용 머티리얼(선택). 기본적으로 URP/Unlit 생성해 사용함.")]
        [SerializeField] private Material laneMat;

        // URP/내장 호환을 위해 색상 프로퍼티 ID 캐시
        static readonly int BaseColorID = Shader.PropertyToID("_BaseColor"); // URP
        static readonly int ColorID     = Shader.PropertyToID("_Color");     // Built-in fallback

        // ── References & Settings ─────────────────────────────────────────────────
        [Header("References")]
        public Transform player;

        [Header("Segment Settings")]
        public float segmentLength = 5f;
        public float segmentWidth  = 8f;
        public float segmentHeight = 1f;
        public Color segmentColor  = new Color(0.2f, 0.25f, 0.3f);

        [Header("Runtime Settings")]
        public float spawnAheadDistance = 100f;

        [Header("Gap Settings")]
        public bool enableGaps = false;
        [Range(0f, 1f)] public float gapChance = 0.12f;
        public Vector2 gapLengthRange = new Vector2(0.8f, 1.6f);
        public int minSegmentsBetweenGaps = 2;
        public float gapSafeZoneDistance = 40f;

        // ── Internals ─────────────────────────────────────────────────────────────
        private readonly List<GameObject> _segments = new List<GameObject>();
        private float _nextSpawnZ;
        private int _segmentsSinceLastGap = 0;
        private bool _inGap = false;
        private float _currentGapEnd = 0f;

        private MaterialPropertyBlock _trackPropBlock; // fallback 트랙 색
        private MaterialPropertyBlock _lanePropBlock;  // 레일 색

        private Material _fallbackURPLit;  // groundMat이 없을 때 사용할 기본 URP Lit
        private bool _usingFallbackTrackMat;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            // MPB 준비
            _trackPropBlock = new MaterialPropertyBlock();
            _lanePropBlock  = new MaterialPropertyBlock();

            // 레일 색(연한 회색-푸른톤)
            SetColorOnMPB(_lanePropBlock, new Color(0.35f, 0.4f, 0.5f));

            // 레일/트랙 머티리얼 준비
            EnsureMaterials();
        }

        private void Start()
        {
            if (player == null)
            {
                var playerObj = GameObject.FindWithTag("Player");
                if (playerObj != null) player = playerObj.transform;
            }

            _nextSpawnZ = -segmentLength * 10f; // 시작 시 뒤쪽으로 넉넉히 깔기
            MaintainSegments();
        }

        private void Update()      => MaintainSegments();
        private void LateUpdate()  => MoveWorldBackward();

        // ── Helpers ───────────────────────────────────────────────────────────────
        private void EnsureMaterials()
        {
            // Ground(바닥) 머티리얼: 제공되지 않았다면 URP/Lit을 런타임 생성해 사용
            if (groundMat == null)
            {
                var sh = Shader.Find("Universal Render Pipeline/Lit");
                if (sh == null) sh = Shader.Find("Universal Render Pipeline/Simple Lit");
                _fallbackURPLit = new Material(sh) { enableInstancing = true };
                _fallbackURPLit.SetFloat("_Smoothness", 0f);
                _usingFallbackTrackMat = true;

                // 기본 색
                _trackPropBlock.SetColor(BaseColorID, segmentColor);
                _trackPropBlock.SetColor(ColorID,     segmentColor); // Built-in 대비
            }
            else
            {
                _usingFallbackTrackMat = false; // Ground.mat을 그대로 사용
            }

            // 레일 머티리얼: 없으면 Unlit 생성
            if (laneMat == null)
            {
                var sh = Shader.Find("Universal Render Pipeline/Unlit");
                if (sh == null) sh = Shader.Find("Universal Render Pipeline/Lit");
                laneMat = new Material(sh) { enableInstancing = true };
            }
        }

        // URP/내장 둘 다 커버하기 위한 컬러 세팅
        private static void SetColorOnMPB(MaterialPropertyBlock mpb, Color c)
        {
            mpb.SetColor(BaseColorID, c); // URP
            mpb.SetColor(ColorID,     c); // Built-in
        }

        private void MoveWorldBackward()
        {
            if (player == null) return;

            var runner = player.GetComponent<RunnerPlayer>();
            if (runner == null || runner.isDead) return;

            transform.position += Vector3.back * runner.CurrentSpeed * Time.deltaTime;
        }

        private void MaintainSegments()
        {
            if (player == null) return;

            float traveled = -transform.position.z;
            float targetZ  = traveled + spawnAheadDistance;

            int guard = 0;
            while (_nextSpawnZ < targetZ)
            {
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
                    SpawnSegment(_nextSpawnZ - segmentLength * 0.5f);
                    _segmentsSinceLastGap++;
                }

                _nextSpawnZ += segmentLength;
                if (++guard > 50) break;
            }
        }

        private void SpawnSegment(float centerZLocal)
        {
            // 바닥 세그먼트
            GameObject seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seg.name = $"Segment_{centerZLocal:F1}";
            seg.transform.SetParent(transform, false);
            seg.transform.localPosition = new Vector3(0f, -segmentHeight * 0.5f, centerZLocal);
            seg.transform.localScale    = new Vector3(segmentWidth, segmentHeight, segmentLength + 0.1f);

            var renderer = seg.GetComponent<Renderer>();
            if (renderer != null)
            {
                // Ground.mat이 있으면 그대로, 없으면 fallback URP/Lit
                renderer.sharedMaterial = groundMat != null ? groundMat : _fallbackURPLit;

                // fallback을 쓰는 경우에만 색 MPB 적용(지정 머티리얼은 건드리지 않음)
                if (_usingFallbackTrackMat)
                    renderer.SetPropertyBlock(_trackPropBlock);
                else
                    renderer.SetPropertyBlock(null); // 남은 MPB 제거
            }

            _segments.Add(seg);

            // 레일 가이드(시각적 보조)
            for (int i = -1; i <= 1; i++)
            {
                GameObject lane = GameObject.CreatePrimitive(PrimitiveType.Cube);
                lane.name = $"LaneGuide_{centerZLocal:F1}_{i + 1}";
                lane.transform.SetParent(seg.transform, false);
                lane.transform.localScale    = new Vector3(0.05f, 1.02f, 1.001f);
                lane.transform.localPosition = new Vector3(i * 2f, 0.005f, 0f);

                var r = lane.GetComponent<Renderer>();
                if (r != null)
                {
                    r.sharedMaterial = laneMat;
                    r.SetPropertyBlock(_lanePropBlock);
                }

                var c = lane.GetComponent<Collider>();
                if (c != null) Destroy(c); // 충돌 불필요
            }
        }

        private bool ShouldCreateGap(float traveled)
        {
            if ((traveled + gapSafeZoneDistance) > _nextSpawnZ) return false;
            if (_segmentsSinceLastGap < minSegmentsBetweenGaps)  return false;
            if (_inGap) return false;
            return Random.value < gapChance;
        }

        private void CreateGap()
        {
            _inGap = true;
            float gapLength = Random.Range(gapLengthRange.x, gapLengthRange.y);
            _currentGapEnd = _nextSpawnZ + gapLength;
            _segmentsSinceLastGap = 0;
        }

        private void EndGap()
        {
            _inGap = false;
        }
    }
}
