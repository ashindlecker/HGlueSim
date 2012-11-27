using System.IO;
using SFML.Graphics;
using SFML.Window;
using Shared;

namespace Client.Entities
{
    internal class Resources : EntityBase
    {
        public ushort RemainingResources;
        public ResourceTypes ResourceType;
        public byte ResourcesPerTrip;


        public Resources()
        {
            ResourceType = ResourceTypes.Glue;
            ResourcesPerTrip = 0;
            RemainingResources = 0;
        }

        protected override void ParseCustom(MemoryStream memoryStream)
        {
        }

        protected override void ParseUpdate(MemoryStream memoryStream)
        {
            var reader = new BinaryReader(memoryStream);

            Position = new Vector2f(reader.ReadSingle(), reader.ReadSingle());

            ResourceType = (ResourceTypes) reader.ReadByte();
            RemainingResources = reader.ReadUInt16();
            ResourcesPerTrip = reader.ReadByte();
        }

        public override void Render(RenderTarget target)
        {
            //debug drawing
            var sprite = new Sprite(ExternalResources.GTexture("Resources/Sprites/TestTile.png"));
            sprite.Origin = new Vector2f(sprite.TextureRect.Width/2, sprite.TextureRect.Height/2);
            sprite.Position = Position;
            sprite.Color = new Color(100, 100, 255);
            target.Draw(sprite);
        }

        public override void Update(float ts)
        {
        }

        public override void Use(EntityBase user)
        {
            base.Use(user);
            if (user is Worker)
            {
                var workerCast = (Worker) user;

                if (!workerCast.IsHoldingResources)
                {
                    byte toGive = ResourcesPerTrip;
                    if (RemainingResources < ResourcesPerTrip)
                    {
                        toGive = (byte) RemainingResources;
                    }
                    RemainingResources -= toGive;

                    workerCast.GiveResource(ResourceType, toGive);
                }
            }
        }
    }
}