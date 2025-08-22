using UnityEngine;

namespace BananaRun.Runner
{
    [RequireComponent(typeof(CharacterController))]
    public class RunnerPlayer : MonoBehaviour
    {
        [Header("Movement Settings")]
        public float forwardSpeed = 12f; // z+ 방향으로 계속 전진
        public bool isInvincible = false; // 무적 상태 여부
        public bool isFlying = false; // 로켓 비행 상태 여부
        public float maxSpeed = 50f; // 도달할 최대 속도
        public float speedIncreaseRate = 0.1f; // 초당 속도 증가량
        public float speedMultiplier = 1f; // 아이템 등으로 인한 속도 증폭기
        public float CurrentSpeed => forwardSpeed * speedMultiplier; // 최종 계산된 현재 속도
        public float laneOffset = 2f;   // 레인 간격
        public int laneCount = 3;       // 3레인 (0,1,2)
        public float laneChangeSpeed = 18f; // 레인 이동을 더 빠르고 명확하게
        public float deathHeight = -10f; // 이 높이 아래로 떨어지면 게임 오버

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

        [Header("Powerup Effects")]
        public GameObject auraBuffPrefab;
        private GameObject _auraBuffInstance;
        [Header("Powerdown Effects")]
        public GameObject powerdownEffectPrefab;
        private GameObject _powerdownEffectInstance;
        [Header("Invincible Effects")]
        public GameObject invincibleEffectPrefab;
        private GameObject _invincibleEffectInstance;
        [Header("Rocket Effects")]
        public GameObject rocketEffectPrefab;
        private GameObject _rocketEffectInstance;

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

            isDead = false;
            _remainingAirJumps = maxAirJumps;
            _normalGravity = gravity; // 초기 중력 값 저장

            // 오라 버프 프리팹 인스턴스화
            if (auraBuffPrefab != null)
            {
                _auraBuffInstance = Instantiate(auraBuffPrefab, transform);
                _auraBuffInstance.transform.localPosition = new Vector3(0, 0, 0); // 머리 위에 위치
                _auraBuffInstance.SetActive(false);
            }
            // 파워다운 이펙트 프리팹 인스턴스화
            if (powerdownEffectPrefab != null)
            {
                _powerdownEffectInstance = Instantiate(powerdownEffectPrefab, transform);
                _powerdownEffectInstance.transform.localPosition = new Vector3(0, 1.0f, 0); // 머리 위에 위치
                _powerdownEffectInstance.SetActive(false);
            }
            // 무적 이펙트 프리팹 인스턴스화
            if (invincibleEffectPrefab != null)
            {
                _invincibleEffectInstance = Instantiate(invincibleEffectPrefab, transform);
                _invincibleEffectInstance.transform.localPosition = new Vector3(0, 0, 0); // 머리 위에 위치
                _invincibleEffectInstance.SetActive(false);
            }
            // 로켓 이펙트 프리팹 인스턴스화
            if (rocketEffectPrefab != null)
            {
                _rocketEffectInstance = Instantiate(rocketEffectPrefab, transform);
                _rocketEffectInstance.transform.localPosition = new Vector3(0, 0, 0); // 머리 위에 위치
                _rocketEffectInstance.SetActive(false);
            }
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

        // Trigger 충돌 감지 추가 (Obstacle이 isTrigger=true로 설정되어 있음)
        private void OnTriggerEnter(Collider other)
        {
            if (isDead) return;
            
            var obstacle = other.GetComponent<Obstacle>();
            if (obstacle != null)
            {
                Debug.Log($"🚨 Trigger 충돌 감지: {other.name}");
                HandleObstacleCollision(obstacle, other);
            }
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.currentGameState != GameManager.GameState.Playing)
            {
                return;
            }

            if (isDead) return; // 이미 죽었다면 아래 로직 실행 안 함

            // 속도 점진적 증가
            if (forwardSpeed < maxSpeed)
            {
                forwardSpeed += speedIncreaseRate * Time.deltaTime;
                forwardSpeed = Mathf.Min(forwardSpeed, maxSpeed); // 최대 속도 초과 방지
            }

            // 낙하 사망 판정
            if (transform.position.y < deathHeight)
            {
                isDead = true;
                Debug.Log($"💀 추락으로 사망! (Y: {transform.position.y:F1} < {deathHeight:F1}). Game Over.");
                return; // 사망 처리 후 즉시 업데이트 종료
            }

