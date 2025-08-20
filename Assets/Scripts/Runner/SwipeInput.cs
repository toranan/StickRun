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

        private Vector2 _startPos;
        private float _startTime;
        private bool _isInputStarted;

        private void Update()
        {
            HandleTouch();
            HandleEditorSimulation();
        }

        private void HandleTouch()
        {
#if UNITY_IOS || UNITY_ANDROID || UNITY_EDITOR
            if (Input.touchCount == 0)
            {
                return;
            }

            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                _isInputStarted = true;
                _startPos = touch.position;
                _startTime = Time.time;
            }
            else if (_isInputStarted && (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled))
            {
                _isInputStarted = false;
                float elapsed = Time.time - _startTime;
                Vector2 delta = touch.position - _startPos;

                // 탭 감지
                if (elapsed <= maxTapTime && delta.magnitude < maxTapDistancePixels)
                {
                    if (_startPos.x < Screen.width / 2)
                    {
                        Debug.Log("🟡 왼쪽 탭 감지");
                        OnSwipeLeft?.Invoke();
                    }
                    else
                    {
                        Debug.Log("🟡 오른쪽 탭 감지");
                        OnSwipeRight?.Invoke();
                    }
                }
                // 스와이프 감지
                else if (elapsed <= maxSwipeTime && delta.magnitude >= minSwipeDistancePixels)
                {
                    DetectSwipeDirection(delta);
                }
            }
#endif
        }

        private void HandleEditorSimulation()
        {
#if ENABLE_INPUT_SYSTEM
            // 새 입력 시스템 우선 사용
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame) OnSwipeUp?.Invoke();
                if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame) OnSwipeDown?.Invoke();
                if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame) OnSwipeLeft?.Invoke();
                if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame) OnSwipeRight?.Invoke();
            }
#elif UNITY_EDITOR || UNITY_STANDALONE
            // 새 입력 시스템이 없을 때만 구 입력 처리
            if (Input.GetKeyDown(KeyCode.UpArrow)) OnSwipeUp?.Invoke();
            if (Input.GetKeyDown(KeyCode.DownArrow)) OnSwipeDown?.Invoke();
            if (Input.GetKeyDown(KeyCode.LeftArrow)) OnSwipeLeft?.Invoke();
            if (Input.GetKeyDown(KeyCode.RightArrow)) OnSwipeRight?.Invoke();
            if (Input.GetKeyDown(KeyCode.W)) OnSwipeUp?.Invoke();
            if (Input.GetKeyDown(KeyCode.S)) OnSwipeDown?.Invoke();
            if (Input.GetKeyDown(KeyCode.A)) OnSwipeLeft?.Invoke();
            if (Input.GetKeyDown(KeyCode.D)) OnSwipeRight?.Invoke();
#endif
        }

        private void DetectSwipeDirection(Vector2 delta)
        {
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            {
                // Horizontal swipes are now ignored
            }
            else
            {
                if (delta.y > 0f) 
                {
                    Debug.Log("🟡 위쪽 스와이프 감지 → 점프");
                    OnSwipeUp?.Invoke(); 
                }
                else 
                {
                    Debug.Log("🟡 아래쪽 스와이프 감지 → 슬라이드");
                    OnSwipeDown?.Invoke();
                }
            }
        }

        // 디버그/외부 호출용 메서드들
        public void TriggerSwipeLeft() => OnSwipeLeft?.Invoke();
        public void TriggerSwipeRight() => OnSwipeRight?.Invoke();
        public void TriggerSwipeUp() => OnSwipeUp?.Invoke();
        public void TriggerSwipeDown() => OnSwipeDown?.Invoke();
    }
}