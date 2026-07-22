using System.Collections.Generic;

namespace RightOfBlood.Prototype {
    public partial class QuestGame {
        private void OfferTopStatusAuthority() {
            if (state.CityDecreeUnlocked) {
                ShowDialogue("City Decree", "Your seal is enough to change the district regime.", new[] {
                    new DialogueChoice("Declare quarantine", () => ResolveTopAuthority("Quarantine strengthens official routes and angers the streets.", 1, 0, -1, 0, true)),
                    new DialogueChoice("Redirect the guard", () => ResolveTopAuthority("One district is safer; another is left exposed.", 1, 0, 0, 1, false)),
                    new DialogueChoice("Cancel", CloseDialogue) });
                return;
            }
            if (state.CouncilConclaveUnlocked) {
                ShowDialogue("Council Conclave", "Your vote decides whether knowledge is opened or sealed.", new[] {
                    new DialogueChoice("Open research", () => ResolveTopAuthority("The Council opens blood research; knowledge and danger both grow.", 0, 2, 0, 1, false)),
                    new DialogueChoice("Seal the leak", () => ResolveTopAuthority("The records are sealed under your authority.", 0, 1, 0, 0, true)),
                    new DialogueChoice("Cancel", CloseDialogue) });
                return;
            }
            if (state.GuildCommandUnlocked) {
                ShowDialogue("Guild Command", "The streets wait to learn whom you protect and whom you make an example of.", new[] {
                    new DialogueChoice("Protect the district", () => ResolveTopAuthority("The Guild secures the district and its hidden routes.", 0, 0, 2, 1, true)),
                    new DialogueChoice("Order a raid", () => ResolveTopAuthority("The gang crushes resistance; authority rises and guards answer with a raid.", 0, 0, 3, 3, false)),
                    new DialogueChoice("Cancel", CloseDialogue) });
            }
        }

        private void ResolveTopAuthority(string result, int influence, int knowledge, int strength, int threat, bool openRoute) {
            if (influence != 0) ApplyInfluenceDelta(influence, "S3 authority");
            if (knowledge != 0) ApplyKnowledgeDelta(knowledge, "S3 authority");
            if (strength != 0) ApplyStrengthDelta(strength, "S3 authority");
            if (threat != 0) ApplyThreatDelta(threat, "S3 authority");
            if (state.CityDecreeUnlocked) state.QuarantineRouteOpen = true;
            if (openRoute) {
                state.BlackArchiveEntranceKnown = true;
                state.CriminalWorldAccess = state.CriminalWorldAccess || state.GuildCommandUnlocked;
                state.SecretLibraryAccess = state.SecretLibraryAccess || state.CouncilConclaveUnlocked;
            }
            ShowMessage("Authority consequence", result);
        }
    }
}
