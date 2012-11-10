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
using SettlersEngine;
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
        protected Dictionary<Keyboard.Key, List<EntityBase>> controlGroups;

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
        private const Keyboard.Key GlueFactoryHotkey = Keyboard.Key.A;

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

        private SpatialAStar<PathNode, object> pathFinding; 
 

        //Drawables
        private Sprite bottomHUDGUI;
        private Sprite alertHUDAlert;
        private Sprite alertHUDUnitCreated;
        private Sprite alertHUDBuildingCreated;
        private Sprite avatarWorker;
        private Sprite hudBoxUnit;
        private Sprite hudBoxBuilding;
        private Sprite hudControlBox;
        private Sprite viewBounds;  //Darkness around edges


        protected Vector2f CameraPosition;
        private const float CAMERAMOVESPEED = .5f;

        private Vector2f _mousePosition;

        public StandardMelee(InputHandler handler)
        {
            _mousePosition = new Vector2f(500, 500);

            CurrentStatus = StatusState.WaitingForPlayers;

            workerUIState = WorkerUIStateTypes.Normal;
            buildingToPlace = 0;

            InputHandler = handler;
            myId = 0;
            idSet = false;
            map = new TileMap();

            selectedUnits = null;
            controlGroups = new Dictionary<Keyboard.Key, List<EntityBase>>();

            for (int i = 27; i <= 35; i++)
            {
                controlGroups.Add((Keyboard.Key) i, new List<EntityBase>());
            }

                controlBoxP1 = new Vector2f(0, 0);
            controlBoxP2 = new Vector2f(0,0);
            selectedAttackMove = false;
            releaseSelect = false;

            CameraPosition = new Vector2f(0, 0);

            //Load Sprites
            bottomHUDGUI = new Sprite(ExternalResources.GTexture("Resources/Sprites/HUD/BottomGUI.png"));
            alertHUDAlert = new Sprite(ExternalResources.GTexture("Resources/Sprites/HUD/Alert_Alert.png"));
            alertHUDUnitCreated = new Sprite(ExternalResources.GTexture("Resources/Sprites/HUD/Alert_UnitCreated.png"));
            alertHUDBuildingCreated = new Sprite(ExternalResources.GTexture("Resources/Sprites/HUD/Alert_BuildingFinished.png"));

            avatarWorker = new Sprite(ExternalResources.GTexture("Resources/Sprites/HUD/HUD_AVATAR_WORKER.png"));

            hudBoxUnit = new Sprite(ExternalResources.GTexture("Resources/Sprites/HUD/HUD_BOX_Unit.png"));
            hudBoxBuilding = new Sprite(ExternalResources.GTexture("Resources/Sprites/HUD/HUD_BOX_Building.png"));

            hudControlBox = new Sprite(ExternalResources.GTexture("Resources/Sprites/HUD/ControlGroupBox.png"));
            hudControlBox.Origin = new Vector2f(hudControlBox.TextureRect.Width/2, 0);

            viewBounds = new Sprite(ExternalResources.GTexture("Resources/Sprites/Hud/ViewBounds.png"));
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
                        CurrentStatus = (StatusState)status;
                        pathFinding = new SpatialAStar<PathNode, object>(map.GetPathNodeMap());
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

        private void sendMoveCommand(float x, float y, bool reset = true)
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
                    InputHandler.SendMoveInput(x, y, idList.ToArray(), reset, selectedAttackMove);
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
                    }
                }
                else if (!selectedAttackMove)
                {
                    controlBoxP1 = convertedPos;
                    controlBoxP2 = controlBoxP1;
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
                //SetControlUnits(new FloatRect(controlBoxP1.X, controlBoxP1.Y, controlBoxP2.X - controlBoxP1.X,
                                              //controlBoxP2.Y - controlBoxP1.Y));
                SetControlUnits(CorrectedRect(controlBoxP1, controlBoxP2));
                releaseSelect = false;
            }
            if(button == Mouse.Button.Right)
            {

            }
        }

        public override void KeyPress(KeyEventArgs keyEvent)
        {
            if((byte)keyEvent.Code >= 27 && (byte)keyEvent.Code <= 35)
            {
                if (keyEvent.Shift)
                {
                    if (selectedUnits != null)
                    {
                        var copy = new List<EntityBase>(controlGroups[keyEvent.Code]);

                        foreach (var selectedUnit in selectedUnits)
                        {
                            if(copy.Contains(selectedUnit) == false)
                            {
                                controlGroups[keyEvent.Code].Add(selectedUnit);
                            }
                        }
                    }
                }
                else if (keyEvent.Control)
                {
                    if (selectedUnits != null)
                        controlGroups[keyEvent.Code] = new List<EntityBase>(selectedUnits);
                }
                else if (controlGroups.ContainsKey(keyEvent.Code))
                {
                    if(controlGroups[keyEvent.Code].Count > 0)
                    selectedUnits = controlGroups[keyEvent.Code].ToArray();
                }
            }

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

                    if(keyEvent.Code == GlueFactoryHotkey)
                    {
                        workerUIState = WorkerUIStateTypes.ReadyToPlace;
                        buildingToPlace = (byte) WorkerSpellIds.BuildGlueFactory;
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

            if(workerUIState == WorkerUIStateTypes.Normal && keyEvent.Code == AttackHotkey)
            {
                selectedAttackMove = true;
            }

        }

        public override void MouseMoved(int x, int y)
        {
            base.MouseMoved(x, y);
            if (releaseSelect)
            {
                Vector2f convertedPos = Program.window.ConvertCoords(new Vector2i(x, y));
                controlBoxP2 = convertedPos;
            }
            _mousePosition = new Vector2f(x, y);
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

        static FloatRect CorrectedRect(Vector2f point1, Vector2f point2)
        {
            float left = point1.X;
            if (point2.X < left) 
                left = point2.X;
            float top = point1.Y;
            if(point2.Y < top)
                top = point2.Y;

            float right = point2.X;
            if (point1.X > right)
                right = point1.X;


            float bottom = point2.Y;
            if (point1.Y > bottom)
                bottom = point1.Y;

            return new FloatRect(left, top, right - left, bottom - top);
        }

        public override void Update(float ms)
        {
            
            FilterSelectedUnits(ref selectedUnits);
            foreach (var controlGroup in controlGroups.Values)
            {
                FilterSelectedUnits(controlGroup);
            }

            UpdateAlerts(ms);
            if(CurrentStatus == StatusState.InProgress || CurrentStatus == StatusState.Completed)
            {
                var readOnly = new Dictionary<ushort, EntityBase>(entities);
                foreach (var entityBase in readOnly.Values)
                {
                    entityBase.Update(ms);
                    if (entityBase.Health >= entityBase.MaxHealth)
                        entityBase.Health = entityBase.MaxHealth;
                    if (entityBase.Energy >= entityBase.MaxEnergy)
                        entityBase.Energy = entityBase.MaxEnergy;
                }
                SpaceUnits(ms);
            }

            const float CAMERA_LEFT_BOUNDS = 20;
            const float CAMERA_TOP_BOUNDS = 20;

            if(_mousePosition.X <= CAMERA_LEFT_BOUNDS)
            {
                CameraPosition.X -= CAMERAMOVESPEED*ms;
            }
            if (_mousePosition.Y <= CAMERA_TOP_BOUNDS)
            {
                CameraPosition.Y -= CAMERAMOVESPEED * ms;
            }

            if (_mousePosition.X >= Program.window.Size.X - CAMERA_LEFT_BOUNDS)
            {
                CameraPosition.X += CAMERAMOVESPEED * ms;
            }

            if (_mousePosition.Y >= Program.window.Size.Y - CAMERA_TOP_BOUNDS)
            {
                CameraPosition.Y += CAMERAMOVESPEED * ms;
            }

        }

        protected void FilterSelectedUnits(ref EntityBase[] listArray)
        {
            if (listArray != null)
            {
                List<EntityBase> list = new List<EntityBase>(listArray);
                foreach (var entityBase in listArray)
                {
                    if (entities.ContainsValue(entityBase) == false)
                        list.Remove(entityBase);

                }

                listArray = list.ToArray();
            }
        }

        protected void FilterSelectedUnits(List<EntityBase> listArray)
        {
            if (listArray != null)
            {
                List<EntityBase> list = new List<EntityBase>(listArray);
                foreach (var entityBase in listArray)
                {
                    if (entities.ContainsValue(entityBase) == false)
                        list.Remove(entityBase);
                }

                listArray.Clear();
                for (int i = 0; i < list.Count; i++)
                {
                    listArray.Add(list[i]);
                }
            }
        }

        public override PathFindReturn PathFindNodes(float sx, float sy, float x, float y)
        {
            sx /= map.TileSize.X;
            x /= map.TileSize.X;
            sy /= map.TileSize.Y;
            y /= map.TileSize.Y;

            if (sx < 0 || sy < 0 || x < 0 || y < 0 || sx >= map.Tiles.GetLength(0) || x >= map.Tiles.GetLength(0) || sy >= map.Tiles.GetLength(1) || y >= map.Tiles.GetLength(1))
            {
                return new PathFindReturn()
                {
                    List = null,
                    MapSize = map.TileSize,
                };
            }
            var path =
                pathFinding.Search(
                    new System.Drawing.Point((int)sx,
                              (int)sy),
                    new System.Drawing.Point((int)x,
                              (int)y), null);


            return new PathFindReturn()
            {
                List = path,
                MapSize = map.TileSize,
            };
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
            View view = target.GetView();
            view.Center = CameraPosition;
            target.SetView(view);

            map.Render(target);


            const float selectCircleRadius = 20;
            CircleShape selectedCircle = new CircleShape(selectCircleRadius);
            selectedCircle.OutlineColor = new Color(100, 255, 100, 200);
            selectedCircle.OutlineThickness = 5;
            selectedCircle.FillColor = new Color(0, 0, 0, 0);
            selectedCircle.Origin = new Vector2f(selectCircleRadius, selectCircleRadius);
            if(selectedUnits != null)
            {
                foreach (var entityBase in selectedUnits)
                {
                    selectedCircle.Position = entityBase.Position;
                    target.Draw(selectedCircle);
                }
            }


            Text debugHPText = new Text();
            debugHPText.Scale = new Vector2f(.6f, .6f);
            debugHPText.Color = new Color(255, 255, 255, 100);
            var readOnly = new Dictionary<ushort, EntityBase>(entities);

            foreach (var entityBase in readOnly.Values)
            {
                debugHPText.Color = new Color(255, 255, 255,200);
                entityBase.Render(target);
                debugHPText.DisplayedString = "HP: " + entityBase.Health.ToString();
                debugHPText.Origin = new Vector2f(debugHPText.GetGlobalBounds().Width/2,
                                                  debugHPText.GetGlobalBounds().Height);
                debugHPText.Position = entityBase.Position;

                target.Draw(debugHPText);
                if(entityBase is Entities.BuildingBase)
                {
                    var buildingCast = (Entities.BuildingBase) entityBase;
                    if(buildingCast.IsProductingUnit)
                    {
                        debugHPText.Color = new Color(255, 255, 0,100);
                        debugHPText.DisplayedString = buildingCast.UnitBuildCompletePercent.ToString();
                        debugHPText.Position += new Vector2f(0, 50);
                        target.Draw(debugHPText);
                    }
                }
            }

            if(releaseSelect)
            {
                var rect = new FloatRect(controlBoxP1.X, controlBoxP1.Y, controlBoxP2.X - controlBoxP1.X,
                                         controlBoxP2.Y - controlBoxP1.Y);
                RectangleShape rectangle = new RectangleShape(new Vector2f(rect.Width, rect.Height));
                rectangle.Position = new Vector2f(rect.Left, rect.Top);
                rectangle.FillColor = new Color(100, 200, 100, 100);
                target.Draw(rectangle);
            }

            //Draw HUD

            //Draw Alerts
            const int ALERTXOFFSET = 10;
            const int ALERTYMULTIPLE = 60;
            const int ALERTYOFFSET = 20;
            for(int i = 0; i < alerts.Count; i++)
            {
                var alert = alerts[i];

                Sprite sprite = null;
                switch (alert.Type)
                {
                    case HUDAlert.AlertTypes.CreatedUnit:
                        sprite = alertHUDUnitCreated;
                        break;
                    case HUDAlert.AlertTypes.UnitUnderAttack:
                        sprite = alertHUDAlert;
                        break;
                    case HUDAlert.AlertTypes.UnitCreated:
                        sprite = alertHUDUnitCreated;
                        break;
                    case HUDAlert.AlertTypes.BuildingCompleted:
                        sprite = alertHUDBuildingCreated;
                        break;
                    default:
                        break;
                }

                if(sprite != null)
                {
                    sprite.Position = target.ConvertCoords(new Vector2i(ALERTXOFFSET, ALERTYOFFSET + ALERTYMULTIPLE*i));
                    sprite.Color = new Color(255, 255, 255, (byte)alert.Alpha);
                    target.Draw(sprite);
                }
            }



            //Draw bottom GUI
            bottomHUDGUI.Position = target.ConvertCoords(new Vector2i(0, 0));
            target.Draw(bottomHUDGUI);

            //Draw unit stats HUD

            if (selectedUnits != null)
            {
                if (selectedUnits.Length == 1)
                {
                    var hpText = new Text(selectedUnits[0].Health + "/" + selectedUnits[0].MaxHealth);
                    hpText.Position = target.ConvertCoords(new Vector2i(500, (int)target.Size.Y - 50));
                    hpText.Scale = new Vector2f(.7f, .7f);
                    target.Draw(hpText);

                    var priorUnit = prioritySelectedUnit();

                    Sprite unitAvatar = avatarWorker;

                    switch (priorUnit.Type)
                    {
                        case Entity.EntityType.Unit:
                            break;
                        case Entity.EntityType.Building:
                            break;
                        default:
                        case Entity.EntityType.Worker:
                            unitAvatar = avatarWorker;
                            break;
                        case Entity.EntityType.Resources:
                            break;
                        case Entity.EntityType.HomeBuilding:
                            break;
                        case Entity.EntityType.SupplyBuilding:
                            break;

                    }

                    if(unitAvatar != null)
                    {
                        unitAvatar.Position = hpText.Position - new Vector2f(50, 175);
                        target.Draw(unitAvatar);
                    }
                }
                else if (selectedUnits.Length > 1)
                {
                    const byte MAXROWCOUNT = 8;

                    byte XCount = 0, YCount = 0, IterateCount = 0;
                    for(int i =0 ; i < selectedUnits.Length; i++)
                    {
                        Sprite boxSprite = hudBoxUnit;
                        if(selectedUnits[i] is BuildingBase)
                        {
                            boxSprite = hudBoxBuilding;
                        }
                        else if(selectedUnits[i] is UnitBase)
                        {
                            boxSprite = hudBoxUnit;
                        }

                        if(boxSprite != null)
                        {
                            const float XOFFSET = 300;
                            const float YOFFSET = 545;
                            const float XMULTIPLY = 55;
                            const float YMULTIPLY = 55;

                            boxSprite.Position = new Vector2f(XOFFSET + (XCount*XMULTIPLY), YOFFSET + (YCount*YMULTIPLY));
                            boxSprite.Position =
                                target.ConvertCoords(new Vector2i((int)boxSprite.Position.X, (int)boxSprite.Position.Y));
                            target.Draw(boxSprite);

                            
                        }

                        XCount++;
                        if(XCount == MAXROWCOUNT)
                        {
                            XCount = 0;
                            YCount++;
                        }
                    }
                }
            }

            //Draw Control Groups
            const int CONTROLBOXXOFFSET = 310;
            const int CONTROLBOXYOFFSET = 505;
            const int CONTROLBOXXMULTIPLY = 55;

            var counter = 0;
            var controlNumberText = new Text("0");

            foreach (var controlGroup in controlGroups.Values)
            {
                if (controlGroup != null && controlGroup.Count > 0)
                {
                    controlNumberText.DisplayedString = (counter + 1).ToString();
                    controlNumberText.Scale = new Vector2f(.4f, .4f);
                    controlNumberText.Origin = new Vector2f((controlNumberText.GetGlobalBounds().Width/2),
                                                            (controlNumberText.GetGlobalBounds().Height/2));

                    hudControlBox.Position =
                        target.ConvertCoords(new Vector2i(CONTROLBOXXOFFSET + CONTROLBOXXMULTIPLY*counter,
                                                          CONTROLBOXYOFFSET));
                    controlNumberText.Position = hudControlBox.Position + new Vector2f(0, 24);
                    target.Draw(hudControlBox);
                    target.Draw(controlNumberText);

                    controlNumberText.DisplayedString = controlGroup.Count.ToString();
                    controlNumberText.Position = hudControlBox.Position + new Vector2f(0, 5);
                    controlNumberText.Scale = new Vector2f(.7f, .7f);
                    target.Draw(controlNumberText);
                }
                counter++;
            }


            if(myPlayer != null)
            {
                debugHPText.Origin = new Vector2f(0, 0);
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

            if(CurrentStatus == StatusState.WaitingForPlayers)
            {
                var shape = new RectangleShape(new Vector2f(target.Size.X, target.Size.Y));
                shape.Position = target.ConvertCoords(new Vector2i(0, 0));
                shape.FillColor = new Color(0, 0, 0, 200);
                target.Draw(shape);

                var text = new Text("Waiting for Players...");
                text.Position = target.ConvertCoords(new Vector2i((int)target.Size.X/2, (int)target.Size.Y/2));
                text.Origin = new Vector2f(text.GetGlobalBounds().Width/2, text.GetGlobalBounds().Height/2);
                text.Color = new Color(255, 255, 255, 100);
                target.Draw(text);
            }

            if(CurrentStatus == StatusState.Completed)
            {
                var text = new Text("Game Completed...");
                text.Position = target.ConvertCoords(new Vector2i((int)target.Size.X / 2, 10));
                text.Origin = new Vector2f(text.GetGlobalBounds().Width / 2, 0);
                text.Color = new Color(255, 255, 255, 100);
                target.Draw(text);
            }

            viewBounds.Position = target.ConvertCoords(new Vector2i(0, 0));
            target.Draw(viewBounds);
        }
    }
}
