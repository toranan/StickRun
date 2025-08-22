using UnityEngine;
using TMPro; // TextMeshPro 사용을 위해 추가

namespace BananaRun.Runner
{
    // 화면 좌측 상단에 1초마다 증가하는 점수를 표시 (TextMeshPro 사용)
    public class ScoreUI : MonoBehaviour
    {
        [Header("Style")]
        public float fontSize = 100f;
        public Color fontColor = Color.white;
        public Vector2 margin = new Vector2(60f, 16f); // 좌, 상 여백(px)

        private TextMeshProUGUI _text;
        private RectTransform _rect;
        private int _lastScore = -1;
        private Rect _lastSafeArea;

        private void Awake()
        {
            EnsureCanvasAndText();
            ApplySafeArea();

            // 초기에는 텍스트 비활성화
            if (_text != null) _text.gameObject.SetActive(false);
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            if (gm != null)
            {
                // 게임이 Playing 상태일 때만 점수 업데이트 및 표시
                if (gm.currentGameState == GameManager.GameState.Playing)
                {
                    if (!_text.gameObject.activeSelf)
                    {
                        _text.gameObject.SetActive(true);
                    }
                    if (gm.score != _lastScore)
                    {
                        _lastScore = gm.score;
                        _text.text = _lastScore.ToString();
                    }
                }
                else // 게임이 Playing 상태가 아니면 점수 숨기기
                {
                    if (_text.gameObject.activeSelf)
                    {
                        _text.gameObject.SetActive(false);
                    }
                }
            }

            // 런타임 회전에 따른 세이프에어리어 변화 대응
            if (_lastSafeArea != Screen.safeArea)
            {
                ApplySafeArea();
            }
        }

        private void EnsureCanvasAndText()
        {
            var canvas = gameObject.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                // CanvasScaler는 TextMeshPro와 잘 동작하므로 그대로 둡니다.
                var scaler = gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
                scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                scaler.matchWidthOrHeight = 1f; // 세로 기준

                gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }

            var textGO = new GameObject("ScoreText_Runtime", typeof(RectTransform));
            textGO.transform.SetParent(transform, false);
            _rect = textGO.GetComponent<RectTransform>();
            _rect.anchorMin = new Vector2(0f, 1f);   // 좌상단 고정
            _rect.anchorMax = new Vector2(0f, 1f);
            _rect.pivot = new Vector2(0f, 1f);

            // Text 대신 TextMeshProUGUI를 추가합니다.
            _text = textGO.AddComponent<TextMeshProUGUI>();

            // TextMeshPro 폰트 에셋을 로드합니다.
            // 중요: 이전에 안내드린 대로 'Cafe24PROUP' 폰트 파일로 Font Asset을 생성해야 합니다.
            // 생성된 Font Asset의 이름이 'Cafe24PROUP SDF'라고 가정합니다.
            TMP_FontAsset loadedFont = Resources.Load<TMP_FontAsset>("Cafe24PROUP SDF");
            if (loadedFont != null)
            {
                _text.font = loadedFont;
            }
            else
            {
                Debug.LogError("Failed to load TMP_FontAsset: Cafe24PROUP SDF. Make sure it's in a Resources folder and you've created the Font Asset.");
                // TMP 기본 폰트로 대체
            }

            _text.fontSize = fontSize;
            _text.color = fontColor;
            _text.alignment = TextAlignmentOptions.TopLeft;
            _text.enableWordWrapping = false; // 자동 줄바꿈 비활성화
        }

        private void ApplySafeArea()
        {
            _lastSafeArea = Screen.safeArea;
            if (_rect == null) return;

            // 좌상단 기준 마진 + 세이프에어리어 오프셋 적용
            float left = _lastSafeArea.xMin;
            float top = Screen.height - _lastSafeArea.yMax;
            _rect.anchoredPosition = new Vector2(left + margin.x, -top - margin.y);
        }
    }
}
