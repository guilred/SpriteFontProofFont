using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SpriteFontProofFont;

public sealed class SFProofFont : IDisposable {
    private class AtlasData {
        public Texture2D Atlas = null!;
        public Dictionary<char, (int x, int w)> CharsData = [];
        public int Size;
    }
    private readonly List<AtlasData> _atlases = [];

    public float Spacing { get; set; } = 10;
    public float LineSpacing { get; set; } = 5;

    public SFProofFont(GraphicsDevice graphics, string sfpfFilePath) {
        string tempFolder = Path.Combine(Path.GetTempPath(), $"SFPFTemp_{Guid.NewGuid()}");

        try {
            ZipFile.ExtractToDirectory(sfpfFilePath, tempFolder);

            string metadataPath = Path.Combine(tempFolder, "metadata");
            if (!File.Exists(metadataPath)) {
                throw new Exception("Font file is missing metadata. Please regenerate with new generator.");
            }

            string metadata = File.ReadAllText(metadataPath);
            var sizes = metadata.Replace("sizes:", "").Trim()
                .Split(',')
                .Select(s => int.Parse(s.Trim()))
                .OrderBy(s => s);
            foreach (int size in sizes) {
                string atlasPath = Path.Combine(tempFolder, $"atlas_{size}");
                string charsDataPath = Path.Combine(tempFolder, $"chars_data_{size}");

                if (!File.Exists(atlasPath) || !File.Exists(charsDataPath)) {
                    continue;
                }

                var atlasData = new AtlasData {
                    CharsData = []
                };

                using (var stream = File.OpenRead(atlasPath)) {
                    atlasData.Atlas = Texture2D.FromStream(graphics, stream);
                }

                Color[] buffer = new Color[atlasData.Atlas.Width * atlasData.Atlas.Height];
                atlasData.Atlas.GetData(buffer);
                for (int i = 0; i < buffer.Length; i++) {
                    buffer[i] = Color.FromNonPremultiplied(
                        buffer[i].R, buffer[i].G, buffer[i].B, buffer[i].A
                    );
                }
                atlasData.Atlas.SetData(buffer);

                var charIntPairs = File.ReadAllText(charsDataPath).Split('\n');
                foreach (var cip in charIntPairs) {
                    if (string.IsNullOrWhiteSpace(cip)) continue;
                    var inTChar = cip.Split(' ');
                    char c = inTChar[0][0];
                    int x = int.Parse(inTChar[1]);
                    int w = int.Parse(inTChar[2]);
                    atlasData.CharsData.Add(c, (x, w));
                }
                atlasData.Size = atlasData.Atlas.Height;
                _atlases.Add(atlasData);
            }

            if (_atlases.Count == 0) {
                throw new Exception("No valid atlases found in font file");
            }
        }
        finally {
            if (Directory.Exists(tempFolder)) {
                Directory.Delete(tempFolder, true);
            }
        }
    }
    private AtlasData GetBestAtlas(float targetHeight) {
        int left = 0;
        int right = _atlases.Count - 1;
        if (targetHeight <= _atlases[0].Size) return _atlases[0];
        if (targetHeight >= _atlases[right].Size) return _atlases[right];
        while (left <= right) {
            int mid = left + (right - left) / 2;
            int atlasSize = _atlases[mid].Size;

            if (atlasSize == targetHeight) {
                return _atlases[mid];
            }
            if (atlasSize < targetHeight) {
                left = mid + 1;
            }
            else {
                right = mid - 1;
            }
        }
        if (left >= _atlases.Count) return _atlases[right];
        if (right < 0) return _atlases[left];
        float distLeft = Math.Abs(_atlases[left].Size - targetHeight);
        float distRight = Math.Abs(_atlases[right].Size - targetHeight);
        return distLeft <= distRight ? _atlases[left] : _atlases[right];
    }
    public Vector2 MeasureString(string text, float height, float? spacing = null, float? lineSpacing = null, int? index = null, int? length = null) {
        if (text.Length == 0) return Vector2.Zero;

        var atlas = GetBestAtlas(height);
        float spacingScale = height / 120f;
        float atlasScale = height / atlas.Size;
        Vector2 size = Vector2.UnitY * height;
        float curr_x = 0;

        spacing ??= Spacing;
        lineSpacing ??= LineSpacing;
        index ??= 0;
        length ??= text.Length;

        for (int i = index.Value; i < index.Value + length.Value && i < text.Length; i++) {
            char c = text[i];

            if (c == ' ') {
                if (atlas.CharsData.TryGetValue('$', out var dlrData)) {
                    curr_x += (dlrData.w - 2) * atlasScale - spacing.Value * 2 * spacingScale;
                }
                else {
                    curr_x += spacing.Value * spacingScale;
                }
                continue;
            }
            else if (c == '\n') {
                size.Y += height + lineSpacing.Value * spacingScale;
                size.X = Math.Max(size.X, curr_x - spacing.Value * spacingScale);
                curr_x = 0;
                continue;
            }

            if (!atlas.CharsData.TryGetValue(c, out var offset)) {
                if (!atlas.CharsData.TryGetValue('?', out var fallback))
                    continue;
                offset = fallback;
            }
            curr_x += offset.w * atlasScale + spacing.Value * spacingScale;
        }

        size.X = Math.Max(size.X, curr_x - spacing.Value * spacingScale);
        return size;
    }

