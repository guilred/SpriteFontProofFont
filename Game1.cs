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
        font.DrawString(
            _spriteBatch,
            "Hello!\nw Line? " + RandomString(),
            new(100, 30),
            Color.Green, 60, null,
            float.Pi * 0.6f * (1 - wave),
            0.25f + 0.75f * wave,
            null
        );
        _spriteBatch.End();

        base.Draw(gameTime);
    }
    private Random rng = new();
    public string RandomString(int length = 8) {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        return new string(Enumerable.Range(0, length).Select(_ => chars[rng.Next(chars.Length)]).ToArray());
    }
}
