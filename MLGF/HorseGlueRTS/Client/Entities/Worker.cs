using System.IO;
using SFML.Graphics;
using Shared;

namespace Client.Entities
{
    internal class Worker : UnitBase
    {
        public ResourceTypes HeldResource;
        public byte ResourceCount;

        public Worker()
        {
            ResourceCount = 0;
            HeldResource = 0;

            SpriteFolder = "Worker";
        }

        public bool IsHoldingResources
        {
            get { return ResourceCount > 0; }
        }

        public void GiveResource(ResourceTypes type, byte amount)
        {
            if (!IsHoldingResources)
            {
                HeldResource = type;
                ResourceCount = amount;
            }
        }

        public override void OnUseChange(EntityBase entity)
        {
            base.OnUseChange(entity);
            MyGameMode.PlayUseSound(ExternalResources.UseSounds.CliffUsing);
        }

        protected override void ParseUpdate(MemoryStream memoryStream)
        {
            base.ParseUpdate(memoryStream);

            var reader = new BinaryReader(memoryStream);
            HeldResource = (ResourceTypes) reader.ReadByte();
            ResourceCount = reader.ReadByte();
        }

        protected override void onSetIdleAnimation()
        {
            base.onSetIdleAnimation();
            if (IsHoldingResources)
            {
                CurrentAnimation = AnimationTypes.IdleWithResources;
            }
        }

        protected override void onSetMovingAnimation()
        {
            base.onSetMovingAnimation();
            if (IsHoldingResources)
            {
                CurrentAnimation = AnimationTypes.MovingWithResources;
            }
        }
    }
}