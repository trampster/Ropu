using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Ropu.Bender
{
    class Program
    {
        static int Main(string[] args)
        {
            ThreadPool.GetMaxThreads(out int max, out int completetions);

            if(args.Length == 0)
            {
                Console.Error.WriteLine("You must supply a config file to load.");
                return 1;
            }
            var file = args[0];
            if(!File.Exists(file))
            {
                Console.Error.WriteLine($"Could not load config file {file}");
                return 1;
            }
            var config = File.ReadAllText(file);

            Rope[] ropes = JsonConvert.DeserializeObject<Rope[]>(config);
            var runner = new RopeRunner();

            var tasks = new List<Task>();
            var clientThreads = new List<Thread>();
            foreach(var rope in Expand(ropes))
            {
                if(rope.Name != null && rope.Name.StartsWith("Client"))
                {
                    Console.WriteLine($"Starting {rope.Name}");
                    clientThreads.Add(StartClient(rope));
                }
                else
                {
                    tasks.Add(Task.Run(() => runner.Run(rope)));
                }
            }

            Task.WaitAll(tasks.ToArray());

            return 0;
        }

        static Thread StartClient(Rope rope)
        {
            Thread thread = new Thread(_ => 
            {
                var program = new Ropu.Client.Program();
                program.Run(rope.Args == null ? new string[0] : rope.Args.Split(' ', StringSplitOptions.RemoveEmptyEntries));
            });
            thread.Start();
            return thread;
        }

        static IEnumerable<Rope> Expand(Rope[] ropes)
        {
            foreach(var rope in ropes)
            {
                if(rope.ArgParam == null)
                {
                    yield return rope;
                    continue;
                }
                var param = rope.ArgParam;
                foreach(var index in Enumerable.Range(param.Start, param.End - param.Start))
                {
                    yield return new Rope()
                    {
                        Name = $"{rope.Name}_{index}",
                        Folder = rope.Folder,
                        Command = rope.Command,
                        Args = rope.Args == null ? "" : string.Format(rope.Args, index)
                    };
                }
            }
        }
    }
}
