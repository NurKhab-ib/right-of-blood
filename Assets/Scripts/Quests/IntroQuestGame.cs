using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RightOfBlood.Prototype {
    public sealed class IntroQuestGame : MonoBehaviour {
        public const float DefaultInteractionRange = 1.2f;
        private const int RequiredOfficialInfluenceForCouncilLawPath = 2;
        private const int RequiredMafiaReputationForCouncilCriminalPath = -1;

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
        private LocationId currentLocation;

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
                RefreshWorldState();
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
            foreach (var interactable in interactables) {
                if (interactable != null) interactable.CaptureScenePlacement();
            }
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

            currentLocation = locationId;

            if (player != null) {
                var spawn = targetLocation.GetSpawn(spawnId);
                if (spawn != null) player.transform.position = spawn.position;
                player.MovementBounds = targetLocation.MovementBounds;
                player.UseBounds = targetLocation.UseMovementBounds;
            }

            RefreshWorldState();
            nearestInteractable = null;
            currentPrompt = string.Empty;
        }

        private void RefreshWorldState() {
            if (interactables.Count == 0) return;

            foreach (var interactable in interactables) {
                if (interactable == null) continue;

                switch (interactable.Kind) {
                    case PrototypeInteractionKind.council_scholar:
                        RouteCouncilScholar(interactable);
                        break;
                    case PrototypeInteractionKind.mafia_fixer:
                        RouteMafiaFixer(interactable);
                        break;
                    case PrototypeInteractionKind.former_archivist:
                        RouteFormerArchivist(interactable);
                        break;
                    case PrototypeInteractionKind.black_archive_door:
                        interactable.SetVisible(!state.DocumentFound);
                        break;
                }
            }
        }

        private void RouteCouncilScholar(PrototypeInteractable interactable) {
            if (ShouldScholarWaitAtCouncil()) {
                MoveCharacterHome(interactable, new Vector2(-4.7f, 0.6f));
                return;
            }

            if (ShouldScholarWaitInCity()) {
                MoveCharacterToCity(interactable, new Vector2(-4.7f, 0.6f));
                return;
            }

            interactable.SetVisible(false);
        }

        private void RouteMafiaFixer(PrototypeInteractable interactable) {
            if (ShouldFixerWaitInDarkStreets()) {
                MoveCharacterHome(interactable, new Vector2(3.7f, 1.2f));
                return;
            }

            if (ShouldFixerWaitInCity()) {
                MoveCharacterToCity(interactable, new Vector2(3.7f, 1.2f));
                return;
            }

            interactable.SetVisible(false);
        }

        private void RouteFormerArchivist(PrototypeInteractable interactable) {
            if (ShouldFormerArchivistWaitInCity()) {
                MoveCharacterToCity(interactable, new Vector2(-7.9f, 1.9f));
                return;
            }

            interactable.SetVisible(false);
        }

        private bool ShouldScholarWaitInCity() {
            return state.Stage == QuestStage.choose_archive_access && !state.DocumentFound && state.Access != AccessMethod.council;
        }

        private bool ShouldScholarWaitAtCouncil() {
            return state.Access == AccessMethod.council || state.CouncilHasCopy || state.CouncilQuestStage != CouncilQuestStage.locked;
        }

        private bool ShouldFixerWaitInCity() {
            return state.Stage == QuestStage.choose_archive_access && !state.DocumentFound && state.Access != AccessMethod.mafia;
        }

        private bool ShouldFixerWaitInDarkStreets() {
            return state.Access == AccessMethod.mafia || state.MafiaHasCopy || state.CouncilQuestStage == CouncilQuestStage.negotiate_with_mafia || state.CriminalWorldAccess;
        }

        private bool ShouldFormerArchivistWaitInCity() {
            return state.CouncilQuestStage == CouncilQuestStage.investigate_intrigue ||
                   (state.Stage == QuestStage.choose_archive_access && !state.DocumentFound);
        }

        private void MoveCharacterToCity(PrototypeInteractable interactable, Vector2 fallbackOffset) {
            var city = GetLocation(LocationId.city);
            if (interactable.ScenePlacementBelongsTo(city)) {
                interactable.MoveToScenePlacement();
                return;
            }

            interactable.MoveToLocation(city, "default", fallbackOffset);
        }

        private void MoveCharacterHome(PrototypeInteractable interactable, Vector2 fallbackOffset) {
            var home = GetLocation(interactable.TargetLocation);
            if (interactable.ScenePlacementBelongsTo(home)) {
                interactable.MoveToScenePlacement();
                return;
            }

            interactable.MoveToLocation(home, interactable.TargetSpawnId, fallbackOffset);
        }

        private PrototypeLocation GetLocation(LocationId locationId) {
            foreach (var location in locations) {
                if (location != null && location.Id == locationId) return location;
            }

            return null;
        }

        private void TryTravel(PrototypeInteractable door) {
            if (!CanTravelTo(door.TargetLocation, out var blockedReason)) {
                ShowMessage("Путь закрыт", blockedReason);
                return;
            }

            LoadLocation(door.TargetLocation, door.TargetSpawnId);
        }

        private bool CanTravelTo(LocationId targetLocation, out string blockedReason) {
            blockedReason = string.Empty;

            if (targetLocation == LocationId.council && !CanEnterCouncilLocation()) {
                blockedReason = "Здание Совета закрыто для обычного магистрата. Сначала найдите учёного Совета в городе и получите приглашение или доверие Совета.";
                return false;
            }

            if (targetLocation == LocationId.streets && !CanEnterDarkStreets()) {
                blockedReason = "В тёмные переулки без проводника лучше не идти. Сначала найдите посредника мафии в городе или получите криминальный повод для встречи.";
                return false;
            }

            return true;
        }

        private bool CanEnterCouncilLocation() {
            return state.Access == AccessMethod.council || state.CouncilReputation > 0 || state.CouncilHasCopy ||
                   state.CouncilQuestStage != CouncilQuestStage.locked || state.SecretLibraryAccess;
        }

        private bool CanEnterDarkStreets() {
            return state.Access == AccessMethod.mafia || state.MafiaReputation > 0 || state.MafiaHasCopy ||
                   state.CouncilQuestStage == CouncilQuestStage.negotiate_with_mafia || state.CriminalWorldAccess;
        }
        private static bool IsLegacyDepartmentEntrance(PrototypeInteractable interactable) {
            return interactable != null && interactable.Kind == PrototypeInteractionKind.archive_investigator &&
                   interactable.name == "Department";
        }
        public void Interact(PrototypeInteractable interactable) {
            if (IsLegacyDepartmentEntrance(interactable)) {
                LoadLocation(LocationId.office, "default");
                return;
            }

            switch (interactable.Kind) {
                case PrototypeInteractionKind.missing_document_desk: InspectDesk(); break;
                case PrototypeInteractionKind.chief: TalkToChief(); break;
                case PrototypeInteractionKind.council_scholar: TalkToCouncilScholar(); break;
                case PrototypeInteractionKind.mafia_fixer: TalkToMafiaFixer(); break;
                case PrototypeInteractionKind.former_archivist: TalkToFormerArchivist(); break;
                case PrototypeInteractionKind.archive_shelf: InspectArchiveShelf(); break;
                case PrototypeInteractionKind.archive_security: TalkToArchiveSecurity(); break;
                case PrototypeInteractionKind.archive_investigator: TalkToArchiveSecurity(); break;
                case PrototypeInteractionKind.black_archive_door: TrySoloAccess(); break;
                case PrototypeInteractionKind.door:
                    TryTravel(interactable); break;
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

        private void TalkToArchiveSecurity() {
            if (state.ArchiveSecurityAlerted) {
                ShowMessage("Охрана архива",
                    "Охрана заметила следы проникновения и начала внутреннюю проверку.");
                return;
            }

            if (state.CanEnterRestrictedArchive) {
                ShowMessage("Охрана архива",
                    "Ваш допуск принят. Не задерживайтесь у закрытых полок дольше положенного.");
                return;
            }

            ShowDialogue("Охрана архива",
                "Рабочий архив закрыт - требуется доступ.",
                new[] {
                    new DialogueChoice("Подать служебный запрос", () => {
                        state.Access = AccessMethod.official_blocked;
                        state.OfficialAttemptBlocked = true;
                        state.OfficialInfluence -= 1;
                        ShowMessage("Служебный путь заблокирован",
                            "Начальник отдела отклоняет запрос по собственным мотивам. Документ не найден.");
                    }),
                    new DialogueChoice("Отойти", CloseDialogue)
                });
        }

        private void TalkToCouncilScholar() {
            if (state.Stage == QuestStage.completed && TryUnlockCouncilQuest()) {
                TalkToCouncilProblem();
                return;
            }

            if (state.DocumentFound) {
                ShowMessage("Учёный Совета",
                    state.CouncilHasCopy
                        ? "Совет уже изучает копию. Ваш род упоминался в старой кровной таблице, но доступ к ней придётся заслужить."
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
            if (state.CouncilQuestStage == CouncilQuestStage.negotiate_with_mafia) {
                ShowDialogue("Посредник мафии",
                    "Совет просит защиты, когда сам задолжал улицам. Выполните для нас одно поручение - и наши люди станут вежливее.",
                    new[] {
                        new DialogueChoice("Выполнить поручение мафии", () => {
                            state.CouncilSolution = CouncilProblemSolution.criminal;
                            state.MafiaReputation += 2;
                            state.CouncilReputation -= 1;
                            state.CriminalWorldAccess = true;
                            state.ThreatLevel += 1;
                            FinishCouncilQuest("Мафия отзывает людей от здания Совета. Совет получает тишину, но понимает, что вы решили проблему чужими руками.");
                        }),
                        new DialogueChoice("Отказаться", CloseDialogue)
                    });
                return;
            }

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
            if (state.CouncilQuestStage == CouncilQuestStage.investigate_intrigue) {
                ShowDialogue("Бывший архивный работник",
                    "Совет - не жертва, а неудачный должник. Они подкупили людей мафии, получили охранные схемы района, а потом отказались платить.",
                    new[] {
                        new DialogueChoice("Забрать сведения для шантажа", () => {
                            state.CouncilBlackmailLeverage = true;
                            state.CouncilQuestStage = CouncilQuestStage.return_to_council;
                            ShowMessage("Компромат найден",
                                "Теперь можно вернуться к учёному Совета и потребовать доступ к тайной библиотеке без открытой войны фракций.");
                        }),
                        new DialogueChoice("Уйти", CloseDialogue)
                    });
                return;
            }

            if (state.DocumentFound) {
                ShowMessage("Бывший работник архива", "После такого следа охрана начнёт считать каждый ключ.");
                return;
            }

            ShowDialogue("Бывший архивный работник",
                "У старого архива есть чёрный ход. Снаружи он выглядит как кладовая. Внутри - прямой коридор к закрытым полкам.",
                new[] {
                    new DialogueChoice("Запомнить путь", () => {
                        state.BlackArchiveEntranceKnown = true;
                        state.ThreatLevel += 1;
                        ShowMessage("Зацепка",
                            "Теперь можно попробовать самостоятельный доступ через чёрный ход у здания архива.");
                    }),
                    new DialogueChoice("Уйти", CloseDialogue)
                });
        }

        private void TrySoloAccess() {
            if (!state.BlackArchiveEntranceKnown) {
                ShowMessage("Вход", "Магистрат не может пройти.");
                return;
            }

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
            state.ArchiveSecurityAlerted = true;

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

            TryUnlockCouncilQuest();
            ShowDialogue("Пропавший архивный документ", GetCompletionText(),
                new[] { new DialogueChoice("Завершить квест", CloseDialogue) });
        }

        private bool TryUnlockCouncilQuest() {
            if (state.CouncilQuestStage != CouncilQuestStage.locked) return true;
            if (state.Stage != QuestStage.completed || !state.CouncilHasCopy || !state.BloodKnowledgeUnlocked) return false;

            state.CouncilQuestStage = CouncilQuestStage.choose_solution;
            return true;
        }

        private void TalkToCouncilProblem() {
            switch (state.CouncilQuestStage) {
                case CouncilQuestStage.choose_solution:
                    ShowCouncilSolutionChoice();
                    break;
                case CouncilQuestStage.negotiate_with_mafia:
                    ShowMessage("Учёный Совета",
                        "Пока мафия держит район в страхе, вход в тайную библиотеку останется закрытым. Найдите посредника и договоритесь.");
                    break;
                case CouncilQuestStage.investigate_intrigue:
                    ShowMessage("Учёный Совета",
                        "Если вы выбрали путь интриг, ищите не на кафедрах, а среди тех, кто помнит грязные сделки архива.");
                    break;
                case CouncilQuestStage.return_to_council:
                    ShowDialogue("Учёный Совета",
                        "Вы нашли причину давления мафии? Совет слушает очень внимательно.",
                        new[] {
                            new DialogueChoice("Потребовать доступ за молчание", () => {
                                state.CouncilSolution = CouncilProblemSolution.intrigue;
                                FinishCouncilQuest("Компромат заставляет Совет открыть тайную библиотеку. Фракции публично остаются в равновесии, а награда получена шантажом.");
                            }),
                            new DialogueChoice("Уйти", CloseDialogue)
                        });
                    break;
                case CouncilQuestStage.completed:
                    ShowMessage("Тайная библиотека",
                        "Совет уже открыл вам закрытый зал. Новая ветвь магии крови закреплена в ваших знаниях.");
                    break;
                default:
                    ShowMessage("Учёный Совета", "Совет не готов обсуждать тайную библиотеку без найденной архивной копии.");
                    break;
            }
        }

        private void ShowCouncilSolutionChoice() {
            ShowDialogue("Проблема Совета",
                "У здания Совета орудуют люди мафии. Совет обещает доступ к тайной библиотеке, если вы обеспечите району безопасность.",
                new[] {
                    new DialogueChoice("Перенаправить патрули стражи", TryLawCouncilSolution),
                    new DialogueChoice("Договориться с мафией", TryCriminalCouncilSolution),
                    new DialogueChoice("Выяснить истинную причину", () => {
                        state.CouncilSolution = CouncilProblemSolution.intrigue;
                        state.CouncilQuestStage = CouncilQuestStage.investigate_intrigue;
                        ShowMessage("Путь интриг",
                            "Вы начинаете выяснять причину травли со стороны мафии. Кажется, нужен человек, который знает о тёмных сделках Совета.");
                    }),
                    new DialogueChoice("Решить позже", CloseDialogue)
                });
        }

        private void TryLawCouncilSolution() {
            if (state.OfficialInfluence < RequiredOfficialInfluenceForCouncilLawPath) {
                ShowMessage("Недостаточно влияния",
                    "Чтобы снять патрули с других районов и поставить их у здания Совета, нужно больше служебного влияния.");
                return;
            }

            state.CouncilSolution = CouncilProblemSolution.law;
            state.CouncilDistrictSecured = true;
            state.CouncilReputation += 2;
            state.MafiaReputation -= 2;
            state.OfficialInfluence -= 1;
            state.OtherDistrictSafety -= 1;
            FinishCouncilQuest("Патрули защищают район Совета от преступников. Совет благодарен, мафия злится, а безопасность других улиц проседает.");
        }

        private void TryCriminalCouncilSolution() {
            if (state.MafiaReputation < RequiredMafiaReputationForCouncilCriminalPath) {
                ShowMessage("Недостаточно контактов",
                    "Мафия не станет говорить о районе Совета, пока ваша репутация на улицах слишком низка.");
                return;
            }

            state.CouncilSolution = CouncilProblemSolution.criminal;
            state.CouncilQuestStage = CouncilQuestStage.negotiate_with_mafia;
            ShowMessage("Криминальный путь",
                "Найдите посредника мафии. Совет получит защиту, но цена этой услуги будет неофициальной.");
        }

        private void FinishCouncilQuest(string resolutionText) {
            state.CouncilQuestStage = CouncilQuestStage.completed;
            state.SecretLibraryAccess = true;
            state.BloodMagicAdvancedUnlocked = true;
            state.BloodKnowledgeUnlocked = true;

            ShowDialogue("Тайная библиотека", resolutionText + " Совет допускает вас к закрытым записям, и кровь отвечает новым знанием.",
                new[] { new DialogueChoice("Завершить квест Совета", CloseDialogue) });
        }
        private string GetCompletionText() {
            if (state.Access == AccessMethod.council) {
                return
                    "Вы находите документ и создаёте копию для Совета. Совет получает доступ к знанию, а мафия начинает угрожать. Совет готов открыть путь к тайной библиотеке, если вы решите его районную проблему.";
            }

            if (state.Access == AccessMethod.mafia) {
                return
                    "Вы находите документ и создаёте копию для мафии. Мафия получает рычаг влияния, вам открывается криминальный маршрут города.";
            }

            return
                "Вы сохраняете документ только у себя. Копия не создана, фракции не получают прямого доступа, но давление обеих сторон растёт.";
        }

        private string GetObjectiveText() {
            if (state.Stage == QuestStage.completed && state.CouncilQuestStage != CouncilQuestStage.locked) {
                return GetCouncilObjectiveText();
            }

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

        private string GetCouncilObjectiveText() {
            switch (state.CouncilQuestStage) {
                case CouncilQuestStage.choose_solution:
                    return "Поговорите с учёным и решите, как защитить район от подозрительных людей.";
                case CouncilQuestStage.negotiate_with_mafia:
                    return "Договоритесь с посредником мафии, чтобы преступники оставили Совет в покое.";
                case CouncilQuestStage.investigate_intrigue:
                    return "Найдите, почему мафия начала давить на тайное общество.";
                case CouncilQuestStage.return_to_council:
                    return "Вернитесь к учёному и используйте найденный компромат.";
                case CouncilQuestStage.completed:
                    return "Квест  завершён. Тайная библиотека открыта, новая магия крови изучена.";
                default:
                    return "Квест недоступен: нужна завершённая архивная зацепка и копия у Совета.";
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

            currentPrompt = nearestInteractable == null ? string.Empty : $"[ E ] {nearestInteractable.Label}";
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
