﻿using System.Diagnostics;

namespace tetr15
{
    internal partial class Program
    {
        public class tetr15
        {
            /// <summary>
            /// Game board: 0, 0 is top left, first 4 lines are invisible.
            /// x = 0, y = 1
            /// </summary>
            private Piece[,] _board;

            private List<Piece[,]> _nextBoards;

            private Position[] _heldShape;

            private Piece _heldPiece;

            private Position[] _player;

            /// <summary>
            /// Queue of pieces to use
            /// </summary>
            private Queue<Piece> _bag;

            private Piece _currentPiece;

            private bool _isGameOver;

            private Dictionary<Piece, Position[]> _pieceShapes;

            private Stopwatch _gravityStopwatch;

            private Stopwatch _scoreCollectionStopwatch;

            private int _delay;

            private int _graceTicks;

            private Random _rng;

            private bool _wasHeldSwapped;

            private double _score;

            private int _scoreCollectionLines;

            private int _linesClearedUpToTen;

            private int _level;

            private int _levelDelayStepSize;

            private int _tetrisesInRow;

            private int _rotationPoint;

            private bool _showGhostPiece;

            private bool _animateLineClear;

            private Dictionary<string, Position[]> _JLTSZRotations;

            private Dictionary<string, Position[]> _IRotations;

            private object _deletionLineLock;

            private List<int> _deletionLines;

            public tetr15(List<(string setting, bool current)> Settings)
            {
                Console.CursorVisible = false;

                _board = new Piece[10, 24];
                _heldShape = Array.Empty<Position>();
                _player = Array.Empty<Position>();
                _bag = new Queue<Piece>();
                _isGameOver = false;
                _gravityStopwatch = new Stopwatch();
                _scoreCollectionStopwatch = new Stopwatch();
                _delay = 500;
                _graceTicks = 2;
                _rng = new Random();
                _wasHeldSwapped = false;
                _score = 0;
                _linesClearedUpToTen = 0;
                _level = 0;
                _levelDelayStepSize = 20;
                _tetrisesInRow = 0;
                _rotationPoint = 0;
                _showGhostPiece = Settings[0].current;
                _animateLineClear = Settings[1].current;
                _deletionLineLock = new object();
                _deletionLines = new List<int>();

                _nextBoards = new List<Piece[,]>();
                for (int i = 0; i < 3; i++)
                    _nextBoards.Add(new Piece[4, 2]);

                _pieceShapes = new Dictionary<Piece, Position[]>
                    {
                        {Piece.O, new Position[] { (4, 1), (5, 1), (4, 2), (5, 2) } },
                        {Piece.I, new Position[] { (4, 2), (5, 2), (3, 2), (6, 2) } },
                        {Piece.Z, new Position[] { (4, 2), (4, 1), (3, 1), (5, 2) } },
                        {Piece.S, new Position[] { (4, 2), (4, 1), (5, 1), (3, 2) } },
                        {Piece.L, new Position[] { (4, 2), (3, 2), (5, 2), (5, 1) } },
                        {Piece.J, new Position[] { (4, 2), (3, 2), (5, 2), (3, 1) } },
                        {Piece.T, new Position[] { (4, 2), (4, 1), (3, 2), (5, 2) } }
                    };

                _JLTSZRotations = new Dictionary<string, Position[]>
                {
                    {"01", new Position[]{(0, 0) ,(-1, 0) ,(-1,-1) ,( 0, 2) ,(-1, 2)} },
                    {"10", new Position[]{ (0, 0), (1, 0), (1, 1), (0, -2), (1, -2) } },
                    {"12", new Position[]{ (0, 0), (1, 0), (1, 1), (0, -2), (1, -2) } },
                    {"21", new Position[]{ (0, 0), (-1, 0), (-1, -1), (0, 2), (-1, 2) } },
                    {"23", new Position[]{ (0, 0), (1, 0), (1, -1), (0, 2), (1, 2) } },
                    {"32", new Position[]{ (0, 0), (-1, 0), (-1, 1), (0, -2), (-1, -2) } },
                    {"30", new Position[]{ (0, 0), (-1, 0), (-1, 1), (0, -2), (-1, -2) } },
                    {"03", new Position[]{ (0, 0), (1, 0), (1, -1), (0, 2), (1, 2) } }
                };

                _IRotations = new Dictionary<string, Position[]>
                {
                    {"01", new Position[]{ (0, 0), (-2, 0), (1, 0), (1, -2), (-2, 1) } },
                    {"03", new Position[]{ (0, 0), (2, 0), (-1, 0), (-1, -2), (2, 1) } },
                    {"21", new Position[]{ (0, 0), (-2, 0), (1, 0), (-2, -1), (1, 2) } },
                    {"23", new Position[]{ (0, 0), (2, 0), (-1, 0), (2, -1), (-1, 2) } },
                    {"10", new Position[]{ (0, 0), (2, 0), (-1, 0), (2, -1), (-1, 2) } },
                    {"30", new Position[]{ (0, 0), (-2, 0), (1, 0), (-2, -1), (1, 2) } },
                    {"12", new Position[]{ (0, 0), (-1, 0), (2, 0), (-1, -2), (2, 1) } },
                    {"32", new Position[]{ (0, 0), (1, 0), (-2, 0), (1, -2), (-2, 1) } }
                };


                for (int x = 0; x < _board.GetLength(0); x++)
                {
                    for (int y = 0; y < _board.GetLength(1); y++)
                    {
                        _board[x, y] = Piece.non;
                    }
                }
            }

