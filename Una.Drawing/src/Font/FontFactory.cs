using System.Linq;

namespace Una.Drawing.Font;

internal static class FontFactory
{
    internal static IFont CreateFromFontFamily(
        string fontFamily,
        float sizeOffset = 0
    )
    {
        SKFontStyleSet styles    = SKFontManager.Default.GetFontStyles(fontFamily);
        SKFontStyle    fontStyle = styles.FirstOrDefault(
                style => style.Weight >= 400
                    && style.Slant == SKFontStyleSlant.Upright
                )
            ?? (styles.FirstOrDefault() ?? new());

        SKTypeface  srcTypeface = SKTypeface.FromFamilyName(fontFamily, fontStyle);

        return new DynamicFont(srcTypeface, FontRegistry.Glyphs, sizeOffset);
    }

    internal static IFont CreateFromFontFile(FileInfo file, float sizeOffset)
    {
        return new DynamicFont(LoadTypefaceFromFile(file), FontRegistry.Glyphs, sizeOffset);
    }

    private static SKTypeface LoadTypefaceFromFile(FileInfo file)
    {
        if (file.Name.Equals("NotoSansCJK-Medium.ttc", StringComparison.OrdinalIgnoreCase)
            && TryLoadCjkScTypeface(file.FullName) is { } scTypeface) {
            return scTypeface;
        }

        return SKTypeface.FromFile(file.FullName);
    }

    private static SKTypeface? TryLoadCjkScTypeface(string path)
    {
        string? firstFamily = null;

        for (int index = 0; index < 10; index++) {
            var typeface = SKTypeface.FromFile(path, index);
            string family = typeface.FamilyName;

            if (family.Contains("CJK SC", StringComparison.Ordinal)) {
                return typeface;
            }

            if (index == 0) {
                firstFamily = family;
            } else if (family == firstFamily) {
                break;
            }
        }

        return null;
    }

    internal static IFont CreateFromFontStream(Stream stream, float sizeOffset)
    {
        return new DynamicFont(SKTypeface.FromStream(stream), FontRegistry.Glyphs, sizeOffset);
    }
}
