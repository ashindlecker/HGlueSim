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
    class Resources : EntityBase
    {
        public ResourceTypes ResourceType;
        public ushort RemainingResources;
        public byte ResourcesPerTrip;


        public Resources()
        {
            ResourceType = ResourceTypes.Glue;
            ResourcesPerTrip = 0;
            RemainingResources = 0;
        }

        public override void Update(float ts)
        {
        }

        public override void Use(EntityBase user)
        {
            base.Use(user);
            if(user is Worker)
            {
                var workerCast = (Worker) user;

                if(!workerCast.IsHoldingResources)
                {
                    var toGive = ResourcesPerTrip;
                    if (RemainingResources < ResourcesPerTrip)
                    {
                        toGive = (byte)RemainingResources;
                    }
                    RemainingResources -= toGive;

                    workerCast.GiveResource(ResourceType, toGive);
                }
            }
        }

        public override void Render(RenderTarget target)
        {
            //debug drawing
            Sprite sprite = new Sprite(ExternalResources.GTexture("Resources/Sprites/TestTile.png"));
            sprite.Origin = new Vector2f(sprite.TextureRect.Width/2, sprite.TextureRect.Height/2);
            sprite.Position = Position;
            sprite.Color = new Color(100, 100, 255);
            target.Draw(sprite);
        }

        protected override void ParseCustom(MemoryStream memoryStream)
        {
        }

        protected override void ParseUpdate(MemoryStream memoryStream)
        {
            var reader = new BinaryReader(memoryStream);

            Position = new Vector2f(reader.ReadSingle(), reader.ReadSingle());

            ResourceType = (ResourceTypes)reader.ReadByte();
            RemainingResources = reader.ReadUInt16();
            ResourcesPerTrip = reader.ReadByte();

        }
    }
}
