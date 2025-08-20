using UnityEngine;

namespace BananaRun.Runner
{
    public class RunnerAnimatorBridge : MonoBehaviour
    {
        [Header("References")]
        public RunnerPlayer runner;
        public SwipeInput swipeInput;

        [Header("Model/Animator")]
        [Tooltip("캐릭터 모델 루트(선택). 비워두면 자식에서 Animator 자동 탐색")]
        public Transform modelRoot;
        public Animator animator;
        [Tooltip("모델 루트를 플레이어 위치에 고정(루트모션이 남아있어도 시각적 위치 고정)")]
        public bool lockModelToPlayer = true;
        [Tooltip("모델의 로컬 회전을 초기값으로 고정 (애니메이션이 방향을 틀어도 유지)")]
        public bool lockModelRotation = true;

        [Header("Animator Parameters")]
        public string runBoolParam = "Run";
        public string speedFloatParam = "Speed"; // 선택
        public string jumpTriggerParam = "Jump";
        public string slideBoolParam = "Roll";
        public string dieTriggerParam = "Die";
        public float runAnimSpeedMultiplier = 0.1f;

        private bool _prevDead;
        private Vector3 _modelInitialLocalPos;
        private Quaternion _modelInitialLocalRot;

        private void Awake()
        {
            if (runner == null) runner = GetComponent<RunnerPlayer>();
            if (swipeInput == null) swipeInput = GetComponentInChildren<SwipeInput>();

            if (animator == null)
            {
                var root = modelRoot != null ? modelRoot : transform;
                animator = root.GetComponentInChildren<Animator>();
            }

            if (modelRoot == null && animator != null)
            {
                modelRoot = animator.transform;
            }
            if (modelRoot != null)
            {
                _modelInitialLocalPos = modelRoot.localPosition;
                _modelInitialLocalRot = modelRoot.localRotation;
            }
        }

        private void OnEnable()
        {
            if (swipeInput != null)
            {
                swipeInput.OnSwipeUp += HandleJump;
            }
        }

        private void OnDisable()
        {
            if (swipeInput != null)
            {
                swipeInput.OnSwipeUp -= HandleJump;
            }
        }

        private void Start()
        {
            // 시작 시 달리기 상태로
            SetBool(runBoolParam, true);
            SetFloat(speedFloatParam, (runner != null ? runner.forwardSpeed : 8f) * runAnimSpeedMultiplier);
        }

        private void Update()
        {
            if (animator == null || runner == null) return;

            // 달리기/속도 동기화
            SetBool(runBoolParam, !runner.isDead);
            SetFloat(speedFloatParam, runner.forwardSpeed * runAnimSpeedMultiplier);

            // 슬라이드 동기화
            SetBool(slideBoolParam, runner.IsSliding);

            // 사망 트리거
            if (!_prevDead && runner.isDead)
            {
                SetTrigger(dieTriggerParam);
            }
            _prevDead = runner.isDead;

            // 모델 루트 고정(루트모션 잔여 이동 억제)
            if (lockModelToPlayer && modelRoot != null)
            {
                modelRoot.localPosition = _modelInitialLocalPos;
            }
            if (lockModelRotation && modelRoot != null)
            {
                modelRoot.localRotation = _modelInitialLocalRot;
            }
        }

        private void HandleJump()
        {
            SetTrigger(jumpTriggerParam);
        }

        private void SetBool(string name, bool value)
        {
            if (animator == null || string.IsNullOrEmpty(name)) return;
            animator.SetBool(name, value);
        }

        private void SetFloat(string name, float value)
        {
            if (animator == null || string.IsNullOrEmpty(name)) return;
            animator.SetFloat(name, value);
        }

        private void SetTrigger(string name)
        {
            if (animator == null || string.IsNullOrEmpty(name)) return;
            animator.SetTrigger(name);
        }
    }
}


