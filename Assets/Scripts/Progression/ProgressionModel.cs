using System.Collections.Generic;

namespace RightOfBlood.Prototype {
    public enum SkillKind {
        active,
        passive,
        keystone
    }

    public sealed class ProgressionLevelInfo {
        public readonly int Level;
        public readonly int RequiredReputation;
        public readonly string Reward;
        public readonly string Unlocked;

        public ProgressionLevelInfo(int level, int requiredReputation, string reward, string unlocked) {
            Level = level;
            RequiredReputation = requiredReputation;
            Reward = reward;
            Unlocked = unlocked;
        }
    }

    public sealed class BuildInfo {
        public readonly PlayerBuild Build;
        public readonly string Name;
        public readonly string Archetype;
        public readonly string Fantasy;
        public readonly string Strength;
        public readonly string Weakness;
        public readonly string Resource;
        public readonly string PlayStyle;
        public readonly string Utility;
        public readonly string DominanceRisk;
        public readonly string CounterSituation;

        public BuildInfo(PlayerBuild build, string name, string archetype, string fantasy, string strength, string weakness,
            string resource, string playStyle, string utility, string dominanceRisk, string counterSituation) {
            Build = build;
            Name = name;
            Archetype = archetype;
            Fantasy = fantasy;
            Strength = strength;
            Weakness = weakness;
            Resource = resource;
            PlayStyle = playStyle;
            Utility = utility;
            DominanceRisk = dominanceRisk;
            CounterSituation = counterSituation;
        }
    }

    public sealed class SkillInfo {
        public readonly SkillId Id;
        public readonly string Name;
        public readonly PlayerBuild Branch;
        public readonly SkillKind Kind;
        public readonly int RequiredLevel;
        public readonly SkillId? RequiredSkill;
        public readonly string Description;

        public SkillInfo(SkillId id, string name, PlayerBuild branch, SkillKind kind, int requiredLevel,
            SkillId? requiredSkill, string description) {
            Id = id;
            Name = name;
            Branch = branch;
            Kind = kind;
            RequiredLevel = requiredLevel;
            RequiredSkill = requiredSkill;
            Description = description;
        }
    }

    public static class ProgressionModel {
        public const int ReputationForSecondLevel = 2;
        public const int ReputationForThirdLevel = 4;

        public static readonly ProgressionLevelInfo[] Levels = {
            new ProgressionLevelInfo(1, 0, "Первый статус ветки и базовый навык", "переход во 2-й этап при 2 репутации ветки"),
            new ProgressionLevelInfo(2, ReputationForSecondLevel, "Усиленный доступ и новая способность", "переход в 3-й этап при 4 репутации ветки"),
            new ProgressionLevelInfo(3, ReputationForThirdLevel, "Верхний статус ветки и ключевой доступ", "постоянный проход в закрытые зоны своей ветки")
        };

        public static readonly BuildInfo[] Builds = {
            new BuildInfo(PlayerBuild.magistrate, "Магистрат", "Бюрократ / управленец",
                "Решать проблемы служебным влиянием, указами и стражей.",
                "Высокое влияние на город, большой круг связей, подчинение стражи.",
                "Ограничен законом и вынужден действовать публично.",
                "Служебное влияние", "официальный, законопослушный", "больше ресурсов и доступ к рабочему архиву",
                "слишком широкий контроль над NPC", "действия в тёмном переулке и среди мафии"),
            new BuildInfo(PlayerBuild.sage, "Мудрец", "Учёный Совета",
                "Решать проблемы знаниями, связями в Совете и тайнами крови.",
                "Максимум знаний, доступ к зданию Совета и достаточный круг связей.",
                "Формально ограничен законом и слабее влияет на город напрямую.",
                "Знания", "аналитический", "лоровые знания и доступ к библиотеке Совета",
                "ранний полноценный доступ к знаниям", "нет доступа к государственным учреждениям без посредников"),
            new BuildInfo(PlayerBuild.rogue, "Разбойник", "Авторитет улицы / мафия",
                "Решать проблемы через скрытность, силу и связи в мафиозных кругах.",
                "Свободное применение силы и доступ к тёмному переулку.",
                "Преследование законом, меньше знаний и риск предательства.",
                "Уличный авторитет", "негодяй", "чёрный рынок, обходные пути и контент тёмного переулка",
                "скрытность ломает препятствия", "проверки законом и предательство мафии")
        };

        public static readonly SkillInfo[] Skills = {
            new SkillInfo(SkillId.service_seal, "Служебная печать", PlayerBuild.magistrate, SkillKind.active, 1, null,
                "Активно: провести официальный приказ. Даёт служебное влияние и облегчает законные проверки."),
            new SkillInfo(SkillId.archive_procedure, "Архивный регламент", PlayerBuild.magistrate, SkillKind.passive, 2,
                SkillId.service_seal, "Пассивно: снижает риск провала в архиве и открывает доступ к одному делу без взятки."),
            new SkillInfo(SkillId.public_library_access, "Доступ в публичную библиотеку знаний", PlayerBuild.sage, SkillKind.passive, 1, null,
                "Пассивно: прихожанин Совета получает +1 к знаниям через открытую часть библиотеки."),
            new SkillInfo(SkillId.blood_echo, "Эхо крови", PlayerBuild.sage, SkillKind.active, 2,
                SkillId.public_library_access, "Активно: почувствовать след древнего рода в документе и получить подсказку без посредника."),
            new SkillInfo(SkillId.council_cipher, "Шифр Совета", PlayerBuild.sage, SkillKind.passive, 2,
                SkillId.blood_echo, "Пассивно: читать закрытые пометки Совета и обходить часть социальных проверок знаниями."),
            new SkillInfo(SkillId.shadow_entry, "Теневой вход", PlayerBuild.rogue, SkillKind.active, 1, null,
                "Активно: воспользоваться обходным маршрутом, ускоряет игрока и открывает тёмные переулки."),
            new SkillInfo(SkillId.street_debt, "Уличный долг", PlayerBuild.rogue, SkillKind.passive, 2,
                SkillId.shadow_entry, "Пассивно: мафия один раз гасит провал проверки, но угроза растёт."),
            new SkillInfo(SkillId.archive_document_theft, "Кража архивного документа", PlayerBuild.rogue, SkillKind.active, 2,
                SkillId.shadow_entry, "Активно: раз в короткий промежуток времени украсть документ из архива и получить +1 к знаниям."),
            new SkillInfo(SkillId.ancient_blood_mandate, "Право крови", PlayerBuild.undecided, SkillKind.keystone, 3, null,
                "Ключевая способность: предъявить древнее происхождение и заставить закрытую дверь открыться без обычной проверки.")
        };

        public static int GetRequiredReputation(int level) {
            for (var i = 0; i < Levels.Length; i++) {
                if (Levels[i].Level == level) return Levels[i].RequiredReputation;
            }

            return int.MaxValue;
        }

        public static BuildInfo GetBuild(PlayerBuild build) {
            for (var i = 0; i < Builds.Length; i++) {
                if (Builds[i].Build == build) return Builds[i];
            }

            return null;
        }

        public static SkillInfo GetSkill(SkillId id) {
            for (var i = 0; i < Skills.Length; i++) {
                if (Skills[i].Id == id) return Skills[i];
            }

            return null;
        }

        public static IEnumerable<SkillInfo> GetBranch(PlayerBuild build) {
            for (var i = 0; i < Skills.Length; i++) {
                if (Skills[i].Branch == build) yield return Skills[i];
            }
        }
    }
}
