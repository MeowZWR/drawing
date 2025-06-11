using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using System.Text;
using Una.Drawing.Texture;

namespace Una.Drawing.Font;

internal partial class DynamicFont
{
    private List<Chunk> GenerateChunks(object text, int fontSize)
    {
        return text switch {
            string str     => GenerateChunksFromString(str, fontSize),
            SeString seStr => GenerateChunksFromSeString(seStr, fontSize),
            _              => []
        };
    }

    private List<Chunk> GenerateChunksFromString(string text, int fontSize, Color? color = null, Color? edgeColor = null)
    {
        List<Chunk> chunks = [];

        if (string.IsNullOrEmpty(text)) return chunks;

        var buffer = new StringBuilder();

        Chunk.Kind lastKind = CharIsGlyph(text[0]) ? Chunk.Kind.Glyph : Chunk.Kind.Text;
        SKFont     font     = lastKind == Chunk.Kind.Glyph ? GetGlyphFont(fontSize) : GetTextFont(fontSize);

        buffer.Append(text[0]);

        for (int i = 1; i < text.Length; i++) {
            char c = text[i];

            Chunk.Kind currentKind = CharIsGlyph(c) ? Chunk.Kind.Glyph : Chunk.Kind.Text;
            
            if (currentKind != lastKind) {
                chunks.Add(new(lastKind, buffer.ToString(), color, edgeColor, font.MeasureText(buffer.ToString()) + 1f));
                buffer.Clear();
                lastKind = currentKind;
            }
            
            font = currentKind == Chunk.Kind.Glyph ? GetGlyphFont(fontSize) : GetTextFont(fontSize);
            buffer.Append(c);
        }

        if (buffer.Length > 0) {
            chunks.Add(new(lastKind, buffer.ToString(), color, edgeColor, font.MeasureText(buffer.ToString()) + 1f));
        }

        return chunks;
    }

    private List<Chunk> GenerateChunksFromSeString(SeString seString, int fontSize, Color? initialColor = null, Color? initialEdgeColor = null)
    {
        List<Chunk> chunks = [];

        if (seString.Payloads.Count == 0) return chunks;

        Stack<Color?> textColorStack = new();
        Stack<Color?> edgeColorStack = new();

        textColorStack.Push(initialColor);
        edgeColorStack.Push(initialEdgeColor);

        Color? currentColor     = initialColor;
        Color? currentEdgeColor = initialEdgeColor;

        foreach (var payload in seString.Payloads) {
            switch (payload) {
                case TextPayload { Text: not null } textPayload:
                    chunks.AddRange(GenerateChunksFromString(textPayload.Text, fontSize, currentColor, currentEdgeColor));
                    break;
                case IconPayload iconPayload:
                    GfdIcon icon = GfdIconRepository.GetIcon(iconPayload.Icon);
                    chunks.Add(new(Chunk.Kind.BitmapIcon, "", currentColor, currentEdgeColor, icon.Size.X, icon));
                    break;
                case UIForegroundPayload uiForegroundPayload: {
                    uint clr = uiForegroundPayload.ABGR;

                    if (clr == 0) {
                        if (textColorStack.Count > 1) {
                            textColorStack.Pop();
                            currentColor = textColorStack.Peek();
                        }
                    } else {
                        Color newColor = new(clr);
                        textColorStack.Push(newColor);
                        currentColor = newColor;
                    }

                    break;
                }
                case UIGlowPayload uiGlowPayload: {
                    uint clr = uiGlowPayload.ABGR;

                    if (clr == 0) {
                        if (edgeColorStack.Count > 1) {
                            edgeColorStack.Pop();
                            currentEdgeColor = edgeColorStack.Peek();
                        }
                    } else {
                        Color newEdgeColor = new(clr);
                        edgeColorStack.Push(newEdgeColor);
                        currentEdgeColor = newEdgeColor;
                    }

                    break;
                }
            }
        }

        return chunks;
    }

    private static bool CharIsGlyph(char c)
    {
        return c >= 0xE020 && c <= 0xE0DB;
    }
}