using System.Collections.Generic;
using System.IO;
using Lidgren.Network;
using SFML.Window;
using Server.Entities;
using Shared;

namespace Server.GameModes
{
    internal class StandardMelee : GameModeBase
    {
        #region StatusState enum

        public enum StatusState : byte
        {
            InProgress,
            WaitingForPlayers,
            Completed,
        }

        #endregion

        public BuildingBase Building;


        public byte MaxPlayers;

        private StatusState _gameStatus;
        public byte idToGive;

        public StandardMelee(GameServer server, byte mplayers) : base(server)
        {
            MaxPlayers = mplayers;
            GameStatus = StatusState.WaitingForPlayers;
            idToGive = 0;

            SetMap("Resources/Maps/untitled.tmx");

            var worker = UnitBase.CreateUnit(UnitTypes.Worker, Server, null);
            worker.Team = 1;
            worker.Position = new Vector2f(100, 500);
            //AddEntity(worker);


            var build = BuildingBase.CreateBuilding("standardBase", Server, null);
            build.Team = 1;
            build.Position = new Vector2f(100, 500);
            //AddEntity(build);
        }

        public StatusState GameStatus
        {
            get { return _gameStatus; }

            set
            {
                _gameStatus = value;
                SendData(new byte[2] {(byte) StandardMeleeSignature.StatusChanged, (byte) _gameStatus},
                         Gamemode.Signature.Custom);
            }
        }

        public override void AddConnection(NetConnection connection)
        {
            if (players.Count < MaxPlayers)
            {
                //Connected client must be a player

                var nPlayer = new Player { ClientId = idToGive, Team = idToGive };
                nPlayer.Wood = 50;
                nPlayer.Supply = 10;
                players.Add(nPlayer);
                connection.Tag = nPlayer;

                var home =// new HomeBuilding(Server, nPlayer);
                BuildingBase.CreateBuilding("standardBase", Server, nPlayer);
                home.Team = nPlayer.Team;
                home.BuildTime = 0;
                if (TiledMap.SpawnPoints.Count > players.Count - 1)
                {
                    home.Position = TiledMap.SpawnPoints[players.Count - 1];
                }

                AddEntity(home);

                SendAllPlayers();
                SendMap();
                SendAllEntities();
                SetCamera(nPlayer, home.Position);

                if (players.Count >= MaxPlayers)
                {
                    GameStatus = StatusState.InProgress;
                }

                var memory = new MemoryStream();
                var writer = new BinaryWriter(memory);
                writer.Write((byte) Gamemode.Signature.Handshake);
                writer.Write(idToGive);
                Server.SendGameData(memory.ToArray(), connection);
            }
            else
            {
                //Connectd client must be a spectator or something non-player type?
                SendAllPlayers();
                SendData(map.ToBytes(), Gamemode.Signature.MapLoad);
                SendAllEntities();
            }
            idToGive++;
        }

        public override void OnStatusChange(NetConnection connection, NetConnectionStatus status)
        {
            switch (status)
            {
                case NetConnectionStatus.None:
                    break;
                case NetConnectionStatus.InitiatedConnect:
                    break;
                case NetConnectionStatus.RespondedAwaitingApproval:
                    break;
                case NetConnectionStatus.RespondedConnect:
                    break;
                case NetConnectionStatus.Connected:
                    {
                        AddConnection(connection);
                    }
                    break;
                case NetConnectionStatus.Disconnecting:

                    for (int i = 0; i < players.Count; i++)
                    {
                        if (players[i] == connection.Tag)
                        {
                            var memory = new MemoryStream();
                            var writer = new BinaryWriter(memory);

                            players[i].Status = Player.StatusTypes.Left;

                            writer.Write((byte) Gamemode.Signature.PlayerLeft);
                            writer.Write(players[i].ClientId);

                            SendData(memory.ToArray(), Gamemode.Signature.Custom);

                            writer.Close();
                            memory.Close();

                            players.RemoveAt(i);
                            break;
                        }
                    }

                    break;
                case NetConnectionStatus.Disconnected:
                    break;
                default:
                    break;
            }
        }

