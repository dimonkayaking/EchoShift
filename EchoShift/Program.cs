using System;

namespace EchoShift
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                Console.WriteLine("Starting game...");
                using (var game = new Game1())
                {
                    Console.WriteLine("Game instance created, running...");
                    game.Run();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UNHANDLED EXCEPTION: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine("Press Enter to exit.");
                Console.ReadLine();
            }
        }
    }
}