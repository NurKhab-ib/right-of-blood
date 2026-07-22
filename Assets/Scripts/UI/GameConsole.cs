using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RightOfBlood.Prototype {
    public sealed class GameConsole : MonoBehaviour {
        private static GameConsole activeConsole;
        public static bool IsInputBlocked => activeConsole != null && activeConsole.isOpen;
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
        private float nextBackspaceRepeatTime;
        private const float BackspaceRepeatDelay = 0.35f;
        private const float BackspaceRepeatInterval = 0.055f;

        private void Awake() {
            activeConsole = this;
            CreateUi();
            SetOpen(false);
        }

        private void OnEnable() {
            SubscribeToKeyboard(Keyboard.current);
        }

        private void OnDisable() {
            if (subscribedKeyboard != null) subscribedKeyboard.onTextInput -= OnTextInput;
            subscribedKeyboard = null;
            isOpen = false;
            if (activeConsole == this) activeConsole = null;
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
            if (keyboard.tabKey.wasPressedThisFrame) { TryAutoComplete(); return; }
            HandleBackspace(keyboard);
        }

        private void HandleBackspace(Keyboard keyboard) {
            if (keyboard.backspaceKey.wasPressedThisFrame) {
                RemoveLastCharacter();
                nextBackspaceRepeatTime = Time.unscaledTime + BackspaceRepeatDelay;
                return;
            }

            if (!keyboard.backspaceKey.isPressed) {
                nextBackspaceRepeatTime = 0f;
                return;
            }

            if (command.Length > 0 && Time.unscaledTime >= nextBackspaceRepeatTime) {
                RemoveLastCharacter();
                nextBackspaceRepeatTime = Time.unscaledTime + BackspaceRepeatInterval;
            }
        }

        private void RemoveLastCharacter() {
            if (command.Length == 0) return;
            command = command.Substring(0, command.Length - 1);
            RefreshLine();
        }

        private void TryAutoComplete() {
            var entered = command.Trim();
            if (entered.IndexOf(' ') < 0) {
                if (string.Equals(entered, "quests", StringComparison.OrdinalIgnoreCase)) {
                    ApplyCompletion("quests 1_document");
                    return;
                }
                if (string.Equals(entered, "skills", StringComparison.OrdinalIgnoreCase)) {
                    ApplyCompletion("skills magistrate_1");
                    return;
                }
                if (string.Equals(entered, "help", StringComparison.OrdinalIgnoreCase)) {
                    ApplyCompletion("help");
                    return;
                }

                if (string.Equals(entered, "restart", StringComparison.OrdinalIgnoreCase)) {
                    ApplyCompletion("restart");
                    return;
                }
                CycleMatchingCompletion(entered, new[] { "quests", "skills", "restart", "help" });
                return;
            }

            var commandOptions = entered.StartsWith("quests", StringComparison.OrdinalIgnoreCase)
                ? new[] { "quests 1_document", "quests 2_council", "quests 3_scaling", "quests 4_progression", "quests 5_epidemic", "quests 6_final" }
                : entered.StartsWith("skills", StringComparison.OrdinalIgnoreCase)
                    ? new[] { "skills magistrate_1", "skills magistrate_2", "skills magistrate_3", "skills council_1", "skills council_2", "skills council_3", "skills mafia_1", "skills mafia_2", "skills mafia_3" }
                    : System.Array.Empty<string>();
            CycleMatchingCompletion(entered, commandOptions);
        }

        private void CycleMatchingCompletion(string entered, string[] options) {
            if (options == null || options.Length == 0) return;

            for (var i = 0; i < options.Length; i++) {
                if (string.Equals(options[i], entered, StringComparison.OrdinalIgnoreCase)) {
                    ApplyCompletion(options[(i + 1) % options.Length]);
                    return;
                }
            }

            for (var i = 0; i < options.Length; i++) {
                if (options[i].StartsWith(entered, StringComparison.OrdinalIgnoreCase)) {
                    ApplyCompletion(options[i]);
                    return;
                }
            }
        }
        private void ApplyCompletion(string value) {
            command = value;
            RefreshLine();
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
            text.raycastTarget = false;
            text.alignment = alignment;
            text.color = new Color(0.78f, 0.92f, 1f);
            return text;
        }

        private void Execute() {
            var submitted = command.Trim();
            command = string.Empty;
            RefreshLine();
            if (string.IsNullOrEmpty(submitted)) return;
            Print("> /" + submitted);

            var normalized = submitted.TrimStart('/').Trim();
            var separator = normalized.IndexOf(' ');
            var verb = (separator < 0 ? normalized : normalized.Substring(0, separator)).ToLowerInvariant();
            var argument = separator < 0 ? string.Empty : normalized.Substring(separator + 1).Trim();
            if (verb == "help") {
                Print("Квесты: 1_document, 2_council, 3_scaling, 4_progression, 5_epidemic, 6_final. Этапы: magistrate_1..3, council_1..3, mafia_1..3. /restart - начать с нуля.");
                return;
            }

            game ??= FindFirstObjectByType<QuestGame>();
            if (game == null) {
                Print("\u0418\u0433\u0440\u0430 \u0435\u0449\u0451 \u043d\u0435 \u0438\u043d\u0438\u0446\u0438\u0430\u043b\u0438\u0437\u0438\u0440\u043e\u0432\u0430\u043d\u0430.");
                return;
            }

            if (verb == "restart") {
                Print("Прогресс сброшен. Новая история начинается.");
                game.RestartStory();
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
            if (value) FindFirstObjectByType<ProgressionUIHandler>()?.CloseAllPanels();
            if (panel != null) panel.SetActive(value);
            if (closedHint != null) closedHint.gameObject.SetActive(!value);
            if (!value) return;
            RefreshLine();
        }

        private void RefreshLine() {
            if (line != null) line.text = "> /" + command + "_";
        }

        private void Print(string value) {
            history.Add(TextNormalizer.Normalize(value));
            if (history.Count > 5) history.RemoveAt(0);
            if (output != null) output.text = string.Join("\n", history);
        }

    }
}
