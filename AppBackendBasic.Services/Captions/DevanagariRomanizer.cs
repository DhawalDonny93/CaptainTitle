using System.Text;

namespace AppBackendBasic.Services.Captions;

/// <summary>
/// Devanagari -> Roman fallback romanization (port of caption_ai._romanize_fallback / _common_hindi_spelling). Used when the LLM drops a word's index
/// or hands back raw Devanagari, so word.text (the field "Roman Hinglish" mode renders everywhere) is never native script.
/// The old code used indic_transliteration's OPTITRANS scheme, a literal Sanskrit-style transliteration (anusvara -> "m", no word-final schwa deletion),
/// so a small table of the highest-frequency Hindi words with their real Hinglish spelling is consulted first.
/// </summary>
public static class DevanagariRomanizer
{
    // Highest-frequency words in ordinary Hindi speech (pronouns, copula, negation, common verbs, postpositions) - exactly what the raw scheme gets most visibly wrong.
    private static readonly Dictionary<string, string> Common = new()
    {
        ["है"] = "hai", ["हैं"] = "hain", ["था"] = "tha", ["थी"] = "thi", ["थे"] = "the",
        ["हूं"] = "hoon", ["हूँ"] = "hoon", ["हो"] = "ho", ["होगा"] = "hoga", ["होगी"] = "hogi",
        ["होंगे"] = "honge", ["होना"] = "hona", ["रहा"] = "raha", ["रही"] = "rahi", ["रहे"] = "rahe",
        ["मैं"] = "main", ["मुझे"] = "mujhe", ["मुझको"] = "mujhko", ["मेरा"] = "mera", ["मेरी"] = "meri",
        ["मेरे"] = "mere", ["हम"] = "hum", ["हमें"] = "hamein", ["हमारा"] = "hamara", ["हमारी"] = "hamari",
        ["हमारे"] = "hamare", ["तू"] = "tu", ["तुझे"] = "tujhe", ["तेरा"] = "tera", ["तुम"] = "tum",
        ["तुम्हें"] = "tumhein", ["तुम्हारा"] = "tumhara", ["आप"] = "aap", ["आपका"] = "aapka",
        ["आपकी"] = "aapki", ["आपके"] = "aapke", ["आपको"] = "aapko", ["वह"] = "woh", ["वो"] = "woh",
        ["उसे"] = "use", ["उसका"] = "uska", ["उसकी"] = "uski", ["उसके"] = "uske",
        ["ये"] = "ye", ["यह"] = "yeh", ["इसे"] = "ise", ["इसका"] = "iska", ["इसकी"] = "iski", ["इसके"] = "iske",
        ["वे"] = "ve", ["उन्हें"] = "unhein", ["उनका"] = "unka", ["उनकी"] = "unki", ["उनके"] = "unke",
        ["नहीं"] = "nahin", ["मत"] = "mat", ["ना"] = "na",
        ["क्या"] = "kya", ["कौन"] = "kaun", ["कहाँ"] = "kahan", ["कहां"] = "kahan", ["कब"] = "kab",
        ["क्यों"] = "kyun", ["कैसे"] = "kaise", ["कितना"] = "kitna", ["कितनी"] = "kitni", ["कितने"] = "kitne",
        ["और"] = "aur", ["या"] = "ya", ["लेकिन"] = "lekin", ["पर"] = "par", ["भी"] = "bhi", ["तो"] = "to",
        ["ही"] = "hi", ["जो"] = "jo", ["कि"] = "ki", ["से"] = "se", ["को"] = "ko", ["का"] = "ka",
        ["की"] = "ki", ["के"] = "ke", ["में"] = "mein", ["पे"] = "pe", ["तक"] = "tak", ["साथ"] = "saath",
        ["बिना"] = "bina",
        ["करना"] = "karna", ["करता"] = "karta", ["करती"] = "karti", ["करते"] = "karte", ["किया"] = "kiya",
        ["कर"] = "kar", ["करो"] = "karo", ["करें"] = "karein", ["करूंगा"] = "karunga",
        ["जाना"] = "jana", ["जाता"] = "jata", ["जाती"] = "jati", ["जाते"] = "jate", ["गया"] = "gaya",
        ["गयी"] = "gayi", ["गई"] = "gai", ["गए"] = "gaye", ["जाओ"] = "jao",
        ["आना"] = "aana", ["आता"] = "aata", ["आती"] = "aati", ["आते"] = "aate", ["आया"] = "aaya",
        ["आई"] = "aai", ["आए"] = "aaye",
        ["देना"] = "dena", ["देता"] = "deta", ["दिया"] = "diya", ["लेना"] = "lena", ["लेता"] = "leta",
        ["लिया"] = "liya", ["कहना"] = "kehna", ["कहा"] = "kaha",
        ["सुनना"] = "sunna", ["सुना"] = "suna", ["देखना"] = "dekhna", ["देखा"] = "dekha", ["देखो"] = "dekho",
        ["समझना"] = "samajhna", ["समझा"] = "samjha", ["चाहिए"] = "chahiye", ["चाहता"] = "chahta",
        ["चाहती"] = "chahti", ["सकता"] = "sakta", ["सकती"] = "sakti", ["सकते"] = "sakte",
        ["मिलना"] = "milna", ["मिला"] = "mila", ["रखना"] = "rakhna", ["रखा"] = "rakha",
        ["बहुत"] = "bahut", ["अच्छा"] = "accha", ["अच्छी"] = "acchi", ["अच्छे"] = "acche",
        ["बड़ा"] = "bada", ["बड़ी"] = "badi", ["छोटा"] = "chota", ["नया"] = "naya",
        ["सब"] = "sab", ["सभी"] = "sabhi", ["कुछ"] = "kuch", ["कोई"] = "koi", ["हर"] = "har",
        ["अपना"] = "apna", ["अपनी"] = "apni", ["अपने"] = "apne", ["खुद"] = "khud",
        ["एक"] = "ek", ["दो"] = "do", ["तीन"] = "teen",
        ["ऐसा"] = "aisa", ["ऐसी"] = "aisi", ["ऐसे"] = "aise", ["वैसा"] = "vaisa", ["वैसी"] = "vaisi",
        ["जैसा"] = "jaisa", ["जैसी"] = "jaisi", ["जैसे"] = "jaise", ["कैसा"] = "kaisa", ["कैसी"] = "kaisi",
        ["थोड़ा"] = "thoda", ["थोड़ी"] = "thodi", ["ज्यादा"] = "zyada", ["ज़्यादा"] = "zyada",
        ["शायद"] = "shayad", ["ज़रूर"] = "zaroor", ["जरूर"] = "zaroor", ["यार"] = "yaar",
        ["अभी"] = "abhi", ["फिर"] = "phir", ["सच"] = "sach", ["झूठ"] = "jhooth", ["पता"] = "pata",
        ["समय"] = "samay", ["वक़्त"] = "waqt", ["वक्त"] = "waqt", ["बात"] = "baat", ["काम"] = "kaam",
        ["दिन"] = "din", ["रात"] = "raat", ["घर"] = "ghar", ["यहाँ"] = "yahan", ["यहां"] = "yahan",
        ["वहाँ"] = "wahan", ["वहां"] = "wahan",
    };

