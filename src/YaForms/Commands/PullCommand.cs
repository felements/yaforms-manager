namespace YaForms.Commands;

public static class PullCommand
{
    public static Task<int> ExecuteAsync()
    {
        Console.Error.WriteLine("Error: pull command is not implemented yet.");
        return Task.FromResult(1);
    }
}
