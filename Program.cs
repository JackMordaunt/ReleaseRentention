using System;
using System.IO;

namespace ReleaseRetention
{
    class Program
    {
        static void Main(string[] args)
        {
            int n;
            string path;

            if (args.Length < 1)
            {
                Console.Write("specify number of releases to retain\n");
                return;
            }

            n = int.Parse(args[0]);

            if (args.Length < 2)
            {
                path = Path.GetFullPath("data");
            }
            else
            {
                path = Path.GetFullPath(args[1]);
            }

            Console.Write("loading json files from {0}\n", path);
            Console.Write("releases to retain per project {0}\n", n);

            Retainer r = Retainer.Load(path);
            foreach (var retained in r.Retain(n))
            {
                Console.Write("  -> {0}\n", retained);
            }
        }
    }
}