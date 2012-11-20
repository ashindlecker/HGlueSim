using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SFML.Graphics;
using SFML.Window;
using Shared;

namespace Client.Entities
{
    class Worker : UnitBase
    {
        public ResourceTypes HeldResource;
        public byte ResourceCount;

        public bool IsHoldingResources
        {
            get { return ResourceCount > 0; }
        }

        public Worker()
        {
            ResourceCount = 0;
            HeldResource = 0;

            SpriteFolder = "Worker";
        }

        public void GiveResource(ResourceTypes type, byte amount)
        {
            if (!IsHoldingResources)
            {
                HeldResource = type;
                ResourceCount = amount;
            }
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

        public override void OnUseChange(EntityBase entity)
        {
            base.OnUseChange(entity);
            MyGameMode.PlayUseSound(ExternalResources.UseSounds.CliffUsing);
        }

        public override void Render(RenderTarget target)
        {
            base.Render(target);
        }

        protected override void ParseUpdate(MemoryStream memoryStream)
        {
            base.ParseUpdate(memoryStream);

            var reader = new BinaryReader(memoryStream);
            HeldResource = (ResourceTypes)reader.ReadByte();
            ResourceCount = reader.ReadByte();
        }
    }
}
