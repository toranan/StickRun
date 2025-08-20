using UnityEngine;

namespace BananaRun.Runner
{
    [RequireComponent(typeof(CharacterController))]
    public class RunnerPlayer : MonoBehaviour
    {
        [Header("Movement Settings")]
        public float forwardSpeed = 12f; // z+ 방향으로 계속 전진
        public float laneOffset = 2f;   // 레인 간격
        public int laneCount = 3;       // 3레인 (0,1,2)
        public float laneChangeSpeed = 18f; // 레인 이동을 더 빠르고 명확하게

        [Header("Jump/Slide Settings")]
        public float jumpHeight = 2f;
        public float gravity = -24f;
        public float slideDuration = 0.7f;
        public float slideHeightScale = 0.5f; // 캡슐 높이 축소 비율
        [Tooltip("공중에서 추가 가능한 점프 횟수 (1이면 더블 점프)")]
        public int maxAirJumps = 1;
        [Tooltip("공중 점프의 높이 배율 (1 = 지상 점프와 동일)")]
        public float airJumpHeightMultiplier = 1f;

        [Header("Glide Settings")]
        [Tooltip("글라이딩 시 중력 감소 배율 (0~1, 0에 가까울수록 천천히 떨어짐)")]
        [Range(0f, 1f)] public float glideGravityMultiplier = 0.2f;

        [Header("References")]
        public SwipeInput swipeInput;
        
        [Header("Debug")]
        [Tooltip("충돌 범위를 시각적으로 표시")]
        public bool showCollisionDebug = true;

        private CharacterController _controller;
        private int _currentLaneIndex = 1; // 가운데에서 시작
        private float _verticalVelocity;
        public bool _isSliding; // Changed to public for RunnerAnimatorBridge
        private float _slideEndTime;
        public bool isDead;
        
        public bool IsGliding { get; private set; } = false; // Changed to public property
        private float _normalGravity;

        // 디버그용 프로퍼티
        public int CurrentLaneIndex => _currentLaneIndex;
        // public bool IsSliding => _isSliding; // No longer needed as _isSliding is public

        private float _originalHeight;
        private Vector3 _originalCenter;
        private int _remainingAirJumps;

        private Animator _animator; // 애니메이터 참조 추가

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            if (swipeInput == null)
            {
                swipeInput = GetComponentInChildren<SwipeInput>();
            }
            _animator = GetComponent<Animator>(); // 애니메이터 컴포넌트 가져오기

            _originalHeight = _controller.height;
            _originalCenter = _controller.center;

            if (forwardSpeed <= 0.01f)
            {
                forwardSpeed = 8f; // 전진 속도 기본값 보장
            }
            isDead = false;
            _remainingAirJumps = maxAirJumps;
            _normalGravity = gravity; // 초기 중력 값 저장
            
            Debug.Log($"무한 러너 시작! 현재 레인: {_currentLaneIndex + 1} (1-3레인 중), 속도: {forwardSpeed}");
        }

        private void OnValidate()
        {
            laneCount = Mathf.Max(1, laneCount);
            forwardSpeed = Mathf.Max(0f, forwardSpeed);
            if (slideHeightScale <= 0f) slideHeightScale = 0.5f;
        }

        private void OnEnable()
        {
            if (swipeInput != null)
            {
                swipeInput.OnSwipeLeft += HandleSwipeLeft;
                swipeInput.OnSwipeRight += HandleSwipeRight;
                swipeInput.OnSwipeUp += HandleSwipeUp;
                swipeInput.OnSwipeDown += HandleSwipeDown;
                swipeInput.OnHoldStart += HandleHoldStart;
                swipeInput.OnHoldEnd += HandleHoldEnd;
            }
        }

        private void OnDisable()
        {
            if (swipeInput != null)
            {
                swipeInput.OnSwipeLeft -= HandleSwipeLeft;
                swipeInput.OnSwipeRight -= HandleSwipeRight;
                swipeInput.OnSwipeUp -= HandleSwipeUp;
                swipeInput.OnSwipeDown -= HandleSwipeDown;
                swipeInput.OnHoldStart -= HandleHoldStart;
                swipeInput.OnHoldEnd -= HandleHoldEnd;
            }
        }

