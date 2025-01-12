using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Reflection.Metadata;

namespace tetr15
{
    internal class Program
    {
        static void Main(string[] args)
        {
            tetr15 t = new tetr15();
            int score = t.StartGame();
        }

        public class tetr15
        {
            /// <summary>
            /// Game board: 0, 0 is top left, first 4 lines are invisible.
            /// x = 0, y = 1
            /// </summary>
            private Piece[,] _board;

            private Position[] _player;

            /// <summary>
            /// Queue of pieces to use
            /// </summary>
            private Queue<Piece> _bag;

            private Piece _currentPiece;

            private bool _isGamerOver;

            private Dictionary<Piece, Position[]> PiecesShapes;

            private Stopwatch _gravityStopwatch;

            private int _delay;

            private int _graceTicks;

            private Random _rng;

            public tetr15()
            {
                Console.CursorVisible = false;

                _board = new Piece[10, 24];
                _player = new Position[1];
                _bag = new Queue<Piece>();
                _isGamerOver = false;
                _gravityStopwatch = new Stopwatch();
                _delay = 500;
                _graceTicks = 2;
                _rng = new Random();

                PiecesShapes = new Dictionary<Piece, Position[]>
                    {
                        {Piece.O, new Position[] { (4, 1), (5, 1), (4, 2), (5, 2) } },
                        {Piece.I, new Position[] { (4, 2), (5, 2), (3, 2), (6, 2) } },
                        {Piece.S, new Position[] { (4, 1), (5, 1), (4, 2), (3, 2) } },
                        {Piece.Z, new Position[] { (4, 1), (3, 1), (4, 2), (5, 2) } },
                        {Piece.L, new Position[] { (4, 2), (3, 2), (5, 2), (5, 1) } },
                        {Piece.J, new Position[] { (4, 2), (3, 2), (5, 2), (3, 1) } },
                        {Piece.T, new Position[] { (4, 2), (4, 1), (3, 2), (5, 2) } }
                    };

                for (int x = 0; x < _board.GetLength(0); x++)
                {
                    for (int y = 0; y < _board.GetLength(1); y++)
                    {
                        _board[x, y] = Piece.non;
                    }
                }
            }

            public int StartGame()
            {
                Task.Factory.StartNew(() =>
                {
                    while (!_isGamerOver)
                    {
                        InputTick();
                    }
                });

                _gravityStopwatch.Start();
                while (!_isGamerOver)
                {
                    Tick();
                }

                return 0;
            }

            private void InputTick()
            {
                while (_player.Length < 4)
                    Thread.Sleep(1);
                ConsoleKey CurrentInput = Console.ReadKey(true).Key;
                switch (CurrentInput)
                {
                    case ConsoleKey.W: //Rotate
                    case ConsoleKey.UpArrow:
                        RotateCurrentPiece();
                        break;

                    case ConsoleKey.A: //Left
                    case ConsoleKey.LeftArrow:
                        CheckAndMovePlayer(-1, 0);
                        break;

                    case ConsoleKey.S: //Down
                    case ConsoleKey.DownArrow:
                        DropTick();
                        break;

                    case ConsoleKey.D: //Right
                    case ConsoleKey.RightArrow:
                        CheckAndMovePlayer(1, 0);
                        break;

                    case ConsoleKey.Spacebar: //Drop
                        HardDropPlayer();
                        break;

                    case ConsoleKey.C: //Hold

                        break;
                }

            }

            private void RotateCurrentPiece()
            {
                if (_currentPiece == Piece.O)
                    return;

                Position[] Rotated = GetRotated(_player, 1);
                if (IsValidAndNotPlayer(Rotated))
                    _player = Rotated;
                if (_currentPiece == Piece.I)
                    Swap(_player, 0, 1);
            }


            private void HardDropPlayer()
            {
                int yOffset = 1;
                Position[] Current = GetMoved(_player, 0, yOffset);
                Position[] Previous = GetCopy<Position>(_player);
                while (IsValidAndNotPlayer(Current))
                {
                    yOffset++;
                    Previous = GetCopy(Current);
                    Current = GetMoved(_player, 0, yOffset);
                }
                _player = Previous;
                ClearPlayer();
            }

            private void CheckAndMovePlayer(int x, int y)
            {
                if (IsValidAndNotPlayer(GetMoved(_player, x, y)))
                {
                    _graceTicks = 1;
                    _player = GetMoved(_player, x, y);
                }
            }

            private void Tick()
            {
                if (_bag.Count() == 0) FillBag();
                if (_player.Length != 4) ResetPlayer();

                if (_gravityStopwatch.ElapsedMilliseconds > _delay)
                {
                    DropTick();
                    _gravityStopwatch.Restart();
                }

                if (_bag.Count() == 0) FillBag();
                if (_player.Length != 4) ResetPlayer();

                CheckLineClear();

                Console.SetCursorPosition(0, 0);
                Print();
            }

            private void CheckLineClear()
            {
                for (int y = _board.GetLength(1) - 1; y >= 0; y--)
                {
                    bool LineFull = true;
                    for (int x = 0; x < _board.GetLength(0); x++)
                    {
                        if (_board[x, y] == Piece.non)
                        {
                            LineFull = false;
                            break;
                        }
                    }

                    if (LineFull)
                    {
                        DeleteLine(y);
                    }
                }
            }

            private void DeleteLine(int DeletionY)
            {
                for (int y = DeletionY; y > 0; y--)
                {
                    for (int x = 0; x < _board.GetLength(0); x++)
                    {
                        _board[x, y] = _board[x, y - 1];
                    }
                }

                for (int i = 0; i < _player.Length; i++)
                {
                    _player[i].y++;
                }
            }

