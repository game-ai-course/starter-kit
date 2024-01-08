namespace bot;

public record Move(V Destination) : BotCommand;
public record Wait : BotCommand;
public record Fire(V Target, int Power) : BotCommand;