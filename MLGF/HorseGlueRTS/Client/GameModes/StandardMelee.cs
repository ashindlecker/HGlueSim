using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Client.Entities;
using Client.Level;
using SFML.Graphics;
using SFML.Window;
using Shared;
namespace Client.GameModes
{
    class StandardMelee : GameModeBase
    {
        private byte myId;
        private bool idSet;

        private Player myPlayer
        {
            get
            {
                if (players.ContainsKey(myId))
                    return players[myId];
                return null;
            }
        }

        private Level.TileMap map;
        public  InputHandler InputHandler;

        protected EntityBase[]selectedUnits;
        protected bool selectedAttackMove;
        protected bool releaseSelect;
        protected Dictionary<Keyboard.Key, EntityBase[]> controlGroups;

        public enum StatusState : byte
        {
            InProgress,
            WaitingForPlayers,
            Completed,
        }

        public StatusState CurrentStatus;

        //For dragging boxes over units to select them
        private Vector2f controlBoxP1;
        private Vector2f controlBoxP2;


        //HotKeys
        private const Keyboard.Key AttackHotkey = Keyboard.Key.A;
        private const Keyboard.Key NormalBuildingsHotkey = Keyboard.Key.B;
        private const Keyboard.Key AdvancedBuildingsHotkey = Keyboard.Key.V;
        private const Keyboard.Key HomeBaseHotkey = Keyboard.Key.N;
        private const Keyboard.Key SupplyBuildingHotkey = Keyboard.Key.E;
        private const Keyboard.Key WorkerHotkey = Keyboard.Key.E;

        //UI
        private enum WorkerUIStateTypes : byte
        {
            Normal,
            StandardBuildings,
            AdvancedBuildings,
            ReadyToPlace,
        }
        private WorkerUIStateTypes workerUIState;
        private byte buildingToPlace;



        public StandardMelee(InputHandler handler)
        {
            CurrentStatus = StatusState.WaitingForPlayers;

            workerUIState = WorkerUIStateTypes.Normal;
            buildingToPlace = 0;

            InputHandler = handler;
            myId = 0;
            idSet = false;
            map = new TileMap();

            selectedUnits = null;
            controlGroups = new Dictionary<Keyboard.Key, EntityBase[]>();
            controlBoxP1 = new Vector2f(0,0);
            controlBoxP2 = new Vector2f(0,0);
            selectedAttackMove = false;
            releaseSelect = false;
        }

