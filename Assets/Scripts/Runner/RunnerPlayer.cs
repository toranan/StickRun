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

        [Header("References")]
        public SwipeInput swipeInput;
        
        [Header("Debug")]
        [Tooltip("충돌 범위를 시각적으로 표시")]
        public bool showCollisionDebug = true;

        private CharacterController _controller;
        private int _currentLaneIndex = 1; // 가운데에서 시작
        private float _verticalVelocity;
        private bool _isSliding;
        private float _slideEndTime;
        public bool isDead;
        
        // 디버그용 프로퍼티
        public int CurrentLaneIndex => _currentLaneIndex;
        public bool IsSliding => _isSliding;

        private float _originalHeight;
        private Vector3 _originalCenter;
        private int _remainingAirJumps;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            if (swipeInput == null)
            {
                swipeInput = GetComponentInChildren<SwipeInput>();
            }

            _originalHeight = _controller.height;
            _originalCenter = _controller.center;

            if (forwardSpeed <= 0.01f)
            {
                forwardSpeed = 8f; // 전진 속도 기본값 보장
            }
            isDead = false;
            _remainingAirJumps = maxAirJumps;
            
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
            }
        }

        private void Update()
        {
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
            if (grounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = -2f; // 바닥에 붙이기
            }
            if (grounded)
            {
                _remainingAirJumps = maxAirJumps;
            }
            _verticalVelocity += gravity * Time.deltaTime;

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

        // 기존 OnControllerColliderHit는 로깅만 (정확한 충돌 판정 비활성화)
        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            // 로깅만 수행 (실제 충돌 판정은 CheckPreciseCollision에서)
            if (hit.collider != null && hit.collider.GetComponent<Obstacle>() != null)
            {
                Debug.Log($"🟡 OnControllerColliderHit 감지: {hit.collider.name} (무시됨 - 정밀 판정 사용)");
            }
        }

        private bool CanSlideUnderObstacle(Obstacle obstacle)
        {
            // 장애물 높이 확인
            float obstacleHeight = obstacle.size.y;
            float slideHeight = _originalHeight * slideHeightScale;
            
            // 슬라이딩 높이보다 충분히 높은 장애물이면 통과 가능
            float clearanceNeeded = slideHeight + 0.2f; // 여유 공간 0.2m
            bool canSlide = obstacleHeight <= clearanceNeeded || obstacleHeight <= 1.3f; // 1.3m 이하는 무조건 통과
            
            Debug.Log($"🔍 슬라이딩 판정: 장애물 높이 {obstacleHeight:F1}m, 필요 높이 {clearanceNeeded:F1}m → {(canSlide ? "통과 가능" : "충돌")}");
            return canSlide;
        }

        private void HandleSwipeLeft()
        {
            if (isDead) return;
            
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
            if (isDead) return;
            
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
            if (isDead) return;
            
            if (_controller.isGrounded)
            {
                // v^2 = 2gh -> v = sqrt(2gh)
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
            if (isDead) return;
            
            if (!_isSliding)
            {
                StartSlide();
                Debug.Log("롤!");
            }
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

        // 충돌 범위 시각적 디버깅
        private void OnDrawGizmos()
        {
            if (!showCollisionDebug || _controller == null) return;

            float radius = _controller.radius * 0.9f; // 실제 충돌 판정에 사용하는 크기
            float height = _controller.height;
            Vector3 centerWorld = transform.TransformPoint(_controller.center);

            Vector3 up = Vector3.up;
            Vector3 bottom = centerWorld - up * (height * 0.5f - radius);
            Vector3 top = centerWorld + up * (height * 0.5f - radius);

            // 충돌 판정 캡슐 표시
            Gizmos.color = isDead ? Color.red : (_isSliding ? Color.green : Color.yellow);
            
            // 캡슐의 원형 부분들
            Gizmos.DrawWireSphere(bottom, radius);
            Gizmos.DrawWireSphere(top, radius);
            
            // 캡슐의 측면 라인들
            Vector3[] directions = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
            foreach (var dir in directions)
            {
                Vector3 offset = dir * radius;
                Gizmos.DrawLine(bottom + offset, top + offset);
            }

            // 중심점 표시
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(centerWorld, Vector3.one * 0.1f);
        }
    }
}


