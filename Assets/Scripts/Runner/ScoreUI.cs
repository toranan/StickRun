using UnityEngine;
using UnityEngine.UI;

namespace BananaRun.Runner
{
    // 화면 좌측 상단에 1초마다 증가하는 점수를 표시
    public class ScoreUI : MonoBehaviour
    {
        [Header("Style")]
        public int fontSize = 36;
        public Color fontColor = Color.white;
        public Vector2 margin = new Vector2(16f, 16f); // 좌, 상 여백(px)

        private Text _text;
        private RectTransform _rect;
        private int _lastScore = -1;
        private Rect _lastSafeArea;

        private void Awake()
        {
            EnsureCanvasAndText();
            ApplySafeArea();
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            if (gm != null && gm.score != _lastScore)
            {
                _lastScore = gm.score;
                _text.text = _lastScore.ToString();
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

                var scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                scaler.matchWidthOrHeight = 1f; // 세로 기준

                gameObject.AddComponent<GraphicRaycaster>();
            }

            var textGO = new GameObject("ScoreText", typeof(RectTransform));
            textGO.transform.SetParent(transform, false);
            _rect = textGO.GetComponent<RectTransform>();
            _rect.anchorMin = new Vector2(0f, 1f);   // 좌상단 고정
            _rect.anchorMax = new Vector2(0f, 1f);
            _rect.pivot = new Vector2(0f, 1f);

            _text = textGO.AddComponent<Text>();
            _text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _text.fontSize = fontSize;
            _text.color = fontColor;
            _text.alignment = TextAnchor.UpperLeft;
            _text.horizontalOverflow = HorizontalWrapMode.Overflow;
            _text.verticalOverflow = VerticalWrapMode.Overflow;

            // 그림자 가독성
            var shadow = textGO.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);
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


