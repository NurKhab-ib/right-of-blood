using System.Text;

namespace RightOfBlood.Prototype {
    /// <summary>Repairs legacy Russian strings that were saved with an incorrect code page.</summary>
    public static class TextNormalizer {
        public static string Normalize(string value) {
            if (string.IsNullOrEmpty(value)) return value;

            if (value.IndexOf("??", System.StringComparison.Ordinal) >= 0) {
                return "\u0421\u043e\u0431\u044b\u0442\u0438\u0435 \u0433\u043e\u0440\u043e\u0434\u0430 \u0442\u0440\u0435\u0431\u0443\u0435\u0442 \u0432\u0430\u0448\u0435\u0433\u043e \u0440\u0435\u0448\u0435\u043d\u0438\u044f.";
            }

            var normalized = value;
            for (var pass = 0; pass < 3 && LooksLikeMojibake(normalized); pass++) {
                try {
                    var decoded = Encoding.UTF8.GetString(Encoding.GetEncoding(1252).GetBytes(normalized));
                    if (decoded == normalized || decoded.IndexOf('\uFFFD') >= 0) break;
                    normalized = decoded;
                }
                catch {
                    break;
                }
            }

            return normalized;
        }

        private static bool LooksLikeUnlocalizedRuntimeText(string value) {
            if (value.Length < 3) return false;
            var latinLetters = 0;
            for (var i = 0; i < value.Length; i++) {
                var character = value[i];
                if ((character >= 'A' && character <= 'Z') || (character >= 'a' && character <= 'z')) latinLetters++;
            }
            return latinLetters >= 3;
        }

        private static bool LooksLikeMojibake(string value) {
            return value.IndexOf('\u00D0') >= 0 || value.IndexOf('\u00D1') >= 0 || value.IndexOf('\u00C3') >= 0;
        }
    }
}