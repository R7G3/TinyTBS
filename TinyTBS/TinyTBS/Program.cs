namespace TinyTBS
{
    class Program
    {
        public static void Main(string[] args)
        {
            using var game = new TinyTBS.GameMain();
            game.Run();
        }
    }
}