    public void DrawString(SpriteBatch sb, string text, Vector2 position, Color color, float height, float rotation, float? spacing = null, float? lineSpacing = null, int? index = null, int? length = null) {
        var atlas = GetBestAtlas(height);
        float spacingScale = height / 120f;
        float atlasScale = height / atlas.Size;
        float curr_x = position.X;
        float curr_y = position.Y;

        spacing ??= Spacing;
        lineSpacing ??= LineSpacing;
        index ??= 0;
        length ??= text.Length;

        for (int i = index.Value; i < index.Value + length.Value && i < text.Length; i++) {
            char c = text[i];

            if (c == ' ') {
                if (atlas.CharsData.TryGetValue('$', out var dlrData)) {
                    curr_x += (dlrData.w - 2) * atlasScale - spacing.Value * 2 * spacingScale;
                }
                else {
                    curr_x += spacing.Value * spacingScale;
                }
                continue;
            }
            else if (c == '\n') {
                curr_x = position.X;
                curr_y += height + lineSpacing.Value * spacingScale;
                continue;
            }

            if (!atlas.CharsData.TryGetValue(c, out var offset)) {
                if (!atlas.CharsData.TryGetValue('?', out var fallback))
                    continue;
                offset = fallback;
            }
            Rectangle sourceRect = new(offset.x, 0, offset.w, atlas.Size);
            Vector2 drawPos = rotation != 0
                ? Vector2.Rotate(new Vector2(curr_x, curr_y) - position, rotation) + position
                : new Vector2(curr_x, curr_y);

            sb.Draw(atlas.Atlas, drawPos, sourceRect, color, rotation, Vector2.Zero, atlasScale, SpriteEffects.None, 0);
            curr_x += offset.w * atlasScale + spacing.Value * spacingScale;
        }
    }

    public void DrawStringOutlined(SpriteBatch sb, string text, Vector2 position, Color color, Color outlineColor, float height, float outlineWidth, float step, float rotation, float? spacing = null, float? lineSpacing = null) {
        for (float i = -1; i <= 1; i += step) {
            for (float j = -1; j <= 1; j += step) {
                Vector2 offset = new Vector2(i, j) * outlineWidth;
                DrawString(sb, text, position + offset, outlineColor, height, rotation, spacing, lineSpacing);
            }
        }
        DrawString(sb, text, position, color, height, 0, spacing, lineSpacing);
    }

    public void Dispose() {
        foreach (var atlas in _atlases) {
            atlas.Atlas?.Dispose();
        }
        _atlases.Clear();
    }
}