    private const char Virama = '्';
    private const char Nukta = '़';

    // OPTITRANS scheme (indic_transliteration), Devanagari -> Roman.
    private static readonly Dictionary<char, string> Vowels = new()
    {
        ['अ'] = "a", ['आ'] = "A", ['इ'] = "i", ['ई'] = "I", ['उ'] = "u", ['ऊ'] = "U", ['ऋ'] = "R", ['ॠ'] = "RR", ['ऌ'] = "LLi", ['ॡ'] = "LLI",
        ['ऎ'] = "E", ['ए'] = "e", ['ऐ'] = "ai", ['ऒ'] = "O", ['ओ'] = "o", ['औ'] = "au", ['ऍ'] = "ea", ['ऑ'] = "oa",
    };
    private static readonly Dictionary<char, string> VowelMarks = new()
    {
        ['ा'] = "A", ['ि'] = "i", ['ी'] = "I", ['ु'] = "u", ['ू'] = "U", ['ृ'] = "R", ['ॄ'] = "RR", ['ॢ'] = "LLi", ['ॣ'] = "LLI",
        ['ॆ'] = "E", ['े'] = "e", ['ै'] = "ai", ['ॊ'] = "O", ['ो'] = "o", ['ौ'] = "au", ['ॅ'] = ".c", ['ॉ'] = "oa",
    };
    private static readonly Dictionary<char, string> Yogavaahas = new()
    {
        ['ं'] = "M", ['ः'] = "H", ['ँ'] = ".N",
    };
    private static readonly Dictionary<char, string> Consonants = new()
    {
        ['क'] = "k", ['ख'] = "kh", ['ग'] = "g", ['घ'] = "gh", ['ङ'] = "~N", ['च'] = "ch", ['छ'] = "Ch", ['ज'] = "j", ['झ'] = "jh", ['ञ'] = "~n",
        ['ट'] = "T", ['ठ'] = "Th", ['ड'] = "D", ['ढ'] = "Dh", ['ण'] = "N", ['त'] = "t", ['थ'] = "th", ['द'] = "d", ['ध'] = "dh", ['न'] = "n",
        ['प'] = "p", ['फ'] = "ph", ['ब'] = "b", ['भ'] = "bh", ['म'] = "m", ['य'] = "y", ['र'] = "r", ['ल'] = "l", ['व'] = "v", ['श'] = "sh",
        ['ष'] = "Sh", ['स'] = "s", ['ह'] = "h", ['ळ'] = "L",
        // precomposed nukta forms
        ['\u0958'] = "q", ['\u0959'] = ".kh", ['\u095A'] = ".g", ['\u095B'] = "z", ['\u095C'] = ".D", ['\u095D'] = ".Dh", ['\u095E'] = "f", ['\u095F'] = "Y", ['\u0931'] = ".Rh", ['\u0934'] = ".L",
    };
    private static readonly Dictionary<char, string> NuktaCombos = new()
    {
        ['क'] = "q", ['ख'] = ".kh", ['ग'] = ".g", ['ज'] = "z", ['ड'] = ".D", ['ढ'] = ".Dh", ['फ'] = "f", ['य'] = "Y", ['र'] = ".Rh", ['ळ'] = ".L",
    };
    private static readonly Dictionary<char, string> Symbols = new()
    {
        ['०'] = "0", ['१'] = "1", ['२'] = "2", ['३'] = "3", ['४'] = "4", ['५'] = "5", ['६'] = "6", ['७'] = "7", ['८'] = "8", ['९'] = "9",
        ['ॐ'] = "OM", ['ऽ'] = ".a", ['।'] = "|", ['॥'] = "||",
    };

