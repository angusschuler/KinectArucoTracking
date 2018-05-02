using System;

namespace KinectArucoTracking
{
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            using (var game = new KinectArucoTracking())
                game.Run();
        }
    }
}
