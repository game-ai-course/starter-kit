using System;
using System.Linq;
using System.Reflection;

namespace bot
{
    public abstract class BotCommand
    {
        public abstract override string ToString();
    }

    public class BotCommand<TSelf> : BotCommand where TSelf : BotCommand<TSelf>
    {
        private static readonly FieldInfo[] Fields = typeof(TSelf).GetFields(BindingFlags.Instance | BindingFlags.Public);
        private static readonly FieldInfo[] Args = typeof(TSelf)
            .GetConstructors().Single().GetParameters()
            .Select(p => Fields.Single(f => f.Name.Equals(p.Name, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        public string Message;

        public override string ToString()
        {
            var name = GetType().Name.ToUpper();
            var args = Args.Select(p => p.GetValue(this)).Prepend(name);
            if (!string.IsNullOrWhiteSpace(Message))
                args = args.Append(Message);
            return args.StrJoin(" ");
        }
    }
}