            private void DropTick()
            {
                if (IsValidAndNotPlayer(GetMoved(_player, 0, 1)))
                {
                    _player = GetMoved(_player, 0, 1);
                }
                else
                {
                    if (_graceTicks <= 0)
                    {
                        ClearPlayer();
                        _graceTicks = 2;
                    }
                    else _graceTicks--;
                }

            }

            private void ClearPlayer()
            {
                for (int i = 0; i < _player.Length; i++)
                {
                    _board[_player[i].x, _player[i].y] = _currentPiece;
                }

                _player = new Position[1];
            }

            private void FillBag()
            {
                List<Piece> PullBag = new List<Piece>();

                for (int i = 1; i <= 7; i++)
                {
                    PullBag.Add((Piece)i);
                }

                for (int i = 0; i < 7; i++)
                {
                    int Pick = _rng.Next(0, PullBag.Count);
                    _bag.Enqueue(PullBag[Pick]);
                    PullBag.RemoveAt(Pick);
                }
            }

            private void ResetPlayer()
            {
                _player = new Position[4];

                _currentPiece = _bag.Dequeue();

                Position[] tempPlayer = PiecesShapes[_currentPiece];

                for (int i = 0; i < tempPlayer.Length; i++)
                {
                    _player[i] = tempPlayer[i].GetDeepCopy();
                }
            }

            private bool IsValidAndNotPlayer(Position[] Positions)
            {
                for (int i = 0; i < Positions.Length; i++)
                {
                    int x = Positions[i].x;
                    int y = Positions[i].y;

                    if (x < 0 || y < 0)
                        return false;

                    if (x >= _board.GetLength(0) || y >= _board.GetLength(1))
                        return false;

                    if (_board[x, y] != Piece.non)
                        if (!_player.Contains(Positions[i]))
                            return false;
                }
                return true;
            }

            private T[,] GetCopy<T>(T[,] InputArray)
            {
                T[,] output = new T[InputArray.GetLength(0), InputArray.GetLength(1)];
                for (int i = 0; i < InputArray.GetLength(0); i++)
                {
                    for (int j = 0; j < InputArray.GetLength(1); j++)
                    {
                        output[i, j] = InputArray[i, j];
                    }
                }
                return output;
            }

            private T[] GetCopy<T>(T[] InputArray)
            {
                T[] output = new T[InputArray.Length];
                for (int i = 0; i < InputArray.Length; i++)
                {

                    output[i] = InputArray[i];

                }
                return output;
            }

            private void Swap<T>(T[] arr, int a, int b)
            {
                T temp = arr[a];
                arr[a] = arr[b];
                arr[b] = temp;
            }


            private Piece[,] _printBoard;
            private void Print()
            {
                _printBoard = GetCopy(_board);

                for (int i = 0; i < _player.Length; i++)
                {
                    int x = _player[i].x;
                    int y = _player[i].y;

                    _printBoard[x, y] = _currentPiece;
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("╔═════════════════════╗");
                Console.ForegroundColor = ConsoleColor.White;

                for (int y = 3; y < _printBoard.GetLength(1); y++)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("║");
                    Console.ForegroundColor = ConsoleColor.White;

                    for (int x = 0; x < _printBoard.GetLength(0); x++)
                    {
                        Console.Write(" ");

                        PrintWithColorByPiece(_printBoard[x, y]);

                        if (x == _printBoard.GetLength(0) - 1)
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

            private void PrintWithColorByPiece(Piece InputCharacter)
            {
                switch (InputCharacter)
                {
                    case Piece.O:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write("■");
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                    case Piece.I:
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.Write("■");
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                    case Piece.S:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write("■");
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                    case Piece.Z:
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write("■");
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                    case Piece.L:
                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        Console.Write("■");
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                    case Piece.J:
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.Write("■");
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                    case Piece.T:
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.Write("■");
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                    default:
                        Console.Write(" ");
                        break;
                }
            }

            /// <summary>
            /// Rotation by middle point (middle = first element from input array)
            /// </summary>
            /// <param name="Input"></param>
            /// <param name="Rotations"></param>
            /// <returns></returns>
            public Position[] GetRotated(Position[] Input, int Rotations)
            {
                Position[] output = GetCopy(Input);

                Position RotationPivot = output[0].GetDeepCopy();

                for (int i = 0; i < output.Length; i++)
                {
                    output[i].x = output[i].x - RotationPivot.x;
                    output[i].y = output[i].y - RotationPivot.y;
                }

                for (int rotation = 0; rotation < Rotations; rotation++)
                    for (int i = 0; i < output.Length; i++)
                    {
                        int TempX = output[i].x;
                        output[i].x = -output[i].y;
                        output[i].y = TempX;
                    }

                for (int i = 0; i < output.Length; i++)
                {
                    output[i].x = output[i].x + RotationPivot.x;
                    output[i].y = output[i].y + RotationPivot.y;
                }

                return output;
            }

            public Position[] GetMoved(Position[] Input, int XOffset, int YOffset)
            {
                Position[] output = new Position[Input.Length];

                for (int i = 0; i < output.Length; i++)
                {
                    output[i] = (Input[i].x + XOffset, Input[i].y + YOffset);
                }

                return output;
            }
        }

        public class Position
        {
            public int x, y;
            public Position(int x, int y)
            {
                this.x = x; this.y = y;
            }

            public Position GetDeepCopy()
            {
                return new Position(x, y);
            }

            public static implicit operator Position(ValueTuple<int, int> tuple)
            {
                return new Position(tuple.Item1, tuple.Item2);
            }
        }

        public enum Piece
        {
            non = 0,
            O = 1,
            I = 2,
            S = 3,
            Z = 4,
            L = 5,
            J = 6,
            T = 7
        }
    }
}
