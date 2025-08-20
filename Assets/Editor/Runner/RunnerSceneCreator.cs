using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace BananaRun.Runner.Editor
{
    public static class RunnerSceneCreator
    {
        [MenuItem("BananaRun/Create Runner Sample Scene", priority = 10)]
        public static void CreateSampleScene()
        {
            CreateScene(false);
        }

        [MenuItem("BananaRun/Create Advanced Runner Scene", priority = 11)]
        public static void CreateAdvancedScene()
        {
            CreateScene(true);
        }

        private static void CreateScene(bool useAdvancedSpawner)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 트랙 스포너
            GameObject spawner = new GameObject("TrackSpawner");
            var trackSpawner = spawner.AddComponent<BananaRun.Runner.TrackSpawner>();

            // 플레이어
            GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            player.transform.position = new Vector3(0f, 0.5f, 0f);
            var controller = player.AddComponent<CharacterController>();
            Object.DestroyImmediate(player.GetComponent<CapsuleCollider>());

            var swipe = new GameObject("SwipeInput").AddComponent<BananaRun.Runner.SwipeInput>();
            swipe.transform.SetParent(player.transform, false);

            var runner = player.AddComponent<BananaRun.Runner.RunnerPlayer>();
            runner.swipeInput = swipe;

            trackSpawner.player = player.transform;

            // 장애물 스포너 (기본 또는 고급)
            GameObject obstacleSpawnerObj = new GameObject(useAdvancedSpawner ? "AdvancedObstacleSpawner" : "ObstacleSpawner");
            
            if (useAdvancedSpawner)
            {
                var advancedSpawner = obstacleSpawnerObj.AddComponent<BananaRun.Runner.AdvancedObstacleSpawner>();
                advancedSpawner.player = player.transform;
                advancedSpawner.laneCount = runner.laneCount;
                advancedSpawner.laneOffset = runner.laneOffset;
            }
            else
            {
                var obstacleSpawner = obstacleSpawnerObj.AddComponent<BananaRun.Runner.ObstacleSpawner>();
                obstacleSpawner.player = player.transform;
                obstacleSpawner.laneCount = runner.laneCount;
                obstacleSpawner.laneOffset = runner.laneOffset;
            }

            // 애니메이터 브리지(모델은 사용자가 교체)
            var bridge = player.AddComponent<BananaRun.Runner.RunnerAnimatorBridge>();
            bridge.runner = runner;
            bridge.swipeInput = swipe;

            // 카메라
            GameObject camObj = new GameObject("Main Camera");
            Camera cam = camObj.AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.Skybox;
            camObj.transform.position = new Vector3(0f, 4f, -7f);
            var followCam = camObj.AddComponent<BananaRun.Runner.FollowCamera>();
            followCam.target = player.transform;

            // 게임 매니저
            GameObject gameManagerObj = new GameObject("GameManager");
            gameManagerObj.AddComponent<BananaRun.Runner.GameManager>();
            
            // 디버그 UI 생성 생략 (요청에 따라 UI 최소화)

            // 시작 지점 미리 보이도록 첫 프레임 전에 세그먼트가 생성되지만,
            // 에디터 뷰에서 바닥을 바로 보고 싶다면 아래 한 줄로 초기화 위치를 살짝 조정 가능
            spawner.transform.position = Vector3.zero;

            // 저장
            string path = useAdvancedSpawner ? "Assets/Scenes/AdvancedRunnerSample.unity" : "Assets/Scenes/RunnerSample.unity";
            EditorSceneManager.SaveScene(scene, path);
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

            string spawnerType = useAdvancedSpawner ? "고급 장애물 스포너" : "기본 장애물 스포너";
            Debug.Log($"러너 씬 생성 완료: {path} ({spawnerType})");
        }
    }
}


