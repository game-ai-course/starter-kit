namespace bot
{
	public enum CommandType
	{
		WAIT,
		MOVE,
		DIG, 
		REQUEST,
	}

	public record RobotCommand(CommandType Type) : BotCommand;

	public record Wait(string message = "") : RobotCommand(CommandType.WAIT);

	public record Move(V pos, string message = "") : RobotCommand(CommandType.MOVE);

	public record Dig(V pos, string message = "") : RobotCommand(CommandType.DIG);

	public record Request(EntityType item, string message = "") : RobotCommand(CommandType.REQUEST);
}