        public override void ParseInput(MemoryStream memory, NetConnection client)
        {
            var reader = new BinaryReader(memory);

            var type = (InputSignature) reader.ReadByte();

            switch (type)
            {
                case InputSignature.Movement:
                    {
                        var player = (Player) client.Tag;

                        float posX = reader.ReadSingle();
                        float posY = reader.ReadSingle();
                        bool reset = reader.ReadBoolean();
                        bool attackMove = reader.ReadBoolean();
                        byte unitCount = reader.ReadByte();


                        var outmemory = new MemoryStream();
                        var outwriter = new BinaryWriter(outmemory);

                        outwriter.Write(posX);
                        outwriter.Write(posY);
                        outwriter.Write(reset);
                        outwriter.Write(attackMove);

                        var idsToWrite = new List<ushort>();

                        for (int i = 0; i < unitCount; i++)
                        {
                            ushort entityId = reader.ReadUInt16();
                            if (entities.ContainsKey(entityId) == false) continue;
                            if (entities[entityId].Team != player.Team) continue;

                            idsToWrite.Add(entityId);
                        }

                        outwriter.Write((byte) idsToWrite.Count);
                        for (int i = 0; i < idsToWrite.Count; i++)
                        {
                            outwriter.Write(idsToWrite[i]);
                        }
                        SendData(outmemory.ToArray(), Gamemode.Signature.GroupMovement, true);

                        outmemory.Close();
                        outwriter.Close();

                        for (int i = 0; i < idsToWrite.Count; i++)
                        {
                            ushort entityId = idsToWrite[i];

                            entities[entityId].Move(posX, posY,
                                                    !attackMove
                                                        ? Entity.RallyPoint.RallyTypes.StandardMove
                                                        : Entity.RallyPoint.RallyTypes.AttackMove, reset,
                                                    false);

                            entities[entityId].OnPlayerCustomMove();

                            var unitCast = entities[entityId] as UnitBase;
                            if (unitCast != null)
                            {
                                unitCast.State = attackMove ? UnitBase.UnitState.Agro : UnitBase.UnitState.Standard;
                            }
                        }
                    }
                    break;
                case InputSignature.CreateUnit:
                    /*OBSOLETE
                    {
                        byte unitToCreate = reader.ReadByte();
                        byte unitCount = reader.ReadByte();
                        var player = (Player) client.Tag;

                        BuildingBase buildingToUse = null;

                        /*
                        /// We want to find the building that has the least producing units to make production faster and easier
                        /// For example, if the client has 2 buildings, and want to produce a zealot
                        /// If building 1 has a zealot in production, but building 2 is not in use, it'll choose building 2 to producte

                        for (int i = 0; i < unitCount; i++)
                        {
                            ushort entityId = reader.ReadUInt16();
                            if (entities.ContainsKey(entityId) == false) continue;
                            if (entities[entityId].Team != player.Team) continue;

                            EntityBase entity = entities[entityId];

                            if (entity is BuildingBase)
                            {
                                var building = (BuildingBase) entity;
                                if (buildingToUse == null || building.BuildOrderCount < buildingToUse.BuildOrderCount)
                                {
                                    buildingToUse = building;
                                }
                            }
                        }

                        if (buildingToUse != null)
                        {
                            buildingToUse.StartProduce(unitToCreate);
                        }
                    }
                    break;
                    */
                case InputSignature.ChangeUseEntity:
                    {
                        var player = (Player) client.Tag;
                        ushort useEntity = reader.ReadUInt16();

                        byte unitCount = reader.ReadByte();


                        var idsToWrite = new List<ushort>();

                        for (int i = 0; i < unitCount; i++)
                        {
                            ushort entityId = reader.ReadUInt16();
                            if (entities.ContainsKey(entityId) == false || entities.ContainsKey(useEntity) == false)
                                continue;
                            if (entities[entityId].Team != player.Team) continue;

                            entities[entityId].SetEntityToUse(entities[useEntity]);
                            entities[entityId].Move(entities[useEntity].Position.X, entities[useEntity].Position.Y,
                                                    Entity.RallyPoint.RallyTypes.StandardMove, false, false);
                            idsToWrite.Add(entityId);
                        }

                        if (entities.ContainsKey(useEntity))
                        {
                            var outmemory = new MemoryStream();
                            var outwriter = new BinaryWriter(outmemory);

                            outwriter.Write(entities[useEntity].Position.X);
                            outwriter.Write(entities[useEntity].Position.Y);
                            outwriter.Write(false);
                            outwriter.Write(false);
                            outwriter.Write((byte) idsToWrite.Count);

                            for (int i = 0; i < idsToWrite.Count; i++)
                            {
                                outwriter.Write(idsToWrite[i]);
                            }
                            SendData(outmemory.ToArray(), Gamemode.Signature.GroupMovement);

                            outmemory.Close();
                            outwriter.Close();
                        }
                    }
                    break;
                case InputSignature.SpellCast:
                    {
                        var player = (Player) client.Tag;
                        string spell = reader.ReadString();
                        float x = reader.ReadSingle();
                        float y = reader.ReadSingle();
                        byte unitCount = reader.ReadByte();

                        BuildingBase buildingToUse = null;
                        for (int i = 0; i < unitCount; i++)
                        {
                            ushort entityId = reader.ReadUInt16();
                            if (entities.ContainsKey(entityId) == false) continue;
                            if (entities[entityId].Team != player.Team) continue;

                            if (entities[entityId] is UnitBase)
                            {
                                if (entities[entityId].CastSpell(spell, x, y))
                                {
                                    break;
                                    //we only want one unit to cast a spell at a time, so we don't cast a million spells at the same time
                                }
                            }
                            else if (entities[entityId] is BuildingBase)
                            {
                                var buildingEntity = entities[entityId] as BuildingBase;
                                if (buildingToUse == null)
                                {
                                    buildingToUse = buildingEntity;
                                }
                                else
                                {
                                    if (buildingEntity.BuildOrderCount < buildingToUse.BuildOrderCount)
                                    {
                                        buildingToUse = buildingEntity;
                                    }
                                }
                            }
                        }
                        if(buildingToUse != null)
                        {
                            buildingToUse.CastSpell(spell, x, y);
                        }
                    }
                    break;

                case InputSignature.Surrender:
                    {
                        var outmemory = new MemoryStream();
                        var writer = new BinaryWriter(outmemory);

                        var player = (Player) client.Tag;
                        player.Status = Player.StatusTypes.Left;

                        writer.Write((byte) StandardMeleeSignature.PlayerSurrender);
                        writer.Write(player.ClientId);
                        SendData(outmemory.ToArray(), Gamemode.Signature.Custom);

                        outmemory.Close();
                        writer.Close();
                    }
                    break;
                default:
                    break;
            }
        }

