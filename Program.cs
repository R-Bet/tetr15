namespace tetr15
{
    internal class Program
    {
        static void Main(string[] args)
        {
            tetr15 t = new tetr15();
        }

        public class tetr15
        {
            /// <summary>
            /// Game board: 0, 0 is top left, first 4 lines are invisible.
            /// y = 0, x = 1
            /// </summary>
            private char[,] _board;

            public tetr15()
            {
                _board = new char[10, 24];
                Print();
            }

            private void Print()
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("╔═════════════════════╗");
                Console.ForegroundColor = ConsoleColor.White;

                for (int y = 3; y < _board.GetLength(1); y++)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("║");
                    Console.ForegroundColor = ConsoleColor.White;

                    for (int x = 0; x < _board.GetLength(0); x++)
                    {
                        Console.Write(" ");

                        PrintWithColorByChar(_board[x, y]);

                        if (x == _board.GetLength(0) - 1)
                            Console.Write(" ");
                    }

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("║");
                    Console.ForegroundColor = ConsoleColor.White;
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("╚═════════════════════╝");
                Console.ForegroundColor = ConsoleColor.White;
            }

            private void PrintWithColorByChar(char InputCharacter)
            {
                switch (InputCharacter)
                {
                    default:
                        Console.Write("■");
                        break;
                }
            }
        }
    }
}
