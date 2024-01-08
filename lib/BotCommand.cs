namespace bot;

public record BotCommand
{
    public static readonly string[] messages = new[]
    {
        "Powered by ChatGPT",
        "I'm a bot, I don't understand what you're saying.",
        "I'm a bot, I don't understand what you're saying"
    };
    private static readonly Dictionary<string, Func<object, object>[]> ParamGetters = new();

    private Func<object, object> ParamGetter(ParameterInfo p)
    {
        var fields = GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var field = fields.SingleOrDefault(f => f.Name.Equals(p.Name, StringComparison.OrdinalIgnoreCase));
        if (field != null)
            return o => field.GetValue(o);
        var properties = GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var property = properties.SingleOrDefault(f => f.Name.Equals(p.Name, StringComparison.OrdinalIgnoreCase));
        if (property != null)
            return o => property.GetValue(o);
        return null;
    }

    public string Message;

    public sealed override string ToString()
    {
        var commandName = GetType().Name;
        if (commandName.EndsWith("Command"))
            commandName = commandName.Substring(0, commandName.Length - 7); //"Command".Length

        var commandParams = GetParamGetters(commandName).Select(get => get(this));
        var parts = commandParams.Prepend(commandName.ToUpper());
        if (!string.IsNullOrWhiteSpace(Message))
            parts = parts.Append(Message);
        return parts.StrJoin(" ", FormatCommandPart);
    }

    private string FormatCommandPart(object arg) =>
        arg switch
        {
            bool b => b ? "1" : "0",
            _ => arg.ToString()
        };

    private Func<object, object>[] GetParamGetters(string commandName)
    {
        if (ParamGetters.TryGetValue(commandName, out var getters))
            return getters;
        return ParamGetters[commandName] = 
            GetType()
                .GetConstructors().Single().GetParameters()
                .Select(ParamGetter)
                .Where(g => g != null)
                .ToArray();
    }
}