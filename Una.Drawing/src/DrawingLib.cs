using Dalamud.Interface;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Una.Drawing.Debugger;
using Una.Drawing.Font;
using Una.Drawing.NodeParser;
using Una.Drawing.Templating.StyleParser;
using Una.Drawing.Texture;

namespace Una.Drawing;

public class DrawingLib
{
    public static bool ShowDebugWindow { get; set; }

    /// <summary>
    /// Set up the drawing library. Make sure to call this method in your
    /// plugin before using any of the drawing library's features.
    /// </summary>
    public static async void Setup(IDalamudPluginInterface pluginInterface, bool downloadGameGlyphs = true)
    {
        pluginInterface.Create<DalamudServices>();
        DalamudServices.PluginInterface = pluginInterface;
        DalamudServices.UiBuilder       = pluginInterface.UiBuilder;

        ElementRegistry.Register<Node>();

        DalamudServices.CommandManager.AddHandler("/una-drawing",
            new(OnChatCommand) { HelpMessage = "Una.Drawing Commands", ShowInHelp = false }
        );

        pluginInterface.UiBuilder.Draw += OnDraw;

        if (downloadGameGlyphs) {
            await GameGlyphProvider.DownloadGameGlyphs();
            FontRegistry.SetupGlyphFont();
        }

#if DEBUG
        DebugLogger.Writer = DalamudServices.PluginLog;
#endif

        // Use the Noto Sans font that comes with Dalamud as the default font,
        // as it supports a wide range of characters, including Japanese.
        FontRegistry.SetNativeFontFamily(
            0,
            new FileInfo(
                Path.Combine(
                    pluginInterface.DalamudAssetDirectory.FullName,
                    "UIRes",
                    "NotoSansCJKsc-Medium.otf"
                )
            ),
            0
        );

        FontRegistry.SetNativeFontFamily(
            1,
            new FileInfo(
                Path.Combine(
                    pluginInterface.DalamudAssetDirectory.FullName,
                    "UIRes",
                    "Inconsolata-Regular.ttf"
                )
            ),
            0
        );

        FontRegistry.SetNativeFontFamily(
            2,
            new FileInfo(
                Path.Combine(
                    pluginInterface.DalamudAssetDirectory.FullName,
                    "UIRes",
                    "FontAwesomeFreeSolid.otf"
                )
            ),
            0
        );

        FontRegistry.SetNativeFontFamily(3, "Arial");

        if (GameGlyphProvider.GlyphsFile.Exists) {
            FontRegistry.SetNativeFontFamily(4, GameGlyphProvider.GlyphsFile);
        }

        GfdIconRepository.Setup();
        Renderer.Setup();

        UdtLoader.RegisterAttributeValueParser(new DefaultAttributeValueParser());
        UdtLoader.RegisterDirectiveParser(new SeStringDirectiveParser());
    }

    /// <summary>
    /// Disposes of the allocated resources in the drawing library. Make sure
    /// to call invoke this in your plugin's Dispose method.
    /// </summary>
    public static void Dispose()
    {
        DebugLogger.Log($"Shutting down {nameof(DrawingLib)}...");

        DalamudServices.PluginInterface.UiBuilder.Draw -= OnDraw;

        Renderer.Dispose();
        FontRegistry.Dispose();
        GfdIconRepository.Dispose();
        TextureLoader.Dispose();
        MouseCursor.Dispose();
        StylesheetRegistry.Dispose();
        ElementRegistry.Dispose();
        StyleAttributeParser.Dispose();
        NodeAttributeParser.Dispose();
        QuerySelectorParser.Dispose();
        NodeDebugger.Dispose();
        UdtLoader.Dispose();

        // Force the GC to run to clean up any remaining resources.
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    private static void OnDraw()
    {
        MouseCursor.Update();

        if (ShowDebugWindow) {
            NodeDebugger.Render();
        }
    }

    private static void OnChatCommand(string command, string args)
    {
        if (command != "/una-drawing") return;

        if (args == String.Empty) {
            Print("Available commands: debug, bounds.");
            return;
        }

        switch (args.ToLower()) {
            case "debug":
                ShowDebugWindow = !ShowDebugWindow;
                break;
            case "bounds":
                Node.DrawDebugBoundingBoxes = !Node.DrawDebugBoundingBoxes;
                break;
            default:
                Print($"Unknown command: {args}. Available commands: debug, bounds.");
                break;
        }
    }

    private static void Print(string msg)
    {
        if (DalamudServices.ClientState.IsLoggedIn) {
            DalamudServices.ChatGui.Print(msg);
        } else {
            DalamudServices.PluginLog.Info(msg);
        }
    }
}

internal class DalamudServices
{
    [PluginService] public static IDataManager                 DataManager                 { get; set; } = null!;
    [PluginService] public static ITextureProvider             TextureProvider             { get; set; } = null!;
    [PluginService] public static ITextureSubstitutionProvider TextureSubstitutionProvider { get; set; } = null!;
    [PluginService] public static IPluginLog                   PluginLog                   { get; set; } = null!;
    [PluginService] public static ICommandManager              CommandManager              { get; set; } = null!;
    [PluginService] public static IChatGui                     ChatGui                     { get; set; } = null!;
    [PluginService] public static IClientState                 ClientState                 { get; set; } = null!;
    [PluginService] public static IFramework                   Framework                   { get; set; } = null!;

    public static IDalamudPluginInterface PluginInterface { get; set; } = null!;
    public static IUiBuilder              UiBuilder       { get; set; } = null!;
}