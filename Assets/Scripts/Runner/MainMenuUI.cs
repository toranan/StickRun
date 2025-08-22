using UnityEngine;
using UnityEngine.UI; // UI 요소 사용을 위해 추가
using TMPro; // TextMeshPro 사용을 위해 추가

namespace BananaRun.Runner
{
    public class MainMenuUI : MonoBehaviour
    {
        public Button startButton; // 시작 버튼
        public GameObject startButtonPanel; // 시작 버튼을 포함하는 UI 패널 (숨기거나 보여줄 용도)
        public TextMeshProUGUI countdownScoreText; // 카운트다운 및 점수 표시 텍스트

        private void Start()
        {
            // 시작 버튼 클릭 리스너 등록
            if (startButton != null)
            {
                startButton.onClick.AddListener(OnStartButtonClicked);
            }

            // GameManager에 UI 텍스트 연결
            if (GameManager.Instance != null && countdownScoreText != null)
            {
                GameManager.Instance.scoreText = countdownScoreText;
            }

            // 초기 UI 상태 설정
            if (startButtonPanel != null)
            {
                startButtonPanel.SetActive(true); // 시작 버튼 패널 활성화
            }
            if (countdownScoreText != null)
            {
                countdownScoreText.gameObject.SetActive(false); // 카운트다운/점수 텍스트 비활성화
            }
        }

        private void OnStartButtonClicked()
        {
            Debug.Log("시작 버튼 클릭됨!");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartGame();

                // 시작 버튼 숨기기
                if (startButtonPanel != null)
                {
                    startButtonPanel.SetActive(false);
                }
            }
        }
    }
}
