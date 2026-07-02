using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RightOfBlood.Prototype {
    public class QuestGame : MonoBehaviour {
        public const float DefaultInteractionRange = 1.2f;
        private const int RequiredOfficialInfluenceForCouncilLawPath = 2;
        private const int RequiredMafiaReputationForCouncilCriminalPath = -1;
        private const float ArchiveDocumentTheftCooldown = 20f;

        [Header("Manual Scene Setup")] [SerializeField]
        private LocationId initialLocation = LocationId.office;

        [SerializeField] private string initialSpawnId = "default";
        [SerializeField] private float interactionRange = DefaultInteractionRange;

        [Header("Manual UI")] [SerializeField] private QuestUI questUi;
        [SerializeField] private ProgressionUIHandler progressionUi;

        private readonly List<Location> locations = new List<Location>();
        private readonly List<Interactable> interactables = new List<Interactable>();
        private readonly List<DialogueChoice> activeChoices = new List<DialogueChoice>();

        private IntroQuestState state;
        private PlayerController player;
        private Interactable nearestInteractable;
        private bool dialogueOpen;
        private string currentPrompt;
        private LocationId currentLocation;

        private void Awake() {
            state = new IntroQuestState();
        }

        private void Start() {
            RefreshSceneReferences();
            if (questUi == null) questUi = FindFirstObjectByType<QuestUI>();
            if (questUi != null) questUi.HideDialogue();
            ResolveProgressionUi();
            LoadLocation(initialLocation, initialSpawnId);
            RefreshUi();
        }

        private void Update() {
            if (player == null) player = FindFirstObjectByType<PlayerController>();
            if (questUi == null) questUi = FindFirstObjectByType<QuestUI>();

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
                FindObjectsByType<Location>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            interactables.Clear();
            interactables.AddRange(
                FindObjectsByType<Interactable>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            foreach (var interactable in interactables) {
                if (interactable != null) interactable.CaptureScenePlacement();
            }
            ResolvePlayerController();
        }

        private void ResolvePlayerController() {
            var players = FindObjectsByType<PlayerController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            player = null;

            for (var i = 0; i < players.Length; i++) {
                if (players[i] != null && players[i].gameObject.activeInHierarchy && players[i].name == "Player") {
                    player = players[i];
                    break;
                }
            }

            if (player == null) {
                for (var i = 0; i < players.Length; i++) {
                    if (players[i] != null && players[i].gameObject.activeInHierarchy) {
                        player = players[i];
                        break;
                    }
                }
            }

            if (player == null && players.Length > 0) player = players[0];

            for (var i = 0; i < players.Length; i++) {
                var duplicate = players[i];
                if (duplicate == null || duplicate == player) continue;
                duplicate.gameObject.SetActive(false);
            }
        }
        public void LoadLocation(LocationId locationId, string spawnId = "default") {
            RefreshSceneReferences();

            Location targetLocation = null;
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

        private void RouteCouncilScholar(Interactable interactable) {
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

        private void RouteMafiaFixer(Interactable interactable) {
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

        private void RouteFormerArchivist(Interactable interactable) {
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

        private void MoveCharacterToCity(Interactable interactable, Vector2 fallbackOffset) {
            var city = GetLocation(LocationId.city);
            if (interactable.ScenePlacementBelongsTo(city)) {
                interactable.MoveToScenePlacement();
                return;
            }

            interactable.MoveToLocation(city, "default", fallbackOffset);
        }

        private void MoveCharacterHome(Interactable interactable, Vector2 fallbackOffset) {
            var home = GetLocation(interactable.TargetLocation);
            if (interactable.ScenePlacementBelongsTo(home)) {
                interactable.MoveToScenePlacement();
                return;
            }

            interactable.MoveToLocation(home, interactable.TargetSpawnId, fallbackOffset);
        }

        private Location GetLocation(LocationId locationId) {
            foreach (var location in locations) {
                if (location != null && location.Id == locationId) return location;
            }

            return null;
        }

        private void TryTravel(Interactable door) {
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
        private static bool IsLegacyDepartmentEntrance(Interactable interactable) {
            return interactable != null && interactable.Kind == PrototypeInteractionKind.archive_investigator &&
                   interactable.name == "Department";
        }
        public void Interact(Interactable interactable) {
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

            if (state.Stage == QuestStage.completed) {
                ShowPostQuestHub("Department chief");
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
                        state.OfficialInfluence = Math.Max(0, state.OfficialInfluence - 1);
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
                if (state.Build != PlayerBuild.sage && GetCurrentBranchReputation() >= ProgressionModel.ReputationForSecondLevel) {
                    ShowFactionSwitchOffer("Учёный Совета предлагает сменить фракцию.");
                    return;
                }

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
                if (state.Build != PlayerBuild.rogue && GetCurrentBranchReputation() >= ProgressionModel.ReputationForSecondLevel) {
                    ShowFactionSwitchOffer("Посредник мафии предлагает сменить фракцию.");
                    return;
                }

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
                if (TryStealArchiveDocument()) return;

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
            if (!state.FirstBuildChoiceMade) {
                ShowBuildChoice(GetCompletionText());
            }
            else {
                ShowDialogue("Прогрессия", GetCompletionText(),
                    new[] {
                        new DialogueChoice("Открыть дерево навыков", ShowProgressionTree),
                        new DialogueChoice("Завершить квест", CloseDialogue)
                    });
            }
        }

        private bool TryUnlockCouncilQuest() {
            if (state.CouncilQuestStage != CouncilQuestStage.locked) return true;
            if (state.Stage != QuestStage.completed || !state.CouncilHasCopy || !state.BloodKnowledgeUnlocked || state.Build != PlayerBuild.sage) return false;

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
            state.OfficialInfluence = Math.Max(0, state.OfficialInfluence - 1);
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
        private void ShowBuildChoice(string leadText) {
            ShowDialogue("Прогрессия",
                leadText + "\n\nСлужебный запрос заблокирован, путь магистрата недоступен для первого выбора.",
                new[] {
                    new DialogueChoice("Мудрец: Совет и знания", () => ChooseBuild(PlayerBuild.sage)),
                    new DialogueChoice("Разбойник: мафия и скрытность", () => ChooseBuild(PlayerBuild.rogue))
                });
        }

        private void ChooseBuild(PlayerBuild build) {
            if (build == PlayerBuild.magistrate && !CanSwitchToMagistrate()) {
                ShowMessage("Ветка заблокирована", "Начальник заблокировал служебный запрос. Попробуй стать магистратом позже.");
                return;
            }

            state.Build = build;
            state.FirstBuildChoiceMade = true;
            state.ProgressionIntroSeen = true;

            if (build == PlayerBuild.sage) {
                state.CouncilReputation = Math.Max(state.CouncilReputation, 1);
                UnlockSkill(SkillId.public_library_access);
                ApplyPublicLibraryAccess();
                TryUnlockCouncilQuest();
            }
            else if (build == PlayerBuild.rogue) {
                state.MafiaReputation = Math.Max(state.MafiaReputation, 1);
                state.CriminalWorldAccess = true;
                UnlockSkill(SkillId.shadow_entry);
            }
            else if (build == PlayerBuild.magistrate) {
                state.OfficialInfluence = Math.Max(state.OfficialInfluence, 1);
                UnlockSkill(SkillId.service_seal);
            }

            RefreshProgressionFromReputation();
            ShowDialogue("Билд выбран", GetBuildSummary(state.Build),
                new[] {
                    new DialogueChoice("Открыть дерево навыков", ShowProgressionTree),
                    new DialogueChoice("Продолжить", CloseDialogue)
                });
        }

        private void ShowPostQuestHub(string speaker) {
            RefreshProgressionFromReputation();
            ShowDialogue(speaker,
                "После вводного квеста используй дерево навыков для моделирования 1-ых уровней разных веток.\n\n" + GetProgressionSummary(),
                new[] {
                    new DialogueChoice("Открыть дерево навыков", ShowProgressionTree),
                    new DialogueChoice("закрыть", CloseDialogue)
                });
        }

        public void OpenProgressionTree() {
            ShowProgressionTree();
        }

        public void SimulateArchivistStage() {
            SimulateProgressionStage(PlayerBuild.magistrate, 1);
        }

        public void SimulateArchiveManagerStage() {
            SimulateProgressionStage(PlayerBuild.magistrate, 2);
        }

        public void SimulateCityManagerStage() {
            SimulateProgressionStage(PlayerBuild.magistrate, 3);
        }

        public void SimulateCouncilParishionerStage() {
            SimulateProgressionStage(PlayerBuild.sage, 1);
        }

        public void SimulateCouncilCandidateStage() {
            SimulateProgressionStage(PlayerBuild.sage, 2);
        }

        public void SimulateCouncilMemberStage() {
            SimulateProgressionStage(PlayerBuild.sage, 3);
        }

        public void SimulateRogueNewcomerStage() {
            SimulateProgressionStage(PlayerBuild.rogue, 1);
        }

        public void SimulateExperiencedRogueStage() {
            SimulateProgressionStage(PlayerBuild.rogue, 2);
        }

        public void SimulateGuildHeadStage() {
            SimulateProgressionStage(PlayerBuild.rogue, 3);
        }

        private void ShowProgressionTree() {
            RefreshProgressionFromReputation();
            CloseDialogue();

            var progressionPanel = ResolveProgressionUi();
            if (progressionPanel != null) {
                progressionPanel.OpenProgressionTree();
                return;
            }

            Debug.LogWarning("ProgressionUIHandler not found. Skill tree panel cannot be opened.");
        }

        private void SimulateProgressionStage(PlayerBuild build, int targetLevel) {
            targetLevel = Mathf.Clamp(targetLevel, 1, 3);
            state.Build = build;
            state.FirstBuildChoiceMade = true;
            state.SecondDevelopmentChoiceMade = targetLevel >= 2;
            state.Level = targetLevel;
            state.Stage = QuestStage.completed;
            state.DocumentFound = true;
            state.BloodKnowledgeUnlocked = true;

            if (build == PlayerBuild.magistrate) {
                state.OfficialInfluence = GetReputationForSimulatedLevel(targetLevel);
                UnlockSkill(SkillId.service_seal);
            }
            else if (build == PlayerBuild.sage) {
                state.CouncilReputation = GetReputationForSimulatedLevel(targetLevel);
                state.OfficialInfluence = 0;
                state.Access = AccessMethod.council;
                state.CouncilHasCopy = true;
                state.Owner = DocumentOwner.council;
                UnlockSkill(SkillId.public_library_access);
                ApplyPublicLibraryAccess();
                TryUnlockCouncilQuest();
            }
            else if (build == PlayerBuild.rogue) {
                state.MafiaReputation = GetReputationForSimulatedLevel(targetLevel);
                state.OfficialInfluence = 0;
                state.Access = AccessMethod.mafia;
                state.MafiaHasCopy = true;
                state.Owner = DocumentOwner.mafia;
                state.CriminalWorldAccess = true;
                UnlockSkill(SkillId.shadow_entry);
            }

            RefreshProgressionFromReputation();
            UpdateAdvancedQuestAvailability();
            ShowFactionSwitchOffer("Этап " + state.Level + ": " + GetBuildName(build) + ".");
        }

        private static int GetReputationForSimulatedLevel(int level) {
            if (level >= 3) return ProgressionModel.ReputationForThirdLevel;
            if (level >= 2) return ProgressionModel.ReputationForSecondLevel;
            return ProgressionModel.ReputationForSecondLevel;
        }

        private void ShowFactionSwitchOffer(string leadText) {
            var choices = new List<DialogueChoice>();
            if (state.Build != PlayerBuild.sage) choices.Add(new DialogueChoice("Уйти в Совет", () => SwitchFaction(PlayerBuild.sage)));
            if (state.Build != PlayerBuild.rogue) choices.Add(new DialogueChoice("Переметнуться к мафии", () => SwitchFaction(PlayerBuild.rogue)));
            if (state.Build != PlayerBuild.magistrate && CanSwitchToMagistrate()) choices.Add(new DialogueChoice("Вернуться к службе", () => SwitchFaction(PlayerBuild.magistrate)));
            choices.Add(new DialogueChoice("Остаться", CloseDialogue));

            ShowDialogue("Смена фракции",
                leadText + "\nНакоплено достаточно репутации - попробуешь перейти в новую фракцию?",
                choices);
        }

        private void SwitchFaction(PlayerBuild targetBuild) {
            var oldBuild = state.Build;
            state.Build = targetBuild;
            state.Level = 1;
            state.SecondDevelopmentChoiceMade = false;
            state.ThreatLevel += oldBuild == PlayerBuild.undecided ? 0 : 1;

            if (targetBuild == PlayerBuild.sage) {
                state.CouncilReputation = Math.Max(1, state.CouncilReputation + 1);
                state.MafiaReputation -= oldBuild == PlayerBuild.rogue ? 2 : 1;
                UnlockSkill(SkillId.public_library_access);
                ApplyPublicLibraryAccess();
            }
            else if (targetBuild == PlayerBuild.rogue) {
                state.MafiaReputation = Math.Max(1, state.MafiaReputation + 1);
                state.CouncilReputation -= oldBuild == PlayerBuild.sage ? 2 : 1;
                state.CriminalWorldAccess = true;
                UnlockSkill(SkillId.shadow_entry);
            }
            else if (targetBuild == PlayerBuild.magistrate) {
                state.OfficialInfluence = Math.Max(1, state.OfficialInfluence + 1);
                state.CouncilReputation -= oldBuild == PlayerBuild.sage ? 1 : 0;
                state.MafiaReputation -= oldBuild == PlayerBuild.rogue ? 1 : 0;
                UnlockSkill(SkillId.service_seal);
            }

            RefreshProgressionFromReputation();
            ShowMessage("Фракция сменена", GetProgressionSummary());
        }

        private bool CanSwitchToMagistrate() {
            return state.Stage == QuestStage.completed && state.OfficialAttemptBlocked;
        }

        private void RefreshProgressionFromReputation() {
            if (state.Build == PlayerBuild.undecided) return;

            var reputation = GetCurrentBranchReputation();
            var newLevel = 1;
            if (reputation >= ProgressionModel.ReputationForThirdLevel) newLevel = 3;
            else if (reputation >= ProgressionModel.ReputationForSecondLevel && state.SecondDevelopmentChoiceMade) newLevel = 2;
            state.Level = Math.Max(state.Level, newLevel);
            ApplyLevelRewards();
        }

        private int GetCurrentBranchReputation() {
            switch (state.Build) {
                case PlayerBuild.magistrate: return state.OfficialInfluence;
                case PlayerBuild.sage: return state.CouncilReputation;
                case PlayerBuild.rogue: return state.MafiaReputation;
                default: return 0;
            }
        }

        private void ApplyLevelRewards() {
            if (state.Build == PlayerBuild.magistrate) {
                UnlockSkill(SkillId.service_seal);
                if (state.Level >= 2) UnlockSkill(SkillId.archive_procedure);
            }
            else if (state.Build == PlayerBuild.sage) {
                UnlockSkill(SkillId.public_library_access);
                if (state.Level >= 2) {
                    UnlockSkill(SkillId.blood_echo);
                    UnlockSkill(SkillId.council_cipher);
                }
            }
            else if (state.Build == PlayerBuild.rogue) {
                UnlockSkill(SkillId.shadow_entry);
                if (state.Level >= 2) {
                    UnlockSkill(SkillId.street_debt);
                    UnlockSkill(SkillId.archive_document_theft);
                }
            }

            if (state.Level >= 3 && state.BloodMagicAdvancedUnlocked) UnlockSkill(SkillId.ancient_blood_mandate);
        }

        private void UnlockSkill(SkillId skill) {
            switch (skill) {
                case SkillId.service_seal: state.ServiceSealUnlocked = true; break;
                case SkillId.archive_procedure: state.ArchiveProcedureUnlocked = true; break;
                case SkillId.blood_echo: state.BloodEchoUnlocked = true; break;
                case SkillId.council_cipher: state.CouncilCipherUnlocked = true; break;
                case SkillId.shadow_entry: state.ShadowEntryUnlocked = true; break;
                case SkillId.street_debt: state.StreetDebtUnlocked = true; break;
                case SkillId.ancient_blood_mandate: state.AncientBloodMandateUnlocked = true; break;
                case SkillId.public_library_access: state.PublicLibraryAccessUnlocked = true; break;
                case SkillId.archive_document_theft: state.ArchiveDocumentTheftUnlocked = true; break;
                default: throw new ArgumentOutOfRangeException(nameof(skill), skill, null);
            }
        }

        private bool HasSkill(SkillId skill) {
            switch (skill) {
                case SkillId.service_seal: return state.ServiceSealUnlocked;
                case SkillId.archive_procedure: return state.ArchiveProcedureUnlocked;
                case SkillId.blood_echo: return state.BloodEchoUnlocked;
                case SkillId.council_cipher: return state.CouncilCipherUnlocked;
                case SkillId.shadow_entry: return state.ShadowEntryUnlocked;
                case SkillId.street_debt: return state.StreetDebtUnlocked;
                case SkillId.ancient_blood_mandate: return state.AncientBloodMandateUnlocked;
                case SkillId.public_library_access: return state.PublicLibraryAccessUnlocked;
                case SkillId.archive_document_theft: return state.ArchiveDocumentTheftUnlocked;
                default: return false;
            }
        }

        private void ApplyPublicLibraryAccess() {
            if (!state.PublicLibraryAccessUnlocked) return;
            state.CouncilReputation = Math.Max(state.CouncilReputation, 1);
            state.BloodKnowledgeUnlocked = true;
        }

        private bool TryStealArchiveDocument() {
            if (!state.ArchiveDocumentTheftUnlocked) return false;
            var timeSinceUse = Time.time - state.LastArchiveDocumentTheftTime;
            if (timeSinceUse < ArchiveDocumentTheftCooldown) {
                ShowMessage("Архив", "Кража документа временно недоступна");
                return true;
            }

            state.LastArchiveDocumentTheftTime = Time.time;
            state.CouncilReputation += 1;
            state.BloodKnowledgeUnlocked = true;
            RefreshProgressionFromReputation();
            ShowMessage("Кража документа из архива", "Ты украл тонкую папку из архива, за что получил репутацию у совета");
            return true;
        }

        private string BuildProgressionTreeText() {
            return "Магистрат: Архивариус - Управляющий архивом - Управляющий городом\n" +
                   "Мудрец: Прихожанин Совета - Кандидат в Совет - Член Совета\n" +
                   "Разбойник: Новобранец - Бывалый - Глава гильдии\n\n" +
                   "Навыки: " + GetUnlockedSkillsText();
        }

        private string GetBuildSummary(PlayerBuild build) {
            var info = ProgressionModel.GetBuild(build);
            if (info == null) return "Билд не выбран";

            return info.Name + ": " + info.Fantasy + "\nСила: " + info.Strength +
                   "\nСлабость: " + info.Weakness + "\nРесурс: " + info.Resource +
                   "\nНавыки: " + GetUnlockedSkillsText();
        }

        private string GetProgressionSummary() {
            return "Этап " + state.Level + ", билд: " + GetBuildName(state.Build) +
                   ", репутация " + GetCurrentBranchReputation() + "/" + GetNextReputationText() +
                   ".\nНавыки: " + GetUnlockedSkillsText();
        }

        private string GetNextReputationText() {
            if (state.Level >= 3) return "max";
            return ProgressionModel.GetRequiredReputation(state.Level + 1).ToString();
        }

        private string GetBuildName(PlayerBuild build) {
            var info = ProgressionModel.GetBuild(build);
            return info == null ? "none" : info.Name;
        }

        private string GetUnlockedSkillsText() {
            var names = new List<string>();
            foreach (var skill in ProgressionModel.Skills) {
                if (HasSkill(skill.Id)) names.Add(skill.Name);
            }

            return names.Count == 0 ? "none" : string.Join(", ", names);
        }

        public void RunScalingCheckQuest() {
            UpdateAdvancedQuestAvailability();
            if (state.ScalingCheckQuestStatus == PrototypeQuestStatus.locked) {
                ShowMessage("Проверка закрыта", "Сначала завершите квест Совета или выберите путь Новобранца после вводного квеста.");
                return;
            }

            if (state.ScalingCheckQuestStatus == PrototypeQuestStatus.completed) {
                ShowMessage("Проверка уже пройдена", state.ScalingCheckOutcome);
                return;
            }

            var result = ResolveScalingCheckResult();
            state.ScalingCheckOutcome = result;
            state.ScalingCheckQuestStatus = PrototypeQuestStatus.completed;
            UpdateAdvancedQuestAvailability();
            ShowMessage("Квест 3: скейлинг проверки", result);
        }

        public void RunProgressionBehaviorQuest() {
            UpdateAdvancedQuestAvailability();
            if (state.ProgressionBehaviorQuestStatus == PrototypeQuestStatus.locked) {
                ShowMessage("Сцена закрыта", "Сначала завершите квест 3. Сцена доступна только Архивариусу, Управляющему городом или Члену Совета.");
                return;
            }

            if (state.ProgressionBehaviorQuestStatus == PrototypeQuestStatus.completed) {
                ShowMessage("Сцена уже пройдена", state.ProgressionBehaviorOutcome);
                return;
            }

            var result = ResolveProgressionBehaviorResult();
            state.ProgressionBehaviorOutcome = result;
            state.ProgressionBehaviorQuestStatus = PrototypeQuestStatus.completed;
            ShowMessage("Квест 4: прогрессия меняет поведение", result);
        }

        public void RunBuildApproachQuest() {
            UpdateAdvancedQuestAvailability();
            if (state.BuildApproachQuestStatus == PrototypeQuestStatus.locked) {
                ShowMessage("Проблема закрыта", "Сначала завершите квест 3: скейлинг проверки.");
                return;
            }

            if (state.BuildApproachQuestStatus == PrototypeQuestStatus.completed) {
                ShowMessage("Проблема уже решена", state.BuildApproachOutcome);
                return;
            }

            var result = ResolveBuildApproachResult();
            state.BuildApproachOutcome = result;
            state.BuildApproachQuestStatus = PrototypeQuestStatus.completed;
            ShowMessage("Квест 5: разные билды решают по-разному", result);
        }

        public string GetReputationPanelText() {
            UpdateAdvancedQuestAvailability();
            return "Совет: " + state.CouncilReputation + "\n" +
                   "Мафия: " + state.MafiaReputation + "\n" +
                   "Служебное влияние: " + state.OfficialInfluence + "\n" +
                   "Текущая ветка: " + GetBuildName(state.Build) + "\n" +
                   "Репутация ветки: " + GetCurrentBranchReputation() + "/" + GetNextReputationText() + "\n" +
                   "Угроза: " + state.ThreatLevel;
        }

        public string GetDebugFlagsPanelText() {
            UpdateAdvancedQuestAvailability();
            return "Флаги\n" +
                   "IntroQuestStarted: " + state.IntroQuestStarted + "\n" +
                   "DocumentFound: " + state.DocumentFound + "\n" +
                   "CouncilHasCopy: " + state.CouncilHasCopy + "\n" +
                   "MafiaHasCopy: " + state.MafiaHasCopy + "\n" +
                   "BloodKnowledgeUnlocked: " + state.BloodKnowledgeUnlocked + "\n" +
                   "SecretLibraryAccess: " + state.SecretLibraryAccess + "\n" +
                   "BloodMagicAdvancedUnlocked: " + state.BloodMagicAdvancedUnlocked + "\n" +
                   "CriminalWorldAccess: " + state.CriminalWorldAccess + "\n" +
                   "Build: " + state.Build + "\n" +
                   "Level: " + state.Level + "\n" +
                   "Quest3: " + state.ScalingCheckQuestStatus + "\n" +
                   "Quest4: " + state.ProgressionBehaviorQuestStatus + "\n" +
                   "Quest5: " + state.BuildApproachQuestStatus;
        }

        public string GetQuestPanelText() {
            UpdateAdvancedQuestAvailability();
            return "Цель: " + GetObjectiveText() + "\n\n" +
                   "1. Пропажа документа: " + (state.Stage == QuestStage.completed ? "завершён" : "активен") + "\n" +
                   "2. Квест Совета: " + state.CouncilQuestStage + "\n" +
                   "3. Скейлинг проверки: " + FormatQuestStatus(state.ScalingCheckQuestStatus, GetScalingCheckUnlockHint()) + "\n" +
                   "4. Прогрессия меняет поведение: " + FormatQuestStatus(state.ProgressionBehaviorQuestStatus, GetProgressionBehaviorUnlockHint()) + "\n" +
                   "5. Разные билды действуют по-разному: " + FormatQuestStatus(state.BuildApproachQuestStatus, GetBuildApproachUnlockHint());
        }

        private void UpdateAdvancedQuestAvailability() {
            if (state.ScalingCheckQuestStatus == PrototypeQuestStatus.locked && CanUnlockScalingCheckQuest()) {
                state.ScalingCheckQuestStatus = PrototypeQuestStatus.active;
            }

            if (state.ScalingCheckQuestStatus == PrototypeQuestStatus.completed) {
                if (state.BuildApproachQuestStatus == PrototypeQuestStatus.locked) {
                    state.BuildApproachQuestStatus = PrototypeQuestStatus.active;
                }

                if (state.ProgressionBehaviorQuestStatus == PrototypeQuestStatus.locked && CanUnlockProgressionBehaviorQuest()) {
                    state.ProgressionBehaviorQuestStatus = PrototypeQuestStatus.active;
                }
            }
        }

        private bool CanUnlockScalingCheckQuest() {
            var afterSecondQuest = state.CouncilQuestStage == CouncilQuestStage.completed;
            var rookieAfterFirstQuest = state.Stage == QuestStage.completed && state.Build == PlayerBuild.rogue && state.Level <= 1;
            return afterSecondQuest || rookieAfterFirstQuest;
        }

        private bool CanUnlockProgressionBehaviorQuest() {
            if (state.ScalingCheckQuestStatus != PrototypeQuestStatus.completed) return false;
            return IsArchivistStage() || IsCityManagerStage() || IsCouncilMemberStage();
        }

        private bool IsArchivistStage() {
            return state.Build == PlayerBuild.magistrate && state.Level <= 1;
        }

        private bool IsCityManagerStage() {
            return state.Build == PlayerBuild.magistrate && state.Level >= 3;
        }

        private bool IsCouncilMemberStage() {
            return state.Build == PlayerBuild.sage && state.Level >= 3;
        }

        private string ResolveScalingCheckResult() {
            if (state.Level >= 3 && (state.Build == PlayerBuild.magistrate || state.Build == PlayerBuild.sage || state.AncientBloodMandateUnlocked)) {
                state.ThreatLevel += 1;
                return "Уровень 3: закрытое крыло архива открывается без проверки. Цена - агенты мафии начинают слежку, угроза +1.";
            }

            if (state.Level >= 2 || state.ArchiveProcedureUnlocked || state.CouncilCipherUnlocked) {
                state.OfficialInfluence = Math.Max(0, state.OfficialInfluence - 1);
                state.CouncilReputation += 1;
                return "Уровень 2: проверка знания архива средней сложности. Вы получаете доступ к одному делу, но начальник замечает обход регламента: служебное влияние -1, Совет +1.";
            }

            if (state.Build == PlayerBuild.rogue || state.ShadowEntryUnlocked) {
                state.ThreatLevel += 1;
                state.MafiaReputation += 1;
                return "Новобранец: проверка слишком сложная, но теневой вход помогает украсть часть сведений. Мафия +1, угроза +1.";
            }

            state.OfficialInfluence = Math.Max(0, state.OfficialInfluence - 1);
            return "Архивариус: проверка высокой сложности провалена. Охрана вызывает начальника, служебное влияние -1.";
        }

        private string ResolveProgressionBehaviorResult() {
            if (IsCouncilMemberStage()) {
                state.CouncilReputation += 1;
                return "Член Совета приходит к закрытой библиотеке. Охрана открывает дверь сама, а библиотекарь отвечает на вопрос о древнем существе. Совет +1.";
            }

            if (IsCityManagerStage()) {
                state.OfficialInfluence += 1;
                return "Управляющий городом требует принести книгу в кабинет и может привести стражу. Сцена проходит без уговоров, служебное влияние +1.";
            }

            state.ThreatLevel += 1;
            return "Архивариуса у входа останавливает охрана. Приходится уговаривать и искать обход, сцена завершается без доступа, угроза +1.";
        }

        private string ResolveBuildApproachResult() {
            if (state.Build == PlayerBuild.magistrate) {
                state.OfficialInfluence += 1;
                state.MafiaReputation -= 1;
                return "Эпидемия и контрабанда: Магистрат вводит карантин и отправляет грузы на официальный досмотр. Решение медленное, но законное: служебное влияние +1, мафия -1.";
            }

            if (state.Build == PlayerBuild.sage) {
                state.CouncilReputation += 1;
                state.CriminalWorldAccess = true;
                return "Эпидемия и контрабанда: Мудрец находит растение-противоядие и заключает сделку ради доступа к складу. Совет +1, открыт криминальный маршрут.";
            }

            if (state.Build == PlayerBuild.rogue) {
                state.MafiaReputation += 2;
                state.ThreatLevel += 1;
                return "Эпидемия и контрабанда: Разбойник устраивает налёт, сжигает товар и пугает банду. Быстро и эффективно: мафия +2, угроза +1.";
            }

            state.ThreatLevel += 1;
            return "Без выбранного билда проблему удаётся только отсрочить. Угроза +1.";
        }

        private string FormatQuestStatus(PrototypeQuestStatus status, string lockedHint) {
            if (status == PrototypeQuestStatus.locked) return "locked (" + lockedHint + ")";
            return status.ToString();
        }

        private string GetScalingCheckUnlockHint() {
            return "после квеста Совета или после 1-го квеста для Новобранца";
        }

        private string GetProgressionBehaviorUnlockHint() {
            return "после квеста 3, только Архивариус / Управляющий городом / Член Совета";
        }

        private string GetBuildApproachUnlockHint() {
            return "после квеста 3";
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
            UpdateAdvancedQuestAvailability();
            if (state.ScalingCheckQuestStatus == PrototypeQuestStatus.active) return "Пройдите квест 3: проверку со скейлингом в закрытом крыле архива.";
            if (state.ProgressionBehaviorQuestStatus == PrototypeQuestStatus.active) return "Доступен квест 4: сцена у входа в библиотеку Совета.";
            if (state.BuildApproachQuestStatus == PrototypeQuestStatus.active) return "Доступен квест 5: проблема контрабанды и эпидемии.";

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
            RefreshProgressionFromReputation();
            UpdateAdvancedQuestAvailability();
            if (questUi == null) return;
            var prompt = player == null ? "no Player w PrototypePlayerController" : currentPrompt;
            questUi.RefreshHud(state, GetObjectiveText(), prompt);
        }

        private ProgressionUIHandler ResolveProgressionUi() {
            if (progressionUi == null) {
                progressionUi = FindFirstObjectByType<ProgressionUIHandler>(FindObjectsInactive.Include);
            }

            return progressionUi;
        }
    }
}
