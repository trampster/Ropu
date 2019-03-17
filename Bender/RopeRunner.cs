using System;
using System.Diagnostics;

namespace Ropu.Bender
{
    public class RopeRunner
    {
        public void Run(Rope rope)
        {
            var processStartInfo = new ProcessStartInfo()
            {
                WorkingDirectory = rope.Folder,
                FileName = rope.Command,
                Arguments = rope.Args,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            while(true)   
            {
                Console.WriteLine($"Starting {rope.Name}");
                using(var process = new Process())
                {
                    process.OutputDataReceived += (sender, args) =>
                    {
                        Console.WriteLine($"[{rope.Name}] [{DateTime.Now}] {args.Data}");
                    };
                    process.ErrorDataReceived += (sender, args) =>
                    {
                        Console.Error.WriteLine($"[{rope.Name}] [{DateTime.Now}] [ERROR] {args.Data}");
                    };
                    process.StartInfo = processStartInfo;
                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    process.WaitForExit();
                    Console.WriteLine($"{rope.Name} exited with code {process.ExitCode}, restarting.");
                }
            }
        }
    }
}