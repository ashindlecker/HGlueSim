using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SettlersEngine;
using Shared;
using System.IO;
using Lidgren.Network;
using Server.Entities;
using SFML.Window;

namespace Server.GameModes
{
    internal abstract class GameModeBase
    {
        public GameServer Server;
        protected Dictionary<ushort, EntityBase> entities;
        protected List<Player> players;

        public Dictionary<ushort, EntityBase> WorldEntities
        {
            get { return entities; }
        }

        private ushort entityWorldIdToGive;

        protected GameModeBase(GameServer server)
        {
            players = new List<Player>();
            entityWorldIdToGive = 0;
            Server = server;
            entities = new Dictionary<ushort, EntityBase>();
        }

        public virtual void Update(float ms)
        {
            var readOnlyList = new Dictionary<ushort, EntityBase>(entities);

            foreach (var entityBase in readOnlyList.Values)
            {
                entityBase.Update(ms);
                if (entityBase.Health >= entityBase.MaxHealth)
                    entityBase.Health = entityBase.MaxHealth;
                if (entityBase.Energy >= entityBase.MaxEnergy)
                    entityBase.Energy = entityBase.MaxEnergy;

                if(entityBase.RemoveOnNoHealth && entityBase.Health <= 0)
                {
                    entityBase.OnDeath();
                    Remove(entityBase);
                }
            }
        }

        public void Remove(EntityBase entity)
        {
            entities.Remove(entity.WorldId);

            var memory = new MemoryStream();
            var writer = new BinaryWriter(memory);

            writer.Write(entity.WorldId);
            SendData(memory.ToArray(), Gamemode.Signature.RemoveEntity);
        }

        public class PathFindReturn
        {
            public LinkedList<PathNode> List;
            public Vector2i MapSize;
        }
        public abstract PathFindReturn PathFindNodes(float sx, float sy, float x, float y);

        public abstract void OnStatusChange(NetConnection connection, NetConnectionStatus status);
        public abstract void ParseInput(MemoryStream memory, NetConnection client);
        public abstract byte[] HandShake();

        public abstract void UpdatePlayer(Player player);

        public virtual void AddEntity(EntityBase entity, ushort id)
        {
            if (entities.ContainsKey(id)) return;

            entity.MyGameMode = this;
            entity.WorldId = id;
            entities.Add(id, entity);

            var memory = new MemoryStream();
            var writer = new BinaryWriter(memory);

            writer.Write(id);
            writer.Write((byte) entity.EntityType);
            writer.Write(entity.UpdateData());
            SendData(memory.ToArray(), Gamemode.Signature.EntityAdd);
        }

        public void AddEntity(EntityBase ent)
        {
            AddEntity(ent, entityWorldIdToGive);
            entityWorldIdToGive++;
        }

        public void SendAllEntities()
        {
            var memory = new MemoryStream();
            var writer = new BinaryWriter(memory);

            writer.Write((ushort) entities.Count);
            foreach (var entity in entities.Values)
            {
                writer.Write((ushort) entity.WorldId);
                writer.Write((byte) entity.EntityType);
                writer.Write(entity.ToBytes());
            }

            SendData(memory.ToArray(), Gamemode.Signature.EntityLoad);

            writer.Close();
            memory.Close();
        }

        public void SendAllPlayers()
        {

            var memory = new MemoryStream();
            var writer = new BinaryWriter(memory);

            writer.Write((byte) players.Count);

            for(int i = 0; i < players.Count; i++)
            {
                writer.Write(players[i].ToBytes());
            }

            SendData(memory.ToArray(), Gamemode.Signature.PlayersLoad);

            writer.Close();
            memory.Close();
        }

        protected void SendData(byte[] data, Gamemode.Signature signature)
        {
            var memory = new MemoryStream();
            var writer = new BinaryWriter(memory);

            writer.Write((byte) signature);
            writer.Write(data);

            Server.SendGameData(memory.ToArray());

            memory.Close();
            writer.Close();
        }
    }
}
