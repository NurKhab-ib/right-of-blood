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

    public enum PrototypeInteractionKind {
        missing_document_desk,
        chief,
        archive_guard,
        council_scholar,
        mafia_fixer,
        former_archivist,
        archive_shelf,
        archive_investigator,
        black_archive_door,
        door
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
        public bool ArchiveInvestigationStarted;
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