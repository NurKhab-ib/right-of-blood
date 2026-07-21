using UnityEngine;

namespace RightOfBlood.Prototype {
    public partial class QuestGame {
        private void SyncWorldStateFlags() {
            state.CityMood = ResolveCityMood();
            state.OfficeControl = WorldControlState.magistrate;
            state.CityControl = ResolveCityControl();
            state.ArchiveControl = ResolveArchiveControl();
            state.CouncilControl = ResolveCouncilControl();
            state.StreetsControl = ResolveStreetsControl();
            state.ArchiveFrontDoorState = ResolveArchiveFrontDoorState();
            state.ArchiveBackDoorState = ResolveArchiveBackDoorState();
            state.CouncilSecretLibraryState = CanShowSecretLibraryEntrance() ? WorldRouteState.secret : WorldRouteState.closed;
            state.DarkStreetsRouteState = CanEnterDarkStreets() ? WorldRouteState.open : WorldRouteState.closed;
            state.ChiefState = state.Stage == QuestStage.completed ? WorldObjectState.used : WorldObjectState.available;
            state.CouncilScholarState = CanEnterCouncilLocation() || state.CouncilQuestStage != CouncilQuestStage.locked || state.PublicLibraryVisited
                ? WorldObjectState.available
                : WorldObjectState.hidden;
            state.MafiaFixerState = CanEnterDarkStreets() || state.CriminalWorldAccess || state.CouncilQuestStage == CouncilQuestStage.negotiate_with_mafia
                ? WorldObjectState.available
                : WorldObjectState.hidden;
            state.FormerArchivistState = state.CouncilQuestStage == CouncilQuestStage.investigate_intrigue ||
                                        state.BuildApproachQuestStatus == PrototypeQuestStatus.active ||
                                        state.BuildApproachQuestStatus == PrototypeQuestStatus.completed ||
                                        (state.Stage == QuestStage.choose_archive_access && !state.DocumentFound)
                ? WorldObjectState.available
                : WorldObjectState.hidden;
            SyncStructuredWorldState();
        }

        private void SyncStructuredWorldState() {
            SyncFactionState(state.Council, state.CouncilReputation, CanEnterCouncilLocation(), state.CouncilQuestStage == CouncilQuestStage.negotiate_with_mafia);
            SyncFactionState(state.Mafia, state.MafiaReputation, CanEnterDarkStreets(), state.ArchiveSecurityAlerted);
            SyncFactionState(state.Magistrate, state.OfficialInfluence, state.ArchiveFrontDoorState == WorldRouteState.open, state.ThreatLevel >= 5);
            state.World.Threat = state.ThreatLevel;
            state.World.Stability = GetCityStability();
            state.World.CityControl = state.CityControl;
            state.World.Streets = state.ThreatLevel >= 8 ? StreetMode.raid : state.ThreatLevel >= 3 ? StreetMode.patrolled : StreetMode.free;
            state.World.Quarantine = state.QuarantineRouteOpen || state.CityMood == WorldMoodState.locked_down;
            state.World.Siege = state.CityMood == WorldMoodState.crisis;
            SyncLocationState(state.Office, true, state.OfficeControl, 0, "\u041a\u0430\u043d\u0446\u0435\u043b\u044f\u0440\u0438\u044f", "\u0414\u0435\u043b\u0430");
            SyncLocationState(state.City, true, state.CityControl, state.ThreatLevel, "\u0413\u043e\u0440\u043e\u0434", "\u041a\u0430\u043d\u0446\u0435\u043b\u044f\u0440\u0438\u044f", "\u0413\u043e\u0440\u043e\u0434\u0441\u043a\u043e\u0439 \u043f\u0430\u0442\u0440\u0443\u043b\u044c");
            SyncLocationState(state.Archive, state.ArchiveFrontDoorState != WorldRouteState.closed, state.ArchiveControl, state.ThreatLevel, "\u0410\u0440\u0445\u0438\u0432", "\u041e\u0445\u0440\u0430\u043d\u0430", "\u0410\u0440\u0445\u0438\u0432\u043d\u044b\u0435 \u043f\u043e\u043b\u043a\u0438");
            SyncLocationState(state.CouncilLocation, CanEnterCouncilLocation(), state.CouncilControl, state.ThreatLevel, "\u0410\u0440\u0445\u0438\u0432", "\u0421\u043e\u0432\u0435\u0442");
            SyncLocationState(state.Streets, CanEnterDarkStreets(), state.StreetsControl, state.ThreatLevel, "\u041a\u0430\u043d\u0446\u0435\u043b\u044f\u0440\u0438\u044f", "\u041f\u043e\u0441\u0440\u0435\u0434\u043d\u0438\u043a\u0438", "\u0413\u043e\u0440\u043e\u0434\u0441\u043a\u043e\u0439 \u043f\u0430\u0442\u0440\u0443\u043b\u044c");        }