        private void Update()
        {
            // 게임이 플레이 중이 아니면 아무것도 하지 않음
            if (GameManager.Instance != null && GameManager.Instance.currentGameState != GameManager.GameState.Playing)
            {
                return;
            }

            if (!isDead && forwardSpeed <= 0.01f)
            {
                forwardSpeed = 8f; // 씬 세팅 누락 시 자동 복구
            }
            // 목표 레인 X 위치 계산 → deltaX를 Move로 적용
            float half = (laneCount - 1) * 0.5f;
            float targetX = (_currentLaneIndex - half) * laneOffset;
            float nextX = Mathf.MoveTowards(transform.position.x, targetX, laneChangeSpeed * Time.deltaTime);
            float deltaX = isDead ? 0f : (nextX - transform.position.x);

            // 중력/점프
            bool grounded = _controller.isGrounded;
            if (grounded)
            {
                if (_verticalVelocity < 0f) _verticalVelocity = -2f; // 바닥에 붙이기
                if (IsGliding) EndGlide(); // 땅에 닿으면 글라이딩 종료
                _remainingAirJumps = maxAirJumps;
            }
            
            // 글라이딩 중이면 중력 감소
            float currentGravity = IsGliding ? _normalGravity * glideGravityMultiplier : _normalGravity;
            _verticalVelocity += currentGravity * Time.deltaTime;

            // 최종 이동: 좌우 + 중력만 (Z는 제자리, 월드가 뒤로 이동)
            Vector3 displacement = new Vector3(
                deltaX,
                _verticalVelocity * Time.deltaTime,
                0f // 플레이어는 Z축 제자리
            );
            _controller.Move(displacement);

            // 정확한 충돌 판정 (매 프레임 체크)
            if (!isDead)
            {
                CheckPreciseCollision();
            }

            // 슬라이드 종료 복구
            if (_isSliding && Time.time >= _slideEndTime)
            {
                EndSlide();
            }
        }

        private void CheckPreciseCollision()
        {
            // 플레이어 캡슐의 정확한 위치와 크기 계산
            float radius = _controller.radius * 0.9f; // 약간 작게 해서 여유 공간 확보
            float height = _controller.height;
            Vector3 centerWorld = transform.TransformPoint(_controller.center);

            Vector3 up = Vector3.up;
            Vector3 bottom = centerWorld - up * (height * 0.5f - radius);
            Vector3 top = centerWorld + up * (height * 0.5f - radius);

            // 현재 플레이어 캡슐과 겹치는 장애물 찾기
            Collider[] overlapping = Physics.OverlapCapsule(bottom, top, radius, ~0, QueryTriggerInteraction.Collide);
            
            foreach (var collider in overlapping)
            {
                var obstacle = collider.GetComponent<Obstacle>();
                if (obstacle != null)
                {
                    // 실제로 중심부에서 충돌했는지 추가 검증
                    if (IsRealCollision(collider, centerWorld, radius))
                    {
                        HandleObstacleCollision(obstacle, collider);
                        return; // 한 번에 하나씩만 처리
                    }
                }
            }
        }

        private bool IsRealCollision(Collider obstacleCollider, Vector3 playerCenter, float playerRadius)
        {
            // 장애물의 가장 가까운 점까지의 거리 계산
            Vector3 closestPoint = obstacleCollider.ClosestPoint(playerCenter);
            float distance = Vector3.Distance(playerCenter, closestPoint);
            
            // 플레이어 반지름보다 가까우면 실제 충돌
            bool isColliding = distance < playerRadius;
            
            if (isColliding)
            {
                Debug.Log($"🔍 정밀 충돌 감지: {obstacleCollider.name}, 거리: {distance:F2}m (반지름: {playerRadius:F2}m)");
            }
            
            return isColliding;
        }

        private void HandleObstacleCollision(Obstacle obstacle, Collider collider)
        {
            // 슬라이딩 중이고 장애물이 충분히 낮으면 피할 수 있음
            if (_isSliding && CanSlideUnderObstacle(obstacle))
            {
                Debug.Log($"🏃‍♂️ 슬라이딩으로 {collider.name} 통과!");
                return;
            }
            
            // 충돌로 사망
            isDead = true;
            string deathReason = _isSliding ? "슬라이딩 중 높은 장애물과 충돌" : "장애물과 충돌";
            Debug.Log($"💀 {deathReason}: {collider.name}. Game Over.");
        }

        private bool CanSlideUnderObstacle(Obstacle obstacle)
        {
            float obstacleHeight = obstacle.size.y;
            float slideHeight = _originalHeight * slideHeightScale;
            
            float clearanceNeeded = slideHeight + 0.2f;
            bool canSlide = obstacleHeight <= clearanceNeeded || obstacleHeight <= 1.3f;
            
            Debug.Log($"🔍 슬라이딩 판정: 장애물 높이 {obstacleHeight:F1}m, 필요 높이 {clearanceNeeded:F1}m → {(canSlide ? "통과 가능" : "충돌")}");
            return canSlide;
        }

