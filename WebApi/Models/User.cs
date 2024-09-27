namespace WebApi.Models;

public record User
{
    public required string Email
    {
        get;
        set;
    }

    public required string Name
    {
        get;
        set;
    }
}