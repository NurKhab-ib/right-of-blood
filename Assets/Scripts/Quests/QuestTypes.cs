using System;
using System.Collections.Generic;

namespace RightOfBlood.Prototype {
    public enum LocationId {
        office,
        city,
        archive,
        council,
        streets
    }

    public enum QuestStage {
        inspect_missing_document,
        talk_to_chief,
        choose_archive_access,
        find_document_in_archive,
        completed
    }

    public enum AccessMethod {
        none,
        official_blocked,
        council,
        mafia,
        solo
    }

    public enum DocumentOwner {
        none,
        player,
        council,
        mafia
    }

    public enum CouncilQuestStage {
        locked,
        choose_solution,
        negotiate_with_mafia,
        investigate_intrigue,
        return_to_council,
        completed
    }

    public enum CouncilProblemSolution {
        none,
        law,
        criminal,
        intrigue
    }

    public enum PlayerBuild {
        undecided,
        magistrate,
        sage,
        rogue
    }

    public enum ProgressionChoice {
        none,
        public_office,
        forbidden_knowledge,
        street_authority
    }

    public enum WorldControlState {
        magistrate,
        council,
        mafia,
        contested
    }

    public enum WorldMoodState {
        stable,
        tense,
        locked_down,
        crisis
    }

    public enum WorldRouteState {
        closed,
        open,
        secret
    }

    public enum WorldObjectState {
        hidden,
        locked,
        available,
        unlocked,
        blocked,
        used,
        altered,
        destroyed,
        controlled_by_council,
        controlled_by_mafia
    }
    public enum ReputationTier {
        low,
        neutral,
        high,
        dominant
    }

    public enum StreetMode {
        free,
        patrolled,
        raid
    }

    [Serializable]
    public sealed class FactionState {
        public int Reputation;
        public int Trust;
        public int Suspicion;
        public bool HasAccess;

        public ReputationTier Tier {
            get {
                if (Reputation >= 6) return ReputationTier.dominant;
                if (Reputation >= 3) return ReputationTier.high;
                if (Reputation <= -2) return ReputationTier.low;
                return ReputationTier.neutral;
            }
        }
    }

    [Serializable]
    public sealed class WorldState {
        public int Threat;
        public int Stability = 100;
        public WorldControlState CityControl = WorldControlState.contested;
        public StreetMode Streets = StreetMode.free;
        public bool Quarantine;
        public bool Siege;
    }

    [Serializable]
    public sealed class LocationState {
        public bool IsOpen = true;
        public WorldControlState Control = WorldControlState.contested;
        public string[] ActiveObjects = Array.Empty<string>();
        public int RiskLevel;
    }

    [Serializable]
    public sealed class InteractableState {
        public WorldObjectState State = WorldObjectState.available;
        public int Progress;
    }
    public enum SkillId {
        service_seal,
        archive_procedure,
        blood_echo,
        council_cipher,
        shadow_entry,
        street_debt,
        ancient_blood_mandate,
        public_library_access,
        archive_document_theft,
        city_decree,
        council_conclave,
        guild_command
    }

    public enum ReactiveEventKind {
        none,
        magistrate_petition,
        magistrate_audit,
        magistrate_riot,
        council_specimen,
        council_manuscript,
        council_leak,
        mafia_debt,
        mafia_protection,
        mafia_succession

    }

    public enum PrototypeQuestStatus {
        locked,
        active,
        completed
    }

    public enum FinaleChoice {
        none,
        summon,
        seal,
        council,
        mafia,
        mafia_rule,
        mafia_truce,
        mafia_escape,
        reveal
    }
    public enum GameOutcome {
        none,
        summoned_creature,
        sealed_archive,
        council_custody,
        mafia_custody,
        public_truth,
        mafia_rule,
        mafia_truce,
        archive_escape,
        death
    }

    public enum PrototypeInteractionKind {
        missing_document_desk = 0,
        chief = 1,
        archive_security = 2,
        council_scholar = 3,
        mafia_fixer = 4,
        former_archivist = 5,
        archive_shelf = 6,
        archive_investigator = 7,
        black_archive_door = 8,
        door = 9,
        council_public_library = 10,
        council_secret_library = 11,
        seal = 12,
        notice_board = 13,
        cache = 15,
        contraband_container = 16,
        street_patrol = 19,
        guard_post = 20,
        blackmail_point = 21,
        street_raid = 22,
        event_messenger = 23
    }

