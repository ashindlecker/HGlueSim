using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Server.Entities.Buildings;
using Shared;
using System.Xml.Linq;
using System.Xml;

namespace Server.Entities
{
    //Producing units
    internal class BuildingBase : EntityBase
    {
        private readonly Stopwatch stopwatch;
        protected List<string> buildOrder;
        private float elapsedBuildTime;


        public BuildingBase(GameServer server, Player player) : base(server, player)
        {
            IsBuilding = true;
            BuildTime = 1000;
            elapsedBuildTime = 0;

            buildOrder = new List<string>();

            EntityType = Entity.EntityType.Building;
            stopwatch = new Stopwatch();

            Health = 1;
            MaxHealth = 100;
        }


        public ushort BuildTime //in milliseconds
        { get; set; }

        public int BuildOrderCount
        {
            get { return buildOrder.Count; }
        }

        public bool IsBuilding { get; private set; }

        private void Complete()
        {
            stopwatch.Stop();

            var memory = new MemoryStream();
            var writer = new BinaryWriter(memory);

            writer.Write((byte) BuildingSignature.ProductionComplete);
            writer.Write(buildOrder[0]);

            BuildCompleteData buildData = onComplete(buildOrder[0]);
            buildOrder.RemoveAt(0);

            buildData.producedEntity.Position = Position;
            buildData.producedEntity.Team = Team;

            if (EntityToUse == null)
            {
                if (rallyPoints.Count > 0)
                    buildData.producedEntity.rallyPoints = new List<Entity.RallyPoint>(rallyPoints);
            }
            MyGameMode.AddEntity(buildData.producedEntity);

            if (EntityToUse != null)
            {
                buildData.producedEntity.SetEntityToUse(EntityToUse);
            }

            writer.Write(buildData.messageData);

            SendData(memory.ToArray(), Entity.Signature.Custom);

            memory.Close();
            writer.Close();
        }

        public override void OnPlayerCustomMove()
        {
            EntityToUse = null;
        }

        public void StartProduce(string type)
        {
            if (buildOrder.Count >= 5) return;
            bool allow = false;

            foreach (var spellData in spells)
            {
                if (spellData.Key == type)
                {
                    var buildProduceData = spellData.Value;
                    if (MyPlayer.Apples >= buildProduceData.AppleCost && MyPlayer.Glue >= buildProduceData.GlueCost &&
                        MyPlayer.Wood >= buildProduceData.WoodCost && MyPlayer.FreeSupply >= buildProduceData.SupplyCost)
                    {
                        MyPlayer.Apples -= buildProduceData.AppleCost;
                        MyPlayer.Glue -= buildProduceData.GlueCost;
                        MyPlayer.Wood -= buildProduceData.WoodCost;
                        MyPlayer.UsedSupply += buildProduceData.SupplyCost;
                        allow = true;
                        break;
                    }
                }
            }

            if (!allow) return;

            var memory = new MemoryStream();
            var writer = new BinaryWriter(memory);

            writer.Write((byte)BuildingSignature.StartProduction);
            writer.Write(type);

            SendData(memory.ToArray(), Entity.Signature.Custom);

            memory.Close();
            writer.Close();

            buildOrder.Add(type);
            onStartProduce(type);
            MyGameMode.UpdatePlayer(MyPlayer);
        }

        public override void Update(float ms)
        {
            if (IsBuilding)
            {
                elapsedBuildTime += ms;
                Health += ((MaxHealth/BuildTime))*ms;

                if (elapsedBuildTime >= BuildTime)
                {
                    IsBuilding = false;

                    //Notify clients the building completed
                    var memory = new MemoryStream();
                    var writer = new BinaryWriter(memory);

                    writer.Write((byte) BuildingSignature.BuildingFinished);
                    writer.Write(Health);
                    SendData(memory.ToArray(), Entity.Signature.Custom);

                    writer.Close();
                    memory.Close();

                    onBuildComplete();
                }
            }
            else
            {
                if (buildOrder.Count > 0 && stopwatch.IsRunning == false)
                {
                    stopwatch.Restart();
                }

                if (buildOrder.Count > 0 && stopwatch.IsRunning)
                {
                    foreach (var spellData in spells)
                    {
                        
                        if (spellData.Key == buildOrder[0] &&
                            stopwatch.ElapsedMilliseconds >= spellData.Value.CastTime)
                        {
                            Complete();
                            break;
                        }
                    }
                }
            }
        }


