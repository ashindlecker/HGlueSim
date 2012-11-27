using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Server.Entities.Buildings;
using Server.Entities.Units;
using Shared;
using System.Xml.Linq;
using System.Xml;

namespace Server.Entities
{
    //Producing units
    internal class BuildingBase : EntityBase
    {
        private readonly Stopwatch stopwatch;
        protected List<byte> buildOrder;
        private float elapsedBuildTime;


        public BuildingBase(GameServer server, Player player) : base(server, player)
        {
            IsBuilding = true;
            BuildTime = 1000;
            elapsedBuildTime = 0;

            buildOrder = new List<byte>();

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

        public void StartProduce(byte type)
        {
            if (buildOrder.Count >= 5) return;
            bool allow = false;

            foreach (var spellData in spells)
            {
                if (spellData.Value.BuildType == type)
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

            writer.Write((byte) BuildingSignature.StartProduction);
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
                        
                        if (spellData.Value.BuildType == buildOrder[0] &&
                            stopwatch.ElapsedMilliseconds >= spellData.Value.CastTime)
                        {
                            Complete();
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
            return memory.ToArray();
        }

        public virtual void onBuildComplete()
        {
        }

        protected virtual BuildCompleteData onComplete(byte unit)
        {
            switch ((UnitBuildIds)unit)
            {
                default:
                case UnitBuildIds.Worker: //Worker
                    return new BuildCompleteData
                    {
                        messageData = new byte[0],
                        producedEntity = new StandardWorker(Server, MyPlayer)
                    };
                    break;
            }
        }

        protected virtual void onStartProduce(byte type)
        {
            //Usually blank
        }

        private static BuildingBase CreateBuildingFromXML(string building, GameServer server, Player player)
        {
            const string BUILDINGSFILE = "Resources/Data/Buildings.xml";

            BuildingBase retBuilding = null;

            var xElement = XDocument.Load(BUILDINGSFILE);
            var buildingElements = xElement.Element("buildings").Elements("building");

            foreach (var buildingElement in buildingElements)
            {
                if(buildingElement.Attribute("name").Value.ToLower() != building)
                    continue;

                switch (buildingElement.Attribute("name").Value.ToLower())
                {
                    default:
                    case "base":
                        retBuilding = new HomeBuilding(server, player);
                        break;
                    case "supply":
                        {
                            var supplyAttribute = buildingElement.Attribute("supply");
                            if(supplyAttribute != null)
                                retBuilding = new SupplyBuilding(server, player, Convert.ToByte(supplyAttribute.Value));
                            else
                            {
                                retBuilding = new SupplyBuilding(server, player, 12);
                            }
                        }
                        break;
                    case "gluefactory":
                        retBuilding = new GlueFactory(server, player);
                        break;
                }

                var unitElements = buildingElement.Element("units").Elements("unit");

                foreach (var unitElement in unitElements)
                {
                    var appleCost = (ushort)0;
                    var woodCost = (ushort)0;
                    var buildTime = (ushort)0;
                    var glueCost = (ushort)0;
                    var unitid = (byte)0;
                    var supply = (byte) 0;

                    if (unitElement.Attribute("glue") != null)
                        glueCost = Convert.ToUInt16(unitElement.Attribute("glue").Value);
                    if (unitElement.Attribute("apples") != null)
                        appleCost = Convert.ToUInt16(unitElement.Attribute("apples").Value);
                    if (unitElement.Attribute("wood") != null)
                        woodCost = Convert.ToUInt16(unitElement.Attribute("wood").Value);
                    if (unitElement.Attribute("buildtime") != null)
                        buildTime = Convert.ToUInt16(unitElement.Attribute("buildtime").Value);
                    if (unitElement.Attribute("unit") != null)
                        unitid = Convert.ToByte(unitElement.Attribute("unit").Value);
                    if (unitElement.Attribute("supply") != null)
                        unitid = Convert.ToByte(unitElement.Attribute("supply").Value);


                    retBuilding.spells.Add((byte)retBuilding.spells.Count, new SpellData(0,null)
                                                        {
                                                            AppleCost = appleCost,
                                                            WoodCost = woodCost,
                                                            CastTime= buildTime,
                                                            GlueCost = glueCost,
                                                            SupplyCost = supply,
                                                            IsBuildSpell = true,
                                                            BuildType = unitid,
                                                        });
                }
            }

            return retBuilding;
        }
        public static BuildingBase CreateBuilding(BuildingTypes building, GameServer server, Player player)
        {
            switch (building)
            {
                    default:
                    case BuildingTypes.Base:
                    return CreateBuildingFromXML("base", server, player);
                    break;
                    case BuildingTypes.Supply:
                    return CreateBuildingFromXML("supply", server, player);
                    break;
                    case BuildingTypes.GlueFactory:
                    return CreateBuildingFromXML("gluefactory", server, player);
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