namespace InventoryControl.Models;

public enum TagStatus
{
    PRINTED,
    STANDBY,
    IN_STOCK,
    ALLOCATED,
    RESERVED,
    OUT
}

public enum DoStatus
{
    DRAFT,
    PREPARATION,
    COMPLETED
}

public enum TransactionType
{
    STOCK_IN,
    STOCK_PREPARATION,
    STOCK_OUT,
    STOCK_TAKING_FINALIZE
}

public enum HistoryType
{
    PRINT,
    REGISTER_TAG,
    STOCK_IN,
    STOCK_PREPARATION,
    STOCK_OUT,
    STOCK_ADJUSTMENT
}


public enum TakingStatus
{
    OPEN,
    COMPLETED,
}

public enum TakingAction
{
    SYSTEM,
    FOUND,
    ADD_MANUAL,
    REMOVE
}
public enum ReaderSearchMode
{
    SingleTarget,
    DualTarget
}
public enum ReaderSession
{
    S0,
    S1,
    S2,
    S3
}