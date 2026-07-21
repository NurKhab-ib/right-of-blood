using System.Text;

namespace RightOfBlood.Prototype {
    /// <summary>Repairs legacy Russian strings that were saved with an incorrect code page.</summary>
    public static class TextNormalizer {
        public static string Normalize(string value) {
            if (string.IsNullOrEmpty(value)) return value;

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

        private static bool LooksLikeMojibake(string value) {
            return value.IndexOf('\u00D0') >= 0 || value.IndexOf('\u00D1') >= 0 || value.IndexOf('\u00C3') >= 0;
        }
    }
}