using System.Collections.Generic;
using UnityEngine;

namespace RightOfBlood.Prototype {
    public partial class QuestGame {
        private ReactiveEventVisitor activeEventVisitor;

        private void UpdateReactiveEvents() {
            if (IsTerminal || dialogueOpen || player == null || state.Build == PlayerBuild.undecided || state.ActiveReactiveEvent != ReactiveEventKind.none || Time.time < state.NextReactiveEventTime) return;
            var kind = GetAvailableReactiveEvent();
            if (kind != ReactiveEventKind.none) SpawnReactiveEvent(kind);
        }

        private ReactiveEventKind GetAvailableReactiveEvent() {
            var candidates = new List<ReactiveEventKind>();
            AddEventCandidate(candidates, PlayerBuild.magistrate, ReactiveEventKind.magistrate_petition, ReactiveEventKind.magistrate_audit, ReactiveEventKind.magistrate_riot);
            AddEventCandidate(candidates, PlayerBuild.sage, ReactiveEventKind.council_specimen, ReactiveEventKind.council_manuscript, ReactiveEventKind.council_leak);
            AddEventCandidate(candidates, PlayerBuild.rogue, ReactiveEventKind.mafia_debt, ReactiveEventKind.mafia_protection, ReactiveEventKind.mafia_succession);
            return candidates.Count == 0 ? ReactiveEventKind.none : candidates[Random.Range(0, candidates.Count)];
        }

        private void AddEventCandidate(List<ReactiveEventKind> candidates, PlayerBuild build, ReactiveEventKind first, ReactiveEventKind second, ReactiveEventKind third) {
            if (state.Build != build) return;
            var kind = state.Level >= 3 ? third : state.Level == 2 ? second : first;
            if (!state.ResolvedReactiveEvents.Contains(kind)) candidates.Add(kind);
        }

        private void SpawnReactiveEvent(ReactiveEventKind kind) {
            var template = FindEventNpcTemplate();
            var location = GetLocation(currentLocation);
            if (template == null || location == null) return;
            var npc = Instantiate(template.gameObject, location.transform);
            npc.name = "\u0421\u043e\u0431\u044b\u0442\u0438\u0435: " + EventLabel(kind);
            npc.transform.position = player.transform.position + new Vector3(0.9f, 0.15f, 0f);
            var interactable = npc.GetComponent<Interactable>();
            interactable.ConfigureRuntime("\u0421\u043e\u0431\u044b\u0442\u0438\u0435: " + EventLabel(kind), PrototypeInteractionKind.event_messenger);
            activeEventVisitor = npc.GetComponent<ReactiveEventVisitor>() ?? npc.AddComponent<ReactiveEventVisitor>();
            activeEventVisitor.Kind = kind;
            (npc.GetComponent<WorldObject>() ?? npc.AddComponent<WorldObject>()).SetState(WorldObjectState.available);
            interactables.Add(interactable);
            state.ActiveReactiveEvent = kind;
            state.NextReactiveEventTime = Time.time + 45f;
            ShowMessage("\u0421\u043b\u0443\u0447\u0430\u0439\u043d\u043e\u0435 \u0441\u043e\u0431\u044b\u0442\u0438\u0435", "\u0420\u044f\u0434\u043e\u043c \u043f\u043e\u044f\u0432\u0438\u043b\u0441\u044f \u0433\u043e\u0440\u043e\u0436\u0430\u043d\u0438\u043d: " + EventLabel(kind) + ". \u0415\u0433\u043e \u0441\u0443\u0434\u044c\u0431\u0430 \u0437\u0430\u0432\u0438\u0441\u0438\u0442 \u043e\u0442 \u0432\u0430\u0448\u0435\u0433\u043e \u0440\u0435\u0448\u0435\u043d\u0438\u044f.");
        }

        private Interactable FindEventNpcTemplate() {
            foreach (var interactable in interactables) if (interactable != null && WorldObject.IsNpc(interactable.Kind)) return interactable;
            return null;
        }

