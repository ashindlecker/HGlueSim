using SFML.Graphics;
using SFML.Window;

namespace Client.GameStates
{
    internal class MainMenuState : GameStateBase
    {
        public string PlayerName;

        public MainMenuState()
        {
            PlayerName = "NO NAME";
        }

        public override void End()
        {
        }

        public override void Init(object loadData)
        {
        }

        public override void Render(RenderTarget target)
        {
            var renderName = new Text(PlayerName);
            renderName.Position = target.ConvertCoords(new Vector2i(10, 10));
            renderName.Scale = new Vector2f(.6f, .6f);
            target.Draw(renderName);

            var gameName = new Text("MY LITTLE GLUE FACTORY");
            gameName.Scale = new Vector2f(2, 2);
            gameName.Origin = new Vector2f(gameName.GetGlobalBounds().Width/2, gameName.GetGlobalBounds().Height/2);
            gameName.Position = target.ConvertCoords(new Vector2i((int) target.Size.X/2, 10));
            target.Draw(gameName);
        }

        public override void Update(float ts)
        {
        }
    }
}