    [Serializable]
    public sealed class IntroQuestState {
        public QuestStage Stage = QuestStage.inspect_missing_document;
        public AccessMethod Access = AccessMethod.none;
        public DocumentOwner Owner = DocumentOwner.none;
        public int CouncilReputation;
        public int MafiaReputation;
        public int OfficialInfluence;
        public int ThreatLevel;
        public bool IntroQuestStarted;
        public bool IgnoredFirstHook;
        public bool OfficialAttemptBlocked = true;
        public bool CanEnterRestrictedArchive;
        public bool DocumentFound;
        public bool CopyCreated;
        public bool CouncilHasCopy;
        public bool MafiaHasCopy;
        public bool PlayerOnlyAccess;
        public bool IndependentDocumentHeld;
        public bool CouncilFragmentShared;
        public bool MafiaRouteShared;
        public bool ConditionalCouncilAlly;
        public bool ConditionalMafiaAlly;
        public bool BloodKnowledgeUnlocked;
        public bool ArchiveSecurityAlerted;
        public bool BlackArchiveEntranceKnown;
        public CouncilQuestStage CouncilQuestStage = CouncilQuestStage.locked;
        public CouncilProblemSolution CouncilSolution = CouncilProblemSolution.none;
        public int OtherDistrictSafety;
        public bool CouncilDistrictSecured;
        public bool CriminalWorldAccess;
        public bool CouncilBlackmailLeverage;
        public bool SecretLibraryAccess;
        public bool BloodMagicAdvancedUnlocked;
        public bool PublicLibraryVisited;
        public bool EpidemicLeadLearned;
        public PlayerBuild Build = PlayerBuild.undecided;
        public ProgressionChoice SecondDevelopmentChoice = ProgressionChoice.none;
        public int Level = 1;
        public bool ServiceSealUnlocked;
        public bool ArchiveProcedureUnlocked;
        public bool BloodEchoUnlocked;
        public bool CouncilCipherUnlocked;
        public bool ShadowEntryUnlocked;
        public bool StreetDebtUnlocked;
        public bool AncientBloodMandateUnlocked;
        public bool CityDecreeUnlocked;
        public bool CouncilConclaveUnlocked;
        public bool GuildCommandUnlocked;
        public bool PublicLibraryAccessUnlocked;
        public bool ArchiveDocumentTheftUnlocked;
        public bool ProgressionIntroSeen;
        public bool FirstBuildChoiceMade;
        public bool SecondDevelopmentChoiceMade;
        public bool ArchiveWingScalingTestCompleted;
        public bool CouncilGateProgressionTestCompleted;
        public PrototypeQuestStatus ScalingCheckQuestStatus = PrototypeQuestStatus.locked;
        public PrototypeQuestStatus ProgressionBehaviorQuestStatus = PrototypeQuestStatus.locked;
        public PrototypeQuestStatus BuildApproachQuestStatus = PrototypeQuestStatus.locked;
        public PrototypeQuestStatus FinaleQuestStatus = PrototypeQuestStatus.locked;
        public PrototypeQuestStatus MafiaFinaleQuestStatus = PrototypeQuestStatus.locked;
        public FinaleChoice FinaleChoice = FinaleChoice.none;
        public GameOutcome Outcome = GameOutcome.none;
        public bool GameEnded;
        public string EndingSummary;
        public bool QuarantineRouteOpen;
        public bool AntidoteDistributed;
        public bool BlackWarehouseDestroyed;
        public bool ArchiveWingOpen;
        public bool GuardHostile;
        public string ScalingCheckOutcome;
        public string ProgressionBehaviorOutcome;
        public string BuildApproachOutcome;
        public float LastArchiveDocumentTheftTime = -999f;
        public WorldMoodState CityMood = WorldMoodState.stable;
        public WorldControlState OfficeControl = WorldControlState.magistrate;
        public WorldControlState CityControl = WorldControlState.contested;
        public WorldControlState ArchiveControl = WorldControlState.magistrate;
        public WorldControlState CouncilControl = WorldControlState.council;
        public WorldControlState StreetsControl = WorldControlState.mafia;
        public WorldRouteState ArchiveFrontDoorState = WorldRouteState.open;
        public WorldRouteState ArchiveBackDoorState = WorldRouteState.secret;
        public WorldRouteState CouncilSecretLibraryState = WorldRouteState.closed;
        public WorldRouteState DarkStreetsRouteState = WorldRouteState.closed;
        public WorldObjectState ChiefState = WorldObjectState.available;
        public WorldObjectState CouncilScholarState = WorldObjectState.hidden;
        public WorldObjectState MafiaFixerState = WorldObjectState.hidden;
        public WorldObjectState FormerArchivistState = WorldObjectState.hidden;
        public FactionState Council = new FactionState();
        public FactionState Mafia = new FactionState();
        public FactionState Magistrate = new FactionState();
        public WorldState World = new WorldState();
        public LocationState Office = new LocationState();
        public LocationState City = new LocationState();
        public LocationState Archive = new LocationState();
        public List<ReactiveEventKind> ResolvedReactiveEvents = new List<ReactiveEventKind>();
        public ReactiveEventKind ActiveReactiveEvent = ReactiveEventKind.none;
        public float NextReactiveEventTime = 20f;
        public LocationState CouncilLocation = new LocationState();
        public LocationState Streets = new LocationState();
    }

    public sealed class DialogueChoice {
        public readonly string Text;
        public readonly Action Action;

        public DialogueChoice(string text, Action action) {
            Text = text;
            Action = action;
        }
    }
}
