using System;
using System.Text;
using UnityEngine;

namespace RightOfBlood.Prototype {
    public sealed class BalanceHandler {
        private readonly IntroQuestState state;
        private readonly Func<LocationId> currentLocationGetter;

        private int manualThreatOffset;
        private int lastBaseThreat;
        private int lastFinalThreat;
        private string lastCalculation = "Расчёт ещё не выполнялся.";

        public BalanceHandler(IntroQuestState state, Func<LocationId> currentLocationGetter) {
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            this.currentLocationGetter = currentLocationGetter;
            RecalculateThreat("Инициализация баланса");
        }

        public void ApplyInfluenceDelta(int delta, string source) {
            ApplyStatDelta(() => state.OfficialInfluence, value => state.OfficialInfluence = value, delta, source, "Влияние");
        }

        public void ApplyKnowledgeDelta(int delta, string source) {
            ApplyStatDelta(() => state.CouncilReputation, value => state.CouncilReputation = value, delta, source, "Знания");
        }

        public void ApplyStrengthDelta(int delta, string source) {
            ApplyStatDelta(() => state.MafiaReputation, value => state.MafiaReputation = value, delta, source, "Сила");
        }

        public void ApplyOfficialReputationDelta(int delta, string source) {
            ApplyInfluenceDelta(delta, source);
        }

        public void ApplyBranchSupport(PlayerBuild supportedBuild, int supportedDelta, int competitorDelta, string source) {
            switch (supportedBuild) {
                case PlayerBuild.magistrate:
                    ApplyInfluenceDelta(supportedDelta, source);
                    if (competitorDelta != 0) {
                        ApplyKnowledgeDelta(competitorDelta, source);
                        ApplyStrengthDelta(competitorDelta, source);
                    }
                    break;
                case PlayerBuild.sage:
                    ApplyKnowledgeDelta(supportedDelta, source);
                    if (competitorDelta != 0) {
                        ApplyInfluenceDelta(competitorDelta, source);
                        ApplyStrengthDelta(competitorDelta, source);
                    }
                    break;
                case PlayerBuild.rogue:
                    ApplyStrengthDelta(supportedDelta, source);
                    if (competitorDelta != 0) {
                        ApplyInfluenceDelta(competitorDelta, source);
                        ApplyKnowledgeDelta(competitorDelta, source);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(supportedBuild), supportedBuild, null);
            }
        }

        public void SetMinimumInfluence(int minimum, string source) {
            if (state.OfficialInfluence >= minimum) return;
            ApplyInfluenceDelta(minimum - state.OfficialInfluence, source);
        }

        public void SetMinimumKnowledge(int minimum, string source) {
            if (state.CouncilReputation >= minimum) return;
            ApplyKnowledgeDelta(minimum - state.CouncilReputation, source);
        }

        public void SetMinimumStrength(int minimum, string source) {
            if (state.MafiaReputation >= minimum) return;
            ApplyStrengthDelta(minimum - state.MafiaReputation, source);
        }

        public void ApplyThreatDelta(int delta, string source) {
            if (delta == 0) return;
            manualThreatOffset += delta;
            RecalculateThreat(source);
        }

        public void RecalculateThreat(string source) {
            lastBaseThreat = GetStageBaseThreat();
            var cityModifier = GetCityModifier();
            var influencePressure = Mathf.Max(0, state.OfficialInfluence - state.Level);
            var knowledgePressure = Mathf.Max(0, state.CouncilReputation - state.Level);
            var strengthPressure = Mathf.Max(0, state.MafiaReputation - state.Level);
            var stagePressure = Mathf.Max(0, state.Level - 1);

            var finalThreat = lastBaseThreat + cityModifier + manualThreatOffset + stagePressure + strengthPressure - influencePressure - knowledgePressure;
            lastFinalThreat = Mathf.Max(0, finalThreat);
            state.ThreatLevel = lastFinalThreat;

            lastCalculation = $"{source}: база {lastBaseThreat}, город {cityModifier}, ручные поправки {manualThreatOffset}, " +
                              $"влияние -{influencePressure}, знания -{knowledgePressure}, сила +{strengthPressure}, " +
                              $"итог {lastFinalThreat}";
        }

        public void LogSkillActivation(SkillId skillId, string source) {
            var skill = ProgressionModel.GetSkill(skillId);
            var skillName = skill == null ? skillId.ToString() : skill.Name;
            Debug.Log($"[Balance] Навык активен: {skillName}. Источник: {source}");
        }

        public string BuildQuestRewardPreview(string title, int coefficient, int influenceBase, int knowledgeBase, int strengthBase, int reputationBase, string reputationLabel) {
            var influence = coefficient * influenceBase;
            var knowledge = coefficient * knowledgeBase;
            var strength = coefficient * strengthBase;
            var reputation = coefficient * reputationBase;

            var builder = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(title)) {
                builder.AppendLine(title);
            }

            builder.AppendLine($"Коэффициент: S{coefficient}");
            builder.AppendLine($"Влияние: {FormatDelta(influenceBase)} × S = {FormatDelta(influence)}");
            builder.AppendLine($"Знания: {FormatDelta(knowledgeBase)} × S = {FormatDelta(knowledge)}");
            builder.AppendLine($"Сила: {FormatDelta(strengthBase)} × S = {FormatDelta(strength)}");
            builder.AppendLine($"{reputationLabel}: {FormatDelta(reputationBase)} × S = {FormatDelta(reputation)}");
            return builder.ToString().TrimEnd();
        }

        public string GetDebugText() {
            var builder = new StringBuilder();
            builder.AppendLine("Б А Л А Н С И Р О В К А А А А");
            builder.AppendLine($"Этап: {state.Level}");
            builder.AppendLine($"Влияние: {state.OfficialInfluence}");
            builder.AppendLine($"Знания: {state.CouncilReputation}");
            builder.AppendLine($"Сила: {state.MafiaReputation}");
            // builder.AppendLine($"Состояние города: {GetCityStateLabel()}");
            builder.AppendLine($"Базовая угроза этапа: {lastBaseThreat}");
            builder.AppendLine($"Итоговая угроза: {state.ThreatLevel}");
            builder.AppendLine($"Последний расчёт: {lastCalculation}");
            return builder.ToString().TrimEnd();
        }

        public string GetLastCalculation() {
            return lastCalculation;
        }

        private void ApplyStatDelta(Func<int> getter, Action<int> setter, int delta, string source, string label) {
            if (delta == 0) return;

            var nextValue = Mathf.Max(0, getter() + delta);
            setter(nextValue);
            Debug.Log($"[Balance] {label} {FormatDelta(delta)} ({source}). Теперь: {nextValue}");
            RecalculateThreat(source);
        }

        private int GetStageBaseThreat() {
            switch (state.Level) {
                case 1: return 1;
                case 2: return 2;
                case 3: return 3;
                default: return Mathf.Max(1, state.Level);
            }
        }

        private int GetCityModifier() {
            var location = currentLocationGetter == null ? LocationId.office : currentLocationGetter();
            switch (location) {
                case LocationId.office: return 0;
                case LocationId.city: return 1;
                case LocationId.archive: return 2;
                case LocationId.council: return 1;
                case LocationId.streets: return 2;
                default: return 0;
            }
        }

        private string GetCityStateLabel() {
            return $"{currentLocationGetter?.Invoke() ?? LocationId.office}";
        }

        private static string FormatDelta(int value) {
            return value >= 0 ? $"+{value}" : value.ToString();
        }
    }
}
