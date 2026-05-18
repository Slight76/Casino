namespace Casino.Server.Models;

public class Player
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid Token { get; set; }
}
