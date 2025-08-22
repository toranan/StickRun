using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro; // TextMeshPro 사용을 위해 추가
using UnityEngine.UI; // Button 사용을 위해 추가

namespace BananaRun.Runner
{
    [RequireComponent(typeof(AudioSource))] // 오디오 소스 컴포넌트 강제 추가
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
        public float virtualDistance = 0f; // 가상 진행 거리

        [Header("Power-up Settings")]
        public float speedBoostMultiplier = 3f;
        public float speedBoostDuration = 5f;

        [Header("UI")]
        public TextMeshProUGUI scoreText;
        public Button restartButton;

        [Header("Score")]
        public int score = 0;

        private RunnerPlayer _player;
        private ObstacleSpawner _spawner;
        private AudioSource _audioSource; // 사운드 재생기
        private Coroutine _speedBoostCoroutine; // 스피드 부스트 코루틴 저장용

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                _audioSource = GetComponent<AudioSource>(); // 오디오 소스 컴포넌트 가져오기
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "RunnerSample")
            {
                InitializeGame();
            }
        }

        private void InitializeGame()
        {
            _player = FindFirstObjectByType<RunnerPlayer>();
            _spawner = FindFirstObjectByType<ObstacleSpawner>();

            GameObject scoreTextObject = GameObject.FindWithTag("ScoreText");
            if (scoreTextObject != null)
            {
                scoreText = scoreTextObject.GetComponent<TextMeshProUGUI>();
            }

            GameObject restartButtonObject = GameObject.FindWithTag("RestartButton");
            if (restartButtonObject != null)
            {
                restartButton = restartButtonObject.GetComponent<Button>();
                if (restartButton != null)
                {
                    restartButton.onClick.AddListener(RestartGame);
                    restartButton.gameObject.SetActive(false);
                }
            }

            if (FindObjectOfType<MainMenuUI>() == null)
            {
                StartCoroutine(CountdownAndStartGame());
            }
            else
            {
                SetGameState(GameState.Ready);
            }
        }

        private void Update()
        {
            if (currentGameState == GameState.Playing && !isGameOver)
            {
                // 가상 진행 거리 업데이트 (아이템 효과가 적용된 최종 속도 사용)
                if (_player != null && !_player.isDead)
                {
                    virtualDistance += _player.CurrentSpeed * Time.deltaTime;
                }

                // 거리를 기준으로 점수 계산 (1미터당 1점)
                int newScore = Mathf.FloorToInt(virtualDistance);
                if (newScore != score)
                {
                    score = newScore;
                }

                // 플레이어 사망 체크
                if (_player != null && _player.isDead && !isGameOver)
                {
                    GameOver();
                }
            }

            if (Input.GetKeyDown(KeyCode.R) && currentGameState == GameState.GameOver)
            {
                RestartGame();
            }
        }

        public void StartGame()
        {
            if (currentGameState == GameState.Ready)
            {
                SceneManager.LoadScene("RunnerSample");
            }
        }

        private IEnumerator CountdownAndStartGame()
        {
            if (scoreText != null)
            {
                scoreText.gameObject.SetActive(true);
            }

            SetGameState(GameState.Countdown);

            for (int i = 3; i > 0; i--)
            {
                if (scoreText != null) scoreText.text = i.ToString();
                yield return new WaitForSeconds(1f);
            }

            if (scoreText != null) scoreText.text = "Go!";
            yield return new WaitForSeconds(0.5f);

            if (scoreText != null) scoreText.gameObject.SetActive(false);

            // 게임 시작 직전, 상태 초기화
            SetGameState(GameState.Playing);
            isGameOver = false;
            score = 0;
            virtualDistance = 0f;
        }

        private void SetGameState(GameState newState)
        {
            currentGameState = newState;
        }

        private void GameOver()
        {
            SetGameState(GameState.GameOver);
            isGameOver = true;

            if (scoreText != null)
            {
                scoreText.gameObject.SetActive(true);
                scoreText.text = $"Game Over!\nScore: {score}";
            }

            if (restartButton != null)
            {
                restartButton.gameObject.SetActive(true);
            }
        }

        public void RestartGame()
        {
            // 현재 씬을 다시 로드하면 OnSceneLoaded 이벤트가 모든 것을 다시 초기화해줍니다.
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void OnItemCollected(ItemType type, AudioClip collectionSound, GameObject itemObject)
        {
            // 사운드 재생
            if (collectionSound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(collectionSound);
            }

            // 아이템 타입에 따른 효과 처리
            switch (type)
            {
                case ItemType.Coin:
                    score += 10; // 코인은 10점 추가
                    Debug.Log("코인 획득! 점수 +10");
                    break;
                case ItemType.Magnet:
                    Debug.Log("자석 획득!");
                    break;
                case ItemType.Invincible:
                    if (_player != null) StartCoroutine(InvincibleRoutine());
                    Debug.Log("무적 획득!");
                    break;
                case ItemType.SpeedBoost:
                    if (_speedBoostCoroutine != null) StopCoroutine(_speedBoostCoroutine);
                    _speedBoostCoroutine = StartCoroutine(SpeedBoostRoutine());
                    Debug.Log("스피드 부스트 획득!");
                    break;
                case ItemType.Rocket:
                    if (_player != null) StartCoroutine(RocketRoutine());
                    Debug.Log("로켓 획득! 5초간 비행");
                    break;
                case ItemType.Slowdown:
                    if (_slowdownCoroutine != null) StopCoroutine(_slowdownCoroutine);
                    _slowdownCoroutine = StartCoroutine(SlowdownRoutine());
                    Debug.Log("슬로우다운 획득! 5초간 속도 감소");
                    break;
            }

            // 아이템 오브젝트 파괴
            if (itemObject != null)
            {
                Destroy(itemObject);
            }
        }

        private Coroutine _slowdownCoroutine; // 슬로우다운 코루틴 참조
        private IEnumerator SlowdownRoutine()
        {
            if (_player == null) yield break;
            // 파워다운 이펙트 활성화
            _player.SetPowerdownEffect(true);
            float originalSpeed = _player.forwardSpeed;
            _player.forwardSpeed = originalSpeed * 0.5f;
            Debug.Log($"🐢 Slowdown! 속도가 {_player.forwardSpeed}로 감소");
            yield return new WaitForSeconds(5f);
            _player.forwardSpeed = originalSpeed;
            // 파워다운 이펙트 비활성화
            _player.SetPowerdownEffect(false);
            Debug.Log($"🐢 Slowdown 종료! 속도가 {_player.forwardSpeed}로 복구");
            _slowdownCoroutine = null;
        }

        private IEnumerator SpeedBoostRoutine()
        {
            if (_player == null) yield break;

            // 오라 이펙트 활성화
            _player.SetSpeedBoostEffect(true);

            // 원래 forwardSpeed 저장
            float originalSpeed = _player.forwardSpeed;

            // forwardSpeed를 3배로 증가
            _player.forwardSpeed = originalSpeed * speedBoostMultiplier;

            // 5초간 대기
            yield return new WaitForSeconds(speedBoostDuration);

            // forwardSpeed를 원래 값으로 복구
            _player.forwardSpeed = originalSpeed;

            // 오라 이펙트 비활성화
            _player.SetSpeedBoostEffect(false);

            _speedBoostCoroutine = null; // 코루틴 참조 정리
        }

        private IEnumerator InvincibleRoutine()
        {
            if (_player == null) yield break;
            _player.isInvincible = true;
            // 무적 이펙트 활성화
            _player.SetInvincibleEffect(true);
            Debug.Log("무적 상태 ON");
            yield return new WaitForSeconds(5f); // 5초간 무적
            _player.isInvincible = false;
            // 무적 이펙트 비활성화
            _player.SetInvincibleEffect(false);
            Debug.Log("무적 상태 OFF");
        }

        private IEnumerator RocketRoutine()
        {
            if (_player == null) yield break;
            _player.isFlying = true;
            // 로켓 이펙트 활성화
            _player.SetRocketEffect(true);
            Debug.Log("비행 시작!");
            yield return new WaitForSeconds(5f);
            _player.isFlying = false;
            // 로켓 이펙트 비활성화
            _player.SetRocketEffect(false);
            Debug.Log("비행 종료!");
        }
    }
}