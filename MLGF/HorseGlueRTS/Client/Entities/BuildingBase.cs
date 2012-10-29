using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SFML.Graphics;
using System.Diagnostics;
using SFML.Window;
using Shared;

namespace Client.Entities
{
    class BuildingBase : EntityBase
    {
        protected List<byte> buildOrder;

        public ushort BuildTime //in milliseconds
        {
            get;
            protected set;
        }

        public bool IsBuilding { get; private set; }    //Is building currently being build (NOT BUILDING UNITS)
        public bool IsProductingUnit
        {
            get { return buildOrder.Count > 0; }
        }

        public byte UnitInProduction
        {
            get
            {
                if (IsProductingUnit)
                    return buildOrder[0];
                return 0;
            }
        }

        public float UnitBuildCompletePercent
        {
            get
            {
                if (!IsProductingUnit)
                    return 0;

                return ((float) stopwatch.ElapsedMilliseconds / (float) creationTime(buildOrder[0]))*100f;
            }
        }

        public byte SupplyGain;
        private float elapsedBuildTime;

        private Stopwatch stopwatch;

        public BuildingBase()
        {
            IsBuilding = true;
            BuildTime = 1;
            elapsedBuildTime = 0;
            buildOrder = new List<byte>();
            stopwatch = new Stopwatch();

            Health = 1;
            MaxHealth = 100;
        }

        protected override void ParseCustom(MemoryStream memoryStream)
        {
            var reader = new BinaryReader(memoryStream);
            var signature = (BuildingSignature) reader.ReadByte();

            switch(signature)
            {
                case BuildingSignature.ProductionComplete:
                    {
                        if(buildOrder.Count > 0)
                        {
                            onCompleteProduction(buildOrder[0]);
                            buildOrder.RemoveAt(0);
                            stopwatch.Reset();
                        }
                    }
                    break;
                case BuildingSignature.StartProduction:
                    {
                        var type = reader.ReadByte();
                        onStartProduction(type);
                        buildOrder.Add(type);
                    }
                    break;
                case BuildingSignature.BuildingFinished:
                    {
                        IsBuilding = false;
                        Health = reader.ReadSingle();
                    }
                    break;
                default:
                    break;
            }
        }

        protected override void ParseUpdate(MemoryStream memoryStream)
        {
            var reader = new BinaryReader(memoryStream);

            Health = reader.ReadSingle();
            MaxHealth = reader.ReadSingle();
            IsBuilding = reader.ReadBoolean();
            BuildTime = reader.ReadUInt16();
            Energy = reader.ReadUInt16();
            Position.X = reader.ReadSingle();
            Position.Y = reader.ReadSingle();
            byte buildCount = reader.ReadByte();
            buildOrder.Clear();
            for(var i = 0; i < buildCount; i++)
            {
                buildOrder.Add(reader.ReadByte());
            }

            var rallyCount = reader.ReadByte();
            rallyPoints.Clear();
            for (var i = 0; i < rallyCount; i++)
            {
                rallyPoints.Add(new Vector2f(reader.ReadSingle(), reader.ReadSingle()));
            }
        }


        protected virtual void onCompleteProduction(byte type)
        {
            //play sound or add something to HUD
        }
        protected virtual void onStartProduction(byte type)
        {
            //play sound or add something to HUD
        }

        protected virtual uint creationTime(byte type)
        {
            //TODO:DEBUG UNITS FOR TESTING, REMOVE LATER

            switch (type)
            {
                default:
                case 0:
                    return 1000;
                    break;
            }
            return 0;
        }

        public override void Update(float ms)
        {
            if (IsBuilding)
            {
                elapsedBuildTime += ms;
                Health += (float)((MaxHealth / BuildTime)) * ms;
            }

            if (buildOrder.Count > 0 && stopwatch.IsRunning == false)
            {
                stopwatch.Restart();
            }
        }

        public override void Render(RenderTarget target)
        {
            Sprite sprite = new Sprite(ExternalResources.GTexture("Resources/Sprites/TestTile.png"));

            sprite.Position = Position;
            sprite.Origin = new Vector2f(sprite.TextureRect.Width/2, sprite.TextureRect.Height/2);
            sprite.Color = new Color(255, 255, 0);
            if(IsBuilding)
            {
                sprite.Color = new Color(255, 0, 255, 200);
            }
            target.Draw(sprite);


        }
    }
}