        private void TalkToReactiveEvent(ReactiveEventVisitor visitor) {
            if (visitor == null || visitor != activeEventVisitor || visitor.Kind != state.ActiveReactiveEvent) return;
            ShowDialogue(EventSpeaker(visitor.Kind), EventText(visitor.Kind), EventChoices(visitor.Kind));
        }

        private IReadOnlyList<DialogueChoice> EventChoices(ReactiveEventKind kind) {
            var choices = new List<DialogueChoice>();
            var labels = EventOptions(kind);
            choices.Add(new DialogueChoice(GetEventChoiceLabel(kind, true), () => ResolveReactiveEvent(labels[2], labels[4], labels[5], labels[6], labels[7])));
            choices.Add(new DialogueChoice(GetEventChoiceLabel(kind, false), () => ResolveReactiveEvent(labels[3], labels[8], labels[9], labels[10], labels[11])));
            choices.Add(new DialogueChoice("\u0417\u0430\u043a\u0440\u044b\u0442\u044c \u0434\u0438\u0430\u043b\u043e\u0433", CloseDialogue));
            return choices;
        }

        private string[] EventOptions(ReactiveEventKind kind) {
            switch (kind) {
                case ReactiveEventKind.magistrate_petition: return new[] { "Process petition", "Bypass the queue", "The petition enters the register.", "The family is saved quickly, but officials notice the breach.", "1", "0", "0", "0", "0", "0", "0", "1" };
                case ReactiveEventKind.magistrate_audit: return new[] { "Seize the file", "Demand proof", "The archive route is cleared, but the Mafia loses leverage.", "The Council shares a disputed record.", "1", "0", "-1", "0", "0", "1", "0", "0" };
                case ReactiveEventKind.magistrate_riot: return new[] { "Declare quarantine", "Start an inquiry", "Guards close the quarter; order returns at a cost.", "You name the guilty. The conflict becomes public.", "1", "0", "-1", "0", "2", "0", "-1", "2" };
                case ReactiveEventKind.council_specimen: return new[] { "Give a blood sample", "Demand protocol", "The experiment reveals a new lead.", "You obtain the protocol without becoming a subject.", "0", "1", "0", "1", "0", "1", "0", "0" };
                case ReactiveEventKind.council_manuscript: return new[] { "Give it to Council", "Hide the manuscript", "Council recognizes your standing, but seals the original.", "The knowledge remains yours; keepers grow suspicious.", "0", "2", "0", "0", "0", "1", "0", "1" };
                case ReactiveEventKind.council_leak: return new[] { "Publish truth", "Seal the leak", "The city learns of the blood pact.", "Panic is contained, but people distrust the closed circle.", "0", "1", "-1", "2", "0", "2", "0", "0" };
                case ReactiveEventKind.mafia_debt: return new[] { "Pay with a service", "Hand debtor to guards", "The streets remember the debt being paid.", "The guards approve; the streets mark you unreliable.", "0", "0", "1", "0", "1", "0", "-1", "1" };
                case ReactiveEventKind.mafia_protection: return new[] { "Protect the shop", "Make peace", "The shop pays you; a rival gang takes offence.", "Bloodshed is avoided, but authority grows less.", "0", "0", "2", "1", "0", "0", "1", "0" };
                case ReactiveEventKind.mafia_succession: return new[] { "Recruit the guard", "Order execution", "You gain a source inside the office.", "The streets obey through fear; a raid begins.", "1", "0", "2", "1", "0", "0", "3", "3" };
                default: return new[] { "Continue", "Leave", "", "", "0", "0", "0", "0", "0", "0", "0", "0" };
            }
        }

