using DbOut.Continuation;

namespace DbOut.Console;

public class ConsoleInteractiveConfirmation : IInteractiveConfirmation
{
    private readonly CleanConfirmMode _confirmMode;

    public ConsoleInteractiveConfirmation(CleanConfirmMode confirmMode)
    {
        _confirmMode = confirmMode;
    }
    
    public bool Confirm(string prompt)
    {
        if (_confirmMode != CleanConfirmMode.Confirm) return true;

        System.Console.WriteLine(prompt);
        System.Console.Write("Type yes to confirm: ");
        return System.Console.ReadLine() == "yes";
    }
}