using System.Collections.Generic;
using RightOfBlood.Prototype;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RightOfBlood.Prototype {
    public sealed class GameConsole : MonoBehaviour {
        public static bool IsInputBlocked { get; private set; }
        [SerializeField] private QuestGame game;
        [SerializeField] private KeyCode toggleKey = KeyCode.Slash;

        private Canvas consoleCanvas;
        private GameObject panel;
        private TMP_Text output;
        private TMP_Text line;
        private TMP_Text closedHint;
        private Keyboard subscribedKeyboard;
        private readonly List<string> history = new List<string>();
        private string command = string.Empty;
        private bool isOpen;

        private void Awake() {
            CreateUi();
            SetOpen(false);
        }

        private void OnEnable() {
            SubscribeToKeyboard(Keyboard.current);
        }

        private void OnDisable() {
            if (subscribedKeyboard != null) subscribedKeyboard.onTextInput -= OnTextInput;
            subscribedKeyboard = null;
            IsInputBlocked = false;
        }

        private void Update() {
            var keyboard = Keyboard.current;
            SubscribeToKeyboard(keyboard);
            if (keyboard == null) return;
            if (!isOpen && keyboard.slashKey.wasPressedThisFrame) {
                SetOpen(true);
                return;
            }
            if (!isOpen) return;
            if (keyboard.escapeKey.wasPressedThisFrame) { SetOpen(false); return; }
            if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame) { Execute(); return; }
            if (keyboard.backspaceKey.wasPressedThisFrame && command.Length > 0) {
                command = command.Substring(0, command.Length - 1);
                RefreshLine();
            }
        }

        private void OnTextInput(char character) {
            if (!isOpen) {
                if (character == '/') SetOpen(true);
                return;
            }

            if (char.IsControl(character) || character == '/') return;
            command += character;
            RefreshLine();
        }

        private void CreateUi() {
            consoleCanvas = CreateConsoleCanvas();
            panel = new GameObject("Отладочная консоль", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(consoleCanvas.transform, false);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.02f, 0.02f);
            panelRect.anchorMax = new Vector2(0.50f, 0.29f);
            panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0.025f, 0.035f, 0.06f, 0.96f);

            output = CreateText("Output", panel.transform, new Vector2(16, 45), new Vector2(-16, -12), 20, TextAlignmentOptions.BottomLeft);
            line = CreateText("Line", panel.transform, new Vector2(16, 8), new Vector2(-16, 38), 23, TextAlignmentOptions.Left);
            closedHint = CreateText("Подсказка отладки", consoleCanvas.transform, new Vector2(16, 14), new Vector2(760, 50), 19, TextAlignmentOptions.BottomLeft);
            closedHint.text = "\"/\" для ввода команд";
            Print("/help, /quests <id_название>, /skills <билд_этап>");
        }

        private Canvas CreateConsoleCanvas() {
            var root = new GameObject("Canvas отладочной консоли", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 1000;

            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }
        private void SubscribeToKeyboard(Keyboard keyboard) {
            if (keyboard == subscribedKeyboard) return;
            if (subscribedKeyboard != null) subscribedKeyboard.onTextInput -= OnTextInput;
            subscribedKeyboard = keyboard;
            if (subscribedKeyboard != null) subscribedKeyboard.onTextInput += OnTextInput;
        }

        private static TMP_Text CreateText(string name, Transform parent, Vector2 min, Vector2 max, float fontSize, TextAlignmentOptions alignment) {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = min;
            rect.offsetMax = max;
            var text = go.GetComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = fontSize;
            text.enableWordWrapping = true;
            text.alignment = alignment;
            text.color = new Color(0.78f, 0.92f, 1f);
            return text;
        }

        private void Execute() {
            var submitted = command.Trim();
            command = string.Empty;
            RefreshLine();
            if (string.IsNullOrEmpty(submitted)) return;
            Print("> " + submitted);

            var normalized = submitted.TrimStart('/').Trim();
            var separator = normalized.IndexOf(' ');
            var verb = (separator < 0 ? normalized : normalized.Substring(0, separator)).ToLowerInvariant();
            var argument = separator < 0 ? string.Empty : normalized.Substring(separator + 1).Trim();
            if (verb == "help") {
                Print("Квесты: 1_document, 2_council, 3_scaling, 4_progression, 5_epidemic, 6_final. Этапы: magistrate_1..3, council_1..3, mafia_1..3.");
                return;
            }

            game ??= FindFirstObjectByType<QuestGame>();
            if (game == null) {
                Print("\u0418\u0433\u0440\u0430 \u0435\u0449\u0451 \u043d\u0435 \u0438\u043d\u0438\u0446\u0438\u0430\u043b\u0438\u0437\u0438\u0440\u043e\u0432\u0430\u043d\u0430.");
                return;
            }

            if (verb == "quests") {
                Print(game.RunConsoleQuest(argument));
                return;
            }
            if (verb == "skills") {
                Print(game.RunConsoleStage(argument));
                return;
            }
            Print("\u041d\u0435\u0438\u0437\u0432\u0435\u0441\u0442\u043d\u0430\u044f \u043a\u043e\u043c\u0430\u043d\u0434\u0430. \u0418\u0441\u043f\u043e\u043b\u044c\u0437\u0443\u0439\u0442\u0435 /help.");
        }

        private void SetOpen(bool value) {
            isOpen = value;
            IsInputBlocked = value;
            if (value) FindFirstObjectByType<ProgressionUIHandler>()?.CloseAllPanels();
            if (panel != null) panel.SetActive(value);
            if (closedHint != null) closedHint.gameObject.SetActive(!value);
            if (!value) return;
            RefreshLine();
        }

        private void RefreshLine() {
            if (line != null) line.text = "> " + command + "_";
        }

        private void Print(string value) {
            history.Add(TextNormalizer.Normalize(value));
            if (history.Count > 5) history.RemoveAt(0);
            if (output != null) output.text = string.Join("\n", history);
        }

    }
}
