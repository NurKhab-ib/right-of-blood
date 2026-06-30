using System;

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

    public enum SkillId {
        service_seal,
        archive_procedure,
        blood_echo,
        council_cipher,
        shadow_entry,
        street_debt,
        ancient_blood_mandate,
        public_library_access,
        archive_document_theft
    }


    public enum PrototypeQuestStatus {
        locked,
        active,
        completed
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
        door = 9
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
        public string ScalingCheckOutcome;
        public string ProgressionBehaviorOutcome;
        public string BuildApproachOutcome;
        public float LastArchiveDocumentTheftTime = -999f;
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
