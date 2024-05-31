using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Flow.Launcher.Plugin;

namespace FlowLauncherCommunity.Plugin.SegoeFluentIconsSearch;

public class Main : IPlugin, IContextMenu {
    private const string FontName = "Segoe Fluent Icons";
    private const string IcoPath = "icon.png";
    private PluginInitContext _context;
    private IconData[] _icons;

    enum IconFormat {
        AmpersandX,
        Ampersand,
        BackslashU,
        BackslashUCurly,
    }

    public void Init(PluginInitContext context) {
        _context = context;

        var path = Path.Combine(context.CurrentPluginMetadata.PluginDirectory, "data.json");
        var jsonString = File.ReadAllText(path);
        _icons = JsonSerializer.Deserialize<IconData[]>(jsonString, new JsonSerializerOptions {
            PropertyNameCaseInsensitive = true,
        });
    }

    public List<Result> Query(Query query) {
        if (string.IsNullOrWhiteSpace(query.Search))
            return new List<Result> { new() { IcoPath = IcoPath, Title = "Waiting for input..." } };

        return _icons
            .Where(icon =>
                _context.API.FuzzySearch(query.Search, icon.Label).IsSearchPrecisionScoreMet() ||
                _context.API.FuzzySearch(query.Search, $"{icon.Value:X4}").IsSearchPrecisionScoreMet())
            .Select(icon => GetResult(icon, IconFormat.AmpersandX))
            .ToList();
    }

    private Result GetResult(IconData icon, IconFormat format) {
        var value = format switch {
            IconFormat.AmpersandX => $"&#x{icon.Value:X4};",
            IconFormat.Ampersand => $"&#{icon.Value};",
            IconFormat.BackslashU => $@"\u{icon.Value:X4}",
            IconFormat.BackslashUCurly => $@"\u{{{icon.Value:X4}}}",
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null),
        };

        return new Result {
            Title = icon.Label,
            SubTitle = value,
            Glyph = new GlyphInfo(FontName, char.ConvertFromUtf32(icon.Value)),
            ContextData = icon,
            CopyText = value,
            IcoPath = IcoPath,
            Action = _ => {
                _context.API.CopyToClipboard(value);
                return true;
            },
        };
    }

    public List<Result> LoadContextMenus(Result selectedResult) {
        if (selectedResult.ContextData is not IconData icon)
            return new List<Result>();

        return new List<Result> {
            GetResult(icon, IconFormat.AmpersandX),
            GetResult(icon, IconFormat.Ampersand),
            GetResult(icon, IconFormat.BackslashU),
            GetResult(icon, IconFormat.BackslashUCurly),
        };
    }
}
