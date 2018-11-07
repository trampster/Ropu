namespace Ropu.LoadBalancer
{
    public interface IRegisteredController
    {
        bool IsExpired();
    }
}