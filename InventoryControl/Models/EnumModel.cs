namespace InventoryControl.Models;

public enum TagStatus
{
    PRINTED,
    STANDBY,
    ALLOCATED,
    RESERVED,
    OUT,
    IN_STOCK
}

public enum DoStatus
{
    DRAFT,
    PREPARATION,
    COMPLETED
}

public enum TransactionType
{
    STOCK_PREPARATION,
    STOCK_IN,
    STOCK_OUT,
    STOCK_TAKING_FINALIZE
}

public enum HistoryType
{
    STOCK_IN,
    STOCK_OUT,
    STOCK_PREPARATION,
    STOCK_ADJUSTMENT,
    PRINT,
    REGISTER_TAG
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