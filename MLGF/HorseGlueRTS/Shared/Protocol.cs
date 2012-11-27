namespace Shared
{
    public enum Protocol : byte
    {
        Input,
        GameData,
        Chat,
    }

    public enum UnitSignature : byte
    {
        RallyCompleted = 0,
        Attack = 1,
        PopFirstRally = 2,
        ClearRally = 3,
        ChangeMovementAllow = 4,
        StartAttack = 5,
        GrabbingResources = 6,
    }

    public enum UnitTypes : byte
    {
        Default,
        Worker,
        StandardWorker,
        Unicorn,
        //ToDo: Add stalkers and shit
    }

    public enum BuildingTypes : byte
    {
        Base,
        Supply,
        GlueFactory,
    }

    public enum BuildingSignature : byte
    {
        ProductionComplete = 0,
        StartProduction = 1,
        BuildingFinished = 2,
    }

    public enum InputSignature : byte
    {
        Movement,
        SpellCast,
        CreateUnit,
        ChangeUseEntity,
        Surrender,
    }

    public enum ResourceTypes : byte
    {
        Tree,
        Glue,
        Apple,
    }

    public enum UnitBuildIds : byte
    {
        Worker = 0,
    }

    public enum WorkerSpellIds : byte
    {
        BuildHomeBase = 0,
        BuildSupplyBuilding = 1,
        BuildGlueFactory = 2,
    }

    public enum StandardMeleeSignature : byte
    {
        StatusChanged,
        PlayerSurrender,
        PlayerElimination,
    }
}