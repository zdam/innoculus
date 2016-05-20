using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace innoculus
{
    public interface ILogger
    {
        void Log(string message);
    }

    public class Logger : ILogger
    {
        public void Log(string message)
        {
            string path = @"c:\temp\inocculus-logger.txt";
            
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(path, true))
            {
                file.WriteLine(message);
            }
        }
    }
}
