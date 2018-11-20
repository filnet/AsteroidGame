using System;
using System.Threading;

namespace AsteroidGame
{
#if WINDOWS || XBOX
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (AsteroidGame game = new AsteroidGame())
            {
                Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en");
                game.Run();
            }
        }
    }
#endif
}

