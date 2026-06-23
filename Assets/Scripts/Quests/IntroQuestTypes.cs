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
        public int OfficialInfluence = 1;
        public int ThreatLevel;
        public bool IntroQuestStarted;
        public bool IgnoredFirstHook;
        public bool OfficialAttemptBlocked;
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
