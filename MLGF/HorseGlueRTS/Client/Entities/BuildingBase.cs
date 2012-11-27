using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Client.GameModes;
using SFML.Graphics;
using SFML.Window;
using Shared;
using System;

namespace Client.Entities
{
    internal class BuildingBase : EntityBase
    {
        #region AnimationTypes enum

        public enum AnimationTypes : byte
        {
            BeingBuilt,
            Standard,
            Producing,
            Wrecked,
            WreckedProducing,
        }

        #endregion

        private readonly Stopwatch stopwatch;

        protected AnimationTypes CurrentAnimation;
        protected Dictionary<AnimationTypes, AnimatedSprite> Sprites;
        public byte SupplyGain;


        protected List<byte> buildOrder;
        private float elapsedBuildTime;
        protected List<BuildProduceData> supportedBuilds;

        public BuildingBase()
        {
            IsBuilding = true;
            BuildTime = 10000;
            elapsedBuildTime = 0;
            buildOrder = new List<byte>();
            supportedBuilds = new List<BuildProduceData>();
            stopwatch = new Stopwatch();

            Health = 1;
            MaxHealth = 100;

            Sprites = new Dictionary<AnimationTypes, AnimatedSprite>();

            const byte AnimationTypeCount = 5;


            for (int i = 0; i < AnimationTypeCount; i++)
            {
                Sprites.Add((AnimationTypes) i, new AnimatedSprite(100));
            }

            Sprite[] buildSprites = ExternalResources.GetSprites("Resources/Sprites/Buildings/BeingBuilt/");
            Sprites[AnimationTypes.BeingBuilt].Sprites.AddRange(buildSprites);

            SetSprites();
        }

        public ushort BuildTime //in milliseconds
        { get; protected set; }

        public bool IsBuilding { get; private set; } //Is building currently being build (NOT BUILDING UNITS)
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
                foreach (BuildProduceData buildProduceData in supportedBuilds)
                {
                    if (buildProduceData.id == buildOrder[0])
                    {
                        return (stopwatch.ElapsedMilliseconds/((float) buildProduceData.CreationTime)*100f);
                    }
                }
                return 0;
            }
        }

        protected override void ParseCustom(MemoryStream memoryStream)
        {
            var reader = new BinaryReader(memoryStream);
            var signature = (BuildingSignature) reader.ReadByte();

            switch (signature)
            {
                case BuildingSignature.ProductionComplete:
                    {
                        if (buildOrder.Count > 0)
                        {
                            onCompleteProduction(buildOrder[0]);
                            buildOrder.RemoveAt(0);
                            stopwatch.Reset();
                        }
                    }
                    break;
                case BuildingSignature.StartProduction:
                    {
                        byte type = reader.ReadByte();
                        onStartProduction(type);
                        buildOrder.Add(type);
                    }
                    break;
                case BuildingSignature.BuildingFinished:
                    {
                        IsBuilding = false;
                        Health = reader.ReadSingle();
                        MyGameMode.AddAlert(GameModeBase.HUDAlert.AlertTypes.BuildingCompleted);
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
            Position = new Vector2f(reader.ReadSingle(), reader.ReadSingle());
            byte buildCount = reader.ReadByte();
            buildOrder.Clear();
            for (int i = 0; i < buildCount; i++)
            {
                buildOrder.Add(reader.ReadByte());
            }

            byte rallyCount = reader.ReadByte();
            rallyPoints.Clear();
            for (int i = 0; i < rallyCount; i++)
            {
                rallyPoints.Add(new Vector2f(reader.ReadSingle(), reader.ReadSingle()));
            }
        }

        public override void Render(RenderTarget target)
        {
            if (Sprites.ContainsKey(CurrentAnimation) && Sprites[CurrentAnimation].Sprites.Count > 0)
            {
                Sprite spr = Sprites[CurrentAnimation].CurrentSprite;
                spr.Position = Position;
                spr.Origin = new Vector2f(spr.TextureRect.Width/2, spr.TextureRect.Height/2);
                target.Draw(spr);
            }
        }

        protected void SetSprites()
        {
            Sprites[AnimationTypes.BeingBuilt].Delay =
                (uint) (BuildTime/(float) Sprites[AnimationTypes.BeingBuilt].Sprites.Count);
        }

        public override void Update(float ms)
        {
            if (Sprites.ContainsKey(CurrentAnimation))
            {
                Sprites[CurrentAnimation].Update(ms);
            }

            if (IsBuilding)
            {
                CurrentAnimation = AnimationTypes.BeingBuilt;
                elapsedBuildTime += ms;
                Health += ((MaxHealth/BuildTime))*ms;
            }
            else
            {
                if (buildOrder.Count > 0)
                {
                    CurrentAnimation = AnimationTypes.Producing;
                }
                else
                {
                    CurrentAnimation = AnimationTypes.Standard;
                }
            }


            if (buildOrder.Count > 0 && stopwatch.IsRunning == false)
            {
                stopwatch.Restart();
            }
        }

        protected virtual void onCompleteProduction(byte type)
        {
            //play sound or add something to HUD
            MyGameMode.AddAlert(GameModeBase.HUDAlert.AlertTypes.UnitCreated);

            foreach (BuildProduceData buildProduceData in supportedBuilds)
            {
                if (buildProduceData.id == type)
                {
                    MyGameMode.PlayUnitFinishedSound(buildProduceData.Sound);
                }
            }
        }

        protected virtual void onStartProduction(byte type)
        {
            //play sound or add something to HUD
        }
    }
}