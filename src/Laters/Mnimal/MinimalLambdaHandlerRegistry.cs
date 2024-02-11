namespace Laters.Mnimal;

public class MinimalLambdaHandlerRegistry
{
    readonly Dictionary<Type, object> _commands = new();

    public IEnumerable<Type> Supported => _commands.Keys;
    
    public void Add<T>(MinimalHandler<T> minimalHandler)
    {
        _commands.Add(typeof(T), minimalHandler);
    }
    
    public MinimalHandler<T>? Get<T>()
    {
        return !_commands.TryGetValue(typeof(T), out var commandBoxed) 
            ? default 
            : (MinimalHandler<T>)commandBoxed;
    }
}