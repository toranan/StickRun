using UnityEngine;
using UnityEngine.SceneManagement;

namespace BananaRun.Runner
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance;
        
        [Header("Game State")]
        public bool isGameOver = false;
        public float playTime = 0f;
        public float virtualDistance = 0f; // 가상 진행 거리
        
        [Header("Score")]
        public int score = 0; // 초마다 1점
        
        private RunnerPlayer _player;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            _player = FindFirstObjectByType<RunnerPlayer>();
            Debug.Log("🎮 무한 러너 게임 시작!");
            Debug.Log("📍 3레인 시스템: 레인 1 ← → 레인 2 ← → 레인 3");
            Debug.Log("🎯 한 번의 스와이프/키 입력 = 한 레인씩 이동");
            Debug.Log("📝 레인 1 → 레인 3으로 가려면 2번 이동 필요!");
            Debug.Log("🏃‍♂️ 롤: S키/아래 스와이프 → 🟢초록 장애물 통과 가능!");
            Debug.Log("🦘 점프: W키/위 스와이프 → 🔴빨간 장애물 점프로 피하기!");
            
            // 프리팹 설정 정보 표시
            var spawner = FindFirstObjectByType<ObstacleSpawner>();
            if (spawner != null)
            {
                if (spawner.obstaclePrefabs != null && spawner.obstaclePrefabs.Length > 0)
                {
                    int validPrefabs = System.Array.FindAll(spawner.obstaclePrefabs, p => p != null).Length;
                    Debug.Log($"🎲 프리팹 장애물: {validPrefabs}개 등록됨 (프리팹 전용 모드: {spawner.usePrefabsOnly})");
                }
                else
                {
                    Debug.Log("🎲 프리팹 없음 → 랜덤 큐브 생성 모드");
                }
            }
        }

        private void Update()
        {
            if (!isGameOver)
            {
                playTime += Time.deltaTime;
                
                // 초마다 1점: 경과 시간을 내림하여 점수로 사용
                int newScore = Mathf.FloorToInt(playTime);
                if (newScore != score)
                {
                    score = newScore;
                }
                
                // 가상 진행 거리 업데이트
                if (_player != null && !_player.isDead)
                {
                    virtualDistance += _player.forwardSpeed * Time.deltaTime;
                }
                
                // 플레이어 죽음 체크
                if (_player != null && _player.isDead && !isGameOver)
                {
                    GameOver();
                }
            }
            
            // R키로 리스타트
            if (Input.GetKeyDown(KeyCode.R))
            {
                RestartGame();
            }
        }

        private void GameOver()
        {
            isGameOver = true;
            Debug.Log($"게임 오버! 달린 거리: {virtualDistance:F1}m, 플레이 시간: {playTime:F1}초. R키를 눌러서 다시 시작하세요.");
        }

        public void RestartGame()
        {
            Debug.Log("게임 재시작!");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
