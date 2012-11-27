using Client.Effects;
using SFML.Graphics;

namespace Client.Entities
{
    internal class GlueFactory : BuildingBase
    {
        public GlueFactory()
        {
            SetSprites();
            ;
        }

        public override void SetTeam(byte team)
        {
            base.SetTeam(team);

            Sprite[] standardSprites =
                ExternalResources.GetSprites("Resources/Sprites/GlueFactory/" + team.ToString() + "/");
            Sprites[AnimationTypes.Standard].Sprites.AddRange(standardSprites);

            Sprite[] producingSprites =
                ExternalResources.GetSprites("Resources/Sprites/GlueFactory/" + team.ToString() + "/");
            Sprites[AnimationTypes.Producing].Sprites.AddRange(producingSprites);

            SetSprites();
        }

        public override void Use(EntityBase user)
        {
            base.Use(user);
            //TODO: Blood/Glue effect
            for (int i = 0; i < 5; i++)
            {
                MyGameMode.AddEffect(new GlueParticle(Position));
            }
        }
    }
}