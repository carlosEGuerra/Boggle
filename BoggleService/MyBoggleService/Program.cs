using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyBoggleService
{
    class Program
    {
        /// <summary>
        /// Launches a Boggle Service Server on port 60000
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            //Creates the new Boggle Service on port 60000
            new BoggleService(60000);

            //Output is sent here
            Console.ReadLine();
        }


    }
}
