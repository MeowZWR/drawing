/* Una.Drawing                                                 ____ ___
 *   A declarative drawing library for FFXIV.                 |    |   \____ _____        ____                _
 *                                                            |    |   /    \\__  \      |    \ ___ ___ _ _ _|_|___ ___
 * By Una. Licensed under AGPL-3.                             |    |  |   |  \/ __ \_    |  |  |  _| .'| | | | |   | . |
 * https://github.com/una-xiv/drawing                         |______/|___|  (____  / [] |____/|_| |__,|_____|_|_|_|_  |
 * ----------------------------------------------------------------------- \/ --- \/ ----------------------------- |__*/

using Dalamud.Interface;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Una.Drawing.Font;
using Una.Drawing.Texture;

namespace Una.Drawing;

public class DrawingLib
{
    /// <summary>
    /// Set up the drawing library. Make sure to call this method in your
    /// plugin before using any of the drawing library's features.
    /// </summary>
    public static async void Setup(IDalamudPluginInterface pluginInterface, bool downloadGameGlyphs = true)
    {
        pluginInterface.Create<DalamudServices>();
        DalamudServices.PluginInterface = pluginInterface;
        DalamudServices.UiBuilder       = pluginInterface.UiBuilder;

        pluginInterface.UiBuilder.Draw += OnDraw;

        if (downloadGameGlyphs)
        {
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

        FontRegistry.SetNativeFontFamily(3, "Arial", SKFontStyleWeight.ExtraBold);

        if (GameGlyphProvider.GlyphsFile.Exists) {
            FontRegistry.SetNativeFontFamily(4, GameGlyphProvider.GlyphsFile);
        }

        GfdIconRepository.Setup();
        Renderer.Setup();
    }

    /// <summary>
    /// Disposes of the allocated resources in the drawing library. Make sure
    /// to call invoke this in your plugin's Dispose method.
    /// </summary>
    public static void Dispose()
    {
        DalamudServices.PluginInterface.UiBuilder.Draw -= OnDraw;

        Renderer.Dispose();
        FontRegistry.Dispose();
        GfdIconRepository.Dispose();
        TextureLoader.Dispose();
        MouseCursor.Dispose();
    }

    private static void OnDraw()
    {
        MouseCursor.Update();
    }
}

internal class DalamudServices
{
    [PluginService] public static IDataManager                 DataManager                 { get; set; } = null!;
    [PluginService] public static ITextureProvider             TextureProvider             { get; set; } = null!;
    [PluginService] public static ITextureSubstitutionProvider TextureSubstitutionProvider { get; set; } = null!;
    [PluginService] public static IPluginLog                   PluginLog                   { get; set; } = null!;

    public static IDalamudPluginInterface PluginInterface { get; set; } = null!;
    public static IUiBuilder              UiBuilder       { get; set; } = null!;
}