        private static void SyncFactionState(FactionState faction, int reputation, bool hasAccess, bool suspicious) {
            faction.Reputation = reputation;
            faction.Trust = Mathf.Max(0, reputation);
            faction.Suspicion = suspicious ? Mathf.Max(1, -reputation + 1) : Mathf.Max(0, -reputation);
            faction.HasAccess = hasAccess;
        }

        private static void SyncLocationState(LocationState location, bool isOpen, WorldControlState control, int riskLevel, params string[] activeObjects) {
            location.IsOpen = isOpen;
            location.Control = control;
            location.RiskLevel = riskLevel;
            location.ActiveObjects = activeObjects;
        }

        private int GetCityStability() {
            var finaleModifier = 0;
            switch (state.FinaleChoice) {
                case FinaleChoice.summon: finaleModifier = 30; break;
                case FinaleChoice.council: finaleModifier = 15; break;
                case FinaleChoice.mafia: finaleModifier = -15; break;
                case FinaleChoice.mafia_rule: finaleModifier = -20; break;
                case FinaleChoice.mafia_truce: finaleModifier = 5; break;
                case FinaleChoice.mafia_escape: finaleModifier = 10; break;
                case FinaleChoice.reveal: finaleModifier = -10; break;
            }

            var epidemicModifier = state.AntidoteDistributed ? 15 : state.BlackWarehouseDestroyed ? -10 : state.QuarantineRouteOpen ? -4 : 0;
            return Mathf.Clamp(100 - state.ThreatLevel * 10 + finaleModifier + epidemicModifier, 0, 100);
        }

        private WorldMoodState ResolveCityMood() {
            var stability = GetCityStability();
            if (stability < 20) return WorldMoodState.crisis;
            if (stability < 40) return WorldMoodState.locked_down;
            if (stability <= 70) return WorldMoodState.tense;
            return WorldMoodState.stable;
        }

        private WorldControlState ResolveCityControl() {
            if (state.FinaleChoice == FinaleChoice.summon) return WorldControlState.magistrate;
            if (state.FinaleChoice == FinaleChoice.council) return WorldControlState.council;
            if (state.FinaleChoice == FinaleChoice.mafia) return WorldControlState.mafia;
            if (state.FinaleChoice == FinaleChoice.mafia_rule || state.FinaleChoice == FinaleChoice.mafia_truce) return WorldControlState.mafia;
            if (state.FinaleChoice == FinaleChoice.mafia_escape || state.FinaleChoice == FinaleChoice.reveal) return WorldControlState.contested;

            if (state.CouncilQuestStage == CouncilQuestStage.negotiate_with_mafia || state.MafiaReputation > state.CouncilReputation + 1) {
                return WorldControlState.mafia;
            }

            if (state.CouncilQuestStage == CouncilQuestStage.completed || state.CouncilReputation > state.MafiaReputation + 1) {
                return WorldControlState.council;
            }

            if (state.OfficialInfluence > state.MafiaReputation && state.OfficialInfluence > state.CouncilReputation) {
                return WorldControlState.magistrate;
            }

            return WorldControlState.contested;
        }