    private static bool IsDevanagari(char c) => c >= 'ऀ' && c <= 'ॿ';
    public static bool ContainsDevanagari(string? text) => !string.IsNullOrEmpty(text) && text.Any(IsDevanagari);

    private static string Transliterate(string text)
    {
        var sb = new StringBuilder();
        bool hadConsonant = false; // previous char was a consonant whose inherent 'a' is still pending
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '‌' || c == '‍') continue;
            string? consonant = null; int consumed = 1;
            // conjunct keys of the scheme (क्ष -> x, ज्ञ -> jn) win over their parts
            if (string.CompareOrdinal(text, i, "क्ष", 0, 3) == 0) { consonant = "x"; consumed = 3; }
            else if (string.CompareOrdinal(text, i, "ज्ञ", 0, 3) == 0) { consonant = "jn"; consumed = 3; }
            else
            if (NuktaCombos.TryGetValue(c, out var nukta) && i + 1 < text.Length && text[i + 1] == Nukta) { consonant = nukta; consumed = 2; }
            else if (Consonants.TryGetValue(c, out var plain)) consonant = plain;
            if (consonant != null)
            {
                if (hadConsonant) sb.Append('a');
                sb.Append(consonant);
                i += consumed - 1;
                hadConsonant = true;
                continue;
            }
            if (VowelMarks.TryGetValue(c, out var mark)) { sb.Append(mark); hadConsonant = false; continue; }
            if (c == Virama) { hadConsonant = false; continue; }
            if (hadConsonant) { sb.Append('a'); hadConsonant = false; }
            if (Vowels.TryGetValue(c, out var vowel)) sb.Append(vowel);
            else if (Yogavaahas.TryGetValue(c, out var yoga)) sb.Append(yoga);
            else if (Symbols.TryGetValue(c, out var sym)) sb.Append(sym);
            else sb.Append(c);
        }
        if (hadConsonant) sb.Append('a');
        return sb.ToString();
    }

    private static readonly char[] TrailingPunct = { '.', ',', '!', '?', '"', '\'' };

    /// <summary>Best-effort algorithmic Devanagari -> Roman transliteration; text with no Devanagari is returned unchanged.</summary>
    public static string RomanizeFallback(string? text)
    {
        if (string.IsNullOrEmpty(text) || !ContainsDevanagari(text)) return text ?? "";
        var cleaned = text.Replace("।", ".").Replace("॥", ".");
        var stripped = cleaned.TrimEnd(TrailingPunct);
        if (Common.TryGetValue(stripped, out var natural)) return natural + cleaned[stripped.Length..];
        return Transliterate(cleaned).ToLowerInvariant();
    }

    private static readonly char[] SourceTrailing = { '.', ',', '!', '?', '"', '\'', '।', '॥' };

    /// <summary>Looks the high-frequency word table up directly against the raw ASR source text (word.original), or null when the word is not in it.</summary>
    public static string? CommonHindiSpelling(string? originalText)
    {
        if (string.IsNullOrEmpty(originalText)) return null;
        var cleaned = originalText.Trim();
        var stripped = cleaned.TrimEnd(SourceTrailing);
        if (!Common.TryGetValue(stripped, out var natural)) return null;
        var trailing = cleaned[stripped.Length..].Replace("।", ".").Replace("॥", ".");
        return natural + trailing;
    }
}
