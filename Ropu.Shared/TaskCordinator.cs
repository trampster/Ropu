using System.Threading.Tasks;

namespace Ropu.Shared
{
    public static class TaskCordinator
    {

        /// <summary>
        /// Waits for all tasks to complete but throws imediately if one fails
        /// </summary>
        /// <param name="tasks"></param>
        public static async Task WaitAll(params Task[] tasks)
        {
            //this is requried to immediately throw if any task fails            
            var completedTask = await Task.WhenAny(tasks);
            await completedTask;//this will throw if the task complete with an error.

            //make sure all are complete before returning
            foreach(var task in tasks)
            {
                if(!task.IsCanceled) await task;
            }
        }
    }
}