            float half = (laneCount - 1) * 0.5f;
            float targetX = (_currentLaneIndex - half) * laneOffset;
            float nextX = Mathf.MoveTowards(transform.position.x, targetX, laneChangeSpeed * Time.deltaTime);
            float deltaX = nextX - transform.position.x;

            bool grounded = _controller.isGrounded;
            if (grounded)
            {
                if (_verticalVelocity < 0f) _verticalVelocity = -2f;
                if (IsGliding) EndGlide();
                _remainingAirJumps = maxAirJumps;
            }

            float currentGravity = IsGliding ? _normalGravity * glideGravityMultiplier : _normalGravity;
            _verticalVelocity += currentGravity * Time.deltaTime;

            float targetY = isFlying ? 10f : transform.position.y + _verticalVelocity * Time.deltaTime;
            float nextY = isFlying ? Mathf.MoveTowards(transform.position.y, targetY, 30f * Time.deltaTime) : targetY;

            Vector3 displacement = new Vector3(
                deltaX,
                nextY - transform.position.y,
                0f
            );
            _controller.Move(displacement);

            CheckPreciseCollision();

            if (_isSliding && Time.time >= _slideEndTime)
            {
                EndSlide();
            }
        }

        private void CheckPreciseCollision()
        {
            float radius = _controller.radius * 0.9f;
            float height = _controller.height;
            Vector3 centerWorld = transform.TransformPoint(_controller.center);

            Vector3 up = Vector3.up;
            Vector3 bottom = centerWorld - up * (height * 0.5f - radius);
            Vector3 top = centerWorld + up * (height * 0.5f - radius);

            Collider[] overlapping = Physics.OverlapCapsule(bottom, top, radius, ~0, QueryTriggerInteraction.Collide);
            
            Debug.Log($"🔍 CheckPreciseCollision: {overlapping.Length}개의 Collider 감지됨");
            
            foreach (var collider in overlapping)
            {
                Debug.Log($"  - 감지된 객체: {collider.name} (Layer: {collider.gameObject.layer})");
                
                var obstacle = collider.GetComponent<Obstacle>();
                if (obstacle != null)
                {
                    Debug.Log($"  - Obstacle 발견: {collider.name}");
                    if (IsRealCollision(collider, centerWorld, radius))
                    {
                        Debug.Log($"  - 실제 충돌 확인됨: {collider.name}");
                        HandleObstacleCollision(obstacle, collider);
                        return;
                    }
                }
            }
        }

        private bool IsRealCollision(Collider obstacleCollider, Vector3 playerCenter, float playerRadius)
        {
            Vector3 closestPoint = obstacleCollider.ClosestPoint(playerCenter);
            float distance = Vector3.Distance(playerCenter, closestPoint);
            bool isColliding = distance < playerRadius;
            
            if (isColliding)
            {
                Debug.Log($"🔍 정밀 충돌 감지: {obstacleCollider.name}, 거리: {distance:F2}m (반지름: {playerRadius:F2}m)");
            }
            
            return isColliding;
        }

        private void HandleObstacleCollision(Obstacle obstacle, Collider collider)
        {
            if (isInvincible)
            {
                Debug.Log($"🛡️ 무적 상태로 {collider.name} 무시!");
                return;
            }
            if (_isSliding && CanSlideUnderObstacle(obstacle))
            {
                Debug.Log($"🏃‍♂️ 슬라이딩으로 {collider.name} 통과!");
                return;
            }
            
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
            if (IsGliding) EndGlide();
            
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
            if (IsGliding) EndGlide();
            
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
            if (IsGliding) EndGlide();
            
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
            if (IsGliding) EndGlide();
            
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
            Debug.Log("🚀 글라이딩 시작!");
        }

        private void EndGlide()
        {
            IsGliding = false;
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

        public void SetSpeedBoostEffect(bool active)
        {
            if (_auraBuffInstance != null)
            {
                _auraBuffInstance.SetActive(active);
            }
        }
        public void SetPowerdownEffect(bool active)
        {
            if (_powerdownEffectInstance != null)
            {
                _powerdownEffectInstance.SetActive(active);
            }
        }
        public void SetInvincibleEffect(bool active)
        {
            if (_invincibleEffectInstance != null)
            {
                _invincibleEffectInstance.SetActive(active);
            }
        }
        public void SetRocketEffect(bool active)
        {
            if (_rocketEffectInstance != null)
            {
                _rocketEffectInstance.SetActive(active);
            }
        }
    }
}
