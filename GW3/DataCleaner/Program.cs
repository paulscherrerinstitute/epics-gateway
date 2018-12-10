using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataCleaner
{
    /// <summary>
    /// Cleanup old data files
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            if(args.Length < 2)
            {
                Console.WriteLine("DataCleaner [path] [days]");
                return;
            }

            var path = args[0];
            var days = int.Parse(args[1]);

            var toTrashDate = DateTime.UtcNow.AddDays(-days);
            var toTrash = "" + toTrashDate.Year + ("" + toTrashDate.Month).PadLeft(2, '0') + ("" + toTrashDate.Day).PadLeft(2, '0');

            foreach (var i in Directory.EnumerateFiles(path).Where(row=>!row.ToLower().EndsWith(".xml") && !row.ToLower().EndsWith(".sessions")))
            {
                var p = Path.GetFileName(i).Split('.');
                if(p[1].CompareTo(toTrash) <= 0)
                {
                    try
                    {
                        File.Delete(i);
                        Console.WriteLine(i);
                    }
                    catch
                    {
                    }
                }
                /*if ((DateTime.UtcNow - File.GetCreationTimeUtc(i)).TotalDays >= (days + 1))
                {
                    try
                    {
                        File.Delete(i);
                        Console.WriteLine(i);
                    }
                    catch
                    {
                    }
                }*/
            }
        }
    }
}
