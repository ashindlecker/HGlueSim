using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SFML.Window;
using Shared;
using System.Diagnostics;
using System.IO;


namespace Server.Entities
{
    //Producing units
    class BuildingBase : EntityBase
    {
        protected List<byte> buildOrder;
        

        protected List<BuildProduceData> supportedBuilds;
 
        public ushort BuildTime //in milliseconds
        {
            get; set;
        }

        public int BuildOrderCount
        {
            get { return buildOrder.Count; }
        }

        private float elapsedBuildTime;
        public bool IsBuilding { get; private set; }


        private Stopwatch stopwatch;


        public BuildingBase(GameServer server, Player player) : base(server, player)
        {
            supportedBuilds = new List<BuildProduceData>();
            IsBuilding = true;
            BuildTime = 1000;
            elapsedBuildTime = 0;

            buildOrder = new List<byte>();

            EntityType = Entity.EntityType.Building;
            stopwatch = new Stopwatch();

            Health = 1;
            MaxHealth = 100;
        }

        public void StartProduce(byte type)
        {
            if (buildOrder.Count >= 5) return;
            bool allow = false;
            foreach (var buildProduceData in supportedBuilds)
            {
                if (buildProduceData.id == type)
                {
                    if (MyPlayer.Apples >= buildProduceData.AppleCost && MyPlayer.Glue >= buildProduceData.GlueCost && MyPlayer.Wood >= buildProduceData.WoodCost && MyPlayer.FreeSupply >= buildProduceData.SupplyCost)
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

        //Called when a unit just started producing
        protected virtual void onStartProduce(byte type)
        {
            //Usually blank
        }

        //Called when the building is finished building
        public virtual void onBuildComplete()
        {
            
        }

        public override void OnPlayerCustomMove()
        {
            EntityToUse = null;
        }

        public override void Update(float ms)
        {
            if (IsBuilding)
            {
                elapsedBuildTime += ms;
                Health += (float)((MaxHealth / BuildTime)) * ms;

                if (elapsedBuildTime >= BuildTime)
                {
                    IsBuilding = false;

                    //Notify clients the building completed
                    var memory = new MemoryStream();
                    var writer = new BinaryWriter(memory);

                    writer.Write((byte)BuildingSignature.BuildingFinished);
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
                    foreach (var buildProduceData in supportedBuilds)
                    {
                        if(buildProduceData.id == buildOrder[0] && stopwatch.ElapsedMilliseconds >= buildProduceData.CreationTime)
                        {
                            Complete();
                        }
                    }
                }
            }
        }


        private void Complete()
        {
            stopwatch.Stop();

            var memory = new MemoryStream();
            var writer = new BinaryWriter(memory);

            writer.Write((byte) BuildingSignature.ProductionComplete);
            writer.Write((byte) buildOrder[0]);

            var buildData = onComplete(buildOrder[0]);
            buildOrder.RemoveAt(0);

            buildData.producedEntity.Position = Position;
            buildData.producedEntity.Team = Team;

            if (EntityToUse == null)
            {
                if (rallyPoints.Count > 0)
                    buildData.producedEntity.rallyPoints = new List<Entity.RallyPoint>(rallyPoints);
            }
            MyGameMode.AddEntity(buildData.producedEntity);

            if(EntityToUse != null)
            {
                buildData.producedEntity.SetEntityToUse(EntityToUse);
            }

            writer.Write(buildData.messageData);

            SendData(memory.ToArray(), Entity.Signature.Custom);

            memory.Close();
            writer.Close();
        }

        protected struct BuildCompleteData
        {
            public byte[] messageData;
            public EntityBase producedEntity;
        }


        protected virtual BuildCompleteData onComplete(byte unit)
        {
            return new BuildCompleteData();
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

            for(var i = 0; i < buildOrder.Count; i++)
            {
                writer.Write(buildOrder[i]);
            }

            writer.Write((byte) rallyPoints.Count);
            for(var i = 0; i < rallyPoints.Count; i++)
            {
                writer.Write(rallyPoints[i].X);
                writer.Write(rallyPoints[i].Y);
            }
            return memory.ToArray();
        }

    }
}
