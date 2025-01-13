using System.Diagnostics;
using System.Runtime.CompilerServices;

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

            private Piece[,] _nextPieceBoard;
            private Piece[,] _2ndNextPieceBoard;
            private Piece[,] _3rdNextPieceBoard;

            private Position[] _heldShape;

            private Piece _heldPiece;

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

            private bool _wasHeldSwapped;

            private int _score;

            public tetr15()
            {
                Console.CursorVisible = false;

                _board = new Piece[10, 24];
                _nextPieceBoard = new Piece[4, 2];
                _2ndNextPieceBoard = new Piece[4, 2];
                _3rdNextPieceBoard = new Piece[4, 2];
                _heldShape = Array.Empty<Position>();
                _player = Array.Empty<Position>();
                _bag = new Queue<Piece>();
                _isGamerOver = false;
                _gravityStopwatch = new Stopwatch();
                _delay = 500;
                _graceTicks = 2;
                _rng = new Random();
                _wasHeldSwapped = false;
                _score = 0;

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
                        CheckAndRotateCurrentPiece();
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
                        CheckIfSwappedAndHold();
                        break;
                }
            }

            private void CheckIfSwappedAndHold()
            {
                if (!_wasHeldSwapped)
                {
                    Position[] HeldPieceShape = GetCopy(PiecesShapes[_currentPiece]);
                    if (_heldShape.Length == 0)
                    {
                        _heldShape = GetMoved(HeldPieceShape, -3, -1);
                        _heldPiece = _currentPiece;
                        ResetPlayer();
                    }
                    else
                    {
                        _heldShape = GetMoved(HeldPieceShape, -3, -1);
                        _player = GetCopy(PiecesShapes[_heldPiece]);

                        Piece TempHeldPiece = _heldPiece;
                        _heldPiece = _currentPiece;
                        _currentPiece = TempHeldPiece;
                    }

                    _wasHeldSwapped = true;
                }

            }

            private void Tick()
            {
                if (_bag.Count() <= 3) FillBag();
                if (_player.Length != 4) ResetPlayer();

                if (_gravityStopwatch.ElapsedMilliseconds > _delay)
                {
                    DropTick();
                    _gravityStopwatch.Restart();
                }

                if (_bag.Count() == 1) FillBag();
                if (_player.Length != 4) ResetPlayer();

                int LinesCleared = CheckLineClear();

                AwardScore(LinesCleared);

                Console.SetCursorPosition(0, 0);
                Print();
            }

            private void AwardScore(int LinesCleared)
            {
                switch (LinesCleared)
                {
                    case 0:
                        return;
                    case 1:
                        _score += 100;
                        break;
                    case 2:
                        _score += 300;
                        break;
                    case 3:
                        _score += 500;
                        break;
                    case 4:
                        _score += 1000;
                        break;
                }
            }

            //Checks if any lines have been cleared and deletes them using DeleteLine
            //Returns the amount of lines cleared.
            private int CheckLineClear()
            {

                int Lines = 0;
                for (int y = _board.GetLength(1) - 1; y >= 0; y--)
                {
                    bool LineFull = true;
                    for (int x = 0; x < _board.GetLength(0); x++)
                    {
                        if (_board[x, y] == Piece.non || _board[x, y] == Piece.ghost)
                        {
                            LineFull = false;
                            break;
                        }
                    }

                    if (LineFull)
                    {
                        DeleteLine(y);
                        Lines++;
                    }
                }
                return Lines;
            }

            //Deletes a line
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

            //Waits 2 ticks before a player's piece becomes part of the board.
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

            //Makes the player a part of the board.
            //Clears the player so the next tick knows to refill it.
            private void ClearPlayer()
            {
                for (int i = 0; i < _player.Length; i++)
                {
                    _board[_player[i].x, _player[i].y] = _currentPiece;
                }

                _player = Array.Empty<Position>();
                _wasHeldSwapped = false;
            }

            //Fills the queue of next pieces.
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

            //Gets a new shape for the player from the bag.
            private void ResetPlayer()
            {
                _player = new Position[4];

                _currentPiece = _bag.Dequeue();
                Position[] tempPlayer = GetCopy(PiecesShapes[_currentPiece]);

                for (int i = 0; i < tempPlayer.Length; i++)
                {
                    _player[i] = tempPlayer[i].Clone();
                }

                TransferNextPiecesWindows();

                Piece ThirdNextPieceType = _bag.ElementAt(2);
                Position[] ThirdNextPieceShape = GetCopy(PiecesShapes[ThirdNextPieceType]);

                for (int i = 0; i < ThirdNextPieceShape.Length; i++)
                {
                    _3rdNextPieceBoard[ThirdNextPieceShape[i].x - 3, ThirdNextPieceShape[i].y - 1] = ThirdNextPieceType;
                }

                Piece SecondNextPieceType = _bag.ElementAt(1);
                Position[] SecondNextPieceShape = GetCopy(PiecesShapes[SecondNextPieceType]);

                for (int i = 0; i < SecondNextPieceShape.Length; i++)
                {
                    _2ndNextPieceBoard[SecondNextPieceShape[i].x - 3, SecondNextPieceShape[i].y - 1] = SecondNextPieceType;
                }

                Piece NextPieceType = _bag.Peek();
                Position[] NextPieceShape = GetCopy(PiecesShapes[NextPieceType]);

                for (int i = 0; i < NextPieceShape.Length; i++)
                {
                    _nextPieceBoard[NextPieceShape[i].x - 3, NextPieceShape[i].y - 1] = NextPieceType;
                }

            }

            private void TransferNextPiecesWindows()
            {
                _nextPieceBoard = GetCopy(_2ndNextPieceBoard);
                _2ndNextPieceBoard = GetCopy(_3rdNextPieceBoard);

                for (int x = 0; x < _3rdNextPieceBoard.GetLength(0); x++)
                {
                    for (int y = 0; y < _3rdNextPieceBoard.GetLength(1); y++)
                    {
                        _3rdNextPieceBoard[x, y] = Piece.non;
                    }
                }
            }

            private void CheckAndRotateCurrentPiece()
            {
                if (_currentPiece == Piece.O)
                    return;

                Position[] Rotated = GetRotated(_player, 1);
                if (IsValidAndNotPlayer(Rotated))
                {
                    _player = Rotated;
                    if (_currentPiece == Piece.I)
                        Swap(_player, 0, 1);
                    _graceTicks = 1;
                }
            }

            private void HardDropPlayer()
            {
                _player = GetHardDroppedPlayer();
                ClearPlayer();
            }

            private Position[] GetHardDroppedPlayer()
            {
                int yOffset = 1;
                Position[] Current = GetMoved(_player, 0, yOffset);
                Position[] Previous = GetCopy(_player);
                while (IsValidAndNotPlayer(Current))
                {
                    yOffset++;
                    Previous = GetCopy(Current);
                    Current = GetMoved(_player, 0, yOffset);
                }
                return Previous;
            }

            private void CheckAndMovePlayer(int x, int y)
            {
                if (IsValidAndNotPlayer(GetMoved(_player, x, y)))
                {
                    _graceTicks = 1;
                    _player = GetMoved(_player, x, y);
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

                    if (_board[x, y] != Piece.non && _board[x, y] != Piece.ghost)
                        if (!_player.Contains(Positions[i]))
                            return false;
                }
                return true;
            }

            //Rotation by middle point where middle is the first element from input array.
            public Position[] GetRotated(Position[] Input, int Rotations)
            {
                Position[] output = GetCopy(Input);

                Position RotationPivot = output[0].Clone();

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

            private Piece[,] _printBoard;
            private Piece[,] _heldBoard;
            private void Print()
            {
                _printBoard = GetCopy(_board);
                _heldBoard = new Piece[4, 3];
                Position[] HardDropped = GetHardDroppedPlayer();

                for (int i = 0; i < HardDropped.Length; i++)
                {
                    _printBoard[HardDropped[i].x, HardDropped[i].y] = Piece.ghost;
                }

                for (int i = 0; i < _player.Length; i++)
                {
                    _printBoard[_player[i].x, _player[i].y] = _currentPiece;
                }

                for (int i = 0; i < _heldShape.Length; i++)
                {
                    if (_wasHeldSwapped)
                        _heldBoard[_heldShape[i].x, _heldShape[i].y] = Piece.ghost;
                    else
                        _heldBoard[_heldShape[i].x, _heldShape[i].y] = _heldPiece;
                }


                WriteLineGreen("╔══════════════╦════════════════╗");

                WriteLineGreen($"║Level:{GetIntCompletedTo8Digit(_score)}║ Score:{GetIntCompletedTo8Digit(_score)} ║");

                WriteLineGreen("╠══════════════╩════════════════╣");

                for (int y = 3; y < _printBoard.GetLength(1); y++)
                {
                    WriteGreen("║");

                    for (int x = 0; x < _printBoard.GetLength(0); x++)
                    {
                        Console.Write(" ");

                        PrintWithColorByPiece(_printBoard[x, y]);

                        if (x == _printBoard.GetLength(0) - 1)
                            Console.Write(" ");
                    }

                    int SideMenuBase = 3;

                    if (y == SideMenuBase + 10)
                        WriteGreen("╠");
                    else
                        WriteGreen("║");

                    if (y == SideMenuBase + 1)
                    {
                        Console.Write("Next:");
                        WriteLineGreen("    ║");
                    }
                    else if (y > SideMenuBase + 1 && y < SideMenuBase + 4)
                    {
                        for (int x = 0; x < _nextPieceBoard.GetLength(0); x++)
                        {
                            Console.Write(" ");

                            PrintWithColorByPiece(_nextPieceBoard[x, y - (SideMenuBase + 2)]);

                            if (x == _nextPieceBoard.GetLength(0) - 1)
                                Console.Write(" ");
                        }
                        WriteLineGreen("║ ");
                    }
                    else if (y > SideMenuBase + 4 && y < SideMenuBase + 7)
                    {
                        for (int x = 0; x < _2ndNextPieceBoard.GetLength(0); x++)
                        {
                            Console.Write(" ");

                            PrintWithColorByPiece(_2ndNextPieceBoard[x, y - (SideMenuBase + 5)]);

                            if (x == _2ndNextPieceBoard.GetLength(0) - 1)
                                Console.Write(" ");
                        }
                        WriteLineGreen("║ ");
                    }
                    else if (y > SideMenuBase + 7 && y < SideMenuBase + 10)
                    {
                        for (int x = 0; x < _3rdNextPieceBoard.GetLength(0); x++)
                        {
                            Console.Write(" ");

                            PrintWithColorByPiece(_3rdNextPieceBoard[x, y - (SideMenuBase + 8)]);

                            if (x == _3rdNextPieceBoard.GetLength(0) - 1)
                                Console.Write(" ");
                        }
                        WriteLineGreen("║ ");
                    }
                    else if(y == SideMenuBase + 10)
                    {
                        WriteLineGreen("═════════╣");
                    }
                    else if (y == SideMenuBase + 11)
                    {
                        Console.Write("Held:");
                        WriteLineGreen("    ║");
                    }
                    else if (y > SideMenuBase + 11 && y < SideMenuBase + 14)
                    {
                        for (int x = 0; x < _heldBoard.GetLength(0); x++)
                        {
                            Console.Write(" ");

                            PrintWithColorByPiece(_heldBoard[x, y - (SideMenuBase + 12)]);

                            if (x == _heldBoard.GetLength(0) - 1)
                                Console.Write(" ");
                        }
                        WriteLineGreen("║ ");
                    }
                    else
                        WriteLineGreen("         ║");
                }

                WriteLineGreen("╚═════════════════════╩═════════╝");

            }

            private string GetIntCompletedTo8Digit(int input)
            {
                string str = input.ToString();

                if (str.Length > 8) throw new Exception("Error! Number is longer than 8 digits.");

                return str + new string(' ', 8 - str.Length);
            }


            private void WriteGreen(string str)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write(str);
                Console.ForegroundColor = ConsoleColor.White;
            }

            private void WriteLineGreen(string str)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(str);
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
                    case Piece.ghost:
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.Write("■");
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                    default:
                        Console.Write(" ");
                        break;
                }
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

            private Position[] GetCopy(Position[] InputArray)
            {
                Position[] output = new Position[InputArray.Length];
                for (int i = 0; i < InputArray.Length; i++)
                {
                    output[i] = InputArray[i].Clone();
                }
                return output;
            }

            private void Swap<T>(T[] arr, int a, int b)
            {
                T temp = arr[a];
                arr[a] = arr[b];
                arr[b] = temp;
            }
        }

        public class Position
        {
            public int x, y;
            public Position(int x, int y)
            {
                this.x = x; this.y = y;
            }

            public Position Clone()
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
            T = 7,
            ghost = 8
        }
    }
}