        public override byte[] UpdateData()
        {
            var memory = new MemoryStream();
            var writer = new BinaryWriter(memory);

            writer.Write(Health);
            writer.Write(MaxHealth);
            writer.Write(IsBuilding);
            writer.Write(BuildTime);
            writer.Write(Energy);
            writer.Write(Position.X);
            writer.Write(Position.Y);

            writer.Write((byte) buildOrder.Count);

            for (int i = 0; i < buildOrder.Count; i++)
            {
                writer.Write(buildOrder[i]);
            }

            writer.Write((byte) rallyPoints.Count);
            for (int i = 0; i < rallyPoints.Count; i++)
            {
                writer.Write(rallyPoints[i].X);
                writer.Write(rallyPoints[i].Y);
            }

            writer.Write(HotkeyString);
            return memory.ToArray();
        }

        public virtual void onBuildComplete()
        {
        }

        protected virtual BuildCompleteData onComplete(string unit)
        {
            return new BuildCompleteData
            {
                messageData = new byte[0],
                producedEntity = UnitBase.CreateUnit(unit, Server, MyPlayer)
            };
        }

        protected virtual void onStartProduce(string type)
        {
            //Usually blank
        }

        private static BuildingBase CreateBuildingFromXML(string building, GameServer server, Player player)
        {
            var buildingData = Settings.GetBuilding(building);
            if(buildingData == null) return new BuildingBase(server, player);
            BuildingBase ret = new BuildingBase(server, player);
            switch (buildingData.BuildingType.ToLower())
            {
                default:
                    ret = new BuildingBase(server, player);
                    break;
                case "base":
                    ret =  new HomeBuilding(server,player);
                    break;
                case "supply":
                    ret = new SupplyBuilding(server, player, buildingData.SupplyAdd);
                    break;
                case "gluefactory":
                    ret = new GlueFactory(server, player);
                    break;
            }

            foreach (var unitElement in buildingData.Units)
            {
                ret.spells.Add(unitElement.UnitTypeString, new SpellData(0, null)
                {
                    AppleCost = unitElement.AppleCost,
                    WoodCost = unitElement.WoodCost,
                    CastTime = unitElement.CreationTime,
                    GlueCost = unitElement.GlueCost,
                    SupplyCost = unitElement.SupplyCost,
                    SpellType = SpellTypes.UnitCreation,
                });
            }
            ret.Health = buildingData.Health;
            ret.MaxHealth = buildingData.MaxHealth;
            ret.BuildTime = buildingData.BuildTime;
            ret.HotkeyString = buildingData.Name;
            return ret;
        }
        public static BuildingBase CreateBuilding(string building, GameServer server, Player player)
        {
            return CreateBuildingFromXML(building, server, player);
        }
        public static BuildingBase CreateBuilding(BuildingTypes building, GameServer server, Player player)
        {
            switch (building)
            {
                    default:
                    case BuildingTypes.Base:
                    return CreateBuilding("base", server, player);
                    break;
                    case BuildingTypes.Supply:
                    return CreateBuilding("supply", server, player);
                    break;
                    case BuildingTypes.GlueFactory:
                    return CreateBuilding("gluefactory", server, player);
                    break;
            }

        }

        #region Nested type: BuildCompleteData

        protected struct BuildCompleteData
        {
            public byte[] messageData;
            public EntityBase producedEntity;
        }

        #endregion
    }
}