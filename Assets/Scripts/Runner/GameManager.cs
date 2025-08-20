using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro; // TextMeshPro 사용을 위해 추가

namespace BananaRun.Runner
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance;

        public enum GameState
        {
            Ready,
            Countdown,
            Playing,
            GameOver
        }

        [Header("Game State")]
        public GameState currentGameState = GameState.Ready;
        public bool isGameOver = false;
        public float playTime = 0f;
        public float virtualDistance = 0f; // 가상 진행 거리

        [Header("Score")]
        public int score = 0; // 초마다 1점
        public TextMeshProUGUI scoreText; // 점수 및 카운트다운 표시용 UI 텍스트

        private RunnerPlayer _player;
        private ObstacleSpawner _spawner;

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
            _spawner = FindFirstObjectByType<ObstacleSpawner>();

            // 초기 게임 상태 설정
            SetGameState(GameState.Ready);
            isGameOver = false;
            playTime = 0f;
            score = 0;

            // 게임 시작 전 디버그 로그
            Debug.Log("🎮 무한 러너 게임 준비!");
            Debug.Log("📍 3레인 시스템: 레인 1 ← → 레인 2 ← → 레인 3");
            Debug.Log("🎯 한 번의 스와이프/키 입력 = 한 레인씩 이동");
            Debug.Log("📝 레인 1 → 레인 3으로 가려면 2번 이동 필요!");
            Debug.Log("🏃‍♂️ 롤: S키/아래 스와이프 → 🟢초록 장애물 통과 가능!");
            Debug.Log("🦘 점프: W키/위 스와이프 → 🔴빨간 장애물 점프로 피하기!");

            // 프리팹 설정 정보 표시
            if (_spawner != null)
            {
                if (_spawner.obstaclePrefabs != null && _spawner.obstaclePrefabs.Length > 0)
                {
                    int validPrefabs = System.Array.FindAll(_spawner.obstaclePrefabs, p => p != null).Length;
                    Debug.Log($"🎲 프리팹 장애물: {validPrefabs}개 등록됨 (프리팹 전용 모드: {_spawner.usePrefabsOnly})");
                }
                else
                {
                    Debug.Log("🎲 프리팹 없음 → 랜덤 큐브 생성 모드");
                }
            }
        }

        private void Update()
        {
            if (currentGameState == GameState.Playing && !isGameOver)
            {
                playTime += Time.deltaTime;

                // 초마다 1점: 경과 시간을 내림하여 점수로 사용
                int newScore = Mathf.FloorToInt(playTime);
                if (newScore != score)
                {
                    score = newScore;
                    UpdateScoreText(); // 점수 UI 업데이트
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

            // R키로 리스타트 (게임 오버 상태에서만)
            if (Input.GetKeyDown(KeyCode.R) && currentGameState == GameState.GameOver)
            {
                RestartGame();
            }
        }

        public void StartGame()
        {
            if (currentGameState == GameState.Ready)
            {
                StartCoroutine(CountdownAndStartGame());
            }
        }

        private IEnumerator CountdownAndStartGame()
        {
            SetGameState(GameState.Countdown);
            Debug.Log("카운트다운 시작!");

            for (int i = 3; i > 0; i--)
            {
                if (scoreText != null)
                {
                    scoreText.text = i.ToString();
                }
                yield return new WaitForSeconds(1f);
            }

            if (scoreText != null)
            {
                scoreText.text = "Go!";
            }
            yield return new WaitForSeconds(0.5f); // "Go!" 표시 시간

            SetGameState(GameState.Playing);
            isGameOver = false;
            playTime = 0f;
            score = 0;
            UpdateScoreText(); // 게임 시작 시 점수 UI 초기화

            Debug.Log("게임 시작!");
            // 플레이어 및 장애물 스포너 활성화 로직은 RunnerPlayer와 ObstacleSpawner에서 GameManager.Instance.currentGameState를 확인하도록 구현
        }

        private void UpdateScoreText()
        {
            if (scoreText == null) return;

            switch (currentGameState)
            {
                case GameState.Ready:
                    scoreText.text = "Press Start"; // 또는 빈 문자열
                    break;
                case GameState.Playing:
                    scoreText.text = "Score: " + score.ToString();
                    break;
                case GameState.GameOver:
                    scoreText.text = $"Game Over! Score: {score}";
                    break;
                // Countdown 상태는 CountdownAndStartGame 코루틴에서 직접 처리
            }
        }

        private void SetGameState(GameState newState)
        {
            currentGameState = newState;
            UpdateScoreText(); // 상태 변경 시 UI 업데이트
        }

        private void GameOver()
        {
            SetGameState(GameState.GameOver);
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