        private WorldControlState ResolveArchiveControl() {
            if (state.Access == AccessMethod.council || state.CouncilHasCopy || state.BloodKnowledgeUnlocked) return WorldControlState.council;
            if (state.Access == AccessMethod.mafia || state.MafiaHasCopy || state.CriminalWorldAccess) return WorldControlState.mafia;
            if (state.OfficialInfluence > 1) return WorldControlState.magistrate;
            return WorldControlState.contested;
        }

        private WorldControlState ResolveCouncilControl() {
            if (state.CouncilQuestStage == CouncilQuestStage.completed || state.CouncilReputation >= 3) return WorldControlState.council;
            if (state.CouncilQuestStage == CouncilQuestStage.negotiate_with_mafia) return WorldControlState.contested;
            return state.OfficialInfluence > 1 ? WorldControlState.magistrate : WorldControlState.contested;
        }

        private WorldControlState ResolveStreetsControl() {
            if (CanEnterDarkStreets() || state.MafiaReputation > 0) return WorldControlState.mafia;
            if (state.ThreatLevel >= 5) return WorldControlState.contested;
            return WorldControlState.magistrate;
        }

        private WorldRouteState ResolveArchiveFrontDoorState() {
            if (state.Stage == QuestStage.inspect_missing_document) return WorldRouteState.open;
            if (state.OfficialAttemptBlocked && state.Access == AccessMethod.none) return WorldRouteState.closed;
            return WorldRouteState.open;
        }

        private WorldRouteState ResolveArchiveBackDoorState() {
            if (state.BlackArchiveEntranceKnown || state.Access == AccessMethod.mafia || state.Access == AccessMethod.council || state.CriminalWorldAccess) {
                return WorldRouteState.secret;
            }

            if (state.ScalingCheckQuestStatus == PrototypeQuestStatus.active || state.ScalingCheckQuestStatus == PrototypeQuestStatus.completed) {
                return WorldRouteState.open;
            }

            return WorldRouteState.closed;
        }

