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
        protected List<byte> supportedBuilds;
 
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


        public BuildingBase(GameServer server) : base(server)
        {
            supportedBuilds = new List<byte>();
            IsBuilding = true;
            BuildTime = 10000;
            elapsedBuildTime = 0;

            buildOrder = new List<byte>();

            EntityType = Entity.EntityType.Building;
            stopwatch = new Stopwatch();

            Health = 1;
            MaxHealth = 100;
        }

        protected virtual bool allowProduction(byte unitType)
        {
            return false;
        }

        public void StartProduce(byte type)
        {
            bool allow = false;
            for(int i = 0; i < supportedBuilds.Count; i++)
            {
                if(supportedBuilds[i] == type)
                {
                    allow = true;
                    break;
                }
            }
            if (!allow) return;
            if (buildOrder.Count >= 5) return;
            if (!allowProduction(type)) return;

            var memory = new MemoryStream();
            var writer = new BinaryWriter(memory);

            writer.Write((byte) BuildingSignature.StartProduction);
            writer.Write(type);

            SendData(memory.ToArray(), Entity.Signature.Custom);

            memory.Close();
            writer.Close();


            buildOrder.Add(type);
            onStartProduce(type);
        }

        //Called when a unit just started producing
        protected virtual void onStartProduce(byte type)
        {
            //Usually blank
        }

        protected virtual uint creationTime(byte type)
        {
            return 0;
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
                    if (stopwatch.ElapsedMilliseconds >= creationTime(buildOrder[0]))
                    {
                        Complete();
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
            MyGameMode.AddEntity(buildData.producedEntity);

            if(EntityToUse == null)
            {
                if (rallyPoints.Count > 0)
                    buildData.producedEntity.Move(rallyPoints[0].X, rallyPoints[0].Y);
            }
            else
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