        private void ResolveReactiveEvent(string result, string influence, string knowledge, string strength, string threat) {
            var resolved = state.ActiveReactiveEvent;
            var influenceDelta = int.Parse(influence);
            var knowledgeDelta = int.Parse(knowledge);
            var strengthDelta = int.Parse(strength);
            var threatDelta = int.Parse(threat);
            ApplyReactiveDeltas(influenceDelta, knowledgeDelta, strengthDelta, threatDelta);
            if (resolved == ReactiveEventKind.magistrate_riot) state.QuarantineRouteOpen = true;
            if (resolved == ReactiveEventKind.magistrate_audit || resolved == ReactiveEventKind.mafia_protection) state.BlackArchiveEntranceKnown = true;
            state.ResolvedReactiveEvents.Add(resolved);
            state.ActiveReactiveEvent = ReactiveEventKind.none;
            if (activeEventVisitor != null) { interactables.Remove(activeEventVisitor.GetComponent<Interactable>()); Destroy(activeEventVisitor.gameObject); }
            activeEventVisitor = null;
            RefreshProgressionFromReputation();
            ShowMessage("\u041f\u043e\u0441\u043b\u0435\u0434\u0441\u0442\u0432\u0438\u0435: " + EventLabel(resolved), EventResultSummary(influenceDelta, knowledgeDelta, strengthDelta, threatDelta));
        }

        private void ApplyReactiveDeltas(int influence, int knowledge, int strength, int threat) {
            if (influence != 0) ApplyInfluenceDelta(influence, "\u0421\u043b\u0443\u0447\u0430\u0439\u043d\u043e\u0435 \u0441\u043e\u0431\u044b\u0442\u0438\u0435");
            if (knowledge != 0) ApplyKnowledgeDelta(knowledge, "\u0421\u043b\u0443\u0447\u0430\u0439\u043d\u043e\u0435 \u0441\u043e\u0431\u044b\u0442\u0438\u0435");
            if (strength != 0) ApplyStrengthDelta(strength, "\u0421\u043b\u0443\u0447\u0430\u0439\u043d\u043e\u0435 \u0441\u043e\u0431\u044b\u0442\u0438\u0435");
            if (threat != 0) ApplyThreatDelta(threat, "\u0421\u043b\u0443\u0447\u0430\u0439\u043d\u043e\u0435 \u0441\u043e\u0431\u044b\u0442\u0438\u0435");
        }

        private static string GetEventChoiceLabel(ReactiveEventKind kind, bool first) {
            if (kind.ToString().StartsWith("magistrate")) return first ? "\u0420\u0435\u0448\u0438\u0442\u044c \u043f\u043e \u0437\u0430\u043a\u043e\u043d\u0443" : "\u0414\u0435\u0439\u0441\u0442\u0432\u043e\u0432\u0430\u0442\u044c \u0432 \u043e\u0431\u0445\u043e\u0434 \u043f\u0440\u0430\u0432\u0438\u043b";
            if (kind.ToString().StartsWith("council")) return first ? "\u041f\u043e\u0434\u0434\u0435\u0440\u0436\u0430\u0442\u044c \u0438\u0441\u0441\u043b\u0435\u0434\u043e\u0432\u0430\u043d\u0438\u0435" : "\u0421\u043e\u0445\u0440\u0430\u043d\u0438\u0442\u044c \u043a\u043e\u043d\u0442\u0440\u043e\u043b\u044c";
            return first ? "\u0423\u043a\u0440\u0435\u043f\u0438\u0442\u044c \u043f\u043e\u0437\u0438\u0446\u0438\u0438 \u043d\u0430 \u0443\u043b\u0438\u0446\u0430\u0445" : "\u0418\u0441\u043a\u0430\u0442\u044c \u043c\u0438\u0440\u043d\u044b\u0439 \u0432\u044b\u0445\u043e\u0434";
        }