        private void ApplyWorldStateToInteractable(Interactable interactable) {
            if (interactable == null) return;
            var worldObject = interactable.GetComponent<WorldObject>();
            if (worldObject == null) worldObject = interactable.gameObject.AddComponent<WorldObject>();
            worldObject.SetState(ResolveInteractableState(interactable.Kind));


            interactable.ClearLabelOverride();

            switch (interactable.Kind) {
                case PrototypeInteractionKind.missing_document_desk:
                    interactable.SetVisible(true);
                    interactable.SetLabel(state.Stage == QuestStage.inspect_missing_document ? "\u0420\u0430\u0431\u043e\u0447\u0438\u0439 \u0441\u0442\u043e\u043b" : "\u0420\u0430\u0431\u043e\u0447\u0438\u0439 \u0441\u0442\u043e\u043b - \u0434\u043e\u043a\u0443\u043c\u0435\u043d\u0442 \u043f\u0440\u043e\u043f\u0430\u043b");
                    return;
                case PrototypeInteractionKind.chief:
                    interactable.SetVisible(state.Stage != QuestStage.completed || state.IntroQuestStarted);
                    interactable.SetLabel(state.ChiefState == WorldObjectState.available ? "\u041d\u0430\u0447\u0430\u043b\u044c\u043d\u0438\u043a \u043e\u0442\u0434\u0435\u043b\u0430" : "\u041d\u0430\u0447\u0430\u043b\u044c\u043d\u0438\u043a \u043e\u0442\u0434\u0435\u043b\u0430");
                    return;
                case PrototypeInteractionKind.archive_security:
                    interactable.SetVisible(currentLocation == LocationId.archive || state.ThreatLevel >= 2);
                    interactable.SetLabel(state.ThreatLevel >= 5 ? "\u041e\u0445\u0440\u0430\u043d\u0430 \u0430\u0440\u0445\u0438\u0432\u0430" : "\u041e\u0445\u0440\u0430\u043d\u0430 \u0430\u0440\u0445\u0438\u0432\u0430");
                    return;
                case PrototypeInteractionKind.council_scholar:
                    RouteCouncilScholar(interactable);
                    interactable.SetLabel(state.CouncilScholarState == WorldObjectState.available ? "\u0423\u0447\u0451\u043d\u044b\u0439 \u0421\u043e\u0432\u0435\u0442\u0430" : "\u0423\u0447\u0451\u043d\u044b\u0439 \u0421\u043e\u0432\u0435\u0442\u0430");
                    return;
                case PrototypeInteractionKind.mafia_fixer:
                    RouteMafiaFixer(interactable);
                    interactable.SetLabel(state.MafiaFixerState == WorldObjectState.available ? "\u041f\u043e\u0441\u0440\u0435\u0434\u043d\u0438\u043a \u041c\u0430\u0444\u0438\u0438" : "\u041f\u043e\u0441\u0440\u0435\u0434\u043d\u0438\u043a \u041c\u0430\u0444\u0438\u0438");
                    return;
                case PrototypeInteractionKind.former_archivist:
                    RouteFormerArchivist(interactable);
                    interactable.SetLabel("\u0411\u044b\u0432\u0448\u0438\u0439 \u0430\u0440\u0445\u0438\u0432\u0430\u0440\u0438\u0443\u0441");
                    return;
                case PrototypeInteractionKind.archive_shelf:
                    interactable.SetVisible(currentLocation == LocationId.archive && (state.DocumentFound || state.Stage == QuestStage.find_document_in_archive || state.ScalingCheckQuestStatus == PrototypeQuestStatus.active));
                    interactable.SetLabel(state.DocumentFound ? "\u0410\u0440\u0445\u0438\u0432\u043d\u044b\u0435 \u043f\u043e\u043b\u043a\u0438: \u0434\u0435\u043b\u043e \u043d\u0430\u0439\u0434\u0435\u043d\u043e" : "\u0410\u0440\u0445\u0438\u0432\u043d\u044b\u0435 \u043f\u043e\u043b\u043a\u0438");
                    return;
                case PrototypeInteractionKind.archive_investigator:
                    interactable.SetVisible(currentLocation == LocationId.archive || state.ThreatLevel >= 4);
                    interactable.SetLabel("\u0421\u043b\u0435\u0434\u043e\u0432\u0430\u0442\u0435\u043b\u044c \u0430\u0440\u0445\u0438\u0432\u0430");
                    return;
                case PrototypeInteractionKind.street_patrol:
                    interactable.SetVisible(currentLocation == LocationId.city && state.World.Streets == StreetMode.patrolled);
                    interactable.SetLabel("\u0413\u043e\u0440\u043e\u0434\u0441\u043a\u043e\u0439 \u043f\u0430\u0442\u0440\u0443\u043b\u044c");
                    return;
                case PrototypeInteractionKind.street_raid:
                    interactable.SetVisible(currentLocation == LocationId.city && state.World.Streets == StreetMode.raid);
                    interactable.SetLabel("\u0413\u043e\u0440\u043e\u0434\u0441\u043a\u043e\u0439 \u0440\u0435\u0439\u0434");
                    return;                case PrototypeInteractionKind.black_archive_door:
                    interactable.SetVisible(state.ArchiveBackDoorState != WorldRouteState.closed);
                    interactable.SetLabel(state.ArchiveBackDoorState == WorldRouteState.secret ? "\u0427\u0451\u0440\u043d\u044b\u0439 \u0445\u043e\u0434 \u0432 \u0430\u0440\u0445\u0438\u0432" : "\u0417\u0430\u043f\u0435\u0440\u0442\u0430\u044f \u0434\u0432\u0435\u0440\u044c");
                    return;
                case PrototypeInteractionKind.council_public_library:
                    interactable.SetVisible(CanEnterCouncilLocation());
                    interactable.SetLabel(state.PublicLibraryVisited ? "\u041f\u0443\u0431\u043b\u0438\u0447\u043d\u0430\u044f \u0431\u0438\u0431\u043b\u0438\u043e\u0442\u0435\u043a\u0430: \u0438\u0437\u0443\u0447\u0435\u043d\u0430" : "\u041f\u0443\u0431\u043b\u0438\u0447\u043d\u0430\u044f \u0431\u0438\u0431\u043b\u0438\u043e\u0442\u0435\u043a\u0430");
                    return;
                case PrototypeInteractionKind.council_secret_library:
                    interactable.SetVisible(CanShowSecretLibraryEntrance());
                    interactable.SetLabel(state.CouncilSecretLibraryState == WorldRouteState.secret ? "\u0422\u0430\u0439\u043d\u0430\u044f \u0431\u0438\u0431\u043b\u0438\u043e\u0442\u0435\u043a\u0430" : "\u0417\u0430\u043a\u0440\u044b\u0442\u0430\u044f \u0431\u0438\u0431\u043b\u0438\u043e\u0442\u0435\u043a\u0430");
                    return;
                case PrototypeInteractionKind.door:
                    RefreshDoorInteractable(interactable);
                    return;
                default:
                    interactable.SetVisible(true);
                    return;
            }
        }