        protected override void ParseCustom(MemoryStream memory)
        {
            var reader = new BinaryReader(memory);
            var signature = (StandardMeleeSignature) reader.ReadByte();

            switch(signature)
            {
                case StandardMeleeSignature.StatusChanged:
                    {
                        var status = memory.ReadByte();
                        CurrentStatus = (StatusState) status;
                    }
                    break;
                case StandardMeleeSignature.PlayerSurrender:
                    {
                        var id = reader.ReadByte();
                        if(players.ContainsKey(id))
                        {
                            players[id].Status = Player.StatusTypes.Left;
                            onPlayerSurrender(players[id]);
                        }
                    }
                    break;
                case StandardMeleeSignature.PlayerElimination:
                    {
                        var id = reader.ReadByte();
                        if (players.ContainsKey(id))
                        {
                            players[id].Status = Player.StatusTypes.Left;
                            onPlayerElimination(players[id]);
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        protected override void ParseMap(MemoryStream memory)
        {
            map.LoadFromBytes(memory);
        }

        protected override void ParseHandshake(MemoryStream memory)
        {
            var reader = new BinaryReader(memory);
            myId = reader.ReadByte();
            idSet = true;
        }

        private void sendMoveCommand(float x, float y)
        {
            if (selectedUnits != null)
            {
                var idList = new List<ushort>();
                foreach (var entityBase in selectedUnits)
                {
                    idList.Add(entityBase.WorldId);
                }
                if (idList.Count > 0)
                {
                    InputHandler.SendMoveInput(x, y, idList.ToArray(), true, selectedAttackMove);
                }
            }
        }

        private void sendUseCommand(EntityBase entity)
        {
            if (selectedUnits != null)
            {
                var idList = new List<ushort>();
                foreach (var entityBase in selectedUnits)
                {
                    idList.Add(entityBase.WorldId);
                }
                if (idList.Count > 0)
                {
                    InputHandler.SendEntityUseChange(idList.ToArray(), entity.WorldId);
                }
            }
        }

        private void sendBuildCommand(UnitBuildIds id)
        {
            if (selectedUnits != null)
            {
                var idList = new List<ushort>();
                foreach (var entityBase in selectedUnits)
                {
                    if(entityBase is BuildingBase)
                        idList.Add(entityBase.WorldId);
                }
                InputHandler.SendBuildUnit(idList.ToArray(), (byte)id);
            }
        }


        public override void MouseClick(Mouse.Button button, int x, int y)
        {
            Vector2f convertedPos = Program.window.ConvertCoords(new Vector2i(x, y));
            if (button == Mouse.Button.Left)
            {
                if (workerUIState == WorkerUIStateTypes.ReadyToPlace)
                {
                    var priority = prioritySelectedUnit();

                    if (priority != null)
                    {
                        InputHandler.SendSpellInput(convertedPos.X, convertedPos.Y, buildingToPlace,
                                                    new ushort[1] {prioritySelectedUnit().WorldId});
                        InputHandler.SendMoveInput(convertedPos.X, convertedPos.Y,
                                                   new ushort[1] {prioritySelectedUnit().WorldId}, false, false);
                    }
                }
                else if (!selectedAttackMove)
                {
                    controlBoxP1 = convertedPos;
                    releaseSelect = true;
                }
                else
                {
                    sendMoveCommand(convertedPos.X, convertedPos.Y);
                    selectedAttackMove = false;
                }
            }
            if(button == Mouse.Button.Right)
            {
                EntityBase toUse = null;
                foreach (var entity in entities.Values)
                {
                    if(entity.GetBounds().Contains(convertedPos.X, convertedPos.Y))
                    {
                        toUse = entity;
                        break;
                    }
                }
                if(toUse != null)
                {
                    sendUseCommand(toUse);
                }
                else
                {
                    sendMoveCommand(convertedPos.X, convertedPos.Y);
                }
                selectedAttackMove = false;
            }

            workerUIState = WorkerUIStateTypes.Normal;
        }

        public override void MouseRelease(Mouse.Button button, int x, int y)
        {
            Vector2f convertedPos = Program.window.ConvertCoords(new Vector2i(x, y));
            if (releaseSelect && button == Mouse.Button.Left)
            {
                controlBoxP2 = convertedPos;
                SetControlUnits(new FloatRect(controlBoxP1.X, controlBoxP1.Y, controlBoxP2.X - controlBoxP1.X,
                                              controlBoxP2.Y - controlBoxP1.Y));
                releaseSelect = false;
            }
            if(button == Mouse.Button.Right)
            {

            }
        }

        public override void KeyPress(KeyEventArgs keyEvent)
        {
            var priorEntity = prioritySelectedUnit();
            if(priorEntity == null) return;

            if (priorEntity.Type == Entity.EntityType.Worker)
            {
                if (keyEvent.Code == NormalBuildingsHotkey)
                {
                    workerUIState = WorkerUIStateTypes.StandardBuildings;
                }
                else if (keyEvent.Code == AdvancedBuildingsHotkey)
                {
                    workerUIState = WorkerUIStateTypes.AdvancedBuildings;
                }
                else if (workerUIState == WorkerUIStateTypes.StandardBuildings)
                {
                    if (keyEvent.Code == HomeBaseHotkey)
                    {
                        workerUIState = WorkerUIStateTypes.ReadyToPlace;
                        buildingToPlace = (byte) WorkerSpellIds.BuildHomeBase;
                    }

                    if (keyEvent.Code == SupplyBuildingHotkey)
                    {
                        workerUIState = WorkerUIStateTypes.ReadyToPlace;
                        buildingToPlace = (byte) WorkerSpellIds.BuildSupplyBuilding;
                    }
                }
                else if (workerUIState == WorkerUIStateTypes.AdvancedBuildings)
                {

                }
            }
            else
            {
                workerUIState = WorkerUIStateTypes.Normal;
            }
            if(priorEntity.Type == Entity.EntityType.HomeBuilding)
            {
                if(keyEvent.Code == WorkerHotkey)
                {
                    sendBuildCommand(UnitBuildIds.Worker);
                }
            }

            if(keyEvent.Code == AttackHotkey)
            {
                selectedAttackMove = true;
                Console.WriteLine("Is Attacking");
            }
        }


        private void SetControlUnits(FloatRect floatRect)
        {
            List<EntityBase> controlList = new List<EntityBase>();
            foreach (var entity in entities.Values)
            {
                if (floatRect.Contains(entity.Position.X, entity.Position.Y))
                {
                    controlList.Add(entity);
                }
            }

            if(controlList.Count > 0)
            {
                selectedUnits = controlList.ToArray();
            }
        }

        private EntityBase prioritySelectedUnit()
        {
            if(selectedUnits != null)
            {
                foreach (var entity in selectedUnits)
                {
                    if(entity.Type == Entity.EntityType.Worker)
                        return entity;
                }
                foreach (var entity in selectedUnits)
                {
                    if (entity.Type == Entity.EntityType.HomeBuilding)
                        return entity;
                }
                foreach (var entity in selectedUnits)
                {
                    if (entity.Type == Entity.EntityType.Unit)
                        return entity;
                }

                if(selectedUnits.Length > 0)
                    return selectedUnits[0];
            }
            return null;
        }

        public override void Update(float ms)
        {
            var readOnly = new Dictionary<ushort, EntityBase>(entities);
            foreach (var entityBase in readOnly.Values)
            {
                entityBase.Update(ms);
            }
        }

        protected void onPlayerSurrender(Player player)
        {
            //rage quit banner popup or something
        }

        protected void onPlayerElimination(Player player)
        {
            //elimation banner popup or something
        }

        public override void Render(RenderTarget target)
        {
            map.Render(target);
            Text debugHPText = new Text();

            Dictionary<ushort, Entities.EntityBase> readOnly = new Dictionary<ushort, EntityBase>(entities);

            foreach (var entityBase in readOnly.Values)
            {
                debugHPText.Color = new Color(255, 255, 255);
                entityBase.Render(target);
                debugHPText.DisplayedString = "HP: " + entityBase.Health.ToString();
                debugHPText.Position = entityBase.Position;

                target.Draw(debugHPText);
                if(entityBase is Entities.BuildingBase)
                {
                    var buildingCast = (Entities.BuildingBase) entityBase;
                    if(buildingCast.IsProductingUnit)
                    {
                        debugHPText.Color = new Color(255, 255, 0);
                        debugHPText.DisplayedString = buildingCast.UnitBuildCompletePercent.ToString();
                        debugHPText.Position += new Vector2f(0, 50);
                        target.Draw(debugHPText);
                    }
                }
            }

            if(myPlayer != null)
            {
                debugHPText.Scale = new Vector2f(.5f, .5f);
                debugHPText.Position = target.ConvertCoords(new Vector2i(0, 0));
                debugHPText.DisplayedString = "APL: " + myPlayer.Apples.ToString();
                target.Draw(debugHPText);

                debugHPText.Position += new Vector2f(100, 0);
                debugHPText.DisplayedString = "GLU: " + myPlayer.Glue.ToString();
                target.Draw(debugHPText);

                debugHPText.Position += new Vector2f(100, 0);
                debugHPText.DisplayedString = "WOD: " + myPlayer.Wood.ToString();
                target.Draw(debugHPText);

                debugHPText.Position += new Vector2f(100, 0);
                debugHPText.DisplayedString = "SUP: " + myPlayer.UsedSupply.ToString() + "/" + myPlayer.Supply;
                target.Draw(debugHPText);
            }
        }
    }
}
