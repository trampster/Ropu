namespace WebApi.Identity;

public class IdentityRole
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
}