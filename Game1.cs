using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Linq;

namespace SpriteFontProofFont;

public class Game1 : Game {
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private SFProofFont font;
    private Texture2D pixel;
    public Game1() {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize() {
        base.Initialize();
    }

    protected override void LoadContent() {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        font = new(GraphicsDevice, "Content/Audiowide.sfpf");
        pixel = new(GraphicsDevice, 1, 1);
        pixel.SetData([Color.White]);
    }

    protected override void Update(GameTime gameTime) {
        if (Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime) {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        float wave = MathF.Pow(MathF.Sin((float)gameTime.TotalGameTime.TotalSeconds * 0.25f * float.Pi), 2);
        _spriteBatch.Begin();
        var text = " <(`>`)\n(y) * |)\n cD | |D\n    j bb";
        float angle = float.Pi * 0.6f * (1 - wave);
        float scale = 0.25f + 0.75f * wave;
        Vector2 size = font.MeasureString(text, 100 * scale);
        _spriteBatch.Draw(
            pixel,
            new Rectangle(300, 40, (int)size.X, (int)size.Y),
            null, Color.Red,
            angle, Vector2.Zero, SpriteEffects.None, 0
        );
        font.DrawString(
            _spriteBatch,
            text,
            new(300, 40),
            Color.Green, 100 * scale, angle
        );
        _spriteBatch.End();

        base.Draw(gameTime);
    }
    private readonly Random rng = new();
    public string RandomString(int length = 8) {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        return new string([.. Enumerable.Range(0, length).Select(_ => chars[rng.Next(chars.Length)])]);
    }
}