        private static string EventResultSummary(int influence, int knowledge, int strength, int threat) {
            return "\u0420\u0435\u0448\u0435\u043d\u0438\u0435 \u043f\u0440\u0438\u043d\u044f\u0442\u043e. \u0412\u043b\u0438\u044f\u043d\u0438\u0435 " + FormatSignedDelta(influence) + "; \u0437\u043d\u0430\u043d\u0438\u044f " + FormatSignedDelta(knowledge) + "; \u0441\u0438\u043b\u0430 \u041c\u0430\u0444\u0438\u0438 " + FormatSignedDelta(strength) + "; \u0443\u0433\u0440\u043e\u0437\u0430 " + FormatSignedDelta(threat) + ".";
        }

        private static string FormatSignedDelta(int value) { return value >= 0 ? "+" + value : value.ToString(); }
        private static string EventLabel(ReactiveEventKind kind) {
            switch (kind) {
                case ReactiveEventKind.magistrate_petition: return "\u0421\u0440\u043e\u0447\u043d\u043e\u0435 \u043f\u0440\u043e\u0448\u0435\u043d\u0438\u0435";
                case ReactiveEventKind.magistrate_audit: return "\u041f\u0440\u043e\u0432\u0435\u0440\u043a\u0430 \u0410\u0440\u0445\u0438\u0432\u0430";
                case ReactiveEventKind.magistrate_riot: return "\u0411\u0435\u0441\u043f\u043e\u0440\u044f\u0434\u043a\u0438 \u0432 \u043a\u0432\u0430\u0440\u0442\u0430\u043b\u0435";
                case ReactiveEventKind.council_specimen: return "\u041e\u043f\u044b\u0442 \u0441 \u043a\u0440\u043e\u0432\u044c\u044e";
                case ReactiveEventKind.council_manuscript: return "\u0421\u043f\u043e\u0440\u043d\u0430\u044f \u0440\u0443\u043a\u043e\u043f\u0438\u0441\u044c";
                case ReactiveEventKind.council_leak: return "\u0423\u0442\u0435\u0447\u043a\u0430 \u0437\u043d\u0430\u043d\u0438\u0439";
                case ReactiveEventKind.mafia_debt: return "\u0423\u043b\u0438\u0447\u043d\u044b\u0439 \u0434\u043e\u043b\u0433";
                case ReactiveEventKind.mafia_protection: return "\u0421\u043f\u043e\u0440 \u0437\u0430 \u0437\u0430\u0449\u0438\u0442\u0443";
                case ReactiveEventKind.mafia_succession: return "\u041f\u0440\u0438\u043a\u0430\u0437 \u0433\u0438\u043b\u044c\u0434\u0438\u0438";
                default: return "\u0421\u0440\u043e\u0447\u043d\u043e\u0435 \u0434\u0435\u043b\u043e";
            }
        }

        private static string EventSpeaker(ReactiveEventKind kind) {
            return kind.ToString().StartsWith("magistrate") ? "\u0413\u043e\u0440\u043e\u0436\u0430\u043d\u0438\u043d" : kind.ToString().StartsWith("council") ? "\u041f\u043e\u0441\u043b\u0430\u043d\u043d\u0438\u043a \u0421\u043e\u0432\u0435\u0442\u0430" : "\u0423\u043b\u0438\u0447\u043d\u044b\u0439 \u043f\u043e\u0441\u0440\u0435\u0434\u043d\u0438\u043a";
        }

        private static string EventText(ReactiveEventKind kind) {
            return "\u041f\u0435\u0440\u0435\u0434 \u0432\u0430\u043c\u0438 \u0434\u0435\u043b\u043e: " + EventLabel(kind) + ". \u0412\u0430\u0448 \u0432\u044b\u0431\u043e\u0440 \u043c\u0435\u043d\u044f\u0435\u0442 \u043f\u043e\u043a\u0430\u0437\u0430\u0442\u0435\u043b\u0438 \u0433\u043e\u0440\u043e\u0434\u0430; \u0438\u0442\u043e\u0433 \u0431\u0443\u0434\u0435\u0442 \u043f\u043e\u043a\u0430\u0437\u0430\u043d \u043f\u043e\u0441\u043b\u0435 \u0440\u0435\u0448\u0435\u043d\u0438\u044f.";
        }
}
}
