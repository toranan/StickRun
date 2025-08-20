using UnityEngine;

namespace BananaRun.Runner
{
    public static class AutoRuntimeSetup
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureBasicRunnerSetup()
        {
            // 혹시 재생이 멈춰있다면 복구
            if (Time.timeScale <= 0f) Time.timeScale = 1f;

            // 플레이어 없으면 생성
            var player = GameObject.Find("Player");
            if (player == null)
            {
                player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                player.name = "Player";
                player.transform.position = new Vector3(0f, 0.5f, 0f);
                Object.Destroy(player.GetComponent<CapsuleCollider>());
                var cc = player.AddComponent<CharacterController>();
            }

            // 필수 컴포넌트 보장
            var controller = player.GetComponent<CharacterController>();
            if (controller == null) controller = player.AddComponent<CharacterController>();

            var swipeInput = player.GetComponentInChildren<SwipeInput>();
            if (swipeInput == null)
            {
                swipeInput = new GameObject("SwipeInput").AddComponent<SwipeInput>();
                swipeInput.transform.SetParent(player.transform, false);
            }

            var runner = player.GetComponent<RunnerPlayer>();
            if (runner == null) runner = player.AddComponent<RunnerPlayer>();
            runner.swipeInput = swipeInput;
            if (runner.forwardSpeed <= 0.01f) runner.forwardSpeed = 8f;
            runner.isDead = false;

            // 카메라 없으면 생성
            var cam = Camera.main;
            if (cam == null)
            {
                var camObj = new GameObject("Main Camera");
                cam = camObj.AddComponent<Camera>();
                cam.tag = "MainCamera";
                camObj.AddComponent<FollowCamera>().target = player.transform;
            }
            else if (cam.GetComponent<FollowCamera>() == null)
            {
                cam.gameObject.AddComponent<FollowCamera>().target = player.transform;
            }

            // 트랙 스포너 없으면 생성
            var trackSpawner = Object.FindFirstObjectByType<TrackSpawner>();
            if (trackSpawner == null)
            {
                var spawner = new GameObject("TrackSpawner");
                trackSpawner = spawner.AddComponent<TrackSpawner>();
            }
            trackSpawner.player = player.transform;

            // 장애물 스포너 없으면 생성
            var obstacleSpawner = Object.FindFirstObjectByType<ObstacleSpawner>();
            if (obstacleSpawner == null)
            {
                var obsSpawner = new GameObject("ObstacleSpawner");
                obstacleSpawner = obsSpawner.AddComponent<ObstacleSpawner>();
            }
            obstacleSpawner.player = player.transform;
            
            // 게임 매니저 없으면 생성
            if (Object.FindFirstObjectByType<GameManager>() == null)
            {
                var gameManagerObj = new GameObject("GameManager");
                gameManagerObj.AddComponent<GameManager>();
            }
            
            // 디버그 UI 생성 중단: 요청에 따라 표시하지 않음
            var existingDebug = Object.FindFirstObjectByType<DebugUI>();
            if (existingDebug != null)
            {
                Object.Destroy(existingDebug.gameObject);
            }
            
            Debug.Log("러너 시스템 자동 설정 완료 - 무한 생성 활성화");

            // 점수 UI 없으면 생성
            if (Object.FindFirstObjectByType<ScoreUI>() == null)
            {
                var scoreUiObj = new GameObject("ScoreUI");
                scoreUiObj.AddComponent<ScoreUI>();
            }
        }
    }
}