        private void RefreshDoorInteractable(Interactable interactable) {
            var target = interactable.TargetLocation;
            var visible = true;
            var label = interactable.Label;

            if (target == LocationId.council) {
                visible = true;
                label = CanEnterCouncilLocation() ? "\u041f\u0440\u043e\u0445\u043e\u0434 \u043a \u0421\u043e\u0432\u0435\u0442\u0443" : "\u041f\u0440\u043e\u0445\u043e\u0434 \u043a \u0421\u043e\u0432\u0435\u0442\u0443 (\u043d\u0435\u0442 \u0434\u043e\u043f\u0443\u0441\u043a\u0430)";
            }
            else if (target == LocationId.streets) {
                visible = true;
                label = CanEnterDarkStreets() ? "\u0422\u0451\u043c\u043d\u044b\u0435 \u0443\u043b\u0438\u0446\u044b" : "\u0422\u0451\u043c\u043d\u044b\u0435 \u0443\u043b\u0438\u0446\u044b (\u043d\u0435\u0442 \u0441\u0432\u044f\u0437\u0438)";
            }
            else if (target == LocationId.archive) {
                visible = true;
                label = state.ArchiveFrontDoorState == WorldRouteState.open ? "\u0412\u0445\u043e\u0434 \u0432 \u0430\u0440\u0445\u0438\u0432" : "\u0412\u0445\u043e\u0434 \u0432 \u0430\u0440\u0445\u0438\u0432 (\u0437\u0430\u043a\u0440\u044b\u0442)";
            }
            else if (target == LocationId.city) {
                visible = true;
                label = state.CityMood == WorldMoodState.crisis ? "\u0413\u043e\u0440\u043e\u0434 (\u043a\u0430\u0440\u0430\u043d\u0442\u0438\u043d)" : "\u0413\u043e\u0440\u043e\u0434";
            }
            else if (target == LocationId.office) {
                visible = true;
                label = "\u041a\u043e\u0440\u0438\u0434\u043e\u0440 \u043a\u0430\u043d\u0446\u0435\u043b\u044f\u0440\u0438\u0438";
            }

            interactable.SetVisible(visible);
            interactable.SetLabel(label);
        }

        public string GetWorldStateSummaryText() {
            SyncWorldStateFlags();
            return $"\u041c\u0438\u0440: {WorldMoodLabel(state.CityMood)} | \u041a\u0430\u043d\u0446\u0435\u043b\u044f\u0440\u0438\u044f: {ControlLabel(state.OfficeControl)} | " +
                   $"\u0413\u043e\u0440\u043e\u0434: {ControlLabel(state.CityControl)} | \u0410\u0440\u0445\u0438\u0432: {ControlLabel(state.ArchiveControl)} | " +
                   $"\u0421\u043e\u0432\u0435\u0442: {ControlLabel(state.CouncilControl)} | \u0423\u043b\u0438\u0446\u044b: {ControlLabel(state.StreetsControl)} | " +
                   $"\u0412\u0445\u043e\u0434 \u0432 \u0430\u0440\u0445\u0438\u0432: {RouteLabel(state.ArchiveFrontDoorState)} | \u0427\u0451\u0440\u043d\u044b\u0439 \u0445\u043e\u0434: {RouteLabel(state.ArchiveBackDoorState)} | " +
                   $"\u0422\u0430\u0439\u043d\u0430\u044f \u0431\u0438\u0431\u043b\u0438\u043e\u0442\u0435\u043a\u0430: {RouteLabel(state.CouncilSecretLibraryState)} | \u0422\u0451\u043c\u043d\u044b\u0435 \u0443\u043b\u0438\u0446\u044b: {RouteLabel(state.DarkStreetsRouteState)}";
        }

