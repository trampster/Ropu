namespace Ropu.Shared;

public static class TaskHelpers
{
    public static async Task RunTasksAsync(params Task[] tasks)
    {
        var taskList = tasks.ToList();
        while (taskList.Count != 0)
        {
            var task = await Task.WhenAny(taskList);
            await task;
            taskList.Remove(task);
        }
    }
}