namespace WebApi.Identity;

public class IdentityUser
{
    public string Id
    {
        get;
        set;
    } = "";

    public string Email
    {
        get;
        set;
    } = "";

    public bool EmailConfirmed
    {
        get;
        set;
    }

    public string UserName
    {
        get;
        set;
    } = "";

    public string PasswordHash
    {
        get;
        set;
    } = "";
}