            public double StartGame(int StartingLevel)
            {
                Console.SetWindowSize(35, 26);
                _level = StartingLevel;
                _delay -= _levelDelayStepSize * _level;

                FillBag();
                ResetPlayer();

                InitializeNextWindows();

                Task.Factory.StartNew(() =>
                {
                    while (!_isGameOver)
                    {
                        Tick();
                    }
                });

                _gravityStopwatch.Start();

                while (!_isGameOver)
                {
                    InputTick();
                }

                return _score;
            }

            public void InitializeNextWindows()
            {
                Piece SecondNextPieceType = _bag.ElementAt(1);
                Position[] SecondNextPieceShape = GetCopy(_pieceShapes[SecondNextPieceType]);

                for (int i = 0; i < SecondNextPieceShape.Length; i++)
                {
                    _nextBoards[1][SecondNextPieceShape[i].x - 3, SecondNextPieceShape[i].y - 1] = SecondNextPieceType;
                }

                Piece NextPieceType = _bag.Peek();
                Position[] NextPieceShape = GetCopy(_pieceShapes[NextPieceType]);

                for (int i = 0; i < NextPieceShape.Length; i++)
                {
                    _nextBoards[0][NextPieceShape[i].x - 3, NextPieceShape[i].y - 1] = NextPieceType;
                }
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
                        CheckAndRotateCurrentPieceClockwise();
                        break;

                    case ConsoleKey.Z:
                        CheckAndRotateCurrentPieceCounter();
                        break;

                    case ConsoleKey.A: //Left
                    case ConsoleKey.LeftArrow:
                        CheckAndMovePlayer(-1, 0);
                        break;

                    case ConsoleKey.S: //Down
                    case ConsoleKey.DownArrow:
                        DropWithoutTick();
                        break;

                    case ConsoleKey.D: //Right
                    case ConsoleKey.RightArrow:
                        CheckAndMovePlayer(1, 0);
                        break;

                    case ConsoleKey.Spacebar: //Drop
                    case ConsoleKey.Enter:
                        HardDropPlayer();
                        break;

                    case ConsoleKey.C: //Hold
                        CheckIfSwappedAndHold();
                        break;

                    case ConsoleKey.Escape:
                        _score = -2;
                        _isGameOver = true;
                        _score = -2;
                        break;

                    case ConsoleKey.R:
                        _isGameOver = true;
                        _score = -1;
                        break;
                }
            }

