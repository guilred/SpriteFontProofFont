using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;


namespace SpriteFontProofFont;
public sealed class SFProofFont: IDisposable {
    private readonly Dictionary<char, (int, int)> _charsData = [];
    private readonly Texture2D _atlas;
    public int Size => _atlas.Height;
    public float Spacing { get; set; } = 10;
    public float LineSpacing { get; set; } = 5;
    public SFProofFont(GraphicsDevice graphics, string sfpfFilePath) {
        string TempFolder = Path.Combine(Path.GetTempPath(), "DioUITemp");
        ZipFile.ExtractToDirectory(sfpfFilePath, TempFolder);

        using (var stream = File.OpenRead(Path.Combine(TempFolder, "atlas"))) {
            _atlas = Texture2D.FromStream(graphics, stream);
        }
        Color[] buffer = new Color[_atlas.Width * _atlas.Height];
        _atlas.GetData(buffer);

        for (int i = 0; i < buffer.Length; i++) {
            buffer[i] = Color.FromNonPremultiplied(
                buffer[i].R,
                buffer[i].G,
                buffer[i].B,
                buffer[i].A
            );
        }
        _atlas.SetData(buffer);
        var charIntPairs = File.ReadAllText(Path.Combine(TempFolder, "chars_data")).Split('\n');
        foreach (var cip in charIntPairs) {
            var inTChar = cip.Split(' ');
            char c = inTChar[0][0];
            int x = int.Parse(inTChar[1]);
            int w = int.Parse(inTChar[2]);
            _charsData.Add(c, (x, w));
        }
        Directory.Delete(TempFolder, true);
    }
    public void DrawString(SpriteBatch sb, string text, Vector2 position, Color color, float height, float? spacing, float rotation, float scale, float? lineSpacing = null) {
        float fontScale = height / Size;
        float curr_x = position.X;
        float curr_y = position.Y;
        spacing ??= Spacing;
        lineSpacing ??= LineSpacing;
        for (int i = 0; i < text.Length; i++) {
            char c = text[i];
            if (c == ' ') {
                curr_x += spacing.Value * 2;
                continue;
            }
            else if (c == '\n') {
                curr_x = position.X;
                curr_y += height + lineSpacing.Value * fontScale;
                continue;
            }
            var (x, w) = _charsData[c];
            Rectangle sourceRect = new(x, 0, w, Size);
            Vector2 drawPos = Vector2.Rotate((new Vector2(curr_x, curr_y) - position) * scale, rotation) + position;
            sb.Draw(_atlas, drawPos, sourceRect, color, rotation, Vector2.Zero, fontScale * scale, SpriteEffects.None, 0);
            curr_x += (w + spacing.Value) * fontScale;
        }
    }
    public void Dispose() {
        _atlas?.Dispose();
    }
}
