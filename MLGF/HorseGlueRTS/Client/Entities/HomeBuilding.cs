using SFML.Graphics;
using Shared;

namespace Client.Entities
{
    internal class HomeBuilding : BuildingBase
    {
        public HomeBuilding()
        {
            BuildTime = 2000;

            supportedBuilds.Add(new BuildProduceData
                                    {
                                        id = "worker",
                                        AppleCost = 0,
                                        WoodCost = 0,
                                        GlueCost = 0,
                                        SupplyCost = 0,
                                        CreationTime = 7000,
                                    });

            SetSprites();
        }

        public override void SetTeam(byte team)
        {
            base.SetTeam(team);

            Sprite[] standardSprites =
                ExternalResources.GetSprites("Resources/Sprites/HomeBase/" + team.ToString() + "/" + "Standard/");
            Sprites[AnimationTypes.Standard].Sprites.AddRange(standardSprites);

            Sprite[] producingSprites =
                ExternalResources.GetSprites("Resources/Sprites/HomeBase/" + team.ToString() + "/" + "Producing/");
            Sprites[AnimationTypes.Producing].Sprites.AddRange(producingSprites);

            SetSprites();
        }

        public override void Use(EntityBase user)
        {
            base.Use(user);
            if (user is Worker)
            {
                var workerCast = (Worker) user;
                workerCast.ResourceCount = 0;
            }
        }
    }
}