            private void CheckIfSwappedAndHold()
            {
                if (!_wasHeldSwapped)
                {
                    Position[] HeldPieceShape = GetCopy(_pieceShapes[_currentPiece]);
                    if (_heldShape.Length == 0)
                    {
                        _heldShape = GetMoved(HeldPieceShape, -3, -1);
                        _heldPiece = _currentPiece;
                        ResetPlayer();
                    }
                    else
                    {
                        _heldShape = GetMoved(HeldPieceShape, -3, -1);
                        _player = GetCopy(_pieceShapes[_heldPiece]);

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

                if (_scoreCollectionStopwatch.ElapsedMilliseconds > 150)
                {
                    AwardScore(_scoreCollectionLines);

                    DeleteDead();

                    _deletionLines = new List<int>();
                    _scoreCollectionStopwatch.Reset();
                    _scoreCollectionLines = 0;
                }

                if (_bag.Count() == 1) FillBag();
                if (_player.Length != 4) ResetPlayer();

                CheckAndClearLines();

                Console.SetCursorPosition(0, 0);
                Print();
            }

            private void DeleteDead()
            {
                lock (_deletionLineLock)
                {
                    for (int i = 0; i < _deletionLines.Count; i++)
                    {
                        DeleteLine(_deletionLines[i]);
                    }
                }
            }

            private void AwardScore(int LinesCleared)
            {
                float LevelMultiplier = 1 + (_level / 5);

                switch (LinesCleared)
                {
                    case 0:
                        return;
                    case 1:
                        _score += 100 * LevelMultiplier;
                        _tetrisesInRow = 0;
                        break;
                    case 2:
                        _score += 300 * LevelMultiplier;
                        _tetrisesInRow = 0;
                        break;
                    case 3:
                        _score += 500 * LevelMultiplier;
                        _tetrisesInRow = 0;
                        break;
                    case 4:
                        _score += 1000 * (_tetrisesInRow + 1) * LevelMultiplier;
                        _tetrisesInRow++;
                        break;
                }

                _linesClearedUpToTen += LinesCleared;

                if (_level < 20)
                    if (_linesClearedUpToTen >= 10)
                    {
                        _linesClearedUpToTen -= 10;
                        _level++;
                        _delay -= _levelDelayStepSize;
                    }
            }

            //Checks if any lines have been cleared and deletes them using DeleteLine
            //Returns the amount of lines cleared.
            private void CheckAndClearLines()
            {
                int Lines = 0;

                for (int y = 0; y < _board.GetLength(1); y++)
                {
                    bool LineFull = true;
                    for (int x = 0; x < _board.GetLength(0); x++)
                    {
                        if (_board[x, y] == Piece.non || _board[x, y] == Piece.ghost || _board[x, y] == Piece.dead)
                        {
                            LineFull = false;
                            break;
                        }
                    }

                    if (LineFull)
                    {
                        if (!_scoreCollectionStopwatch.IsRunning)
                        {
                            _scoreCollectionStopwatch.Start();
                        }

                        _scoreCollectionLines += 1;

                        if (_animateLineClear)
                        {
                            KillLine(y);
                            _deletionLines.Add(y);
                        }
                        else
                        {
                            DeleteLine(y);
                        }
                    }
                }
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


            private void KillLine(int DeletionY)
            {
                lock (_deletionLineLock)
                {
                    for (int x = 0; x < _board.GetLength(0); x++)
                    {
                        _board[x, DeletionY] = Piece.dead;
                    }
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

            private void DropWithoutTick()
            {
                if (IsValidAndNotPlayer(GetMoved(_player, 0, 1)))
                {
                    _player = GetMoved(_player, 0, 1);
                    _graceTicks = 2;
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

                for (int y = 0; y < 3; y++)
                {
                    for (int x = 0; x < 24; x++)
                    {
                        for (int i = 0; i < _player.Length; i++)
                        {
                            if (_player[i].x == x && _player[i].y == y)
                                _isGameOver = true;
                        }
                    }
                }

                _rotationPoint = 0;
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
                _player = GetCopy(_pieceShapes[_currentPiece]);

                TransferNextPiecesWindows();

                Piece ThirdNextPieceType = _bag.ElementAt(2);
                Position[] ThirdNextPieceShape = GetCopy(_pieceShapes[ThirdNextPieceType]);

                for (int i = 0; i < ThirdNextPieceShape.Length; i++)
                {
                    _nextBoards[2][ThirdNextPieceShape[i].x - 3, ThirdNextPieceShape[i].y - 1] = ThirdNextPieceType;
                }

                Piece SecondNextPieceType = _bag.ElementAt(1);
                Position[] SecondNextPieceShape = GetCopy(_pieceShapes[SecondNextPieceType]);
            }

            private void TransferNextPiecesWindows()
            {
                _nextBoards[0] = GetCopy(_nextBoards[1]);
                _nextBoards[1] = GetCopy(_nextBoards[2]);

                for (int x = 0; x < _nextBoards[2].GetLength(0); x++)
                {
                    for (int y = 0; y < _nextBoards[2].GetLength(1); y++)
                    {
                        _nextBoards[2][x, y] = Piece.non;
                    }
                }
            }

            private void CheckAndRotateCurrentPieceClockwise()
            {
                if (_currentPiece == Piece.O)
                    return;

                Position[] Rotated;
                switch (_currentPiece)
                {
                    case Piece.I:
                        Rotated = RotateIClockwise();
                        break;
                    case Piece.T:
                    case Piece.J:
                    case Piece.Z:
                    case Piece.L:
                    case Piece.S:
                        Rotated = RotateJLTSZClockwise();
                        break;
                    default:
                        return;
                }

                if (Rotated == _player) return;

                _player = Rotated;
                if (_currentPiece == Piece.I)
                    Swap(_player, 0, 1);
                _graceTicks = 1;
            }


            private void CheckAndRotateCurrentPieceCounter()
            {
                if (_currentPiece == Piece.O)
                    return;

                Position[] Rotated;
                switch (_currentPiece)
                {
                    case Piece.I:
                        Rotated = RotateICounter();
                        break;
                    case Piece.T:
                    case Piece.J:
                    case Piece.Z:
                    case Piece.L:
                    case Piece.S:
                        Rotated = RotateJLTSZCounter();
                        break;
                    default:
                        return;
                }

                if (Rotated == _player) return; ;

                _player = Rotated;
                if (_currentPiece == Piece.I)
                    Swap(_player, 0, 1);
                _graceTicks = 1;
            }

            private Position[] RotateIClockwise()
            {
                return GetClockwiseRotation(_IRotations);
            }

            private Position[] RotateICounter()
            {
                return GetCounterRotation(_IRotations);
            }

            private Position[] RotateJLTSZClockwise()
            {
                return GetClockwiseRotation(_JLTSZRotations);
            }

            private Position[] RotateJLTSZCounter()
            {
                return GetCounterRotation(_JLTSZRotations);
            }

            public Position[] GetClockwiseRotation(Dictionary<string, Position[]> RotationDict)
            {
                int NextRotation = (_rotationPoint + 1) % 4;
                Position[] Base = GetRotated(_player, 1);
                string RotationSymbol = _rotationPoint + "" + NextRotation;
                Position[] RotationAttempts = RotationDict[RotationSymbol];

                for (int i = 0; i < RotationAttempts.Length; i++)
                {
                    Position[] Attempt = GetMoved(Base, RotationAttempts[i]);
                    if (IsValidAndNotPlayer(Attempt))
                    {
                        if (_rotationPoint + 1 > 3)
                            _rotationPoint = 0;
                        else
                            _rotationPoint++;
                        return Attempt;
                    }
                }
                return _player;
            }

            public Position[] GetCounterRotation(Dictionary<string, Position[]> RotationDict)
            {
                int NextRotation = (_rotationPoint + 3) % 4;
                Position[] Base = GetRotated(_player, 3);
                string RotationSymbol = _rotationPoint + "" + NextRotation;
                Position[] RotationAttempts = _IRotations[RotationSymbol];

                for (int i = 0; i < RotationAttempts.Length; i++)
                {
                    Position[] Attempt = GetMoved(Base, RotationAttempts[i]);
                    if (IsValidAndNotPlayer(Attempt))
                    {
                        if (_rotationPoint - 1 < 0)
                            _rotationPoint = 3;
                        else
                            _rotationPoint--;
                        return Attempt;
                    }
                }
                return _player;
            }


            private void HardDropPlayer()
            {
                _player = GetHardDroppedPlayer();
                ClearPlayer();
            }

            private Position[] GetHardDroppedPlayer()
            {
                if (_player == null || _player.Length < 4) return _player;

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

            public Position[] GetMoved(Position[] Input, Position Offset)
            {
                int XOffset = Offset.x;
                int YOffset = Offset.y;

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



                WriteLineGreen("╔══════════════════╦════════════╗");

                WriteLineGreen($"║  Score:{GetDoubleCompletedToNthDigit(_score, 8)}  ║  Level:{GetDoubleCompletedToNthDigit(_level, 2)}  ║");

                WriteLineGreen("╠══════════════════╩══╦═════════╣");

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

                    PrintSideMenu(y);
                }

                WriteLineGreen("╚═════════════════════╩═════════╝");
            }

            private void PrintSideMenu(int y)
            {
                int SideMenuBase = 3;

                if (y == SideMenuBase + 10)
                    WriteGreen("╠");
                else
                    WriteGreen("║");

                if (y == SideMenuBase + 1)
                {
                    Console.Write("  Next:");
                    WriteLineGreen("  ║");
                }
                else if (y > SideMenuBase + 1 && y < SideMenuBase + 4)
                {
                    for (int x = 0; x < _nextBoards[0].GetLength(0); x++)
                    {
                        Console.Write(" ");

                        PrintWithColorByPiece(_nextBoards[0][x, y - (SideMenuBase + 2)]);

                        if (x == _nextBoards[0].GetLength(0) - 1)
                            Console.Write(" ");
                    }

                    WriteLineGreen("║ ");
                }
                else if (y > SideMenuBase + 4 && y < SideMenuBase + 7)
                {
                    for (int x = 0; x < _nextBoards[1].GetLength(0); x++)
                    {
                        Console.Write(" ");

                        PrintWithColorByPiece(_nextBoards[1][x, y - (SideMenuBase + 5)]);

                        if (x == _nextBoards[1].GetLength(0) - 1)
                            Console.Write(" ");
                    }

                    WriteLineGreen("║ ");
                }
                else if (y > SideMenuBase + 7 && y < SideMenuBase + 10)
                {
                    for (int x = 0; x < _nextBoards[2].GetLength(0); x++)
                    {
                        Console.Write(" ");

                        PrintWithColorByPiece(_nextBoards[2][x, y - (SideMenuBase + 8)]);

                        if (x == _nextBoards[2].GetLength(0) - 1)
                            Console.Write(" ");
                    }

                    WriteLineGreen("║ ");
                }
                else if (y == SideMenuBase + 10)
                {
                    WriteLineGreen("═════════╣");
                }
                else if (y == SideMenuBase + 11)
                {
                    Console.Write("  Held:");
                    WriteLineGreen("  ║");
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
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write("■");
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                    case Piece.Z:
                        Console.ForegroundColor = ConsoleColor.Red;
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
                    case Piece.dead:
                        Console.Write("■");
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
                ghost = 8,
                dead = 9
            }
        }
    }
}
