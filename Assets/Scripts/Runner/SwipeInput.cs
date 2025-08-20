using System;
using UnityEngine;

namespace BananaRun.Runner
{
    public class SwipeInput : MonoBehaviour
    {
        [Header("Swipe Settings")]
        [Tooltip("스와이프 최소 픽셀 거리")]
        public float minSwipeDistancePixels = 80f;

        [Tooltip("스와이프 허용 최대 시간(초)")]
        public float maxSwipeTime = 0.6f;

        [Header("Tap Settings")]
        [Tooltip("탭으로 간주할 최대 시간(초)")]
        public float maxTapTime = 0.3f;
        [Tooltip("탭으로 간주할 최대 픽셀 이동 거리")]
        public float maxTapDistancePixels = 30f;

        public event Action OnSwipeUp;
        public event Action OnSwipeDown;
        public event Action OnSwipeLeft;
        public event Action OnSwipeRight;
        
        public event Action OnHoldStart;
        public event Action OnHoldEnd;

        private Vector2 _startPos;
        private float _startTime;
        private bool _isInputStarted;
        private bool _isHolding;

        private void Update()
        {
            HandleTouch();
            HandleEditorSimulation();
        }

        private void HandleTouch()
        {
#if UNITY_IOS || UNITY_ANDROID || UNITY_EDITOR
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);

                if (touch.phase == TouchPhase.Began)
                {
                    _isInputStarted = true;
                    _startPos = touch.position;
                    _startTime = Time.time;

                    if (!_isHolding)
                    {
                        _isHolding = true;
                        OnHoldStart?.Invoke();
                        Debug.Log("🟡 홀드 시작");
                    }
                }
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    if (_isHolding)
                    {
                
                        _isHolding = false;
                        OnHoldEnd?.Invoke();
                        Debug.Log("🟡 홀드 종료");
                    }

                    if (!_isInputStarted) return;
                    _isInputStarted = false;

                    float elapsed = Time.time - _startTime;
                    Vector2 delta = touch.position - _startPos;

                    if (elapsed <= maxTapTime && delta.magnitude < maxTapDistancePixels)
                    {
                        if (_startPos.x < Screen.width / 2)
                        {
                            OnSwipeLeft?.Invoke();
                        }
                        else
                        {
                            OnSwipeRight?.Invoke();
                        }
                    }
                    else if (elapsed <= maxSwipeTime && delta.magnitude >= minSwipeDistancePixels)
                    {
                        DetectSwipeDirection(delta);
                    }
                }
            }
            else
            {
                if (_isHolding)
                {
                    _isHolding = false;
                    OnHoldEnd?.Invoke();
                    Debug.Log("🟡 홀드 종료 (터치 없음)");
                }
            }
#endif
        }

        private void HandleEditorSimulation()
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame) OnSwipeUp?.Invoke();
                if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame) OnSwipeDown?.Invoke();
                if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame) OnSwipeLeft?.Invoke();
                if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame) OnSwipeRight?.Invoke();

                // New: Spacebar hold for glide (New Input System)
                if (keyboard.spaceKey.isPressed)
                {
                    if (!_isHolding && Input.touchCount == 0)
                    {
                        _isHolding = true;
                        OnHoldStart?.Invoke();
                        Debug.Log("🟡 스페이스바 홀드 시작 (New Input System)");
                    }
                }
                else
                {
                    if (_isHolding && Input.touchCount == 0)
                    {
                        _isHolding = false;
                        OnHoldEnd?.Invoke();
                        Debug.Log("🟡 스페이스바 홀드 종료 (New Input System)");
                    }
                }
            }
#elif UNITY_EDITOR || UNITY_STANDALONE
            if (Input.GetKeyDown(KeyCode.UpArrow)) OnSwipeUp?.Invoke();
            if (Input.GetKeyDown(KeyCode.DownArrow)) OnSwipeDown?.Invoke();
            if (Input.GetKeyDown(KeyCode.LeftArrow)) OnSwipeLeft?.Invoke();
            if (Input.GetKeyDown(KeyCode.RightArrow)) OnSwipeRight?.Invoke();
            if (Input.GetKeyDown(KeyCode.W)) OnSwipeUp?.Invoke();
            if (Input.GetKeyDown(KeyCode.S)) OnSwipeDown?.Invoke();
            if (Input.GetKeyDown(KeyCode.A)) OnSwipeLeft?.Invoke();
            if (Input.GetKeyDown(KeyCode.D)) OnSwipeRight?.Invoke();

            // New: Spacebar hold for glide (Old Input System)
            if (Input.GetKey(KeyCode.Space))
            {
                if (!_isHolding && Input.touchCount == 0)
                {
                    _isHolding = true;
                    OnHoldStart?.Invoke();
                    Debug.Log("🟡 스페이스바 홀드 시작 (Old Input System)");
                }
            }
            else
            {
                if (_isHolding && Input.touchCount == 0)
                {
                    _isHolding = false;
                    OnHoldEnd?.Invoke();
                    Debug.Log("🟡 스페이스바 홀드 종료 (Old Input System)");
                }
            }
#endif
        }

        private void DetectSwipeDirection(Vector2 delta)
        {
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            {
                // Horizontal swipes are ignored
            }
            else
            {
                if (delta.y > 0f)
                {
                    OnSwipeUp?.Invoke();
                }
                else
                {
                    OnSwipeDown?.Invoke();
                }
            }
        }

        public void TriggerSwipeLeft() => OnSwipeLeft?.Invoke();
        public void TriggerSwipeRight() => OnSwipeRight?.Invoke();
        public void TriggerSwipeUp() => OnSwipeUp?.Invoke();
        public void TriggerSwipeDown() => OnSwipeDown?.Invoke();
    }
}