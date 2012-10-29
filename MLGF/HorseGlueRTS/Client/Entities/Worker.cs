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
        }

        public void GiveResource(ResourceTypes type, byte amount)
        {
            if (!IsHoldingResources)
            {
                HeldResource = type;
                ResourceCount = amount;
            }
        }

        public override void Render(RenderTarget target)
        {
            //debug drawing
            Sprite sprite = new Sprite(ExternalResources.GTexture("Resources/Sprites/TestTile.png"));

            sprite.Origin = new Vector2f(sprite.TextureRect.Width / 2, sprite.TextureRect.Height / 2);
            sprite.Position = Position;
            sprite.Color = new Color(255, 100, 255);
            sprite.Scale = new Vector2f(.5f, .5f);
            target.Draw(sprite);

            if (IsHoldingResources)
            {
                sprite.Position = Position + new Vector2f(50, 0);
                sprite.Color = new Color(100, 100, 200);
                target.Draw(sprite);
            }

            debugDrawRange(target);
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
