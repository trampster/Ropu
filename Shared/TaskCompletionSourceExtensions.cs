namespace Ropu.Shared;

public static class TaskExtensions
{
    public static async Task<bool> WaitOneAsync(this Task task, TimeSpan timeout)
    {
        var completedTask = await Task.WhenAny(task, Task.Delay(timeout));
        if (completedTask == task)
        {
            await task;
            return true;
        }
        return false;
    }
}
