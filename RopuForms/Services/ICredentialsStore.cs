using System.Threading.Tasks;

namespace RopuForms.Services
{
    public interface ICredentialsStore
    {
        Task Save(string email, string password);

        Task<(string email, string password)> Load();
    }
}
