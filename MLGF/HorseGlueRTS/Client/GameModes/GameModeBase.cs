using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Client.Entities;
using Shared;
using SFML.Graphics;
using SFML.Window;
using System.Threading;

namespace Client.GameModes
{
    internal abstract class GameModeBase
    {
        protected Dictionary<ushort, EntityBase> entities;
        protected Dictionary<byte, Player> players;
 
        public GameModeBase()
        {
            entities = new Dictionary<ushort, EntityBase>();
            players = new Dictionary<byte, Player>();
        }

        public void ParseData(MemoryStream stream)
        {
            var reader = new BinaryReader(stream);
            var signature = (Gamemode.Signature) reader.ReadByte();

            switch (signature)
            {
                case Gamemode.Signature.Custom:
                    ParseCustom(stream);
                    break;
                case Gamemode.Signature.Entity:
                    {
                        var id = reader.ReadUInt16();
                        if (entities.ContainsKey(id))
                            entities[id].ParseData(stream);
                    }
                    break;
                case Gamemode.Signature.MapLoad:
                    ParseMap(stream);
                    break;
                case Gamemode.Signature.Handshake:
                    ParseHandshake(stream);
                    break;
                case Gamemode.Signature.EntityAdd:
                    {
                        var id = reader.ReadUInt16();
                        var entityType = reader.ReadByte();
                        EntityBase entity = EntityBase.EntityFactory(entityType);
                        entity.Type = (Entity.EntityType) entityType;
                        entity.WorldEntities = entities;
                        entity.LoadFromBytes(stream);
                        entity.WorldId = id;
                        if (entities.ContainsKey(id) == false) entities.Add(id, entity);
                    }
                    break;
                case Gamemode.Signature.EntityLoad:
                    {
                        var count = reader.ReadUInt16();
                        for (var i = 0; i < count; i++)
                        {
                            var entId = reader.ReadUInt16();
                            var entType = reader.ReadByte();
                            var entAdd = EntityBase.EntityFactory(entType);
                            entAdd.Type = (Entity.EntityType)entType;
                            entAdd.WorldEntities = entities;
                            entAdd.LoadFromBytes(stream);
                            entAdd.WorldId = entId;
                            if (entities.ContainsKey(entId) == false) entities.Add(entId, entAdd);
                        }
                    }
                    break;
                case Gamemode.Signature.PlayerData:
                    {
                        var playerId = reader.ReadByte();
                        if (players.ContainsKey(playerId))
                        {
                            players[playerId].Load(stream);
                        }
                    }
                    break;
                case Gamemode.Signature.PlayersLoad:
                    {
                        var count = reader.ReadByte();
                        for (var i = 0; i < count; i++)
                        {
                            var playerAdd = new Player();
                            playerAdd.Load(stream);
                            if (players.ContainsKey(playerAdd.ClientId) == false)
                            {
                                players.Add(playerAdd.ClientId, playerAdd);
                            }
                        }
                    }
                    break;
                case Gamemode.Signature.RemoveEntity:
                    {
                        var id = reader.ReadUInt16();
                        if (entities.ContainsKey(id))
                        {
                            entities[id].OnDeath();
                            entities.Remove(id);
                        }
                    }
                    break;
            }
        }

        protected abstract void ParseCustom(MemoryStream memory);
        protected abstract void ParseMap(MemoryStream memory);
        protected abstract void ParseHandshake(MemoryStream memory);

        public abstract void Update(float ms);
        public abstract void Render(RenderTarget target);

        public virtual void MouseMoved(int x, int y)
        {
        }

        public virtual void MouseClick(Mouse.Button button, int x, int y)
        {
            
        }

        public virtual void MouseRelease(Mouse.Button button, int x, int y)
        {

        }

        public virtual void KeyPress(KeyEventArgs keyEvent)
        {
            
        }


        public virtual void KeyRelease(KeyEventArgs keyEvent)
        {

        }
    }
}
