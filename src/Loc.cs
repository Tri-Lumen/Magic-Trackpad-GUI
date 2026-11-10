using System.Globalization;
using System.Reflection;
using System.Text.Json;

namespace TrackpadCompanion;

// UI text lookup. Strings live in Lang/<code>.json (embedded in the exe). A missing key falls back to
// English, then to the key itself, so a gap in a translation never crashes the app.
public static class Loc
{
    public static readonly (string Code, string Name)[] Languages =
    {
        ("en", "English"), ("fr", "Français"), ("de", "Deutsch"), ("es", "Español"), ("ja", "日本語"),
        ("zh-Hans", "简体中文"), ("pl", "Polski"), ("ru", "Русский"), ("pt-BR", "Português (Brasil)"), ("af", "Afrikaans")
    };

    static Dictionary<string, string> en = new();
    static Dictionary<string, string> cur = new();
    static bool ready;

    public static string Code { get; private set; } = "en";
    public static CultureInfo Culture { get; private set; } = CultureInfo.CurrentCulture;

    // pref is "auto" or one of the codes above.
    public static void Init(string? pref)
    {
        ready = true;
        try
        {
            Code = Resolve(pref);
            en = Load("en");
            cur = Code == "en" ? en : Load(Code);
            try { Culture = new CultureInfo(Code); } catch { }
        }
        catch (Exception e) { Log.Write("Loc.Init failed: " + e.Message); }
        Log.Write("Language: " + Code);
    }

    public static string Resolve(string? pref)
    {
        if (!string.IsNullOrEmpty(pref) && pref != "auto" && Languages.Any(l => l.Code == pref)) return pref;
        var name = CultureInfo.CurrentUICulture.Name;
        if (name.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
            return name.Contains("Hant", StringComparison.OrdinalIgnoreCase) || name.EndsWith("TW") || name.EndsWith("HK") || name.EndsWith("MO") ? "en" : "zh-Hans";
        return CultureInfo.CurrentUICulture.TwoLetterISOLanguageName switch
        {
            "fr" => "fr", "de" => "de", "es" => "es", "ja" => "ja", "pl" => "pl", "ru" => "ru", "pt" => "pt-BR", "af" => "af",
            _ => "en"
        };
    }

    static Dictionary<string, string> Load(string code)
    {
        var asm = Assembly.GetExecutingAssembly();
        using var s = asm.GetManifestResourceStream("Lang." + code + ".json");
        if (s == null) { Log.Write("Language file missing: " + code); return new(); }
        return JsonSerializer.Deserialize<Dictionary<string, string>>(s) ?? new();
    }

    public static string T(string key)
    {
        if (!ready) Init(App.Cfg.Language);
        return cur.TryGetValue(key, out var v) ? v : en.TryGetValue(key, out var e) ? e : key;
    }

    public static string F(string key, params object[] args)
    {
        try { return string.Format(Culture, T(key), args); }
        catch (FormatException) { return T(key); }
    }

    // The English text, used to tell an untouched default message from one the user edited.
    public static string En(string key) { if (!ready) Init(App.Cfg.Language); return en.TryGetValue(key, out var v) ? v : key; }
}
