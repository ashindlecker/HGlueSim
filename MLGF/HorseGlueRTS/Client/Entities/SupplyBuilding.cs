namespace Client.Entities
{
    internal class SupplyBuilding : BuildingBase
    {
        public override void SetTeam(byte team)
        {
            base.SetTeam(team);
            Sprites[AnimationTypes.Standard].Sprites.AddRange(
                ExternalResources.GetSprites("Resources/Sprites/SupplyBuilding/" + team.ToString() + "/" + "Standard/"));
            SetSprites();
        }
    }
}