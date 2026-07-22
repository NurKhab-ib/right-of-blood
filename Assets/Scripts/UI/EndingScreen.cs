using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RightOfBlood.Prototype {
    /// <summary>Runtime-created terminal screen. It keeps the ending presentation independent from scene wiring.</summary>
    public sealed class EndingScreen : MonoBehaviour {
        private Canvas canvas;
        private Image panel;
        private TMP_Text titleText;
        private TMP_Text outcomeText;
        private Button restartButton;

        public void Show(GameOutcome outcome, string title, string summary, Action onRestart) {
            EnsureUi();
            var isDeath = outcome == GameOutcome.death;
            panel.color = isDeath ? new Color(0.16f, 0.018f, 0.025f, 0.98f) : new Color(0.025f, 0.075f, 0.065f, 0.98f);
            titleText.text = TextNormalizer.Normalize(isDeath ? "СМЕРТЬ" : "ПРАВО КРОВИ");
            outcomeText.text = TextNormalizer.Normalize(title + "\n\n" + summary + "\n\n" +
                (isDeath ? "Ваш род оборвался, но историю можно начать заново." : "Решение принято. Город будет жить с его ценой."));
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(() => onRestart?.Invoke());
            restartButton.GetComponentInChildren<TMP_Text>(true).text = "Начать заново";
            canvas.gameObject.SetActive(true);
        }

        private void EnsureUi() {
            if (canvas != null) return;
            var root = new GameObject("Canvas финала", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 2000;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var panelObject = new GameObject("Финальный экран", typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(canvas.transform, false);
            var panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;
            panel = panelObject.GetComponent<Image>();

            titleText = CreateText("Заголовок", panelObject.transform, new Vector2(170, 690), new Vector2(-170, -150), 58, TextAlignmentOptions.Center, FontStyles.Bold);
            outcomeText = CreateText("Описание исхода", panelObject.transform, new Vector2(300, 250), new Vector2(-300, -340), 31, TextAlignmentOptions.Center, FontStyles.Normal);
            restartButton = CreateButton(panelObject.transform);
        }

        private static TMP_Text CreateText(string name, Transform parent, Vector2 min, Vector2 max, float size, TextAlignmentOptions alignment, FontStyles style) {
            var value = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            value.transform.SetParent(parent, false);
            var rect = value.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = min;
            rect.offsetMax = max;
            var text = value.GetComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.enableWordWrapping = true;
            text.color = new Color(0.95f, 0.91f, 0.78f);
            return text;
        }

        private static Button CreateButton(Transform parent) {
            var value = new GameObject("Начать заново", typeof(RectTransform), typeof(Image), typeof(Button));
            value.transform.SetParent(parent, false);
            var rect = value.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(360, 78);
            rect.anchoredPosition = new Vector2(960, 115);
            var image = value.GetComponent<Image>();
            image.color = new Color(0.62f, 0.18f, 0.12f, 1f);
            var button = value.GetComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(0.82f, 0.3f, 0.2f, 1f);
            button.colors = colors;
            var label = CreateText("Текст", value.transform, Vector2.zero, Vector2.zero, 27, TextAlignmentOptions.Center, FontStyles.Bold);
            label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
            return button;
        }
    }
}