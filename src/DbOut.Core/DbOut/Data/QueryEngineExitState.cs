namespace DbOut.Data;

public enum QueryEngineExitState
{
    Graceful,
    
    ExternallyCancelled,
    
    Faulted
}