        private static string WorldMoodLabel(WorldMoodState mood) {
            switch (mood) {
                case WorldMoodState.stable: return "\u0441\u0442\u0430\u0431\u0438\u043b\u0435\u043d";
                case WorldMoodState.tense: return "\u043d\u0430\u043f\u0440\u044f\u0436\u0451\u043d";
                case WorldMoodState.locked_down: return "\u043a\u0430\u0440\u0430\u043d\u0442\u0438\u043d";
                case WorldMoodState.crisis: return "\u043a\u0440\u0438\u0437\u0438\u0441";
                default: return mood.ToString();
            }
        }

        private static string ControlLabel(WorldControlState control) {
            switch (control) {
                case WorldControlState.magistrate: return "\u041c\u0430\u0433\u0438\u0441\u0442\u0440\u0430\u0442";
                case WorldControlState.council: return "\u0421\u043e\u0432\u0435\u0442";
                case WorldControlState.mafia: return "\u041c\u0430\u0444\u0438\u044f";
                case WorldControlState.contested: return "\u0441\u043f\u043e\u0440\u043d\u044b\u0439";
                default: return control.ToString();
            }
        }

        private WorldObjectState ResolveInteractableState(PrototypeInteractionKind kind) {
            switch (kind) {
                case PrototypeInteractionKind.seal: return state.OfficialInfluence > 0 ? WorldObjectState.unlocked : WorldObjectState.locked;
                case PrototypeInteractionKind.card_index: return CanEnterCouncilLocation() ? WorldObjectState.available : WorldObjectState.locked;
                case PrototypeInteractionKind.cache: return CanEnterDarkStreets() ? WorldObjectState.available : WorldObjectState.locked;
                case PrototypeInteractionKind.contraband_container: return state.EpidemicLeadLearned ? WorldObjectState.used : WorldObjectState.available;
                case PrototypeInteractionKind.sealed_passage:
                case PrototypeInteractionKind.reinforced_door:
                case PrototypeInteractionKind.black_archive_door:
                    return state.ArchiveBackDoorState == WorldRouteState.closed ? WorldObjectState.locked : WorldObjectState.unlocked;
                case PrototypeInteractionKind.guard_post:
                case PrototypeInteractionKind.archive_security:
                    return state.GuardHostile || state.ThreatLevel >= 5 ? WorldObjectState.altered : WorldObjectState.available;
                case PrototypeInteractionKind.street_patrol:
                    return state.World.Streets == StreetMode.free ? WorldObjectState.hidden : WorldObjectState.available;
                case PrototypeInteractionKind.street_raid:
                    return state.World.Streets == StreetMode.raid ? WorldObjectState.altered : WorldObjectState.hidden;
                case PrototypeInteractionKind.blackmail_point:
                    return state.CouncilBlackmailLeverage ? WorldObjectState.used : WorldObjectState.available;
                case PrototypeInteractionKind.council_secret_library:
                    return state.CouncilSecretLibraryState == WorldRouteState.secret ? WorldObjectState.unlocked : WorldObjectState.locked;
                default: return WorldObjectState.available;
            }
        }
        private static string RouteLabel(WorldRouteState route) {
            switch (route) {
                case WorldRouteState.closed: return "\u0437\u0430\u043a\u0440\u044b\u0442";
                case WorldRouteState.open: return "\u043e\u0442\u043a\u0440\u044b\u0442";
                case WorldRouteState.secret: return "\u0442\u0430\u0439\u043d\u044b\u0439";
                default: return route.ToString();
            }
        }    }
}