        private void HandleSwipeLeft()
        {
            if (GameManager.Instance != null && GameManager.Instance.currentGameState != GameManager.GameState.Playing) return;
            if (isDead) return;
            if (IsGliding) EndGlide(); // 글라이딩 중 좌우 이동 시 글라이딩 종료
            
            int previousLane = _currentLaneIndex;
            _currentLaneIndex = Mathf.Max(0, _currentLaneIndex - 1);
            
            if (_currentLaneIndex != previousLane)
            {
                Debug.Log($"◀ 왼쪽 이동: 레인 {previousLane + 1} → 레인 {_currentLaneIndex + 1} (한 레인씩 이동)");
            }
            else
            {
                Debug.Log($"◀ 이미 가장 왼쪽 레인(레인 1)입니다!");
            }
        }

        private void HandleSwipeRight()
        {
            if (GameManager.Instance != null && GameManager.Instance.currentGameState != GameManager.GameState.Playing) return;
            if (isDead) return;
            if (IsGliding) EndGlide(); // 글라이딩 중 좌우 이동 시 글라이딩 종료
            
            int previousLane = _currentLaneIndex;
            _currentLaneIndex = Mathf.Min(laneCount - 1, _currentLaneIndex + 1);
            
            if (_currentLaneIndex != previousLane)
            {
                Debug.Log($"▶ 오른쪽 이동: 레인 {previousLane + 1} → 레인 {_currentLaneIndex + 1} (한 레인씩 이동)");
            }
            else
            {
                Debug.Log($"▶ 이미 가장 오른쪽 레인(레인 {laneCount})입니다!");
            }
        }

        private void HandleSwipeUp()
        {
            if (GameManager.Instance != null && GameManager.Instance.currentGameState != GameManager.GameState.Playing) return;
            if (isDead) return;
            if (IsGliding) EndGlide(); // 점프 시 글라이딩 종료
            
            if (_controller.isGrounded)
            {
                _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                if (_isSliding) EndSlide();
                Debug.Log("점프!");
            }
            else if (_remainingAirJumps > 0)
            {
                _verticalVelocity = Mathf.Sqrt(jumpHeight * airJumpHeightMultiplier * -2f * gravity);
                _remainingAirJumps--;
                if (_isSliding) EndSlide();
                Debug.Log("더블 점프!");
            }
        }

        private void HandleSwipeDown()
        {
            if (GameManager.Instance != null && GameManager.Instance.currentGameState != GameManager.GameState.Playing) return;
            if (isDead) return;
            if (IsGliding) EndGlide(); // 슬라이드 시 글라이딩 종료
            
            if (!_isSliding)
            {
                StartSlide();
                Debug.Log("롤!");
            }
        }

        private void HandleHoldStart()
        {
            if (GameManager.Instance != null && GameManager.Instance.currentGameState != GameManager.GameState.Playing) return;
            if (isDead) return;
            // 공중에 있고, 슬라이딩 중이 아니며, 이미 글라이딩 중이 아닐 때만 글라이딩 시작
            if (!_controller.isGrounded && !_isSliding && !IsGliding)
            {
                StartGlide();
            }
        }

        private void HandleHoldEnd()
        {
            if (GameManager.Instance != null && GameManager.Instance.currentGameState != GameManager.GameState.Playing) return;
            EndGlide();
        }

        private void StartGlide()
        {
            IsGliding = true;
            // Removed: if (_animator != null) _animator.SetBool("IsGliding", true);
            Debug.Log("🚀 글라이딩 시작!");
        }

        private void EndGlide()
        {
            IsGliding = false;
            // Removed: if (_animator != null) _animator.SetBool("IsGliding", false);
            Debug.Log("🚀 글라이딩 종료!");
        }

        private void StartSlide()
        {
            _isSliding = true;
            _slideEndTime = Time.time + slideDuration;

            _controller.height = _originalHeight * slideHeightScale;
            _controller.center = new Vector3(_originalCenter.x, _controller.height / 2f, _originalCenter.z);
        }

        private void EndSlide()
        {
            _isSliding = false;
            _controller.height = _originalHeight;
            _controller.center = _originalCenter;
        }

        private void OnDrawGizmos()
        {
            if (!showCollisionDebug || _controller == null) return;

            float radius = _controller.radius * 0.9f;
            float height = _controller.height;
            Vector3 centerWorld = transform.TransformPoint(_controller.center);

            Vector3 up = Vector3.up;
            Vector3 bottom = centerWorld - up * (height * 0.5f - radius);
            Vector3 top = centerWorld + up * (height * 0.5f - radius);

            Gizmos.color = isDead ? Color.red : (_isSliding ? Color.green : Color.yellow);
            
            Gizmos.DrawWireSphere(bottom, radius);
            Gizmos.DrawWireSphere(top, radius);
            
            Vector3[] directions = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
            foreach (var dir in directions)
            {
                Vector3 offset = dir * radius;
                Gizmos.DrawLine(bottom + offset, top + offset);
            }

            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(centerWorld, Vector3.one * 0.1f);
        }
    }
}
