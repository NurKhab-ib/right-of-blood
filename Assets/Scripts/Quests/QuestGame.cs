using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RightOfBlood.Prototype {
    public partial class QuestGame : MonoBehaviour {
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
        private BalanceHandler balanceHandler;
        private PlayerController player;
        private Interactable nearestInteractable;
        private bool dialogueOpen;
        private string currentPrompt;
        private LocationId currentLocation;

        private void Awake() {
            state = new IntroQuestState();
            balanceHandler = new BalanceHandler(state, () => currentLocation);
        }

        private void ApplyInfluenceDelta(int delta, string source) {
            balanceHandler?.ApplyInfluenceDelta(delta, source);
        }

        private void ApplyKnowledgeDelta(int delta, string source) {
            balanceHandler?.ApplyKnowledgeDelta(delta, source);
        }

        private void ApplyStrengthDelta(int delta, string source) {
            balanceHandler?.ApplyStrengthDelta(delta, source);
        }

        private void ApplyBranchSupport(PlayerBuild build, int supportedDelta, int competitorDelta, string source) {
            balanceHandler?.ApplyBranchSupport(build, supportedDelta, competitorDelta, source);
        }

        private void ApplyThreatDelta(int delta, string source) {
            balanceHandler?.ApplyThreatDelta(delta, source);
        }

        private void SetMinimumInfluence(int minimum, string source) {
            balanceHandler?.SetMinimumInfluence(minimum, source);
        }

        private void SetMinimumKnowledge(int minimum, string source) {
            balanceHandler?.SetMinimumKnowledge(minimum, source);
        }

        private void SetMinimumStrength(int minimum, string source) {
            balanceHandler?.SetMinimumStrength(minimum, source);
        }

        private void RecalculateThreat(string source) {
            balanceHandler?.RecalculateThreat(source);
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
                player.CanMove = !dialogueOpen && !IsTerminal && !GameConsole.IsInputBlocked;
                RefreshWorldState();
                UpdateInteractionPrompt();
            }

            var keyboard = Keyboard.current;
            if (!GameConsole.IsInputBlocked && keyboard != null) {
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
            SyncWorldStateFlags();

            if (interactables.Count == 0) return;

            foreach (var interactable in interactables) {
                if (interactable == null) continue;
                ApplyWorldStateToInteractable(interactable);
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
            return state.Access == AccessMethod.council || state.CouncilHasCopy || state.CouncilQuestStage != CouncilQuestStage.locked ||
                   state.Build == PlayerBuild.sage || state.PublicLibraryAccessUnlocked || state.CouncilReputation > 0;
        }

        private bool ShouldFixerWaitInCity() {
            return state.Stage == QuestStage.choose_archive_access && !state.DocumentFound && state.Access != AccessMethod.mafia;
        }

        private bool ShouldFixerWaitInDarkStreets() {
            return state.Access == AccessMethod.mafia || state.MafiaHasCopy || state.CouncilQuestStage == CouncilQuestStage.negotiate_with_mafia || state.CriminalWorldAccess;
        }

        private bool ShouldFormerArchivistWaitInCity() {
            return state.CouncilQuestStage == CouncilQuestStage.investigate_intrigue ||
                   state.BuildApproachQuestStatus == PrototypeQuestStatus.active ||
                   state.BuildApproachQuestStatus == PrototypeQuestStatus.completed ||
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

        private bool CanShowSecretLibraryEntrance() {
            return CanEnterCouncilLocation() &&
                   (state.SecretLibraryAccess || state.ProgressionBehaviorQuestStatus != PrototypeQuestStatus.locked ||
                    state.PublicLibraryAccessUnlocked || state.Level >= 2);
        }
        private static bool IsLegacyDepartmentEntrance(Interactable interactable) {
            return interactable != null && interactable.Kind == PrototypeInteractionKind.archive_investigator &&
                   interactable.name == "Department";
        }
        public void Interact(Interactable interactable) {
            if (IsTerminal || GameConsole.IsInputBlocked) return;
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
                case PrototypeInteractionKind.council_public_library: VisitPublicLibrary(); break;
                case PrototypeInteractionKind.council_secret_library: TrySecretLibraryAccess(); break;
                case PrototypeInteractionKind.seal: UseServiceSeal(); break;
                case PrototypeInteractionKind.notice_board: ShowMessage("Доска объявлений", GetWorldStateSummaryText()); break;
                case PrototypeInteractionKind.card_index: UseCardIndex(); break;
                case PrototypeInteractionKind.cache: UseCache(); break;
                case PrototypeInteractionKind.contraband_container: UseContrabandContainer(); break;
                case PrototypeInteractionKind.sealed_passage:
                case PrototypeInteractionKind.reinforced_door: TryTravel(interactable); break;
                case PrototypeInteractionKind.street_patrol: TalkToStreetPresence(false); break;
                case PrototypeInteractionKind.street_raid: TalkToStreetPresence(true); break;
                case PrototypeInteractionKind.guard_post: TalkToArchiveSecurity(); break;
                case PrototypeInteractionKind.blackmail_point: UseBlackmailPoint(); break;
                case PrototypeInteractionKind.door:
                    TryTravel(interactable); break;
                default: throw new ArgumentOutOfRangeException();
            }
        }

        private void UseServiceSeal() {
            if (state.OfficialInfluence <= 0) {
                ShowMessage("Служебная печать", "Без влияния магистрата печать не даёт доступа.");
                return;
            }
            if (!state.CanEnterRestrictedArchive) ApplyInfluenceDelta(1, "Служебная печать: подтверждение полномочий");
            state.OfficialAttemptBlocked = false;
            state.CanEnterRestrictedArchive = true;
            ShowMessage("Служебная печать", "Печать подтверждает полномочия. Официальный проход в архив открыт.");
        }

        private void UseCardIndex() {
            if (!CanEnterCouncilLocation()) {
                ShowMessage("Картотека", "Картотека Совета закрыта для посторонних.");
                return;
            }
            ApplyKnowledgeDelta(1, "Картотека Совета");
            state.BlackArchiveEntranceKnown = true;
            ShowMessage("Картотека", "В карточках найдено упоминание чёрного хода и поставщика заражённых лекарств.");
        }

        private void UseCache() {
            if (!CanEnterDarkStreets()) {
                ShowMessage("Тайник", "Тайник отмечен знаком Мафии. Без проводника лучше не трогать его.");
                return;
            }

            if (CanOfferMafiaFinale()) {
                OfferMafiaFinale();
                return;
            }

            state.CriminalWorldAccess = true;
            ApplyStrengthDelta(1, "Тайник Мафии");
            ShowMessage("Тайник", "Ты вскрываешь тайник и получаешь рычаг давления на улицах.");
        }

        private void UseContrabandContainer() {
            if (state.ThreatLevel < 3) ApplyThreatDelta(1, "Контейнер контрабанды");
            state.EpidemicLeadLearned = true;
            ShowMessage("Контейнер контрабанды", "Контейнер связывает болезнь в нижнем квартале с чёрным складом.");
        }

        private void UseBlackmailPoint() {
            if (!state.CouncilBlackmailLeverage) {
                state.CouncilBlackmailLeverage = true;
                state.CouncilQuestStage = CouncilQuestStage.return_to_council;
                ShowMessage("Точка шантажа", "Ты находишь доказательства старой сделки Совета с мафией.");
                return;
            }
            ShowMessage("Точка шантажа", "Доказательства уже у тебя. Реши, кому и когда их показать.");
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
                ShowPostQuestHub("\u041d\u0430\u0447\u0430\u043b\u044c\u043d\u0438\u043a \u043e\u0442\u0434\u0435\u043b\u0430");
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
                        ApplyThreatDelta(1, "Начальник отдела: проигнорировать расследование");
                        ShowMessage("Несколько дней спустя",
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
                        ApplyInfluenceDelta(-1, "Охрана архива: служебный запрос отклонён");
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
                if (CanOfferFactionSwitch() && state.Build != PlayerBuild.sage && GetCurrentBranchReputation() >= ProgressionModel.ReputationForSecondLevel) {
                    ShowFactionSwitchOffer("Учёный Совета предлагает сменить фракцию.");
                    return;
                }

                if (state.CouncilHasCopy) {
                    ShowMessage("Учёный Совета", "Совет уже изучает копию. Ваш род упоминался в старой кровной таблице, но доступ к ней придётся заслужить.");
                    return;
                }

                if (state.Build == PlayerBuild.sage || state.PublicLibraryAccessUnlocked || state.CouncilReputation > 0) {
                    ShowDialogue("Учёный Совета",
                        "Вы пришли как человек Совета, но документ после чёрного хода остался только у вас. Дайте нам копию, и мы сможем открыть дело о тайной библиотеке.",
                        new[] {
                            new DialogueChoice("Передать копию Совету", () => {
                                state.CopyCreated = true;
                                state.CouncilHasCopy = true;
                                state.Owner = DocumentOwner.council;
                                ApplyKnowledgeDelta(1, "Ученый Совета: передача копии");
                                TryUnlockCouncilQuest();
                                ShowMessage("Копия передана", "Учёный уносит копию в закрытый зал Совета. Теперь проблема района Совета становится следующим шагом к тайной библиотеке.");
                            }),
                            new DialogueChoice("Оставить документ при себе", CloseDialogue)
                        });
                    return;
                }

                ShowMessage("Учёный Совета", "Если документ ещё у вас, Совет готов обсудить его позже.");
                return;
            }

            ShowDialogue("Учёный Совета",
                "Совет осведомлён о тёмной стороне городских архивов. Мы дадим вам пропуск, но копия найденного листа должна попасть к нам.",
                new[] {
                    new DialogueChoice("Принять помощь Совета", () => {
                        state.Access = AccessMethod.council;
                        state.Stage = QuestStage.find_document_in_archive;
                        state.CanEnterRestrictedArchive = true;
                        ApplyKnowledgeDelta(2, "Ученый Совета: получить доступ");
                        ApplyStrengthDelta(-1, "Ученый Совета: помощь Совета снижает уличное давление");
                        ApplyThreatDelta(1, "Ученый Совета: временный пропуск");
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
                            ApplyStrengthDelta(2, "Посредник мафии: поручение выполнено");
                            ApplyKnowledgeDelta(-1, "Посредник мафии: ухудшение отношений с Советом");
                            state.CriminalWorldAccess = true;
                            ApplyThreatDelta(1, "Посредник мафии: ответная реакция");
                            FinishCouncilQuest("Мафия отзывает людей от здания Совета. Совет получает тишину, но понимает, что вы решили проблему чужими руками.");
                        }),
                        new DialogueChoice("Отказаться", CloseDialogue)
                    });
                return;
            }

            if (state.DocumentFound) {
                if (CanOfferFactionSwitch() && state.Build != PlayerBuild.rogue && GetCurrentBranchReputation() >= ProgressionModel.ReputationForSecondLevel) {
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
                        ApplyStrengthDelta(2, "Посредник мафии: получить доступ");
                        ApplyKnowledgeDelta(-1, "Посредник мафии: ухудшение отношений с Советом");
                        ApplyThreatDelta(1, "Посредник мафии: временный пропуск");
                        ShowMessage("Доступ мафии получен",
                            "Посредник даёт знак архивариусу - за это мафия получит копию документа.");
                    }),
                    new DialogueChoice("Отказаться", CloseDialogue)
                });
        }

        private void VisitPublicLibrary() {
            if (!CanEnterCouncilLocation()) {
                ShowMessage("Публичная библиотека Совета", "Без приглашения Совета магистрату не дают даже читательский журнал.");
                return;
            }

            if (!state.PublicLibraryVisited) {
                state.PublicLibraryVisited = true;
                ApplyKnowledgeDelta(1, "Публичная библиотека Совета");
                UnlockSkill(SkillId.public_library_access);
                ApplyPublicLibraryAccess();
                ShowMessage("Публичная библиотека Совета",
                    "Открытый зал Совета становится доступен: каталоги, хроники болезней и имена старых архивных работников теперь можно проверять без посредников. Совет +1.");
                return;
            }

            ShowMessage("Публичная библиотека Совета",
                "В открытом зале остаются городские хроники, медицинские сводки и каталоги. Тайные полки видны за охраняемой дверью.");
        }

        private bool CanOfferMafiaFinale() {
            return state.Build == PlayerBuild.rogue &&
                   state.Level >= 3 &&
                   state.AncientBloodMandateUnlocked &&
                   CanUnlockAncientBloodMandate();
        }

        private void OfferMafiaFinale() {
            if (state.MafiaFinaleQuestStatus == PrototypeQuestStatus.completed) {
                ShowMessage("Конец: Власть улиц", GetMafiaFinaleOutcome());
                return;
            }

            state.MafiaFinaleQuestStatus = PrototypeQuestStatus.active;
            ShowDialogue("Власть улиц",
                "В тайнике лежат списки долгов, маршруты рейдов и имена тех, кто готов продать город. Теперь улицы ждут твоего решения.",
                new[] {
                    new DialogueChoice("Забрать город силой", () => ResolveMafiaFinale(FinaleChoice.mafia_rule)),
                    new DialogueChoice("Заключить мир кварталов", () => ResolveMafiaFinale(FinaleChoice.mafia_truce)),
                    new DialogueChoice("Исчезнуть с архивом", () => ResolveMafiaFinale(FinaleChoice.mafia_escape))
                });
        }

        private void ResolveMafiaFinale(FinaleChoice choice) {
            state.FinaleChoice = choice;
            state.MafiaFinaleQuestStatus = PrototypeQuestStatus.completed;
            switch (choice) {
                case FinaleChoice.mafia_rule:
                    ApplyStrengthDelta(2, "Финал Мафии: власть силой");
                    ApplyThreatDelta(2, "Финал Мафии: власть силой");
                    break;
                case FinaleChoice.mafia_truce:
                    ApplyKnowledgeDelta(1, "Финал Мафии: мир кварталов");
                    ApplyThreatDelta(-1, "Финал Мафии: мир кварталов");
                    break;
                case FinaleChoice.mafia_escape:
                    ApplyStrengthDelta(-1, "Финал Мафии: исчезновение");
                    ApplyThreatDelta(-2, "Финал Мафии: исчезновение");
                    break;
            }
            CompleteEnding(OutcomeFor(choice), "Конец: Власть улиц", GetMafiaFinaleOutcome());
        }
        private string GetMafiaFinaleOutcome() {
            switch (state.FinaleChoice) {
                case FinaleChoice.mafia_rule:
                    return "Ты собрал долги в один кулак и взял город силой. Порядок на улицах теперь держится на страхе, а твоё имя стало законом нижних кварталов.";
                case FinaleChoice.mafia_truce:
                    return "Ты продал компромат всем сторонам и заставил кварталы договориться. Улицы остаются опасными, но у города впервые появился мир без приказа сверху.";
                case FinaleChoice.mafia_escape:
                    return "Ты исчез с архивом и оставил город без хозяина. Рейды стихли, долги обнулились, но никто не знает, когда ты вернёшься за своим.";
                default:
                    return "Тайник ждёт твоего решения.";
            }
        }

        private void TalkToStreetPresence(bool raid) {
            if (raid) {
                CheckRaidRisk();
                if (IsTerminal || GameConsole.IsInputBlocked) return;
                if (state.MafiaReputation >= 4) {
                    ShowMessage("Рейд", "Старший рейда узнаёт тебя по уличным связям и велит своим людям не задерживать тебя. Но город помнит, кто провёл их через квартал.");
                    return;
                }

                if (state.OfficialInfluence >= 3) {
                    ShowMessage("Рейд", "Ты предъявляешь служебный знак. Командир рейда пропускает тебя, но просит не вмешиваться в облаву без приказа.");
                    return;
                }

                if (state.CouncilReputation >= 3) {
                    ShowMessage("Рейд", "Командир слышал о твоих связях с Советом. Он отступает на шаг и сухо предупреждает: сегодня знания не остановят обыск.");
                    return;
                }

                ShowMessage("Рейд", "Круги рейда смыкаются на перекрёстке. Тебя пока не трогают, но каждый разговор здесь может стать причиной для обыска.");
                return;
            }

            if (state.OfficialInfluence >= 2) {
                ShowMessage("Патруль", "Патрульный узнаёт служебный знак и сообщает: город напряжён, но для тебя проход остаётся открытым.");
                return;
            }

            if (state.MafiaReputation >= 2) {
                ShowMessage("Патруль", "Патрульный долго смотрит на тебя, затем замечает знак улиц и отводит взгляд. Сегодня Мафия ещё может купить тишину.");
                return;
            }

            if (state.CouncilReputation >= 2) {
                ShowMessage("Патруль", "Патрульный вежливо просит не задерживаться. Имя Совета даёт тебе время, но не избавляет от внимания стражи.");
                return;
            }

            ShowMessage("Патруль", state.Level >= 2
                ? "Патрульный оценивает тебя как человека с делом и пропускает, предупредив: улицы больше не принадлежат случайным прохожим."
                : "Патрульный требует идти дальше и не смотреть по сторонам. Пока у тебя нет имени, за тебя никто не поручится.");
        }

        private void TrySecretLibraryAccess() {
            if (!CanShowSecretLibraryEntrance()) {
                ShowMessage("Тайная библиотека", "За публичным залом есть закрытая дверь, но сейчас это просто стена запретов: нет допуска, статуса или зацепки.");
                return;
            }

            if (state.AncientBloodMandateUnlocked && CanUnlockAncientBloodMandate()) {
                OfferFinale();
                return;
            }

            RunProgressionBehaviorQuest();
        }

        private void OfferFinale() {
            if (state.FinaleQuestStatus == PrototypeQuestStatus.completed) {
                ShowMessage("Цена рода", GetFinaleOutcome());
                return;
            }

            if (!CanUnlockAncientBloodMandate()) {
                ShowMessage("Право крови ещё не заслужено", GetMandateRequirementText());
                return;
            }

            state.FinaleQuestStatus = PrototypeQuestStatus.active;
            ShowDialogue("Цена рода",
                "В закрытом хранилище лежит ритуал, способный изменить город. Право крови даёт тебе право выбрать, но не отменяет цену.",
                new[] {
                    new DialogueChoice("Призвать Существо", () => ResolveFinale(FinaleChoice.summon)),
                    new DialogueChoice("Запечатать хранилище", () => ResolveFinale(FinaleChoice.seal)),
                    new DialogueChoice("Передать ритуал Совету", () => ResolveFinale(FinaleChoice.council)),
                    new DialogueChoice("Передать ритуал Мафии", () => ResolveFinale(FinaleChoice.mafia)),
                    new DialogueChoice("Открыть правду городу", () => ResolveFinale(FinaleChoice.reveal))
                });
        }

        private void ResolveFinale(FinaleChoice choice) {
            state.FinaleChoice = choice;
            state.FinaleQuestStatus = PrototypeQuestStatus.completed;
            switch (choice) {
                case FinaleChoice.summon:
                    state.AncientBloodMandateUnlocked = false;
                    ApplyThreatDelta(-3, "Финал: призыв Существа");
                    break;
                case FinaleChoice.seal:
                    ApplyInfluenceDelta(1, "Финал: хранилище запечатано");
                    ApplyThreatDelta(-1, "Финал: хранилище запечатано");
                    break;
                case FinaleChoice.council:
                    ApplyKnowledgeDelta(2, "Финал: ритуал передан Совету");
                    ApplyThreatDelta(-1, "Финал: порядок Совета");
                    break;
                case FinaleChoice.mafia:
                    ApplyStrengthDelta(3, "Финал: ритуал передан Мафии");
                    ApplyThreatDelta(2, "Финал: теневой контроль");
                    break;
                case FinaleChoice.reveal:
                    ApplyInfluenceDelta(-1, "Финал: открытая правда");
                    ApplyThreatDelta(1, "Финал: открытая правда");
                    break;
            }
            CompleteEnding(OutcomeFor(choice), "Конец: Цена рода", GetFinaleOutcome());
        }

        private string GetFinaleOutcome() {
            switch (state.FinaleChoice) {
                case FinaleChoice.summon: return "Ты призвал Существо и остановил распад города. Улицы вновь спокойны, но право крови исчерпано: город спасён ценой дара.";
                case FinaleChoice.seal: return "Ты запечатал хранилище и отказался от чужой воли. Кризис не исчезает мгновенно, но город получает шанс изменить будущее сам.";
                case FinaleChoice.council: return "Ты передал ритуал Совету. Порядок восстановлен, знания сохранены, но право решать остаётся у закрытого круга.";
                case FinaleChoice.mafia: return "Ты передал ритуал Мафии. Город выжил под быстрым и жестоким контролем улиц.";
                case FinaleChoice.reveal: return "Ты открыл правду городу. Старый порядок расколот, но право крови больше не тайна немногих.";
                default: return "Хранилище ждёт решения.";
            }
        }
        private void OfferEpidemicQuest() {
            UpdateAdvancedQuestAvailability();

            if (state.BuildApproachQuestStatus == PrototypeQuestStatus.completed) {
                ShowMessage("Бывший архивный работник", state.BuildApproachOutcome);
                return;
            }

            if (state.BuildApproachQuestStatus == PrototypeQuestStatus.locked) {
                ShowMessage("Бывший архивный работник",
                    "Я слышал о болезни в нижнем квартале, но пока вы не проверите закрытое крыло архива, у вас нет нужной ниточки: это может быть обычная лихорадка, а может - след контрабанды.");
                return;
            }

            state.EpidemicLeadLearned = true;
            ShowDialogue("Бывший архивный работник",
                "В нижнем квартале люди болеют после дешёвых лекарств с чёрного склада. В архивных описях есть тот же знак поставщика. Можно решить это законом, знанием Совета или ударом по складу.",
                BuildEpidemicChoices());
        }

        private IReadOnlyList<DialogueChoice> BuildEpidemicChoices() {
            var choices = new List<DialogueChoice>();

            if (state.Build == PlayerBuild.magistrate || state.ServiceSealUnlocked || state.OfficialInfluence > 0) {
                choices.Add(new DialogueChoice("Ввести карантин и досмотр", () => CompleteBuildApproachQuest(ResolveLawEpidemicResult())));
            }

            if (state.Build == PlayerBuild.sage || state.PublicLibraryAccessUnlocked || state.BloodEchoUnlocked) {
                choices.Add(new DialogueChoice("Найти противоядие через Совет", () => CompleteBuildApproachQuest(ResolveSageEpidemicResult())));
            }

            if (state.Build == PlayerBuild.rogue || state.CriminalWorldAccess || state.ShadowEntryUnlocked) {
                choices.Add(new DialogueChoice("Разгромить чёрный склад", () => CompleteBuildApproachQuest(ResolveRogueEpidemicResult())));
            }

            if (choices.Count == 0) {
                choices.Add(new DialogueChoice("Изолировать квартал временно", () => CompleteBuildApproachQuest(ResolveDelayEpidemicResult())));
            }

            choices.Add(new DialogueChoice("Решить позже", CloseDialogue));
            return choices;
        }

        private void CompleteBuildApproachQuest(string result) {
            state.BuildApproachOutcome = result;
            state.BuildApproachQuestStatus = PrototypeQuestStatus.completed;
            RefreshProgressionFromReputation();
            UpdateAdvancedQuestAvailability();
            ShowMessage("Квест 5: эпидемия и контрабанда", result);
        }

        private string ResolveLawEpidemicResult() {
            ApplyInfluenceDelta(1, "Квест Совета: законный путь");
            ApplyStrengthDelta(-1, "Квест Совета: законный путь");
            state.OtherDistrictSafety -= 1;
            state.QuarantineRouteOpen = true;
            return "Магистрат вводит карантин, ставит досмотр грузов и перекрывает поставку заражённых лекарств. Решение законное, но медленное: служебное влияние +1, мафия -1, безопасность других районов -1.";
        }

        private string ResolveSageEpidemicResult() {
            ApplyKnowledgeDelta(1, "Квест Совета: путь Совета");
            state.CriminalWorldAccess = true;
            state.AntidoteDistributed = true;
            return "Мудрец сверяет симптомы с хрониками публичной библиотеки и находит растение-противоядие. Сделка с перевозчиком открывает путь к складу: Совет +1, открыт криминальный маршрут.";
        }

        private string ResolveRogueEpidemicResult() {
            ApplyStrengthDelta(2, "Квест Совета: криминальный путь");
            ApplyThreatDelta(1, "Квест Совета: криминальный путь");
            state.BlackWarehouseDestroyed = true;
            return "Разбойник выходит на чёрный склад, сжигает товар и заставляет банду отступить. Быстро и грязно: мафия +2, угроза +1.";
        }

        private string ResolveDelayEpidemicResult() {
            ApplyThreatDelta(1, "Квест Совета: интрига");
            return "Без выбранного билда и связей проблему удаётся только отсрочить: квартал изолирован, но источник лекарств не найден. Угроза +1.";
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

            if (state.BuildApproachQuestStatus == PrototypeQuestStatus.active || state.BuildApproachQuestStatus == PrototypeQuestStatus.completed) {
                OfferEpidemicQuest();
                return;
            }

            if (state.DocumentFound) {
                ShowMessage("Бывший работник архива", "После такого следа охрана начнёт считать каждый ключ. Если слухи о нижнем квартале подтвердятся, приходите ко мне ещё раз.");
                return;
            }

            ShowDialogue("Бывший архивный работник",
                "У старого архива есть чёрный ход. Снаружи он выглядит как кладовая. Внутри - прямой коридор к закрытым полкам.",
                new[] {
                    new DialogueChoice("Запомнить путь", () => {
                        state.BlackArchiveEntranceKnown = true;
                        ApplyThreatDelta(1, "Бывший работник архива: черный ход");
                        ShowMessage("Зацепка",
                            "Теперь можно попробовать самостоятельный доступ через чёрный ход у здания архива.");
                    }),
                    new DialogueChoice("Уйти", CloseDialogue)
                });
        }

        private void TrySoloAccess() {
            if (state.DocumentFound || state.ScalingCheckQuestStatus == PrototypeQuestStatus.active) {
                RunScalingCheckQuest();
                return;
            }

            if (!state.BlackArchiveEntranceKnown) {
                ShowMessage("Вход", "Магистрат не знает, что эта кладовая ведёт к закрытым полкам. Найдите бывшего работника архива в городе или другой намёк на обходной путь.");
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
                        ApplyThreatDelta(2, "Черный ход архива: самостоятельное проникновение");
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
                ApplyKnowledgeDelta(1, "Поведение прогрессии: Совет");
            }
            else if (state.Access == AccessMethod.mafia) {
                state.CopyCreated = true;
                state.MafiaHasCopy = true;
                state.Owner = DocumentOwner.mafia;
                ApplyStrengthDelta(1, "Кража документа: копия у мафии");
            }
            else {
                state.Owner = DocumentOwner.player;
                state.PlayerOnlyAccess = true;
                ApplyKnowledgeDelta(-1, "Кража документа: документ только у игрока");
                ApplyStrengthDelta(-1, "Кража документа: документ только у игрока");
                ApplyThreatDelta(1, "Кража документа: самостоятельный риск");
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
            ApplyKnowledgeDelta(2, "Квест Совета: законный выбор");
            ApplyStrengthDelta(-2, "Квест Совета: законный выбор");
            ApplyInfluenceDelta(1, "Квест Совета: законный выбор");
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
            ApplyKnowledgeDelta(2, "Квест Совета: завершение дела");
            RefreshProgressionFromReputation();

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
                state.CouncilReputation = Math.Max(1, state.CouncilReputation);
                SetMinimumKnowledge(1, "Выбор билда: Совет");
                UnlockSkill(SkillId.public_library_access);
                ApplyPublicLibraryAccess();
                TryUnlockCouncilQuest();
            }
            else if (build == PlayerBuild.rogue) {
                SetMinimumStrength(1, "Выбор билда: мафия");
                state.CriminalWorldAccess = true;
                UnlockSkill(SkillId.shadow_entry);
            }
            else if (build == PlayerBuild.magistrate) {
                SetMinimumInfluence(1, "Выбор билда: служба");
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
                "После вводного квеста открой дерево навыков: оно показывает требования следующих этапов.\n" + GetProgressionSummary(),
                new[] {
                    new DialogueChoice("Дерево навыков", ShowProgressionTree),
                    new DialogueChoice("закрыть", CloseDialogue)
                });
        }

        public void OpenProgressionTree() {
            ShowProgressionTree();
        }

        public void SimulateArchivistStage() {
            ShowStageRequirement(PlayerBuild.magistrate, "Архивариус", 1);
        }

        public void SimulateArchiveManagerStage() {
            ShowStageRequirement(PlayerBuild.magistrate, "Управляющий архивом", 2);
        }

        public void SimulateCityManagerStage() {
            ShowStageRequirement(PlayerBuild.magistrate, "Управляющий городом", 3);
        }

        public void SimulateCouncilParishionerStage() {
            ShowStageRequirement(PlayerBuild.sage, "Прихожанин", 1);
        }

        public void SimulateCouncilCandidateStage() {
            ShowStageRequirement(PlayerBuild.sage, "Кандидат", 2);
        }

        public void SimulateCouncilMemberStage() {
            ShowStageRequirement(PlayerBuild.sage, "Член Совета", 3);
        }

        public void SimulateRogueNewcomerStage() {
            ShowStageRequirement(PlayerBuild.rogue, "Новобранец", 1);
        }

        public void SimulateExperiencedRogueStage() {
            ShowStageRequirement(PlayerBuild.rogue, "Бывалый", 2);
        }

        public void SimulateGuildHeadStage() {
            ShowStageRequirement(PlayerBuild.rogue, "Глава гильдии", 3);
        }

        private void ShowStageRequirement(PlayerBuild build, string stageName, int requiredLevel) {
            var branchName = GetBuildName(build);
            var requirement = requiredLevel == 1
                ? "Выберите эту ветку и завершите вводное расследование."
                : requiredLevel == 2
                    ? "Завершите проверку закрытого крыла и закрепите путь во втором решении развития."
                    : "Накопите 4 репутации в ветке, завершите проверку закрытого крыла и решите проблему эпидемии.";
            ShowMessage(stageName + " - " + branchName,
                "Это узел развития, а не бесплатное повышение. " + requirement);
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
            ResetSimulatedProgressionUnlocks();
            ApplySimulatedBranchAccess(build, targetLevel);
            state.BloodKnowledgeUnlocked = true;

            if (build == PlayerBuild.magistrate) {
                ApplyInfluenceDelta(GetReputationForSimulatedLevel(targetLevel) - state.OfficialInfluence, "Симуляция магистрата");
                UnlockSkill(SkillId.service_seal);
            }
            else if (build == PlayerBuild.sage) {
                ApplyKnowledgeDelta(GetReputationForSimulatedLevel(targetLevel) - state.CouncilReputation, "Симуляция Совета");
                ApplyInfluenceDelta(-state.OfficialInfluence, "Симуляция Совета");
                state.Access = AccessMethod.council;
                state.CouncilHasCopy = true;
                state.Owner = DocumentOwner.council;
                UnlockSkill(SkillId.public_library_access);
                ApplyPublicLibraryAccess();
                TryUnlockCouncilQuest();
            }
            else if (build == PlayerBuild.rogue) {
                ApplyStrengthDelta(GetReputationForSimulatedLevel(targetLevel) - state.MafiaReputation, "Симуляция мафии");
                ApplyInfluenceDelta(-state.OfficialInfluence, "Симуляция мафии");
                state.Access = AccessMethod.mafia;
                state.MafiaHasCopy = true;
                state.Owner = DocumentOwner.mafia;
                state.CriminalWorldAccess = true;
                UnlockSkill(SkillId.shadow_entry);
            }

            RefreshProgressionFromReputation();
            UpdateAdvancedQuestAvailability();
            if (CanOfferFactionSwitch()) {
                ShowFactionSwitchOffer(GetProgressionSummary() + ", этап " + state.Level);
            }
            else {
                ShowMessage("Прогрессия обновлена", GetProgressionSummary() + ", этап " + state.Level);
            }
        }

        private static int GetReputationForSimulatedLevel(int level) {
            if (level >= 3) return ProgressionModel.ReputationForThirdLevel;
            if (level >= 2) return ProgressionModel.ReputationForSecondLevel;
            return ProgressionModel.ReputationForSecondLevel;
        }

        private void ShowFactionSwitchOffer(string leadText) {
            if (!CanOfferFactionSwitch()) {
                ShowMessage("Прогрессия обновлена", leadText + "\n" + GetProgressionSummary());
                return;
            }

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
            ApplyThreatDelta(oldBuild == PlayerBuild.undecided ? 0 : 1, "Смена фракции");

            if (targetBuild == PlayerBuild.sage) {
                state.CouncilReputation = Math.Max(1, state.CouncilReputation + 1);
                ApplyStrengthDelta(-(oldBuild == PlayerBuild.rogue ? 2 : 1), "Смена фракции: переход в Совет");
                UnlockSkill(SkillId.public_library_access);
                ApplyPublicLibraryAccess();
            }
            else if (targetBuild == PlayerBuild.rogue) {
                state.MafiaReputation = Math.Max(1, state.MafiaReputation + 1);
                ApplyKnowledgeDelta(-(oldBuild == PlayerBuild.sage ? 2 : 1), "Смена фракции: переход к мафии");
                state.CriminalWorldAccess = true;
                UnlockSkill(SkillId.shadow_entry);
            }
            else if (targetBuild == PlayerBuild.magistrate) {
                state.OfficialInfluence = Math.Max(1, state.OfficialInfluence + 1);
                ApplyKnowledgeDelta(-(oldBuild == PlayerBuild.sage ? 1 : 0), "Смена фракции: возврат к службе");
                ApplyStrengthDelta(-(oldBuild == PlayerBuild.rogue ? 1 : 0), "Смена фракции: возврат к службе");
                UnlockSkill(SkillId.service_seal);
            }

            RefreshProgressionFromReputation();
            ShowMessage("Фракция сменена", GetProgressionSummary());
        }

        private bool CanSwitchToMagistrate() {
            return state.Stage == QuestStage.completed && state.OfficialAttemptBlocked;
        }

        private bool CanOfferFactionSwitch() {
            return state.Level <= 1 && state.Build != PlayerBuild.magistrate;
        }

        private void RefreshProgressionFromReputation() {
            if (state.Build == PlayerBuild.undecided) return;

            var reputation = GetCurrentBranchReputation();
            var newLevel = 1;
            if (CanReachThirdStage()) newLevel = 3;
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

        private void ResetSimulatedProgressionUnlocks() {
            state.ServiceSealUnlocked = false;
            state.ArchiveProcedureUnlocked = false;
            state.BloodEchoUnlocked = false;
            state.CouncilCipherUnlocked = false;
            state.ShadowEntryUnlocked = false;
            state.StreetDebtUnlocked = false;
            state.ArchiveDocumentTheftUnlocked = false;
            state.PublicLibraryAccessUnlocked = false;
            state.AncientBloodMandateUnlocked = false;

            state.Access = AccessMethod.none;
            state.CanEnterRestrictedArchive = false;
            state.CouncilHasCopy = false;
            state.MafiaHasCopy = false;
            state.PlayerOnlyAccess = false;
            state.CriminalWorldAccess = false;
            state.SecretLibraryAccess = false;
            state.BloodMagicAdvancedUnlocked = false;
            state.CopyCreated = false;
            state.Owner = DocumentOwner.player;
        }

        private void ApplySimulatedBranchAccess(PlayerBuild build, int targetLevel) {
            state.BloodMagicAdvancedUnlocked = targetLevel >= 3;

            if (build == PlayerBuild.magistrate) {
                state.CanEnterRestrictedArchive = targetLevel >= 2;
            }
            else if (build == PlayerBuild.sage) {
                state.Access = AccessMethod.council;
                state.CouncilHasCopy = true;
                state.CopyCreated = true;
                state.CanEnterRestrictedArchive = true;
                state.SecretLibraryAccess = targetLevel >= 3;
            }
            else if (build == PlayerBuild.rogue) {
                state.Access = AccessMethod.mafia;
                state.MafiaHasCopy = true;
                state.CopyCreated = true;
                state.CanEnterRestrictedArchive = true;
                state.CriminalWorldAccess = true;
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

            if (CanUnlockAncientBloodMandate()) UnlockSkill(SkillId.ancient_blood_mandate);
        }

        private void UnlockSkill(SkillId skill) {
            var alreadyUnlocked = HasSkill(skill);

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

            if (!alreadyUnlocked && balanceHandler != null) {
                balanceHandler.LogSkillActivation(skill, "Квест или этап прогрессии");
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
            SetMinimumKnowledge(1, "Публичная библиотека Совета");
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
            ApplyKnowledgeDelta(1, "Эпидемия и контрабанда: Совет помогает");
            state.BloodKnowledgeUnlocked = true;
            RefreshProgressionFromReputation();
            ShowMessage("Кража документа из архива", "Ты украл тонкую папку из архива, за что получил репутацию у совета");
            return true;
        }

        private string BuildProgressionTreeText() {
            return "Магистрат: Архивариус - Управляющий архивом - Управляющий городом\n" +
                   "Совет: Прихожанин - Кандидат - Член Совета\n" +
                   "Мафия: Новобранец - Бывалый - Глава";
        }

        private string GetBuildSummary(PlayerBuild build) {
            var info = ProgressionModel.GetBuild(build);
            if (info == null) return "Билд не выбран";

            return info.Name + ": " + info.Fantasy + "\nСила: " + info.Strength +
                   "\nСлабость: " + info.Weakness + "\nРесурс: " + info.Resource;
        }

        private string GetProgressionSummary() {
            return GetBuildName(state.Build);
        }

        private string GetNextReputationText() {
            if (state.Level >= 3) return "максимум";
            return ProgressionModel.GetRequiredReputation(state.Level + 1).ToString();
        }

        private string GetBuildName(PlayerBuild build) {
            var info = ProgressionModel.GetBuild(build);
            return info == null ? "не выбран" : info.Name;
        }

        private string GetUnlockedSkillsText() {
            var names = new List<string>();
            foreach (var skill in ProgressionModel.Skills) {
                if (HasSkill(skill.Id)) names.Add(skill.Name);
            }

            return names.Count == 0 ? "нет" : string.Join(", ", names);
        }

        public string GetSkillsPanelText() {
            RefreshProgressionFromReputation();

            var builder = new StringBuilder();
            
            foreach (var skill in ProgressionModel.Skills) {
                if (HasSkill(skill.Id)) {
                    AppendSkillEntry(builder, skill, true);
                }
            }

            builder.AppendLine();
            
            foreach (var skill in ProgressionModel.Skills) {
                if (!HasSkill(skill.Id)) {
                    AppendSkillEntry(builder, skill, false);
                }
            }

            return builder.ToString().TrimEnd();
        }

        private void AppendSkillEntry(StringBuilder builder, SkillInfo skill, bool unlocked) {
            builder.AppendLine((unlocked ? "[ОТКРЫТ]" : "[ЗАКРЫТ]") + " " + skill.Name);
            builder.AppendLine("Условие: " + GetSkillUnlockHint(skill));
            builder.AppendLine("Эффект: " + skill.Description);
            builder.AppendLine("Получено после " + skill.UnlockQuestLabel);
            builder.AppendLine("Применение: " + skill.Usage);
            builder.AppendLine();
        }

        private string GetSkillUnlockHint(SkillInfo skill) {
            if (HasSkill(skill.Id)) return "условие выполнено";

            var requiredBranch = skill.Branch == PlayerBuild.undecided || state.Build == skill.Branch;
            var requiredSkillMet = !skill.RequiredSkill.HasValue || HasSkill(skill.RequiredSkill.Value);

            if (!requiredBranch) return "выбрать ветку " + GetSkillBranchName(skill.Branch) + ".";
            if (state.Level < skill.RequiredLevel) return "достичь этапа " + skill.RequiredLevel + " в этой ветке.";
            if (!requiredSkillMet) return "сначала открыть " + ProgressionModel.GetSkill(skill.RequiredSkill.Value).Name + ".";
            return "будет открыт при следующем обновлении прогрессии.";
        }

        private static string GetSkillKindName(SkillKind kind) {
            switch (kind) {
                case SkillKind.active: return "активный";
                case SkillKind.passive: return "пассивный";
                case SkillKind.keystone: return "ключевой";
                default: return kind.ToString();
            }
        }

        private string GetSkillBranchName(PlayerBuild build) {
            return build == PlayerBuild.undecided ? "общая" : GetBuildName(build);
        }
        
        public string RunConsoleQuest(string questName) {
            var key = NormalizeConsoleKey(questName);
            switch (key) {
                case "1_document":
                case "1_документ":
                case "документ":
                case "квест 1":
                case "quest 1":
                    if (state.Stage == QuestStage.inspect_missing_document) InspectDesk();
                    else if (state.Stage == QuestStage.talk_to_chief) TalkToChief();
                    else if (state.Stage == QuestStage.find_document_in_archive) InspectArchiveShelf();
                    else ShowMessage("Квест 1", "Пропажа документа уже завершена.");
                    return "Запущен квест 1: пропажа документа.";
                case "2_council":
                case "2_совет":
                case "совет":
                case "квест 2":
                case "quest 2":
                    TalkToCouncilProblem();
                    return "Запущен квест 2: проблема Совета.";
                case "3_scaling":
                case "3_скейлинг":
                case "скейлинг":
                case "проверка":
                case "квест 3":
                case "quest 3":
                    RunScalingCheckQuest();
                    return "Запущен квест 3: скейлинг проверки.";
                case "4_progression":
                case "4_прогрессия":
                case "прогрессия":
                case "поведение":
                case "квест 4":
                case "quest 4":
                    RunProgressionBehaviorQuest();
                    return "Запущен квест 4: поведение прогрессии.";
                case "5_epidemic":
                case "5_эпидемия":
                case "эпидемия":
                case "контрабанда":
                case "квест 5":
                case "quest 5":
                    RunBuildApproachQuest();
                    return "Запущен квест 5: эпидемия и контрабанда.";
                case "6_final":
                case "6_финал":
                case "финал":
                case "право крови":
                    OfferFinale();
                    return "Открыт финальный выбор права крови.";
                default:
                    return "Неизвестный квест. Используйте 1_document, 2_council, 3_scaling, 4_progression, 5_epidemic или 6_final.";
            }
        }

        public string RunConsoleStage(string stageName) {
            var key = NormalizeConsoleKey(stageName);
            switch (key) {
                case "magistrate_1": return ApplyConsoleStage(PlayerBuild.magistrate, 1, "Архивариус");
                case "magistrate_2": return ApplyConsoleStage(PlayerBuild.magistrate, 2, "Управляющий архивом");
                case "magistrate_3": return ApplyConsoleStage(PlayerBuild.magistrate, 3, "Управляющий городом");
                case "council_1": return ApplyConsoleStage(PlayerBuild.sage, 1, "Прихожанин");
                case "council_2": return ApplyConsoleStage(PlayerBuild.sage, 2, "Кандидат в Совет");
                case "council_3": return ApplyConsoleStage(PlayerBuild.sage, 3, "Член Совета");
                case "mafia_1": return ApplyConsoleStage(PlayerBuild.rogue, 1, "Новобранец");
                case "mafia_2": return ApplyConsoleStage(PlayerBuild.rogue, 2, "Бывалый разбойник");
                case "mafia_3": return ApplyConsoleStage(PlayerBuild.rogue, 3, "Глава гильдии");
                default:
                    return "Неизвестный этап. Используйте magistrate_1..3, council_1..3 или mafia_1..3.";
            }
        }

        private string ApplyConsoleStage(PlayerBuild build, int targetLevel, string stageName) {
            SimulateProgressionStage(build, targetLevel);
            return "Отладка: установлен этап «" + stageName + "» (" + GetBuildName(build) + ").";
        }
        private static string NormalizeConsoleKey(string value) {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
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
            if (!state.SecondDevelopmentChoiceMade) {
                OfferSecondDevelopmentChoice();
                return;
            }
            ShowMessage("Скейлинг проверки", result);
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
                ShowMessage("Проблема закрыта", "Сначала завершите квест 3: скейлинг проверки, затем найдите бывшего работника архива в городе.");
                return;
            }

            OfferEpidemicQuest();
        }

        public string GetReputationPanelText() {
            UpdateAdvancedQuestAvailability();
            return "Совет: " + state.CouncilReputation + "\n" +
                   "Мафия: " + state.MafiaReputation + "\n" +
                   "Служба: " + state.OfficialInfluence + "\n" +
                   "Текущая ветка: " + GetBuildName(state.Build) + "\n" +
                   "Репутация ветки: " + GetCurrentBranchReputation() + "/" + GetNextReputationText() + "\n" +
                   "Угроза: " + state.ThreatLevel;
        }

        public string GetDebugFlagsPanelText() {
            UpdateAdvancedQuestAvailability();
            var builder = new StringBuilder();
            builder.AppendLine("Вводный квест начат: " + state.IntroQuestStarted);
            builder.AppendLine("Документ найден: " + state.DocumentFound);
            builder.AppendLine("Копия у Совета: " + state.CouncilHasCopy);
            builder.AppendLine("Копия у Мафии: " + state.MafiaHasCopy);
            builder.AppendLine("Знание крови открыто: " + state.BloodKnowledgeUnlocked);
            builder.AppendLine("Доступ к тайной библиотеке: " + state.SecretLibraryAccess);
            builder.AppendLine("Продвинутая магия крови: " + state.BloodMagicAdvancedUnlocked);
            builder.AppendLine("Доступ к криминальному миру: " + state.CriminalWorldAccess);
            builder.AppendLine("Путь: " + state.Build);
            builder.AppendLine("Этап: " + state.Level);
            builder.AppendLine("Квест 3: " + state.ScalingCheckQuestStatus);
            builder.AppendLine("Квест 4: " + state.ProgressionBehaviorQuestStatus);
            builder.AppendLine("Квест 5: " + state.BuildApproachQuestStatus);
            builder.AppendLine();
            builder.AppendLine(balanceHandler == null ? "Баланс: недоступен" : balanceHandler.GetDebugText());
            return builder.ToString().TrimEnd();
        }

        public string GetQuestPanelText() {
            UpdateAdvancedQuestAvailability();
            var builder = new StringBuilder();
            builder.AppendLine("Цель: " + GetObjectiveText());
            builder.AppendLine();
            builder.AppendLine("1. Пропажа документа: " + (state.Stage == QuestStage.completed ? "завершён" : "активен"));
            builder.AppendLine("2. Квест Совета: " + state.CouncilQuestStage);
            builder.AppendLine("3. Скейлинг проверки: " + FormatQuestStatus(state.ScalingCheckQuestStatus, GetScalingCheckUnlockHint()));
            builder.AppendLine("4. Прогрессия меняет поведение: " + FormatQuestStatus(state.ProgressionBehaviorQuestStatus, GetProgressionBehaviorUnlockHint()));
            builder.AppendLine("5. Разные билды действуют по-разному: " + FormatQuestStatus(state.BuildApproachQuestStatus, GetBuildApproachUnlockHint()));
            builder.AppendLine("6. Цена рода: " + FormatQuestStatus(state.FinaleQuestStatus, "открой навык Право крови"));
            builder.AppendLine("7. Власть улиц: " + FormatQuestStatus(state.MafiaFinaleQuestStatus, "стань главой Мафии и заверши квест 5"));
            builder.AppendLine();
            builder.AppendLine(GetCurrentQuestRewardPreviewText());
            return builder.ToString().TrimEnd();
        }

        private string GetCurrentQuestRewardPreviewText() {
            var coefficient = Mathf.Max(1, state.Level);
            if (state.Build == PlayerBuild.sage) {
                return balanceHandler.BuildQuestRewardPreview("Результат решения:", coefficient, 2, 12, -3, 10, "Репутация Совета");
            }

            if (state.Build == PlayerBuild.rogue) {
                return balanceHandler.BuildQuestRewardPreview("Результат решения:", coefficient, 2, -3, 12, 10, "Репутация мафии");
            }

            if (state.Build == PlayerBuild.magistrate) {
                return balanceHandler.BuildQuestRewardPreview("Результат решения:", coefficient, 12, 2, -3, 10, "Репутация Магистрата");
            }

            return balanceHandler.BuildQuestRewardPreview("Результат решения:", coefficient, 5, 5, 5, 5, "Репутация стороны");
        }

        private void UpdateAdvancedQuestAvailability() {
            if (state.FinaleQuestStatus == PrototypeQuestStatus.locked && CanUnlockAncientBloodMandate()) {
                state.FinaleQuestStatus = PrototypeQuestStatus.active;
            }
            if (state.MafiaFinaleQuestStatus == PrototypeQuestStatus.locked && CanOfferMafiaFinale()) {
                state.MafiaFinaleQuestStatus = PrototypeQuestStatus.active;
            }
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
                ApplyThreatDelta(1, "Проверка масштаба: уровень 3");
                state.ArchiveWingOpen = true;
                state.GuardHostile = true;
                return "Уровень 3: закрытое крыло архива открывается без проверки. Цена - агенты мафии начинают слежку, угроза +1.";
            }

            if (state.Level >= 2 || state.ArchiveProcedureUnlocked || state.CouncilCipherUnlocked) {
                if (state.Build == PlayerBuild.magistrate) ApplyInfluenceDelta(1, "Проверка масштаба: служебный регламент");
                else ApplyInfluenceDelta(-1, "Проверка масштаба: уровень 2");
                ApplyKnowledgeDelta(1, "Проверка масштаба: уровень 2");
                state.ArchiveWingOpen = true;
                return "Уровень 2: проверка знания архива средней сложности. Вы получаете доступ к одному делу; магистрат оформляет регламент и получает влияние +1, остальные вызывают подозрения, но получают знание Совета +1.";
            }

            if (state.Build == PlayerBuild.rogue || state.ShadowEntryUnlocked) {
                ApplyThreatDelta(1, "Проверка масштаба: новичок");
                ApplyStrengthDelta(1, "Проверка масштаба: новичок");
                state.ArchiveWingOpen = true;
                state.GuardHostile = true;
                return "Новобранец: проверка слишком сложная, но теневой вход помогает украсть часть сведений. Мафия +1, угроза +1.";
            }

            if (state.Build == PlayerBuild.magistrate) {
                ApplyInfluenceDelta(1, "Проверка масштаба: служебная апелляция");
                return "Архивариус: охрана задерживает вас, но служебная апелляция фиксирует право на повторную проверку. Влияние +1.";
            }
            ApplyInfluenceDelta(-1, "Проверка масштаба: провал архивариуса");
            return "Проверка высокой сложности провалена. Охрана вызывает начальника, служебное влияние -1.";
        }

        private string ResolveProgressionBehaviorResult() {
            if (IsCouncilMemberStage()) {
                ApplyKnowledgeDelta(1, "Поведение прогрессии: советник");
                return "Член Совета приходит к закрытой библиотеке. Охрана открывает дверь сама, а библиотекарь отвечает на вопрос о древнем существе. Совет +1.";
            }

            if (IsCityManagerStage()) {
                ApplyInfluenceDelta(1, "Поведение прогрессии: город");
                return "Управляющий городом требует принести книгу в кабинет и может привести стражу. Сцена проходит без уговоров, служебное влияние +1.";
            }

            ApplyThreatDelta(1, "Поведение прогрессии: отказ");
            return "Архивариуса у входа останавливает охрана. Приходится уговаривать и искать обход, сцена завершается без доступа, угроза +1.";
        }

        private string ResolveBuildApproachResult() {
            if (state.Build == PlayerBuild.magistrate) {
                ApplyInfluenceDelta(1, "Эпидемия и контрабанда: магистрат");
                ApplyStrengthDelta(-1, "Эпидемия и контрабанда: магистрат");
                return "Эпидемия и контрабанда: Магистрат вводит карантин и отправляет грузы на официальный досмотр. Решение медленное, но законное: служебное влияние +1, мафия -1.";
            }

            if (state.Build == PlayerBuild.sage) {
                ApplyKnowledgeDelta(1, "Эпидемия и контрабанда: мудрец");
                state.CriminalWorldAccess = true;
                return "Эпидемия и контрабанда: Мудрец находит растение-противоядие и заключает сделку ради доступа к складу. Совет +1, открыт криминальный маршрут.";
            }

            if (state.Build == PlayerBuild.rogue) {
            ApplyStrengthDelta(2, "Эпидемия и контрабанда: нападение на склад");
            ApplyThreatDelta(1, "Эпидемия и контрабанда: нападение на склад");
                return "Эпидемия и контрабанда: Разбойник устраивает налёт, сжигает товар и пугает банду. Быстро и эффективно: мафия +2, угроза +1.";
            }

            ApplyThreatDelta(1, "Эпидемия и контрабанда: затягивание");
            return "Без выбранного билда проблему удаётся только отсрочить. Угроза +1.";
        }

        private string FormatQuestStatus(PrototypeQuestStatus status, string lockedHint) {
            if (status == PrototypeQuestStatus.locked) return "закрыт (" + lockedHint + ")";
            return status == PrototypeQuestStatus.active ? "активен" : "завершён";
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
            if (state.FinaleQuestStatus == PrototypeQuestStatus.active) return "Вернись в тайную библиотеку и реши судьбу города.";
            if (state.MafiaFinaleQuestStatus == PrototypeQuestStatus.active) return "Вернись к тайнику Мафии в тёмных улицах и реши судьбу кварталов.";
            if (state.ScalingCheckQuestStatus == PrototypeQuestStatus.active) return "Пройди проверку со скейлингом в закрытом крыле архива.";
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
            for (var i = 0; i < activeChoices.Count; i++) choiceTexts[i] = NormalizeText(activeChoices[i].Text);
            if (questUi) questUi.ShowDialogue(NormalizeText(speaker), NormalizeText(text), choiceTexts, Choose);
            else Debug.LogWarning("Интерфейс квестов не найден");
        }

        private static string NormalizeText(string value) {
            return TextNormalizer.Normalize(value);
        }

        private void CloseDialogue() {
            dialogueOpen = false;
            activeChoices.Clear();
            if (questUi != null) questUi.HideDialogue();
        }

        private void Choose(int index) {
            if (GameConsole.IsInputBlocked || index < 0 || index >= activeChoices.Count) return;
            var action = activeChoices[index].Action;
            activeChoices.Clear();
            action?.Invoke();
        }

        private void RefreshUi() {
            RefreshProgressionFromReputation();
            UpdateAdvancedQuestAvailability();
            SyncWorldStateFlags();
            if (questUi == null) return;
            var prompt = player == null ? "\u0418\u0433\u0440\u043e\u043a \u043d\u0435 \u043d\u0430\u0439\u0434\u0435\u043d" : currentPrompt;
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
