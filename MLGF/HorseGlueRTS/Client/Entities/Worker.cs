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

            var idleSprites = ExternalResources.GetSprites("Resources/Sprites/Worker/Idle/");
            Sprites[AnimationTypes.Idle].Sprites.AddRange(idleSprites);


            var moveSprites = ExternalResources.GetSprites("Resources/Sprites/Worker/Moving/");
            Sprites[AnimationTypes.Moving].Sprites.AddRange(moveSprites);

            var resourceMoveSprites = ExternalResources.GetSprites("Resources/Sprites/Worker/MovingWithResources/");
            Sprites[AnimationTypes.MovingWithResources].Sprites.AddRange(resourceMoveSprites);

            var resourceIdleSprites = ExternalResources.GetSprites("Resources/Sprites/Worker/IdleWithResources/");
            Sprites[AnimationTypes.IdleWithResources].Sprites.AddRange(resourceMoveSprites);
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
