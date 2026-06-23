using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RightOfBlood.Prototype {
    public sealed class IntroQuestGame : MonoBehaviour {
        public const float DefaultInteractionRange = 1.2f;

        [Header("Manual Scene Setup")] [SerializeField]
        private LocationId initialLocation = LocationId.office;

        [SerializeField] private string initialSpawnId = "default";
        [SerializeField] private float interactionRange = DefaultInteractionRange;

        [Header("Manual UI")] [SerializeField] private PrototypeQuestUi questUi;

        private readonly List<PrototypeLocation> locations = new List<PrototypeLocation>();
        private readonly List<PrototypeInteractable> interactables = new List<PrototypeInteractable>();
        private readonly List<DialogueChoice> activeChoices = new List<DialogueChoice>();

        private IntroQuestState state;
        private PrototypePlayerController player;
        private PrototypeInteractable nearestInteractable;
        private bool dialogueOpen;
        private string currentPrompt;

        private void Awake() {
            state = new IntroQuestState();
        }

        private void Start() {
            RefreshSceneReferences();
            if (questUi == null) questUi = FindFirstObjectByType<PrototypeQuestUi>();
            if (questUi != null) questUi.HideDialogue();
            LoadLocation(initialLocation, initialSpawnId);
            RefreshUi();
        }

        private void Update() {
            if (player == null) player = FindFirstObjectByType<PrototypePlayerController>();
            if (questUi == null) questUi = FindFirstObjectByType<PrototypeQuestUi>();

            if (player != null) {
                player.CanMove = !dialogueOpen;
                UpdateInteractionPrompt();
            }

            var keyboard = Keyboard.current;
            if (keyboard != null) {
                if (dialogueOpen) {
                    if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame) Choose(0);
                    if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame) Choose(1);
                    if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame) Choose(2);
                    if (keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame) Choose(3);
                }
                else if (keyboard.eKey.wasPressedThisFrame && nearestInteractable != null) {
                    Interact(nearestInteractable);
                }
            }

            RefreshUi();
        }

        public void RefreshSceneReferences() {
            locations.Clear();
            locations.AddRange(
                FindObjectsByType<PrototypeLocation>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            interactables.Clear();
            interactables.AddRange(
                FindObjectsByType<PrototypeInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            player = FindFirstObjectByType<PrototypePlayerController>();
        }

        public void LoadLocation(LocationId locationId, string spawnId = "default") {
            RefreshSceneReferences();

            PrototypeLocation targetLocation = null;
            foreach (var location in locations) {
                if (location == null) continue;
                var isTarget = location.Id == locationId;
                if (location.HideWhenInactive) location.gameObject.SetActive(isTarget);
                if (isTarget) targetLocation = location;
            }

            if (targetLocation == null) {
                Debug.LogWarning($"Location {locationId} not found. Create a GameObject with PrototypeLocation.");
                return;
            }

            if (player != null) {
                var spawn = targetLocation.GetSpawn(spawnId);
                if (spawn != null) player.transform.position = spawn.position;
                player.MovementBounds = targetLocation.MovementBounds;
                player.UseBounds = targetLocation.UseMovementBounds;
            }

            nearestInteractable = null;
            currentPrompt = string.Empty;
        }

        public void Interact(PrototypeInteractable interactable) {
            switch (interactable.Kind) {
                case PrototypeInteractionKind.missing_document_desk: InspectDesk(); break;
                case PrototypeInteractionKind.chief: TalkToChief(); break;
                case PrototypeInteractionKind.archive_guard: TalkToArchiveGuard(); break;
                case PrototypeInteractionKind.council_scholar: TalkToCouncilScholar(); break;
                case PrototypeInteractionKind.mafia_fixer: TalkToMafiaFixer(); break;
                case PrototypeInteractionKind.former_archivist: TalkToFormerArchivist(); break;
                case PrototypeInteractionKind.archive_shelf: InspectArchiveShelf(); break;
                case PrototypeInteractionKind.archive_investigator: TalkToArchiveInvestigator(); break;
                case PrototypeInteractionKind.black_archive_door: TrySoloAccess(); break;
                case PrototypeInteractionKind.door:
                    LoadLocation(interactable.TargetLocation, interactable.TargetSpawnId); break;
                default: throw new ArgumentOutOfRangeException();
            }
        }

        private void InspectDesk() {
            if (state.Stage != QuestStage.inspect_missing_document) {
                ShowMessage("Стол", "На месте пропавшей бумаги остался лишь пустой след. Протрите с него пыль, что ли..");
                return;
            }

            state.IntroQuestStarted = true;
            state.Stage = QuestStage.talk_to_chief;
            ShowDialogue("Пропавший документ",
                "В отделе, где числится магистрат, исчез лист. В журнале нет записи о выдаче. Кто-то явно хочет, чтобы пропажу не заметили!..",
                new[] { new DialogueChoice("Поговорить с начальником", CloseDialogue) });
        }

        private void TalkToChief() {
            if (state.Stage == QuestStage.inspect_missing_document) {
                ShowMessage("Начальник отдела",
                    "Сначала осмотрите стол: пропажа должна быть очевидна самому магистрату.");
                return;
            }

            if (state.Stage != QuestStage.talk_to_chief) {
                ShowMessage("Начальник отдела",
                    "Не поднимайте шум! Чем меньше людей знает о пропаже, тем спокойнее будет отдел.");
                return;
            }

            ShowDialogue("Начальник отдела",
                "Документ не числится утраченным! Не вижу причины тревожить архив. Вернитесь к своей работе, магистрат.",
                new[] {
                    new DialogueChoice("Расследовать самостоятельно", () => {
                        state.Stage = QuestStage.choose_archive_access;
                        state.OfficialInfluence += 1;
                        ShowMessage("Решение",
                            "Вы решаете выяснить, почему начальник скрывает пропажу. Найдите, как пройти в рабочий архив!");
                    }),
                    new DialogueChoice("Не расследовать", () => {
                        state.IgnoredFirstHook = true;
                        state.Stage = QuestStage.choose_archive_access;
                        state.ThreatLevel += 1;
                        ShowMessage("X days later..",
                            "Магистрат всячески игнорирует пропажу, но след документа всплывает снова. Теперь промедление выглядит опаснее.");
                    })
                });
        }

        private void TalkToArchiveGuard() {
            if (state.Stage == QuestStage.completed) {
                ShowMessage("Охрана архива", "После ночного происшествия сюда никого не пускают без двойной подписи.");
                return;
            }

            if (state.CanEnterRestrictedArchive) {
                ShowMessage("Охрана архива",
                    "Ваш допуск принят. Не задерживайтесь у закрытых полок дольше положенного.");
                return;
            }

            ShowDialogue("Охрана архива",
                "Рабочий архив закрыт. Нужен допуск, распоряжение Совета или человек, который умеет открывать двери без вопросов.",
                new[] {
                    new DialogueChoice("Подать служебный запрос", () => {
                        state.Access = AccessMethod.official_blocked;
                        state.OfficialAttemptBlocked = true;
                        state.OfficialInfluence -= 1;
                        ShowMessage("Служебный путь заблокирован",
                            "Начальник отдела отклоняет запрос по собственным мотивам. Документ не найден, служебный путь закрыт.");
                    }),
                    new DialogueChoice("Отойти", CloseDialogue)
                });
        }

        private void TalkToCouncilScholar() {
            if (state.DocumentFound) {
                ShowMessage("Учёный Совета",
                    state.CouncilHasCopy
                        ? "Совет уже изучает копию. Ваш род упоминался в старой кровной таблице.."
                        : "Если документ ещё у вас, Совет готов обсудить его позже.");
                return;
            }

            ShowDialogue("Учёный Совета",
                "Совет осведомлён о тёмной стороне городских архивов. Мы дадим вам пропуск, но копия найденного листа должна попасть к нам.",
                new[] {
                    new DialogueChoice("Принять помощь Совета", () => {
                        state.Access = AccessMethod.council;
                        state.Stage = QuestStage.find_document_in_archive;
                        state.CanEnterRestrictedArchive = true;
                        state.CouncilReputation += 2;
                        state.MafiaReputation -= 1;
                        state.ThreatLevel += 1;
                        ShowMessage("Доступ Совета получен",
                            "Учёный выдаёт временный пропуск в архив - за это Совет получит копию документа.");
                    }),
                    new DialogueChoice("Отказаться", CloseDialogue)
                });
        }

        private void TalkToMafiaFixer() {
            if (state.DocumentFound) {
                ShowMessage("Посредник мафии",
                    state.MafiaHasCopy
                        ? "Копия уже ушла по нужным рукам. Теперь вам рады на наших улицах, магистрат."
                        : "Без копии вы нам не интересны.");
                return;
            }

            ShowDialogue("Посредник мафии",
                "Нам известен архивариус, который продаёт тишину дешевле, чем совесть.. Мы проведём вас внутрь, но документ не останется только вашим.",
                new[] {
                    new DialogueChoice("Принять помощь мафии", () => {
                        state.Access = AccessMethod.mafia;
                        state.Stage = QuestStage.find_document_in_archive;
                        state.CanEnterRestrictedArchive = true;
                        state.MafiaReputation += 2;
                        state.CouncilReputation -= 1;
                        state.ThreatLevel += 1;
                        ShowMessage("Доступ мафии получен",
                            "Посредник даёт знак архивариусу - за это мафия получит копию документа.");
                    }),
                    new DialogueChoice("Отказаться", CloseDialogue)
                });
        }

        private void TalkToFormerArchivist() {
            if (state.DocumentFound) {
                ShowMessage("Бывший работник архива", "После такого следа охрана начнёт считать каждый ключ.");
                return;
            }

            ShowDialogue("Бывший архивный работник",
                "У старого архива есть чёрный ход. Снаружи он выглядит как кладовая. Внутри - прямой коридор к закрытым полкам.",
                new[] {
                    new DialogueChoice("Запомнить путь", () => {
                        state.ThreatLevel += 1;
                        ShowMessage("Зацепка",
                            "Теперь можно попробовать самостоятельный доступ через чёрный ход у здания архива.");
                    }),
                    new DialogueChoice("Уйти", CloseDialogue)
                });
        }

        private void TrySoloAccess() {
            if (state.DocumentFound) {
                ShowMessage("Чёрный ход", "Дверь уже опечатана после происшествия.");
                return;
            }

            ShowDialogue("Чёрный ход архива",
                "Можно войти, не имея дело ни с кем - документ останется только у вас, но следы проникновения будет трудно скрыть.",
                new[] {
                    new DialogueChoice("Проникнуть самостоятельно", () => {
                        state.Access = AccessMethod.solo;
                        state.Stage = QuestStage.find_document_in_archive;
                        state.CanEnterRestrictedArchive = true;
                        state.PlayerOnlyAccess = true;
                        state.ThreatLevel += 2;
                        CloseDialogue();
                        LoadLocation(LocationId.archive, "solo");
                    }),
                    new DialogueChoice("Отступить", CloseDialogue)
                });
        }

        private void InspectArchiveShelf() {
            if (state.DocumentFound) {
                ShowMessage("Секция утерянного документа",
                    "Пустое место на полке уже не кажется случайностью. Кто-то заметит пропажу очень скоро.");
                return;
            }

            if (!state.CanEnterRestrictedArchive) {
                ShowMessage("Секция утерянного документа",
                    "Охрана не даёт приблизиться к закрытым полкам. Нужен доступ через Совет, мафию или чёрный ход.");
                return;
            }

            state.Stage = QuestStage.completed;
            state.DocumentFound = true;
            state.BloodKnowledgeUnlocked = true;
            state.ArchiveInvestigationStarted = true;

            if (state.Access == AccessMethod.council) {
                state.CopyCreated = true;
                state.CouncilHasCopy = true;
                state.Owner = DocumentOwner.council;
                state.CouncilReputation += 1;
            }
            else if (state.Access == AccessMethod.mafia) {
                state.CopyCreated = true;
                state.MafiaHasCopy = true;
                state.Owner = DocumentOwner.mafia;
                state.MafiaReputation += 1;
            }
            else {
                state.Owner = DocumentOwner.player;
                state.PlayerOnlyAccess = true;
                state.CouncilReputation -= 1;
                state.MafiaReputation -= 1;
                state.ThreatLevel += 1;
            }

            ShowDialogue("Пропавший архивный документ", GetCompletionText(),
                new[] { new DialogueChoice("Завершить квест", CloseDialogue) });
        }

        private void TalkToArchiveInvestigator() {
            ShowMessage("Следователь архива",
                "Охрана заметила проникновение! Расследование началось, вопрос - кто первым получит правду о вашей крови?");
        }

        private string GetCompletionText() {
            if (state.Access == AccessMethod.council) {
                return
                    "Вы находите документ и создаёте копию для Совета. Совет получает доступ к знанию, а мафия начинает угрожать.";
            }

            if (state.Access == AccessMethod.mafia) {
                return
                    "Вы находите документ и создаёте копию для мафии. Мафия получает рычаг влияния, вам открывается криминальный маршрут города.";
            }

            return
                "Вы сохраняете документ только у себя. Копия не создана, фракции не получают прямого доступа, но давление обеих сторон растёт.";
        }

        private string GetObjectiveText() {
            switch (state.Stage) {
                case QuestStage.inspect_missing_document:
                    return "Осмотрите рабочий стол и след пропавшего документа.";
                case QuestStage.talk_to_chief:
                    return "Поговорите с начальником отдела.";
                case QuestStage.choose_archive_access:
                    return state.OfficialAttemptBlocked
                        ? "Служебный запрос заблокирован. Выберите другой доступ: Совет, мафия или чёрный ход."
                        : "Цель: проникнуть в рабочий архив. Доступ: служебный запрос, Совет, мафия или чёрный ход.";
                case QuestStage.find_document_in_archive:
                    return "Найдите секцию утерянного документа в рабочем архиве.";
                case QuestStage.completed:
                    return "Квест завершён. Расследование по делу взлома архива началось.";
                default:
                    return "Начните вводный квест.";
            }
        }

        private void UpdateInteractionPrompt() {
            nearestInteractable = null;
            var nearestDistance = float.MaxValue;
            var playerPosition = player.transform.position;

            foreach (var interactable in interactables) {
                if (interactable == null || !interactable.gameObject.activeInHierarchy) continue;
                var distance = Vector2.Distance(playerPosition, interactable.transform.position);
                if (distance < interactionRange && distance < nearestDistance) {
                    nearestDistance = distance;
                    nearestInteractable = interactable;
                }
            }

            currentPrompt = nearestInteractable == null ? string.Empty : $"[ E ]";
        }

        private void ShowMessage(string newSpeaker, string text) {
            ShowDialogue(newSpeaker, text, new[] { new DialogueChoice("Продолжить", CloseDialogue) });
        }

        private void ShowDialogue(string speaker, string text, IReadOnlyList<DialogueChoice> choices) {
            activeChoices.Clear();
            activeChoices.AddRange(choices);
            dialogueOpen = true;

            var choiceTexts = new string[activeChoices.Count];
            for (var i = 0; i < activeChoices.Count; i++) {
                choiceTexts[i] = activeChoices[i].Text;
            }

            if (questUi) {
                questUi.ShowDialogue(speaker, text, choiceTexts, Choose);
            }
            else {
                Debug.LogWarning("PrototypeQuestUi null");
            }
        }

        private void CloseDialogue() {
            dialogueOpen = false;
            activeChoices.Clear();
            if (questUi != null) questUi.HideDialogue();
        }

        private void Choose(int index) {
            if (index < 0 || index >= activeChoices.Count) return;
            var action = activeChoices[index].Action;
            activeChoices.Clear();
            action?.Invoke();
        }

        private void RefreshUi() {
            if (questUi == null) return;
            var prompt = player == null ? "no Player w PrototypePlayerController" : currentPrompt;
            questUi.RefreshHud(state, GetObjectiveText(), prompt);
        }
    }
}