        public override void Update(float ms)
        {
            UpdateTiles();
            //base.Update(ms);
            switch (GameStatus)
            {
                case StatusState.InProgress:
                    {
                        //Check if players are still playing

                        //TODO: Change back to non-comment when ready
                        base.Update(ms);
                        int team1Count = 0;
                        int team1Id = 0;
                        bool gameInProgress = false;

                        foreach (Player player in players)
                        {
                            if (player.Status == Player.StatusTypes.InGame)
                            {
                                if (team1Count == 0)
                                {
                                    team1Id = player.Team;
                                    team1Count++;
                                }
                                else
                                {
                                    if (team1Id != player.Team)
                                    {
                                        gameInProgress = true;
                                        break;
                                    }
                                }
                            }
                        }
                        //If there's not enough players playing, the game is completed
                        if (gameInProgress == false)
                        {
                            GameStatus = StatusState.Completed;
                        }

                        //Check if player has lost all their buildings

                        foreach (Player player in players)
                        {
                            byte team = player.Team;
                            bool hasBuilding = false;
                            foreach (EntityBase entity in WorldEntities.Values)
                            {
                                if (entity.Team == team && entity is BuildingBase)
                                {
                                    hasBuilding = true;
                                    break;
                                }
                            }
                            if (hasBuilding == false)
                            {
                                //player has been eliminated
                                GameStatus = StatusState.Completed;
                            }
                        }
                    }

                    break;
                case StatusState.WaitingForPlayers:
                    break;
                case StatusState.Completed:
                    //TODO: Change back to non-comment when ready
                    base.Update(ms);
                    break;
                default:
                    break;
            }
        }

        public override void UpdatePlayer(Player player)
        {
            var memory = new MemoryStream();
            var writer = new BinaryWriter(memory);

            writer.Write(player.ClientId);
            writer.Write(player.ToBytes());
            SendData(memory.ToArray(), Gamemode.Signature.PlayerData);

            memory.Close();
            writer.Close();
        }

        public void UpdateTiles()
        {
            if (pathFinding != null)
            {
                foreach (STileBase sTileBase in map.Tiles)
                {
                    pathFinding.SearchSpace[sTileBase.GridX, sTileBase.GridY].IsWall = (sTileBase.DynamicSolid ||
                                                                                        sTileBase.Solid);
                    sTileBase.DynamicSolid = false;
                }
                foreach (EntityBase entityBase in entities.Values)
                {
                    if (entityBase is BuildingBase == false) continue;

                    Vector2f tilePosition = map.ConvertCoords(entityBase.Position);
                    if (tilePosition.X >= 0 && tilePosition.Y >= 0 && tilePosition.X < map.MapSize.X &&
                        tilePosition.Y < map.MapSize.Y)
                    {
                        map.Tiles[(int) tilePosition.X, (int) tilePosition.Y].DynamicSolid = true;
                    }
                }
            }
        }
    }
}