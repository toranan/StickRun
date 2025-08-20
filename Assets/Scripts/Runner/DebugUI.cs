using UnityEngine;

namespace BananaRun.Runner
{
    public class DebugUI : MonoBehaviour
    {
        private RunnerPlayer _player;
        private bool _showDebugUI = true;
        
        private void Start()
        {
            _player = FindFirstObjectByType<RunnerPlayer>();
        }
        
        private void Update()
        {
            // F1키로 디버그 UI 토글
            if (Input.GetKeyDown(KeyCode.F1))
            {
                _showDebugUI = !_showDebugUI;
            }
        }
        
        private void OnGUI()
        {
            if (!_showDebugUI || _player == null) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label("=== 3레인 러너 디버그 ===");
            GUILayout.Label($"현재 레인: {_player.CurrentLaneIndex + 1} / 3");
            GUILayout.Label($"사망 여부: {(_player.isDead ? "💀 게임 오버" : "✅ 플레이 중")}");
            
            // 슬라이딩 상태 표시
            string slideStatus = GetSlideStatus(_player);
            GUILayout.Label($"롤: {slideStatus}");
            
            // 충돌 디버그 정보
            GUILayout.Label($"충돌 디버그: {(_player.showCollisionDebug ? "🟢 ON" : "🔴 OFF")}");
            
            GUILayout.Space(10);
            GUILayout.Label("테스트 버튼:");
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("◀ 왼쪽 (1레인)"))
            {
                _player.GetComponentInChildren<SwipeInput>()?.TriggerSwipeLeft();
            }
            if (GUILayout.Button("▶ 오른쪽 (1레인)"))
            {
                _player.GetComponentInChildren<SwipeInput>()?.TriggerSwipeRight();
            }
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("▲ 점프"))
            {
                _player.GetComponentInChildren<SwipeInput>()?.TriggerSwipeUp();
            }
            if (GUILayout.Button("▼ 롤"))
            {
                _player.GetComponentInChildren<SwipeInput>()?.TriggerSwipeDown();
            }
            GUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            if (GUILayout.Button("R - 재시작"))
            {
                GameManager.Instance?.RestartGame();
            }
            
            GUILayout.Label("F1키로 이 UI 숨기기/보이기");
            GUILayout.EndArea();
        }

        private string GetSlideStatus(RunnerPlayer player)
        {
            if (player.IsSliding)
            {
                return "🏃‍♂️ 슬라이딩 중 (🟢초록 장애물 통과 가능)";
            }
            else
            {
                return "🚶‍♂️ 일반 상태";
            }
        }
    }
}
