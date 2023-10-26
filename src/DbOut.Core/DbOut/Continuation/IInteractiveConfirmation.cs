namespace DbOut.Continuation;

public interface IInteractiveConfirmation
{
    bool Confirm(string prompt);
}