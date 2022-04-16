using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace TetrisC
{
    class Program
    {
        public enum BlockType { LineClear = -8, ZGhost = -7, TGhost, SGhost, OGhost, LGhost, JGhost, IGhost = -1, Empty = 0, I = 1, J, L, O, S, T, Z = 7 }
        public enum LineClearType { Single = 1, Double, Triple, Tetris = 4 }

        public enum Orientation { North, East, South, West } // 0 (spawn), 1 (right), 2, 3 (left).

        public enum Translation { ShiftLeft, ShiftRight, ShiftDown }
        public enum Rotation { Clockwise, AntiClockwise }

        private static readonly object _lock = new();

        public record Position
        {
            public int Row { get; set; }
            public int Column { get; set; }

            private int _maxRows;
            private int _maxColumns;

            public Position(int row, int column, int maxRows, int maxColumns)
            {
                Trace.Assert(row >= 0 && row < maxRows);
                Trace.Assert(column >= 0 && column < maxColumns);

                Row = row;
                Column = column;

                _maxRows = maxRows;
                _maxColumns = maxColumns;
            }

            public Position(Position position) // Copy constructor.
            {
                Trace.Assert(position.IsValid());

                Row = position.Row;
                Column = position.Column;

                _maxRows = position._maxRows;
                _maxColumns = position._maxColumns;
            }

            public bool IsValid()
            {
                return Row >= 0 && Row < _maxRows && Column >= 0 && Column < _maxColumns;
            }
        }

        public record CellInfo
        {
            public int Id { get; set; } // TODO: .
            public BlockType BlockType { get; set; }
            public Orientation Orientation { get; set; }
            public int SubBlock { get; set; } // 1, 2, 3, 4 or LineClearType. // TODO: .

            public CellInfo(BlockType blockType = BlockType.Empty, Orientation orientation = Orientation.North, int subBlock = 1, int id = 0)
            {
                Trace.Assert(subBlock >= 1 && subBlock <= 4);

                BlockType = blockType;
                Orientation = orientation;
                SubBlock = subBlock;
                Id = id;
            }

            public void Set(BlockType blockType = BlockType.Empty, Orientation orientation = Orientation.North, int subBlock = 1, int id = 0)
            {
                Trace.Assert(subBlock >= 1 && subBlock <= 4);

                BlockType = blockType;
                Orientation = orientation;
                SubBlock = subBlock;
                Id = id;
            }
        }

        public static class Logo
        {
            public static void Print(int xOffset, int yOffset)
            {
                Write("  ╔═════════════════════════════════╗  ", xOffset, yOffset,      ConsoleColor.Blue);
                Write("  ║                                 ║  ", xOffset, yOffset + 1,  ConsoleColor.Blue);
                Write("  ║                                 ║  ", xOffset, yOffset + 2,  ConsoleColor.Blue);
                Write("  ║                                 ║  ", xOffset, yOffset + 3,  ConsoleColor.Blue);
                Write("  ║                                 ║  ", xOffset, yOffset + 4,  ConsoleColor.Blue);
                Write("  ╚═══════════╗         ╔═══════════╝  ", xOffset, yOffset + 5,  ConsoleColor.Blue);
                Write("              ║         ║              ", xOffset, yOffset + 6,  ConsoleColor.Blue);
                Write("              ║         ║              ", xOffset, yOffset + 7,  ConsoleColor.Blue);
                Write("              ║         ║              ", xOffset, yOffset + 8,  ConsoleColor.Blue);
                Write("              ║         ║              ", xOffset, yOffset + 9,  ConsoleColor.Blue);
                Write("              ║         ║              ", xOffset, yOffset + 10, ConsoleColor.Blue);
                Write("              ╚═════════╝              ", xOffset, yOffset + 11, ConsoleColor.Blue);

                Write("┌┬┐",  xOffset + 6,  yOffset + 1, ConsoleColor.Red);
                Write(" │ ",  xOffset + 6,  yOffset + 2, ConsoleColor.Red);
                Write(" ┴ ",  xOffset + 6,  yOffset + 3, ConsoleColor.Red);
                Write("┌─┐",  xOffset + 11, yOffset + 1, ConsoleColor.White);
                Write("├┤ ",  xOffset + 11, yOffset + 2, ConsoleColor.White);
                Write("└─┘",  xOffset + 11, yOffset + 3, ConsoleColor.White);
                Write("┌┬┐",  xOffset + 16, yOffset + 1, ConsoleColor.Yellow);
                Write(" │ ",  xOffset + 16, yOffset + 2, ConsoleColor.Yellow);
                Write(" ┴ ",  xOffset + 16, yOffset + 3, ConsoleColor.Yellow);
                Write("┬─┐",  xOffset + 21, yOffset + 1, ConsoleColor.Green);
                Write("├┬┘",  xOffset + 21, yOffset + 2, ConsoleColor.Green);
                Write("┴└─",  xOffset + 21, yOffset + 3, ConsoleColor.Green);
                Write("┬",    xOffset + 26, yOffset + 1, ConsoleColor.Cyan);
                Write("│",    xOffset + 26, yOffset + 2, ConsoleColor.Cyan);
                Write("┴",    xOffset + 26, yOffset + 3, ConsoleColor.Cyan);
                Write("┌─┐®", xOffset + 29, yOffset + 1, ConsoleColor.Magenta);
                Write("└─┐ ", xOffset + 29, yOffset + 2, ConsoleColor.Magenta);
                Write("└─┘ ", xOffset + 29, yOffset + 3, ConsoleColor.Magenta);

                Write(" Tetris © 1985~2022 Tetris Holding.    ", xOffset, yOffset + 13, ConsoleColor.Gray);
                Write(" Tetris logos, Tetris theme song and   ", xOffset, yOffset + 14, ConsoleColor.Gray);
                Write(" Tetriminos are trademarks of          ", xOffset, yOffset + 15, ConsoleColor.Gray);
                Write(" Tetris Holding.                       ", xOffset, yOffset + 16, ConsoleColor.Gray);
                Write(" The Tetris trade dress is owned by    ", xOffset, yOffset + 17, ConsoleColor.Gray);
                Write(" Tetris Holding.                       ", xOffset, yOffset + 18, ConsoleColor.Gray);
                Write(" Licensed to The Tetris Company.       ", xOffset, yOffset + 19, ConsoleColor.Gray);
                Write(" Tetris Game Design by Alexey Pajitnov.", xOffset, yOffset + 20, ConsoleColor.Gray);
                Write(" Tetris Logo Design by Roger Dean.     ", xOffset, yOffset + 21, ConsoleColor.Gray);
                Write(" All Rights Reserved.                  ", xOffset, yOffset + 22, ConsoleColor.Gray);

                Write("───────────────────────────────────────", xOffset, yOffset + 24, ConsoleColor.DarkGray);

                Write(" https://github.com/LDj3SNuD/TetrisC   ", xOffset, yOffset + 26, ConsoleColor.White);
            }
        }

        public static class BlockLineClear
        {
            public static ConsoleColor Color => ConsoleColor.DarkGray;

            public static string GetBlockString(int line, int maxColumns)
            {
                Trace.Assert(line >= 1 && line <= 3);

                return new[]
                {
                    " ┌" + new String('─', (maxColumns - 1) * 4) + "─┐",
                    " └" + new String('─', (maxColumns - 1) * 4) + "─┘",
                    " │" + new String(' ', (maxColumns - 1) * 4) + " │"
                }[line - 1];
            }
        }

        public static class BlockEmpty
        {
            public static ConsoleColor Color1 => ConsoleColor.DarkGray;
            public static ConsoleColor Color2 => ConsoleColor.Black;

            public static string GetBlockString(int line)
            {
                Trace.Assert(line >= 1 && line <= 2);

                return new[]
                {
                    " ┌─┐",
                    " └─┘"
                }[line - 1];
            }
        }

        public abstract class Block
        {
            public const int MaxLockDelayShortTerm = 500; // ms.
            public const int MaxLockDelayLongTerm = 10;

            public virtual BlockType BlockType { get; }

            public virtual int Height { get; }
            public virtual int Width { get; }

            public List<Position> Positions { get; private set; }
            public Orientation Orientation { get; private set; }

            public bool IsPreLocked { get; set; }
            public bool IsLocked { get; set; }

            public int LockDelayLongTerm { get; set; }

            public bool IsHardDrop { get; set; }

            public bool IsHold { get; set; }
            public bool CanHold { get; set; }

            private Matrix _matrix;

            protected Block(Matrix matrix)
            {
                Positions = new();

                _matrix = matrix;
            }

            protected abstract List<(int RowOffset, int ColumnOffset)> GetSpawnPositions();

            protected abstract List<(int RowOffset, int ColumnOffset)> GetRotateOffsets(Rotation rotation);

            protected virtual List<(int RowOffset, int ColumnOffset)> GetRotateWallKickOffsets(Rotation rotation) // J, L, S, T, Z.
            {
                switch (Orientation)
                {
                    case Orientation.North:
                    {
                        return rotation switch
                        {
                            Rotation.Clockwise => new() { (0, 0), (0, -1), (-1, -1), (2, 0), (2, -1) }, // 0>>1
                            Rotation.AntiClockwise => new() { (0, 0), (0, 1), (-1, 1), (2, 0), (2, 1) }, // 0>>3
                            _ => throw new ArgumentException(nameof(rotation))
                        };
                    }

                    case Orientation.East:
                    {
                        return rotation switch
                        {
                            Rotation.Clockwise => new() { (0, 0), (0, 1), (1, 1), (-2, 0), (-2, 1) }, // 1>>2
                            Rotation.AntiClockwise => new() { (0, 0), (0, 1), (1, 1), (-2, 0), (-2, 1) }, // 1>>0
                            _ => throw new ArgumentException(nameof(rotation))
                        };
                    }

                    case Orientation.South:
                    {
                        return rotation switch
                        {
                            Rotation.Clockwise => new() { (0, 0), (0, 1), (-1, 1), (2, 0), (2, 1) }, // 2>>3
                            Rotation.AntiClockwise => new() { (0, 0), (0, -1), (-1, -1), (2, 0), (2, -1) }, // 2>>1
                            _ => throw new ArgumentException(nameof(rotation))
                        };
                    }

                    case Orientation.West:
                    {
                        return rotation switch
                        {
                            Rotation.Clockwise => new() { (0, 0), (0, -1), (1, -1), (-2, 0), (-2, -1) }, // 3>>0
                            Rotation.AntiClockwise => new() { (0, 0), (0, -1), (1, -1), (-2, 0), (-2, -1) }, // 3>>2
                            _ => throw new ArgumentException(nameof(rotation))
                        };
                    }

                    default:
                    {
                        throw new Exception(nameof(Orientation));
                    }
                }
            }

            private List<(int RowOffset, int ColumnOffset)> GetTranslateOffsets(Translation translation)
            {
                return translation switch
                {
                    Translation.ShiftLeft => new() { (0, -1), (0, -1), (0, -1), (0, -1) },
                    Translation.ShiftRight => new() { (0, 1), (0, 1), (0, 1), (0, 1) },
                    Translation.ShiftDown => new() { (1, 0), (1, 0), (1, 0), (1, 0) },
                    _ => throw new ArgumentException(nameof(translation))
                };
            }

            private Orientation GetRotateOrientation(Rotation rotation)
            {
                switch (Orientation)
                {
                    case Orientation.North:
                    {
                        return rotation switch
                        {
                            Rotation.Clockwise => Orientation.East,
                            Rotation.AntiClockwise => Orientation.West,
                            _ => throw new ArgumentException(nameof(rotation))
                        };
                    }

                    case Orientation.East:
                    {
                        return rotation switch
                        {
                            Rotation.Clockwise => Orientation.South,
                            Rotation.AntiClockwise => Orientation.North,
                            _ => throw new ArgumentException(nameof(rotation))
                        };
                    }

                    case Orientation.South:
                    {
                        return rotation switch
                        {
                            Rotation.Clockwise => Orientation.West,
                            Rotation.AntiClockwise => Orientation.East,
                            _ => throw new ArgumentException(nameof(rotation))
                        };
                    }

                    case Orientation.West:
                    {
                        return rotation switch
                        {
                            Rotation.Clockwise => Orientation.North,
                            Rotation.AntiClockwise => Orientation.South,
                            _ => throw new ArgumentException(nameof(rotation))
                        };
                    }

                    default:
                    {
                        throw new Exception(nameof(Orientation));
                    }
                }
            }

            public bool TrySpawn(int slot = 0)
            {
                lock (Positions)
                {
                    Orientation = Orientation.North;

                    Positions.Clear();

                    foreach (var positions in GetSpawnPositions())
                    {
                        Positions.Add(new(positions.RowOffset + GetSpawnRowOffset(slot), positions.ColumnOffset + GetSpawnColumnOffset(), _matrix.MaxRows, _matrix.MaxColumns));
                    }

                    foreach (Position position in Positions)
                    {
                        if (_matrix[position.Row, position.Column].BlockType > BlockType.Empty)
                        {
                            return false;
                        }
                    }

                    _matrix.Id++;

                    int subBlock = 1;

                    foreach (Position position in Positions)
                    {
                        _matrix[position.Row, position.Column].Set(BlockType, Orientation, subBlock++, _matrix.Id);
                    }

                    return true;
                }
            }

            public bool TryTranslate(Translation translation)
            {
                List<Position> positionsCopy = new();

                foreach (Position position in Positions)
                {
                    positionsCopy.Add(new(position));

                    _matrix[position.Row, position.Column].Set();
                }

                int index = 0;

                foreach (var tOffsets in GetTranslateOffsets(translation))
                {
                    positionsCopy[index].Row += tOffsets.RowOffset;
                    positionsCopy[index++].Column += tOffsets.ColumnOffset;
                }

                int count = 0;

                foreach (Position position in positionsCopy)
                {
                    if (position.IsValid() && _matrix[position.Row, position.Column].BlockType <= BlockType.Empty)
                    {
                        count++;
                    }
                }

                if (count == positionsCopy.Count)
                {
                    index = 0;
                    int subBlock = 1;

                    foreach (Position position in Positions)
                    {
                        position.Row = positionsCopy[index].Row;
                        position.Column = positionsCopy[index++].Column;

                        _matrix[position.Row, position.Column].Set(BlockType, Orientation, subBlock++, _matrix.Id);
                    }

                    return true;
                }
                else
                {
                    int subBlock = 1;

                    foreach (Position position in Positions)
                    {
                        _matrix[position.Row, position.Column].Set(BlockType, Orientation, subBlock++, _matrix.Id);
                    }

                    return false;
                }
            }

            public bool TryRotate(Rotation rotation)
            {
                List<Position> positionsCopy = new();

                foreach (Position position in Positions)
                {
                    positionsCopy.Add(new(position));

                    _matrix[position.Row, position.Column].Set();
                }

                foreach (var wKOffsets in GetRotateWallKickOffsets(rotation))
                {
                    int index = 0;

                    foreach (var rOffsets in GetRotateOffsets(rotation))
                    {
                        positionsCopy[index].Row += rOffsets.RowOffset + wKOffsets.RowOffset;
                        positionsCopy[index++].Column += rOffsets.ColumnOffset + wKOffsets.ColumnOffset;
                    }

                    int count = 0;

                    foreach (Position position in positionsCopy)
                    {
                        if (position.IsValid() && _matrix[position.Row, position.Column].BlockType <= BlockType.Empty)
                        {
                            count++;
                        }
                    }

                    if (count == positionsCopy.Count)
                    {
                        Orientation = GetRotateOrientation(rotation);

                        index = 0;
                        int subBlock1 = 1;

                        foreach (Position position in Positions)
                        {
                            position.Row = positionsCopy[index].Row;
                            position.Column = positionsCopy[index++].Column;

                            _matrix[position.Row, position.Column].Set(BlockType, Orientation, subBlock1++, _matrix.Id);
                        }

                        return true;
                    }
                    else
                    {
                        index = 0;

                        foreach (Position position in Positions)
                        {
                            positionsCopy[index].Row = position.Row;
                            positionsCopy[index++].Column = position.Column;
                        }
                    }
                }

                int subBlock2 = 1;

                foreach (Position position in Positions)
                {
                    _matrix[position.Row, position.Column].Set(BlockType, Orientation, subBlock2++, _matrix.Id);
                }

                return false;
            }

            public bool HasLanded()
            {
                int count = Positions.Count;

                foreach (Position position in Positions)
                {
                    if (position.Row + 1 < _matrix.MaxRows && Positions.Contains(new(position.Row + 1, position.Column, _matrix.MaxRows, _matrix.MaxColumns)))
                    {
                        continue;
                    }

                    if (position.Row + 1 >= _matrix.MaxRows || _matrix[position.Row + 1, position.Column].BlockType > BlockType.Empty)
                    {
                        count--;
                    }
                }

                return count != Positions.Count;
            }

            public bool IsLockDelay()
            {
                return !IsLocked && HasLanded();
            }

            public void Clear()
            {
                lock (Positions)
                {
                    foreach (Position position in Positions)
                    {
                        if (_matrix[position.Row, position.Column].BlockType > BlockType.Empty)
                        {
                            _matrix[position.Row, position.Column].Set();
                        }
                    }

                    Positions.Clear();

                    Orientation = Orientation.North;
                }
            }

            private int GetSpawnRowOffset(int slot) // For Preview use only.
            {
                const int slotHeight = 2;

                Trace.Assert((slot + 1) * slotHeight <= _matrix.MaxRows);

                return slot * slotHeight;
            }

            private int GetSpawnColumnOffset()
            {
                Debug.Assert(Width <= _matrix.MaxColumns);

                int columnOffset;

                if (_matrix.MaxColumns % 2 == 0 && Width % 2 != 0)
                {
                    columnOffset = (_matrix.MaxColumns - Width - 1) / 2;
                }
                else /* if (_maxColumns % 2 != 0 || Width % 2 == 0) */
                {
                    columnOffset = (_matrix.MaxColumns - Width) / 2;
                }

                return columnOffset;
            }
        }

        public class BlockI : Block
        {
            public override BlockType BlockType => BlockType.I;

            public static ConsoleColor Color1 => ConsoleColor.Cyan;
            public static ConsoleColor Color2 => ConsoleColor.DarkCyan;

            public override int Height => Orientation switch { Orientation.North => 1, Orientation.East => 4, Orientation.South => 1, Orientation.West => 4, _ => default };
            public override int Width => Orientation switch { Orientation.North => 4, Orientation.East => 1, Orientation.South => 4, Orientation.West => 1, _ => default };

            public BlockI(Matrix matrix) : base(matrix)
            {
            }

            protected override List<(int RowOffset, int ColumnOffset)> GetSpawnPositions()
            {
                return new() { (1, 0), (1, 1), (1, 2), (1, 3) };
            }

            protected override List<(int RowOffset, int ColumnOffset)> GetRotateOffsets(Rotation rotation)
            {
                switch (Orientation)
                {
                    case Orientation.North:
                    {
                        return rotation switch
                        {
                            Rotation.Clockwise => new() { (-1, 2), (0, 1), (1, 0), (2, -1) },
                            Rotation.AntiClockwise => new() { (2, 1), (1, 0), (0, -1), (-1, -2) },
                            _ => throw new ArgumentException(nameof(rotation))
                        };
                    }

                    case Orientation.East:
                    {
                        return rotation switch
                        {
                            Rotation.Clockwise => new() { (2, 1), (1, 0), (0, -1), (-1, -2) },
                            Rotation.AntiClockwise => new() { (1, -2), (0, -1), (-1, 0), (-2, 1) },
                            _ => throw new ArgumentException(nameof(rotation))
                        };
                    }

                    case Orientation.South:
                    {
                        return rotation switch
                        {
                            Rotation.Clockwise => new() { (1, -2), (0, -1), (-1, 0), (-2, 1) },
                            Rotation.AntiClockwise => new() { (-2, -1), (-1, 0), (0, 1), (1, 2) },
                            _ => throw new ArgumentException(nameof(rotation))
                        };
                    }

                    case Orientation.West:
                    {
                        return rotation switch
                        {
                            Rotation.Clockwise => new() { (-2, -1), (-1, 0), (0, 1), (1, 2) },
                            Rotation.AntiClockwise => new() { (-1, 2), (0, 1), (1, 0), (2, -1) },
                            _ => throw new ArgumentException(nameof(rotation))
                        };
                    }

                    default:
                    {
                        throw new Exception(nameof(Orientation));
                    }
                }
            }

            protected override List<(int RowOffset, int ColumnOffset)> GetRotateWallKickOffsets(Rotation rotation)
            {
                switch (Orientation)
                {
                    case Orientation.North:
                    {
                        return rotation switch
                        {
                            Rotation.Clockwise => new() { (0, 0), (0, -2), (0, 1), (1, -2), (-2, 1) }, // 0>>1
                            Rotation.AntiClockwise => new() { (0, 0), (0, -1), (0, 2), (-2, -1), (1, 2) }, // 0>>3
                            _ => throw new ArgumentException(nameof(rotation))
                        };
                    }

                    case Orientation.East:
                    {
                        return rotation switch
                        {
                            Rotation.Clockwise => new() { (0, 0), (0, -1), (0, 2), (-2, -1), (1, 2) }, // 1>>2
                            Rotation.AntiClockwise => new() { (0, 0), (0, 2), (0, -1), (-1, 2), (2, -1) }, // 1>>0
                            _ => throw new ArgumentException(nameof(rotation))
                        };
                    }

                    case Orientation.South:
                    {
                        return rotation switch
                        {
                            Rotation.Clockwise => new() { (0, 0), (0, 2), (0, -1), (-1, 2), (2, -1) }, // 2>>3
                            Rotation.AntiClockwise => new() { (0, 0), (0, 1), (0, -2), (2, 1), (-1, -2) }, // 2>>1
                            _ => throw new ArgumentException(nameof(rotation))
                        };
                    }

                    case Orientation.West:
                    {
                        return rotation switch
                        {
                            Rotation.Clockwise => new() { (0, 0), (0, 1), (0, -2), (2, 1), (-1, -2) }, // 3>>0
                            Rotation.AntiClockwise => new() { (0, 0), (0, -2), (0, 1), (1, -2), (-2, 1) }, // 3>>2
                            _ => throw new ArgumentException(nameof(rotation))
                        };
                    }

                    default:
                    {
                        throw new Exception(nameof(Orientation));
                    }
                }
            }

            public static string GetSubBlockString(Orientation orientation, int subBlock, int line, int missingSubBlockMask)
            {
                Trace.Assert(subBlock >= 1 && subBlock <= 4);
                Trace.Assert(line >= 1 && line <= 2);

                switch (orientation)
                {
                    case Orientation.North:
                    {
                        switch (missingSubBlockMask)
                        {
                            case 0b0000:
                            {
                                return new[,]
                                {
                                    { " ╔══", "════", "════", "═══╗" },
                                    { " ╚══", "════", "════", "═══╝" }
                                }[line - 1, subBlock - 1];
                            }

                            default:
                            {
                                throw new ArgumentException(nameof(missingSubBlockMask));
                            }
                        }
                    }

                    case Orientation.East:
                    {
                        switch (missingSubBlockMask)
                        {
                            case 0b0000:
                            {
                                return new[,]
                                {
                                    { " ╔═╗", " ║ ║", " ║ ║", " ║ ║" },
                                    { " ║ ║", " ║ ║", " ║ ║", " ╚═╝" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b1000:
                            {
                                return new[,]
                                {
                                    { String.Empty, " ╔═╗", " ║ ║", " ║ ║" },
                                    { String.Empty, " ║ ║", " ║ ║", " ╚═╝" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b0100:
                            {
                                return new[,]
                                {
                                    { " ╔═╗", String.Empty, " ╔═╗", " ║ ║" },
                                    { " ╚═╝", String.Empty, " ║ ║", " ╚═╝" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b0010:
                            {
                                return new[,]
                                {
                                    { " ╔═╗", " ║ ║", String.Empty, " ╔═╗" },
                                    { " ║ ║", " ╚═╝", String.Empty, " ╚═╝" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b0001:
                            {
                                return new[,]
                                {
                                    { " ╔═╗", " ║ ║", " ║ ║", String.Empty },
                                    { " ║ ║", " ║ ║", " ╚═╝", String.Empty }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b1100:
                            {
                                return new[,]
                                {
                                    { String.Empty, String.Empty, " ╔═╗", " ║ ║" },
                                    { String.Empty, String.Empty, " ║ ║", " ╚═╝" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b1010:
                            {
                                return new[,]
                                {
                                    { String.Empty, " ╔═╗", String.Empty, " ╔═╗" },
                                    { String.Empty, " ╚═╝", String.Empty, " ╚═╝" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b1001:
                            {
                                return new[,]
                                {
                                    { String.Empty, " ╔═╗", " ║ ║", String.Empty },
                                    { String.Empty, " ║ ║", " ╚═╝", String.Empty }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b0110:
                            {
                                return new[,]
                                {
                                    { " ╔═╗", String.Empty, String.Empty, " ╔═╗" },
                                    { " ╚═╝", String.Empty, String.Empty, " ╚═╝" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b0101:
                            {
                                return new[,]
                                {
                                    { " ╔═╗", String.Empty, " ╔═╗", String.Empty },
                                    { " ╚═╝", String.Empty, " ╚═╝", String.Empty }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b0011:
                            {
                                return new[,]
                                {
                                    { " ╔═╗", " ║ ║", String.Empty, String.Empty },
                                    { " ║ ║", " ╚═╝", String.Empty, String.Empty }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b1110:
                            {
                                return new[,]
                                {
                                    { String.Empty, String.Empty, String.Empty, " ╔═╗" },
                                    { String.Empty, String.Empty, String.Empty, " ╚═╝" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b1101:
                            {
                                return new[,]
                                {
                                    { String.Empty, String.Empty, " ╔═╗", String.Empty },
                                    { String.Empty, String.Empty, " ╚═╝", String.Empty }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b1011:
                            {
                                return new[,]
                                {
                                    { String.Empty, " ╔═╗", String.Empty, String.Empty },
                                    { String.Empty, " ╚═╝", String.Empty, String.Empty }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b0111:
                            {
                                return new[,]
                                {
                                    { " ╔═╗", String.Empty, String.Empty, String.Empty },
                                    { " ╚═╝", String.Empty, String.Empty, String.Empty }
                                }[line - 1, subBlock - 1];
                            }

                            default:
                            {
                                throw new ArgumentException(nameof(missingSubBlockMask));
                            }
                        }
                    }

                    case Orientation.South:
                    {
                        switch (missingSubBlockMask)
                        {
                            case 0b0000:
                            {
                                return new[,]
                                {
                                    { "═══╗", "════", "════", " ╔══" },
                                    { "═══╝", "════", "════", " ╚══" }
                                }[line - 1, subBlock - 1];
                            }

                            default:
                            {
                                throw new ArgumentException(nameof(missingSubBlockMask));
                            }
                        }
                    }

                    case Orientation.West:
                    {
                        switch (missingSubBlockMask)
                        {
                            case 0b0000:
                            {
                                return new[,]
                                {
                                    { " ║ ║", " ║ ║", " ║ ║", " ╔═╗" },
                                    { " ╚═╝", " ║ ║", " ║ ║", " ║ ║" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b0001:
                            {
                                return new[,]
                                {
                                    { " ║ ║", " ║ ║", " ╔═╗",  String.Empty },
                                    { " ╚═╝", " ║ ║", " ║ ║",  String.Empty }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b0010:
                            {
                                return new[,]
                                {
                                    { " ║ ║", " ╔═╗", String.Empty, " ╔═╗" },
                                    { " ╚═╝", " ║ ║", String.Empty, " ╚═╝" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b0100:
                            {
                                return new[,]
                                {
                                    { " ╔═╗", String.Empty, " ║ ║", " ╔═╗" },
                                    { " ╚═╝", String.Empty, " ╚═╝", " ║ ║" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b1000:
                            {
                                return new[,]
                                {
                                    { String.Empty, " ║ ║", " ║ ║", " ╔═╗" },
                                    { String.Empty, " ╚═╝", " ║ ║", " ║ ║" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b0011:
                            {
                                return new[,]
                                {
                                    { " ║ ║", " ╔═╗", String.Empty, String.Empty },
                                    { " ╚═╝", " ║ ║", String.Empty, String.Empty }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b0101:
                            {
                                return new[,]
                                {
                                    { " ╔═╗", String.Empty, " ╔═╗", String.Empty },
                                    { " ╚═╝", String.Empty, " ╚═╝", String.Empty }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b1001:
                            {
                                return new[,]
                                {
                                    { String.Empty, " ║ ║", " ╔═╗", String.Empty },
                                    { String.Empty, " ╚═╝", " ║ ║", String.Empty }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b0110:
                            {
                                return new[,]
                                {
                                    { " ╔═╗", String.Empty, String.Empty, " ╔═╗" },
                                    { " ╚═╝", String.Empty, String.Empty, " ╚═╝" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b1010:
                            {
                                return new[,]
                                {
                                    { String.Empty, " ╔═╗", String.Empty, " ╔═╗" },
                                    { String.Empty, " ╚═╝", String.Empty, " ╚═╝" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b1100:
                            {
                                return new[,]
                                {
                                    { String.Empty, String.Empty, " ║ ║", " ╔═╗" },
                                    { String.Empty, String.Empty, " ╚═╝", " ║ ║" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b0111:
                            {
                                return new[,]
                                {
                                    { " ╔═╗", String.Empty, String.Empty, String.Empty },
                                    { " ╚═╝", String.Empty, String.Empty, String.Empty }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b1011:
                            {
                                return new[,]
                                {
                                    { String.Empty, " ╔═╗", String.Empty, String.Empty },
                                    { String.Empty, " ╚═╝", String.Empty, String.Empty }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b1101:
                            {
                                return new[,]
                                {
                                    { String.Empty, String.Empty, " ╔═╗", String.Empty },
                                    { String.Empty, String.Empty, " ╚═╝", String.Empty }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b1110:
                            {
                                return new[,]
                                {
                                    { String.Empty, String.Empty, String.Empty, " ╔═╗" },
                                    { String.Empty, String.Empty, String.Empty, " ╚═╝" }
                                }[line - 1, subBlock - 1];
                            }

                            default:
                            {
                                throw new ArgumentException(nameof(missingSubBlockMask));
                            }
                        }
                    }

                    default:
                    {
                        throw new ArgumentException(nameof(orientation));
                    }
                }
            }
        }

        public class BlockJ : Block
        {
            public override BlockType BlockType => BlockType.J;

            public static ConsoleColor Color1 => ConsoleColor.Blue;
            public static ConsoleColor Color2 => ConsoleColor.DarkBlue;

            public override int Height => Orientation switch { Orientation.North => 2, Orientation.East => 3, Orientation.South => 2, Orientation.West => 3, _ => default };
            public override int Width => Orientation switch { Orientation.North => 3, Orientation.East => 2, Orientation.South => 3, Orientation.West => 2, _ => default };

            public BlockJ(Matrix matrix) : base(matrix)
            {
            }

            protected override List<(int RowOffset, int ColumnOffset)> GetSpawnPositions()
            {
                return new() { (0, 0), (1, 0), (1, 1), (1, 2) };
            }

            protected override List<(int RowOffset, int ColumnOffset)> GetRotateOffsets(Rotation rotation)
            {
                switch (Orientation)
                {
                    case Orientation.North:
                    {
                        return rotation switch
                        {
                            Rotation.Clockwise => new() { (0, 2), (-1, 1), (0, 0), (1, -1) },
                            Rotation.AntiClockwise => new() { (2, 0), (1, 1), (0, 0), (-1, -1) },
                            _ => throw new ArgumentException(nameof(rotation))
                        };
                    }

                    case Orientation.East:
                    {
                        return rotation switch
                        {
                            Rotation.Clockwise => new() { (2, 0), (1, 1), (0, 0), (-1, -1) },
                            Rotation.AntiClockwise => new() { (0, -2), (1, -1), (0, 0), (-1, 1) },
                            _ => throw new ArgumentException(nameof(rotation))
                        };
                    }

                    case Orientation.South:
                    {
                        return rotation switch
                        {
                            Rotation.Clockwise => new() { (0, -2), (1, -1), (0, 0), (-1, 1) },
                            Rotation.AntiClockwise => new() { (-2, 0), (-1, -1), (0, 0), (1, 1) },
                            _ => throw new ArgumentException(nameof(rotation))
                        };
                    }

                    case Orientation.West:
                    {
                        return rotation switch
                        {
                            Rotation.Clockwise => new() { (-2, 0), (-1, -1), (0, 0), (1, 1) },
                            Rotation.AntiClockwise => new() { (0, 2), (-1, 1), (0, 0), (1, -1) },
                            _ => throw new ArgumentException(nameof(rotation))
                        };
                    }

                    default:
                    {
                        throw new Exception(nameof(Orientation));
                    }
                }
            }

            public static string GetSubBlockString(Orientation orientation, int subBlock, int line, int missingSubBlockMask)
            {
                Trace.Assert(subBlock >= 1 && subBlock <= 4);
                Trace.Assert(line >= 1 && line <= 2);

                switch (orientation)
                {
                    case Orientation.North:
                    {
                        switch (missingSubBlockMask)
                        {
                            case 0b0000:
                            {
                                return new[,]
                                {
                                    { " ╔═╗", " ║ ╚", "════", "═══╗" },
                                    { " ║ ║", " ╚══", "════", "═══╝" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b1000:
                            {
                                return new[,]
                                {
                                    { String.Empty, " ╔══", "════", "═══╗" },
                                    { String.Empty, " ╚══", "════", "═══╝" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b0111:
                            {
                                return new[,]
                                {
                                    { " ╔═╗", String.Empty, String.Empty, String.Empty },
                                    { " ╚═╝", String.Empty, String.Empty, String.Empty }
                                }[line - 1, subBlock - 1];
                            }

                            default:
                            {
                                throw new ArgumentException(nameof(missingSubBlockMask));
                            }
                        }
                    }

                    case Orientation.East:
                    {
                        switch (missingSubBlockMask)
                        {
                            case 0b0000:
                            {
                                return new[,]
                                {
                                    { "═══╗", " ╔══", " ║ ║", " ║ ║" },
                                    { "═══╝", " ║ ╔", " ║ ║", " ╚═╝" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b1100:
                            {
                                return new[,]
                                {
                                    { String.Empty, String.Empty, " ╔═╗", " ║ ║" },
                                    { String.Empty, String.Empty, " ║ ║", " ╚═╝" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b0010:
                            {
                                return new[,]
                                {
                                    { "═══╗", " ╔══", String.Empty, " ╔═╗" },
                                    { "═══╝", " ╚══", String.Empty, " ╚═╝" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b0001:
                            {
                                return new[,]
                                {
                                    { "═══╗", " ╔══", " ║ ║", String.Empty },
                                    { "═══╝", " ║ ╔", " ╚═╝", String.Empty }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b1110:
                            {
                                return new[,]
                                {
                                    { String.Empty, String.Empty, String.Empty, " ╔═╗" },
                                    { String.Empty, String.Empty, String.Empty, " ╚═╝" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b1101:
                            {
                                return new[,]
                                {
                                    { String.Empty, String.Empty, " ╔═╗", String.Empty },
                                    { String.Empty, String.Empty, " ╚═╝", String.Empty }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b0011:
                            {
                                return new[,]
                                {
                                    { "═══╗", " ╔══", String.Empty, String.Empty },
                                    { "═══╝", " ╚══", String.Empty, String.Empty }
                                }[line - 1, subBlock - 1];
                            }

                            default:
                            {
                                throw new ArgumentException(nameof(missingSubBlockMask));
                            }
                        }
                    }

                    case Orientation.South:
                    {
                        switch (missingSubBlockMask)
                        {
                            case 0b0000:
                            {
                                return new[,]
                                {
                                    { " ║ ║", "═══╗", "════", " ╔══" },
                                    { " ╚═╝", "═╗ ║", "════", " ╚══" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b0111:
                            {
                                return new[,]
                                {
                                    { " ╔═╗", String.Empty, String.Empty, String.Empty },
                                    { " ╚═╝", String.Empty, String.Empty, String.Empty }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b1000:
                            {
                                return new[,]
                                {
                                    { String.Empty, "═══╗", "════", " ╔══" },
                                    { String.Empty, "═══╝", "════", " ╚══" }
                                }[line - 1, subBlock - 1];
                            }

                            default:
                            {
                                throw new ArgumentException(nameof(missingSubBlockMask));
                            }
                        }
                    }

                    case Orientation.West:
                    {
                        switch (missingSubBlockMask)
                        {
                            case 0b0000:
                            {
                                return new[,]
                                {
                                    { " ╔══", "═╝ ║", " ║ ║", " ╔═╗" },
                                    { " ╚══", "═══╝", " ║ ║", " ║ ║" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b0001:
                            {
                                return new[,]
                                {
                                    { " ╔══", "═╝ ║", " ╔═╗", String.Empty },
                                    { " ╚══", "═══╝", " ║ ║", String.Empty }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b0010:
                            {
                                return new[,]
                                {
                                    { " ╔══", "═══╗", String.Empty, " ╔═╗" },
                                    { " ╚══", "═══╝", String.Empty, " ╚═╝" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b1100:
                            {
                                return new[,]
                                {
                                    { String.Empty, String.Empty, " ║ ║", " ╔═╗" },
                                    { String.Empty, String.Empty, " ╚═╝", " ║ ║" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b0011:
                            {
                                return new[,]
                                {
                                    { " ╔══", "═══╗", String.Empty, String.Empty },
                                    { " ╚══", "═══╝", String.Empty, String.Empty }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b1101:
                            {
                                return new[,]
                                {
                                    { String.Empty, String.Empty, " ╔═╗", String.Empty },
                                    { String.Empty, String.Empty, " ╚═╝", String.Empty }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b1110:
                            {
                                return new[,]
                                {
                                    { String.Empty, String.Empty, String.Empty, " ╔═╗" },
                                    { String.Empty, String.Empty, String.Empty, " ╚═╝" }
                                }[line - 1, subBlock - 1];
                            }

                            default:
                            {
                                throw new ArgumentException(nameof(missingSubBlockMask));
                            }
                        }
                    }

                    default:
                    {
                        throw new ArgumentException(nameof(orientation));
                    }
                }
            }
        }

        public class BlockL : Block
        {
            public override BlockType BlockType => BlockType.L;

            public static ConsoleColor Color1 => ConsoleColor.White;
            public static ConsoleColor Color2 => ConsoleColor.Gray;

            public override int Height => Orientation switch { Orientation.North => 2, Orientation.East => 3, Orientation.South => 2, Orientation.West => 3, _ => default };
            public override int Width => Orientation switch { Orientation.North => 3, Orientation.East => 2, Orientation.South => 3, Orientation.West => 2, _ => default };

            public BlockL(Matrix matrix) : base(matrix)
            {
            }

            protected override List<(int RowOffset, int ColumnOffset)> GetSpawnPositions()
            {
                return new() { (0, 2), (1, 0), (1, 1), (1, 2) };
            }

            protected override List<(int RowOffset, int ColumnOffset)> GetRotateOffsets(Rotation rotation)
            {
                switch (Orientation)
                {
                    case Orientation.North:
                    {
                        return rotation switch
                        {
                            Rotation.Clockwise => new() { (2, 0), (-1, 1), (0, 0), (1, -1) },
                            Rotation.AntiClockwise => new() { (0, -2), (1, 1), (0, 0), (-1, -1) },
                            _ => throw new ArgumentException(nameof(rotation))
                        };
                    }

                    case Orientation.East:
                    {
                        return rotation switch
                        {
                            Rotation.Clockwise => new() { (0, -2), (1, 1), (0, 0), (-1, -1) },
                            Rotation.AntiClockwise => new() { (-2, 0), (1, -1), (0, 0), (-1, 1) },
                            _ => throw new ArgumentException(nameof(rotation))
                        };
                    }

                    case Orientation.South:
                    {
                        return rotation switch
                        {
                            Rotation.Clockwise => new() { (-2, 0), (1, -1), (0, 0), (-1, 1) },
                            Rotation.AntiClockwise => new() { (0, 2), (-1, -1), (0, 0), (1, 1) },
                            _ => throw new ArgumentException(nameof(rotation))
                        };
                    }

                    case Orientation.West:
                    {
                        return rotation switch
                        {
                            Rotation.Clockwise => new() { (0, 2), (-1, -1), (0, 0), (1, 1) },
                            Rotation.AntiClockwise => new() { (2, 0), (-1, 1), (0, 0), (1, -1) },
                            _ => throw new ArgumentException(nameof(rotation))
                        };
                    }

                    default:
                    {
                        throw new Exception(nameof(Orientation));
                    }
                }
            }

            public static string GetSubBlockString(Orientation orientation, int subBlock, int line, int missingSubBlockMask)
            {
                Trace.Assert(subBlock >= 1 && subBlock <= 4);
                Trace.Assert(line >= 1 && line <= 2);

                switch (orientation)
                {
                    case Orientation.North:
                    {
                        switch (missingSubBlockMask)
                        {
                            case 0b0000:
                            {
                                return new[,]
                                {
                                    { " ╔═╗", " ╔══", "════", "═╝ ║" },
                                    { " ║ ║", " ╚══", "════", "═══╝" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b1000:
                            {
                                return new[,]
                                {
                                    { String.Empty, " ╔══", "════", "═══╗" },
                                    { String.Empty, " ╚══", "════", "═══╝" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b0111:
                            {
                                return new[,]
                                {
                                    { " ╔═╗", String.Empty, String.Empty, String.Empty },
                                    { " ╚═╝", String.Empty, String.Empty, String.Empty }
                                }[line - 1, subBlock - 1];
                            }

                            default:
                            {
                                throw new ArgumentException(nameof(missingSubBlockMask));
                            }
                        }
                    }

                    case Orientation.East:
                    {
                        switch (missingSubBlockMask)
                        {
                            case 0b0000:
                            {
                                return new[,]
                                {
                                    { "═══╗", " ╔═╗", " ║ ║", " ║ ╚" },
                                    { "═══╝", " ║ ║", " ║ ║", " ╚══" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b0100:
                            {
                                return new[,]
                                {
                                    { "═══╗", String.Empty, " ╔═╗", " ║ ╚" },
                                    { "═══╝", String.Empty, " ║ ║", " ╚══" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b0010:
                            {
                                return new[,]
                                {
                                    { "═══╗", " ╔═╗", String.Empty, " ╔══" },
                                    { "═══╝", " ╚═╝", String.Empty, " ╚══" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b1001:
                            {
                                return new[,]
                                {
                                    { String.Empty, " ╔═╗", " ║ ║", String.Empty },
                                    { String.Empty, " ║ ║", " ╚═╝", String.Empty }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b0110:
                            {
                                return new[,]
                                {
                                    { "═══╗", String.Empty, String.Empty, " ╔══" },
                                    { "═══╝", String.Empty, String.Empty, " ╚══" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b1101:
                            {
                                return new[,]
                                {
                                    { String.Empty, String.Empty, " ╔═╗", String.Empty },
                                    { String.Empty, String.Empty, " ╚═╝", String.Empty }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b1011:
                            {
                                return new[,]
                                {
                                    { String.Empty, " ╔═╗", String.Empty, String.Empty },
                                    { String.Empty, " ╚═╝", String.Empty, String.Empty }
                                }[line - 1, subBlock - 1];
                            }

                            default:
                            {
                                throw new ArgumentException(nameof(missingSubBlockMask));
                            }
                        }
                    }

                    case Orientation.South:
                    {
                        switch (missingSubBlockMask)
                        {
                            case 0b0000:
                            {
                                return new[,]
                                {
                                    { " ║ ║", "═══╗", "════", " ╔══" },
                                    { " ╚═╝", "═══╝", "════", " ║ ╔" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b0111:
                            {
                                return new[,]
                                {
                                    { " ╔═╗", String.Empty, String.Empty, String.Empty },
                                    { " ╚═╝", String.Empty, String.Empty, String.Empty }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b1000:
                            {
                                return new[,]
                                {
                                    { String.Empty, "═══╗", "════", " ╔══" },
                                    { String.Empty, "═══╝", "════", " ╚══" }
                                }[line - 1, subBlock - 1];
                            }

                            default:
                            {
                                throw new ArgumentException(nameof(missingSubBlockMask));
                            }
                        }
                    }

                    case Orientation.West:
                    {
                        switch (missingSubBlockMask)
                        {
                            case 0b0000:
                            {
                                return new[,]
                                {
                                    { " ╔══", " ║ ║", " ║ ║", "═══╗" },
                                    { " ╚══", " ╚═╝", " ║ ║", "═╗ ║" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b1001:
                            {
                                return new[,]
                                {
                                    { String.Empty, " ║ ║", " ╔═╗", String.Empty },
                                    { String.Empty, " ╚═╝", " ║ ║", String.Empty }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b0010:
                            {
                                return new[,]
                                {
                                    { " ╔══", " ╔═╗", String.Empty, "═══╗" },
                                    { " ╚══", " ╚═╝", String.Empty, "═══╝" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b0100:
                            {
                                return new[,]
                                {
                                    { " ╔══", String.Empty, " ║ ║", "═══╗" },
                                    { " ╚══", String.Empty, " ╚═╝", "═╗ ║" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b1011:
                            {
                                return new[,]
                                {
                                    { String.Empty, " ╔═╗", String.Empty, String.Empty },
                                    { String.Empty, " ╚═╝", String.Empty, String.Empty }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b1101:
                            {
                                return new[,]
                                {
                                    { String.Empty, String.Empty, " ╔═╗", String.Empty },
                                    { String.Empty, String.Empty, " ╚═╝", String.Empty }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b0110:
                            {
                                return new[,]
                                {
                                    { " ╔══", String.Empty, String.Empty, "═══╗" },
                                    { " ╚══", String.Empty, String.Empty, "═══╝" }
                                }[line - 1, subBlock - 1];
                            }

                            default:
                            {
                                throw new ArgumentException(nameof(missingSubBlockMask));
                            }
                        }
                    }

                    default:
                    {
                        throw new ArgumentException(nameof(orientation));
                    }
                }
            }
        }

        public class BlockO : Block
        {
            public override BlockType BlockType => BlockType.O;

            public static ConsoleColor Color1 => ConsoleColor.Yellow;
            public static ConsoleColor Color2 => ConsoleColor.DarkYellow;

            public override int Height => 2;
            public override int Width => 2;

            public BlockO(Matrix matrix) : base(matrix)
            {
            }

            protected override List<(int RowOffset, int ColumnOffset)> GetSpawnPositions()
            {
                return new() { (0, 0), (0, 1), (1, 0), (1, 1) };
            }

            protected override List<(int RowOffset, int ColumnOffset)> GetRotateOffsets(Rotation rotation)
            {
                switch (Orientation)
                {
                    case Orientation.North:
                    {
                        return rotation switch
                        {
                            Rotation.Clockwise => new() { (0, 1), (1, 0), (-1, 0), (0, -1) },
                            Rotation.AntiClockwise => new() { (1, 0), (0, -1), (0, 1), (-1, 0) },
                            _ => throw new ArgumentException(nameof(rotation))
                        };
                    }

                    case Orientation.East:
                    {
                        return rotation switch
                        {
                            Rotation.Clockwise => new() { (1, 0), (0, -1), (0, 1), (-1, 0) },
                            Rotation.AntiClockwise => new() { (0, -1), (-1, 0), (1, 0), (0, 1) },
                            _ => throw new ArgumentException(nameof(rotation))
                        };
                    }

                    case Orientation.South:
                    {
                        return rotation switch
                        {
                            Rotation.Clockwise => new() { (0, -1), (-1, 0), (1, 0), (0, 1) },
                            Rotation.AntiClockwise => new() { (-1, 0), (0, 1), (0, -1), (1, 0) },
                            _ => throw new ArgumentException(nameof(rotation))
                        };
                    }

                    case Orientation.West:
                    {
                        return rotation switch
                        {
                            Rotation.Clockwise => new() { (-1, 0), (0, 1), (0, -1), (1, 0) },
                            Rotation.AntiClockwise => new() { (0, 1), (1, 0), (-1, 0), (0, -1) },
                            _ => throw new ArgumentException(nameof(rotation))
                        };
                    }

                    default:
                    {
                        throw new Exception(nameof(Orientation));
                    }
                }
            }

            protected override List<(int RowOffset, int ColumnOffset)> GetRotateWallKickOffsets(Rotation rotation)
            {
                return new() { (0, 0) };
            }

            public static string GetSubBlockString(Orientation orientation, int subBlock, int line, int missingSubBlockMask)
            {
                Trace.Assert(subBlock >= 1 && subBlock <= 4);
                Trace.Assert(line >= 1 && line <= 2);

                switch (orientation)
                {
                    case Orientation.North:
                    {
                        switch (missingSubBlockMask)
                        {
                            case 0b0000:
                            {
                                return new[,]
                                {
                                    { " ╔══", "═══╗", " ║  ", "   ║" },
                                    { " ║  ", "   ║", " ╚══", "═══╝" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b1100:
                            {
                                return new[,]
                                {
                                    { String.Empty, String.Empty, " ╔══", "═══╗" },
                                    { String.Empty, String.Empty, " ╚══", "═══╝" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b0011:
                            {
                                return new[,]
                                {
                                    { " ╔══", "═══╗", String.Empty, String.Empty },
                                    { " ╚══", "═══╝", String.Empty, String.Empty }
                                }[line - 1, subBlock - 1];
                            }

                            default:
                            {
                                throw new ArgumentException(nameof(missingSubBlockMask));
                            }
                        }
                    }

                    case Orientation.East:
                    {
                        switch (missingSubBlockMask)
                        {
                            case 0b0000:
                            {
                                return new[,]
                                {
                                    { "═══╗", "   ║", " ╔══", " ║  " },
                                    { "   ║", "═══╝", " ║  ", " ╚══" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b1010:
                            {
                                return new[,]
                                {
                                    { String.Empty, "═══╗", String.Empty, " ╔══" },
                                    { String.Empty, "═══╝", String.Empty, " ╚══" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b0101:
                            {
                                return new[,]
                                {
                                    { "═══╗", String.Empty, " ╔══", String.Empty },
                                    { "═══╝", String.Empty, " ╚══", String.Empty }
                                }[line - 1, subBlock - 1];
                            }

                            default:
                            {
                                throw new ArgumentException(nameof(missingSubBlockMask));
                            }
                        }
                    }

                    case Orientation.South:
                    {
                        switch (missingSubBlockMask)
                        {
                            case 0b0000:
                            {
                                return new[,]
                                {
                                    { "   ║", " ║  ", "═══╗", " ╔══" },
                                    { "═══╝", " ╚══", "   ║", " ║  " }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b0011:
                            {
                                return new[,]
                                {
                                    { "═══╗", " ╔══", String.Empty, String.Empty },
                                    { "═══╝", " ╚══", String.Empty, String.Empty }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b1100:
                            {
                                return new[,]
                                {
                                    { String.Empty, String.Empty, "═══╗", " ╔══" },
                                    { String.Empty, String.Empty, "═══╝", " ╚══" }
                                }[line - 1, subBlock - 1];
                            }

                            default:
                            {
                                throw new ArgumentException(nameof(missingSubBlockMask));
                            }
                        }
                    }

                    case Orientation.West:
                    {
                        switch (missingSubBlockMask)
                        {
                            case 0b0000:
                            {
                                return new[,]
                                {
                                    { " ║  ", " ╔══", "   ║", "═══╗" },
                                    { " ╚══", " ║  ", "═══╝", "   ║" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b0101:
                            {
                                return new[,]
                                {
                                    { " ╔══", String.Empty, "═══╗", String.Empty },
                                    { " ╚══", String.Empty, "═══╝", String.Empty }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b1010:
                            {
                                return new[,]
                                {
                                    { String.Empty, " ╔══", String.Empty, "═══╗" },
                                    { String.Empty, " ╚══", String.Empty, "═══╝" }
                                }[line - 1, subBlock - 1];
                            }

                            default:
                            {
                                throw new ArgumentException(nameof(missingSubBlockMask));
                            }
                        }
                    }

                    default:
                    {
                        throw new ArgumentException(nameof(orientation));
                    }
                }
            }
        }

        public class BlockS : Block
        {
            public override BlockType BlockType => BlockType.S;

            public static ConsoleColor Color1 => ConsoleColor.Green;
            public static ConsoleColor Color2 => ConsoleColor.DarkGreen;

            public override int Height => Orientation switch { Orientation.North => 2, Orientation.East => 3, Orientation.South => 2, Orientation.West => 3, _ => default };
            public override int Width => Orientation switch { Orientation.North => 3, Orientation.East => 2, Orientation.South => 3, Orientation.West => 2, _ => default };

            public BlockS(Matrix matrix) : base(matrix)
            {
            }

            protected override List<(int RowOffset, int ColumnOffset)> GetSpawnPositions()
            {
                return new() { (0, 1), (0, 2), (1, 0), (1, 1) };
            }

            protected override List<(int RowOffset, int ColumnOffset)> GetRotateOffsets(Rotation rotation)
            {
                switch (Orientation)
                {
                    case Orientation.North:
                    {
                        return rotation switch
                        {
                            Rotation.Clockwise => new() { (1, 1), (2, 0), (-1, 1), (0, 0) },
                            Rotation.AntiClockwise => new() { (1, -1), (0, -2), (1, 1), (0, 0) },
                            _ => throw new ArgumentException(nameof(rotation))
                        };
                    }

                    case Orientation.East:
                    {
                        return rotation switch
                        {
                            Rotation.Clockwise => new() { (1, -1), (0, -2), (1, 1), (0, 0) },
                            Rotation.AntiClockwise => new() { (-1, -1), (-2, 0), (1, -1), (0, 0) },
                            _ => throw new ArgumentException(nameof(rotation))
                        };
                    }

                    case Orientation.South:
                    {
                        return rotation switch
                        {
                            Rotation.Clockwise => new() { (-1, -1), (-2, 0), (1, -1), (0, 0) },
                            Rotation.AntiClockwise => new() { (-1, 1), (0, 2), (-1, -1), (0, 0) },
                            _ => throw new ArgumentException(nameof(rotation))
                        };
                    }

                    case Orientation.West:
                    {
                        return rotation switch
                        {
                            Rotation.Clockwise => new() { (-1, 1), (0, 2), (-1, -1), (0, 0) },
                            Rotation.AntiClockwise => new() { (1, 1), (2, 0), (-1, 1), (0, 0) },
                            _ => throw new ArgumentException(nameof(rotation))
                        };
                    }

                    default:
                    {
                        throw new Exception(nameof(Orientation));
                    }
                }
            }

            public static string GetSubBlockString(Orientation orientation, int subBlock, int line, int missingSubBlockMask)
            {
                Trace.Assert(subBlock >= 1 && subBlock <= 4);
                Trace.Assert(line >= 1 && line <= 2);

                switch (orientation)
                {
                    case Orientation.North:
                    {
                        switch (missingSubBlockMask)
                        {
                            case 0b0000:
                            {
                                return new[,]
                                {
                                    { " ╔══", "═══╗", " ╔══", "═╝ ║" },
                                    { " ║ ╔", "═══╝", " ╚══", "═══╝" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b1100:
                            {
                                return new[,]
                                {
                                    { String.Empty, String.Empty, " ╔══", "═══╗" },
                                    { String.Empty, String.Empty, " ╚══", "═══╝" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b0011:
                            {
                                return new[,]
                                {
                                    { " ╔══", "═══╗", String.Empty, String.Empty },
                                    { " ╚══", "═══╝", String.Empty, String.Empty }
                                }[line - 1, subBlock - 1];
                            }

                            default:
                            {
                                throw new ArgumentException(nameof(missingSubBlockMask));
                            }
                        }
                    }

                    case Orientation.East:
                    {
                        switch (missingSubBlockMask)
                        {
                            case 0b0000:
                            {
                                return new[,]
                                {
                                    { "═══╗", " ║ ║", " ╔═╗", " ║ ╚" },
                                    { "═╗ ║", " ╚═╝", " ║ ║", " ╚══" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b0010:
                            {
                                return new[,]
                                {
                                    { "═══╗", " ║ ║", String.Empty, " ╔══" },
                                    { "═╗ ║", " ╚═╝", String.Empty, " ╚══" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b1001:
                            {
                                return new[,]
                                {
                                    { String.Empty, " ╔═╗", " ╔═╗", String.Empty },
                                    { String.Empty, " ╚═╝", " ╚═╝", String.Empty }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b0100:
                            {
                                return new[,]
                                {
                                    { "═══╗", String.Empty, " ╔═╗", " ║ ╚" },
                                    { "═══╝", String.Empty, " ║ ║", " ╚══" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b1011:
                            {
                                return new[,]
                                {
                                    { String.Empty, " ╔═╗", String.Empty, String.Empty },
                                    { String.Empty, " ╚═╝", String.Empty, String.Empty }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b0110:
                            {
                                return new[,]
                                {
                                    { "═══╗", String.Empty, String.Empty, " ╔══" },
                                    { "═══╝", String.Empty, String.Empty, " ╚══" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b1101:
                            {
                                return new[,]
                                {
                                    { String.Empty, String.Empty, " ╔═╗", String.Empty },
                                    { String.Empty, String.Empty, " ╚═╝", String.Empty }
                                }[line - 1, subBlock - 1];
                            }

                            default:
                            {
                                throw new ArgumentException(nameof(missingSubBlockMask));
                            }
                        }
                    }

                    case Orientation.South:
                    {
                        switch (missingSubBlockMask)
                        {
                            case 0b0000:
                            {
                                return new[,]
                                {
                                    { "═╝ ║", " ╔══", "═══╗", " ╔══" },
                                    { "═══╝", " ╚══", "═══╝", " ║ ╔" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b0011:
                            {
                                return new[,]
                                {
                                    { "═══╗", " ╔══", String.Empty, String.Empty },
                                    { "═══╝", " ╚══", String.Empty, String.Empty }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b1100:
                            {
                                return new[,]
                                {
                                    { String.Empty, String.Empty, "═══╗", " ╔══" },
                                    { String.Empty, String.Empty, "═══╝", " ╚══" }
                                }[line - 1, subBlock - 1];
                            }

                            default:
                            {
                                throw new ArgumentException(nameof(missingSubBlockMask));
                            }
                        }
                    }

                    case Orientation.West:
                    {
                        switch (missingSubBlockMask)
                        {
                            case 0b0000:
                            {
                                return new[,]
                                {
                                    { " ║ ╚", " ╔═╗", " ║ ║", "═══╗" },
                                    { " ╚══", " ║ ║", " ╚═╝", "═╗ ║" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b0100:
                            {
                                return new[,]
                                {
                                    { " ╔══", String.Empty, " ║ ║", "═══╗" },
                                    { " ╚══", String.Empty, " ╚═╝", "═╗ ║" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b1001:
                            {
                                return new[,]
                                {
                                    { String.Empty, " ╔═╗", " ╔═╗", String.Empty },
                                    { String.Empty, " ╚═╝", " ╚═╝", String.Empty }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b0010:
                            {
                                return new[,]
                                {
                                    { " ║ ╚", " ╔═╗", String.Empty, "═══╗" },
                                    { " ╚══", " ║ ║", String.Empty, "═══╝" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b1101:
                            {
                                return new[,]
                                {
                                    { String.Empty, String.Empty, " ╔═╗", String.Empty },
                                    { String.Empty, String.Empty, " ╚═╝", String.Empty }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b0110:
                            {
                                return new[,]
                                {
                                    { " ╔══", String.Empty, String.Empty, "═══╗" },
                                    { " ╚══", String.Empty, String.Empty, "═══╝" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b1011:
                            {
                                return new[,]
                                {
                                    { String.Empty, " ╔═╗", String.Empty, String.Empty },
                                    { String.Empty, " ╚═╝", String.Empty, String.Empty }
                                }[line - 1, subBlock - 1];
                            }

                            default:
                            {
                                throw new ArgumentException(nameof(missingSubBlockMask));
                            }
                        }
                    }

                    default:
                    {
                        throw new ArgumentException(nameof(orientation));
                    }
                }
            }
        }

        public class BlockT : Block
        {
            public override BlockType BlockType => BlockType.T;

            public static ConsoleColor Color1 => ConsoleColor.Magenta;
            public static ConsoleColor Color2 => ConsoleColor.DarkMagenta;

            public override int Height => Orientation switch { Orientation.North => 2, Orientation.East => 3, Orientation.South => 2, Orientation.West => 3, _ => default };
            public override int Width => Orientation switch { Orientation.North => 3, Orientation.East => 2, Orientation.South => 3, Orientation.West => 2, _ => default };

            public BlockT(Matrix matrix) : base(matrix)
            {
            }

            protected override List<(int RowOffset, int ColumnOffset)> GetSpawnPositions()
            {
                return new() { (0, 1), (1, 0), (1, 1), (1, 2) };
            }

            protected override List<(int RowOffset, int ColumnOffset)> GetRotateOffsets(Rotation rotation)
            {
                switch (Orientation)
                {
                    case Orientation.North:
                    {
                        return rotation switch
                        {
                            Rotation.Clockwise => new() { (1, 1), (-1, 1), (0, 0), (1, -1) },
                            Rotation.AntiClockwise => new() { (1, -1), (1, 1), (0, 0), (-1, -1) },
                            _ => throw new ArgumentException(nameof(rotation))
                        };
                    }

                    case Orientation.East:
                    {
                        return rotation switch
                        {
                            Rotation.Clockwise => new() { (1, -1), (1, 1), (0, 0), (-1, -1) },
                            Rotation.AntiClockwise => new() { (-1, -1), (1, -1), (0, 0), (-1, 1) },
                            _ => throw new ArgumentException(nameof(rotation))
                        };
                    }

                    case Orientation.South:
                    {
                        return rotation switch
                        {
                            Rotation.Clockwise => new() { (-1, -1), (1, -1), (0, 0), (-1, 1) },
                            Rotation.AntiClockwise => new() { (-1, 1), (-1, -1), (0, 0), (1, 1) },
                            _ => throw new ArgumentException(nameof(rotation))
                        };
                    }

                    case Orientation.West:
                    {
                        return rotation switch
                        {
                            Rotation.Clockwise => new() { (-1, 1), (-1, -1), (0, 0), (1, 1) },
                            Rotation.AntiClockwise => new() { (1, 1), (-1, 1), (0, 0), (1, -1) },
                            _ => throw new ArgumentException(nameof(rotation))
                        };
                    }

                    default:
                    {
                        throw new Exception(nameof(Orientation));
                    }
                }
            }

            public static string GetSubBlockString(Orientation orientation, int subBlock, int line, int missingSubBlockMask)
            {
                Trace.Assert(subBlock >= 1 && subBlock <= 4);
                Trace.Assert(line >= 1 && line <= 2);

                switch (orientation)
                {
                    case Orientation.North:
                    {
                        switch (missingSubBlockMask)
                        {
                            case 0b0000:
                            {
                                return new[,]
                                {
                                    { " ╔═╗", " ╔══", "═╝ ╚", "═══╗" },
                                    { " ║ ║", " ╚══", "════", "═══╝" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b1000:
                            {
                                return new[,]
                                {
                                    { String.Empty, " ╔══", "════", "═══╗" },
                                    { String.Empty, " ╚══", "════", "═══╝" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b0111:
                            {
                                return new[,]
                                {
                                    { " ╔═╗", String.Empty, String.Empty, String.Empty },
                                    { " ╚═╝", String.Empty, String.Empty, String.Empty }
                                }[line - 1, subBlock - 1];
                            }

                            default:
                            {
                                throw new ArgumentException(nameof(missingSubBlockMask));
                            }
                        }
                    }

                    case Orientation.East:
                    {
                        switch (missingSubBlockMask)
                        {
                            case 0b0000:
                            {
                                return new[,]
                                {
                                    { "═══╗", " ╔═╗", " ║ ╚", " ║ ║" },
                                    { "═══╝", " ║ ║", " ║ ╔", " ╚═╝" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b0100:
                            {
                                return new[,]
                                {
                                    { "═══╗", String.Empty, " ╔══", " ║ ║" },
                                    { "═══╝", String.Empty, " ║ ╔", " ╚═╝" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b1010:
                            {
                                return new[,]
                                {
                                    { String.Empty, " ╔═╗", String.Empty, " ╔═╗" },
                                    { String.Empty, " ╚═╝", String.Empty, " ╚═╝" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b0001:
                            {
                                return new[,]
                                {
                                    { "═══╗", " ╔═╗", " ║ ╚", String.Empty },
                                    { "═══╝", " ║ ║", " ╚══", String.Empty }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b1110:
                            {
                                return new[,]
                                {
                                    { String.Empty, String.Empty, String.Empty, " ╔═╗" },
                                    { String.Empty, String.Empty, String.Empty, " ╚═╝" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b0101:
                            {
                                return new[,]
                                {
                                    { "═══╗", String.Empty, " ╔══", String.Empty },
                                    { "═══╝", String.Empty, " ╚══", String.Empty }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b1011:
                            {
                                return new[,]
                                {
                                    { String.Empty, " ╔═╗", String.Empty, String.Empty },
                                    { String.Empty, " ╚═╝", String.Empty, String.Empty }
                                }[line - 1, subBlock - 1];
                            }

                            default:
                            {
                                throw new ArgumentException(nameof(missingSubBlockMask));
                            }
                        }
                    }

                    case Orientation.South:
                    {
                        switch (missingSubBlockMask)
                        {
                            case 0b0000:
                            {
                                return new[,]
                                {
                                    { " ║ ║", "═══╗", "════", " ╔══" },
                                    { " ╚═╝", "═══╝", "═╗ ╔", " ╚══" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b0111:
                            {
                                return new[,]
                                {
                                    { " ╔═╗", String.Empty, String.Empty, String.Empty },
                                    { " ╚═╝", String.Empty, String.Empty, String.Empty }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b1000:
                            {
                                return new[,]
                                {
                                    { String.Empty, "═══╗", "════", " ╔══" },
                                    { String.Empty, "═══╝", "════", " ╚══" }
                                }[line - 1, subBlock - 1];
                            }

                            default:
                            {
                                throw new ArgumentException(nameof(missingSubBlockMask));
                            }
                        }
                    }

                    case Orientation.West:
                    {
                        switch (missingSubBlockMask)
                        {
                            case 0b0000:
                            {
                                return new[,]
                                {
                                    { " ╔══", " ║ ║", "═╝ ║", " ╔═╗" },
                                    { " ╚══", " ╚═╝", "═╗ ║", " ║ ║" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b0001:
                            {
                                return new[,]
                                {
                                    { " ╔══", " ║ ║", "═══╗", String.Empty },
                                    { " ╚══", " ╚═╝", "═╗ ║", String.Empty }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b1010:
                            {
                                return new[,]
                                {
                                    { String.Empty, " ╔═╗", String.Empty, " ╔═╗" },
                                    { String.Empty, " ╚═╝", String.Empty, " ╚═╝" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b0100:
                            {
                                return new[,]
                                {
                                    { " ╔══", String.Empty, "═╝ ║", " ╔═╗" },
                                    { " ╚══", String.Empty, "═══╝", " ║ ║" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b1011:
                            {
                                return new[,]
                                {
                                    { String.Empty, " ╔═╗", String.Empty, String.Empty },
                                    { String.Empty, " ╚═╝", String.Empty, String.Empty }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b0101:
                            {
                                return new[,]
                                {
                                    { " ╔══", String.Empty, "═══╗", String.Empty },
                                    { " ╚══", String.Empty, "═══╝", String.Empty }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b1110:
                            {
                                return new[,]
                                {
                                    { String.Empty, String.Empty, String.Empty, " ╔═╗" },
                                    { String.Empty, String.Empty, String.Empty, " ╚═╝" }
                                }[line - 1, subBlock - 1];
                            }

                            default:
                            {
                                throw new ArgumentException(nameof(missingSubBlockMask));
                            }
                        }
                    }

                    default:
                    {
                        throw new ArgumentException(nameof(orientation));
                    }
                }
            }
        }

        public class BlockZ : Block
        {
            public override BlockType BlockType => BlockType.Z;

            public static ConsoleColor Color1 => ConsoleColor.Red;
            public static ConsoleColor Color2 => ConsoleColor.DarkRed;

            public override int Height => Orientation switch { Orientation.North => 2, Orientation.East => 3, Orientation.South => 2, Orientation.West => 3, _ => default };
            public override int Width => Orientation switch { Orientation.North => 3, Orientation.East => 2, Orientation.South => 3, Orientation.West => 2, _ => default };

            public BlockZ(Matrix matrix) : base(matrix)
            {
            }

            protected override List<(int RowOffset, int ColumnOffset)> GetSpawnPositions()
            {
                return new() { (0, 0), (0, 1), (1, 1), (1, 2) };
            }

            protected override List<(int RowOffset, int ColumnOffset)> GetRotateOffsets(Rotation rotation)
            {
                switch (Orientation)
                {
                    case Orientation.North:
                    {
                        return rotation switch
                        {
                            Rotation.Clockwise => new() { (0, 2), (1, 1), (0, 0), (1, -1) },
                            Rotation.AntiClockwise => new() { (2, 0), (1, -1), (0, 0), (-1, -1) },
                            _ => throw new ArgumentException(nameof(rotation))
                        };
                    }

                    case Orientation.East:
                    {
                        return rotation switch
                        {
                            Rotation.Clockwise => new() { (2, 0), (1, -1), (0, 0), (-1, -1) },
                            Rotation.AntiClockwise => new() { (0, -2), (-1, -1), (0, 0), (-1, 1) },
                            _ => throw new ArgumentException(nameof(rotation))
                        };
                    }

                    case Orientation.South:
                    {
                        return rotation switch
                        {
                            Rotation.Clockwise => new() { (0, -2), (-1, -1), (0, 0), (-1, 1) },
                            Rotation.AntiClockwise => new() { (-2, 0), (-1, 1), (0, 0), (1, 1) },
                            _ => throw new ArgumentException(nameof(rotation))
                        };
                    }

                    case Orientation.West:
                    {
                        return rotation switch
                        {
                            Rotation.Clockwise => new() { (-2, 0), (-1, 1), (0, 0), (1, 1) },
                            Rotation.AntiClockwise => new() { (0, 2), (1, 1), (0, 0), (1, -1) },
                            _ => throw new ArgumentException(nameof(rotation))
                        };
                    }

                    default:
                    {
                        throw new Exception(nameof(Orientation));
                    }
                }
            }

            public static string GetSubBlockString(Orientation orientation, int subBlock, int line, int missingSubBlockMask)
            {
                Trace.Assert(subBlock >= 1 && subBlock <= 4);
                Trace.Assert(line >= 1 && line <= 2);

                switch (orientation)
                {
                    case Orientation.North:
                    {
                        switch (missingSubBlockMask)
                        {
                            case 0b0000:
                            {
                                return new[,]
                                {
                                    { " ╔══", "═══╗", " ║ ╚", "═══╗" },
                                    { " ╚══", "═╗ ║", " ╚══", "═══╝" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b1100:
                            {
                                return new[,]
                                {
                                    { String.Empty, String.Empty, " ╔══", "═══╗" },
                                    { String.Empty, String.Empty, " ╚══", "═══╝" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b0011:
                            {
                                return new[,]
                                {
                                    { " ╔══", "═══╗", String.Empty, String.Empty },
                                    { " ╚══", "═══╝", String.Empty, String.Empty }
                                }[line - 1, subBlock - 1];
                            }

                            default:
                            {
                                throw new ArgumentException(nameof(missingSubBlockMask));
                            }
                        }
                    }

                    case Orientation.East:
                    {
                        switch (missingSubBlockMask)
                        {
                            case 0b0000:
                            {
                                return new[,]
                                {
                                    { " ╔═╗", "═╝ ║", " ╔══", " ║ ║" },
                                    { " ║ ║", "═══╝", " ║ ╔", " ╚═╝" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b1000:
                            {
                                return new[,]
                                {
                                    { String.Empty, "═══╗", " ╔══", " ║ ║" },
                                    { String.Empty, "═══╝", " ║ ╔", " ╚═╝" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b0110:
                            {
                                return new[,]
                                {
                                    { " ╔═╗", String.Empty, String.Empty, " ╔═╗" },
                                    { " ╚═╝", String.Empty, String.Empty, " ╚═╝" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b0001:
                            {
                                return new[,]
                                {
                                    { " ╔═╗", "═╝ ║", " ╔══", String.Empty },
                                    { " ║ ║", "═══╝", " ╚══", String.Empty }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b1110:
                            {
                                return new[,]
                                {
                                    { String.Empty, String.Empty, String.Empty, " ╔═╗" },
                                    { String.Empty, String.Empty, String.Empty, " ╚═╝" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b1001:
                            {
                                return new[,]
                                {
                                    { String.Empty, "═══╗", " ╔══", String.Empty },
                                    { String.Empty, "═══╝", " ╚══", String.Empty }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b0111:
                            {
                                return new[,]
                                {
                                    { " ╔═╗", String.Empty, String.Empty, String.Empty },
                                    { " ╚═╝", String.Empty, String.Empty, String.Empty }
                                }[line - 1, subBlock - 1];
                            }

                            default:
                            {
                                throw new ArgumentException(nameof(missingSubBlockMask));
                            }
                        }
                    }

                    case Orientation.South:
                    {
                        switch (missingSubBlockMask)
                        {
                            case 0b0000:
                            {
                                return new[,]
                                {
                                    { "═══╗", " ║ ╚", "═══╗", " ╔══" },
                                    { "═══╝", " ╚══", "═╗ ║", " ╚══" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b0011:
                            {
                                return new[,]
                                {
                                    { "═══╗", " ╔══", String.Empty, String.Empty },
                                    { "═══╝", " ╚══", String.Empty, String.Empty }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b1100:
                            {
                                return new[,]
                                {
                                    { String.Empty, String.Empty, "═══╗", " ╔══" },
                                    { String.Empty, String.Empty, "═══╝", " ╚══" }
                                }[line - 1, subBlock - 1];
                            }

                            default:
                            {
                                throw new ArgumentException(nameof(missingSubBlockMask));
                            }
                        }
                    }

                    case Orientation.West:
                    {
                        switch (missingSubBlockMask)
                        {
                            case 0b0000:
                            {
                                return new[,]
                                {
                                    { " ║ ║", " ╔══", "═╝ ║", " ╔═╗" },
                                    { " ╚═╝", " ║ ╔", "═══╝", " ║ ║" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b0001:
                            {
                                return new[,]
                                {
                                    { " ║ ║", " ╔══", "═══╗", String.Empty },
                                    { " ╚═╝", " ║ ╔", "═══╝", String.Empty }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b0110:
                            {
                                return new[,]
                                {
                                    { " ╔═╗", String.Empty, String.Empty, " ╔═╗" },
                                    { " ╚═╝", String.Empty, String.Empty, " ╚═╝" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b1000:
                            {
                                return new[,]
                                {
                                    { String.Empty, " ╔══", "═╝ ║", " ╔═╗" },
                                    { String.Empty, " ╚══", "═══╝", " ║ ║" }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b0111:
                            {
                                return new[,]
                                {
                                    { " ╔═╗", String.Empty, String.Empty, String.Empty },
                                    { " ╚═╝", String.Empty, String.Empty, String.Empty }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b1001:
                            {
                                return new[,]
                                {
                                    { String.Empty, " ╔══", "═══╗", String.Empty },
                                    { String.Empty, " ╚══", "═══╝", String.Empty }
                                }[line - 1, subBlock - 1];
                            }

                            case 0b1110:
                            {
                                return new[,]
                                {
                                    { String.Empty, String.Empty, String.Empty, " ╔═╗" },
                                    { String.Empty, String.Empty, String.Empty, " ╚═╝" }
                                }[line - 1, subBlock - 1];
                            }

                            default:
                            {
                                throw new ArgumentException(nameof(missingSubBlockMask));
                            }
                        }
                    }

                    default:
                    {
                        throw new ArgumentException(nameof(orientation));
                    }
                }
            }
        }

        public abstract class BlockGhost
        {
            public virtual BlockType BlockType { get; }

            public List<Position> Positions { get; private set; }

            private Matrix _matrix;

            protected BlockGhost(Matrix matrix)
            {
                Positions = new();

                _matrix = matrix;
            }

            public bool TryGhost(Block block)
            {
                lock (Positions)
                {
                    foreach (Position position in Positions)
                    {
                        if (_matrix[position.Row, position.Column].BlockType < BlockType.Empty)
                        {
                            _matrix[position.Row, position.Column].Set();
                        }
                    }

                    Positions.Clear();

                    foreach (Position position in block.Positions)
                    {
                        Positions.Add(new(position));
                    }

                    int count = 0;

                    for (int i = 1; i <= block.Height; i++)
                    {
                        if (TryTranslateShiftDown())
                        {
                            count++;
                        }
                    }

                    if (count == block.Height)
                    {
                        while (TryTranslateShiftDown());

                        int subBlock = 1;

                        foreach (Position position in Positions)
                        {
                            _matrix[position.Row, position.Column].Set(BlockType, block.Orientation, subBlock++);
                        }

                        return true;
                    }
                    else
                    {
                        Positions.Clear();

                        return false;
                    }
                }
            }

            private bool TryTranslateShiftDown()
            {
                int count = Positions.Count;

                foreach (Position position in Positions)
                {
                    if (position.Row + 1 < _matrix.MaxRows && Positions.Contains(new(position.Row + 1, position.Column, _matrix.MaxRows, _matrix.MaxColumns)))
                    {
                        continue;
                    }

                    if (position.Row + 1 >= _matrix.MaxRows || _matrix[position.Row + 1, position.Column].BlockType > BlockType.Empty)
                    {
                        count--;
                    }
                }

                if (count == Positions.Count)
                {
                    foreach (Position position in Positions)
                    {
                        position.Row++;
                    }

                    return true;
                }
                else
                {
                    return false;
                }
            }

            public void Clear()
            {
                lock (Positions)
                {
                    foreach (Position position in Positions)
                    {
                        if (_matrix[position.Row, position.Column].BlockType < BlockType.Empty)
                        {
                            _matrix[position.Row, position.Column].Set();
                        }
                    }

                    Positions.Clear();
                }
            }
        }

        public class BlockIGhost : BlockGhost
        {
            public override BlockType BlockType => BlockType.IGhost;

            public static ConsoleColor Color => ConsoleColor.DarkCyan;

            public BlockIGhost(Matrix matrix) : base(matrix)
            {
            }

            public static string GetSubBlockString(Orientation orientation, int subBlock, int line)
            {
                Trace.Assert(subBlock >= 1 && subBlock <= 4);
                Trace.Assert(line >= 1 && line <= 2);

                switch (orientation)
                {
                    case Orientation.North:
                    {
                        return new[,]
                        {
                            { " ┌──", "────", "────", "───┐" },
                            { " └──", "────", "────", "───┘" }
                        }[line - 1, subBlock - 1];
                    }

                    case Orientation.East:
                    {
                        return new[,]
                        {
                            { " ┌─┐", " │ │", " │ │", " │ │" },
                            { " │ │", " │ │", " │ │", " └─┘" }
                        }[line - 1, subBlock - 1];
                    }

                    case Orientation.South:
                    {
                        return new[,]
                        {
                            { "───┐", "────", "────", " ┌──" },
                            { "───┘", "────", "────", " └──" }
                        }[line - 1, subBlock - 1];
                    }

                    case Orientation.West:
                    {
                        return new[,]
                        {
                            { " │ │", " │ │", " │ │", " ┌─┐" },
                            { " └─┘", " │ │", " │ │", " │ │" }
                        }[line - 1, subBlock - 1];
                    }

                    default:
                    {
                        throw new ArgumentException(nameof(orientation));
                    }
                }
            }
        }

        public class BlockJGhost : BlockGhost
        {
            public override BlockType BlockType => BlockType.JGhost;

            public static ConsoleColor Color => ConsoleColor.DarkBlue;

            public BlockJGhost(Matrix matrix) : base(matrix)
            {
            }

            public static string GetSubBlockString(Orientation orientation, int subBlock, int line)
            {
                Trace.Assert(subBlock >= 1 && subBlock <= 4);
                Trace.Assert(line >= 1 && line <= 2);

                switch (orientation)
                {
                    case Orientation.North:
                    {
                        return new[,]
                        {
                            { " ┌─┐", " │ └", "────", "───┐" },
                            { " │ │", " └──", "────", "───┘" }
                        }[line - 1, subBlock - 1];
                    }

                    case Orientation.East:
                    {
                        return new[,]
                        {
                            { "───┐", " ┌──", " │ │", " │ │" },
                            { "───┘", " │ ┌", " │ │", " └─┘" }
                        }[line - 1, subBlock - 1];
                    }

                    case Orientation.South:
                    {
                        return new[,]
                        {
                            { " │ │", "───┐", "────", " ┌──" },
                            { " └─┘", "─┐ │", "────", " └──" }
                        }[line - 1, subBlock - 1];
                    }

                    case Orientation.West:
                    {
                        return new[,]
                        {
                            { " ┌──", "─┘ │", " │ │", " ┌─┐" },
                            { " └──", "───┘", " │ │", " │ │" }
                        }[line - 1, subBlock - 1];
                    }

                    default:
                    {
                        throw new ArgumentException(nameof(orientation));
                    }
                }
            }
        }

        public class BlockLGhost : BlockGhost
        {
            public override BlockType BlockType => BlockType.LGhost;

            public static ConsoleColor Color => ConsoleColor.Gray;

            public BlockLGhost(Matrix matrix) : base(matrix)
            {
            }

            public static string GetSubBlockString(Orientation orientation, int subBlock, int line)
            {
                Trace.Assert(subBlock >= 1 && subBlock <= 4);
                Trace.Assert(line >= 1 && line <= 2);

                switch (orientation)
                {
                    case Orientation.North:
                    {
                        return new[,]
                        {
                            { " ┌─┐", " ┌──", "────", "─┘ │" },
                            { " │ │", " └──", "────", "───┘" }
                        }[line - 1, subBlock - 1];
                    }

                    case Orientation.East:
                    {
                        return new[,]
                        {
                            { "───┐", " ┌─┐", " │ │", " │ └" },
                            { "───┘", " │ │", " │ │", " └──" }
                        }[line - 1, subBlock - 1];
                    }

                    case Orientation.South:
                    {
                        return new[,]
                        {
                            { " │ │", "───┐", "────", " ┌──" },
                            { " └─┘", "───┘", "────", " │ ┌" }
                        }[line - 1, subBlock - 1];
                    }

                    case Orientation.West:
                    {
                        return new[,]
                        {
                            { " ┌──", " │ │", " │ │", "───┐" },
                            { " └──", " └─┘", " │ │", "─┐ │" }
                        }[line - 1, subBlock - 1];
                    }

                    default:
                    {
                        throw new ArgumentException(nameof(orientation));
                    }
                }
            }
        }

        public class BlockOGhost : BlockGhost
        {
            public override BlockType BlockType => BlockType.OGhost;

            public static ConsoleColor Color => ConsoleColor.DarkYellow;

            public BlockOGhost(Matrix matrix) : base(matrix)
            {
            }

            public static string GetSubBlockString(Orientation orientation, int subBlock, int line)
            {
                Trace.Assert(subBlock >= 1 && subBlock <= 4);
                Trace.Assert(line >= 1 && line <= 2);

                switch (orientation)
                {
                    case Orientation.North:
                    {
                        return new[,]
                        {
                            { " ┌──", "───┐", " │  ", "   │" },
                            { " │  ", "   │", " └──", "───┘" }
                        }[line - 1, subBlock - 1];
                    }

                    case Orientation.East:
                    {
                        return new[,]
                        {
                            { "───┐", "   │", " ┌──", " │  " },
                            { "   │", "───┘", " │  ", " └──" }
                        }[line - 1, subBlock - 1];
                    }

                    case Orientation.South:
                    {
                        return new[,]
                        {
                            { "   │", " │  ", "───┐", " ┌──" },
                            { "───┘", " └──", "   │", " │  " }
                        }[line - 1, subBlock - 1];
                    }

                    case Orientation.West:
                    {
                        return new[,]
                        {
                            { " │  ", " ┌──", "   │", "───┐" },
                            { " └──", " │  ", "───┘", "   │" }
                        }[line - 1, subBlock - 1];
                    }

                    default:
                    {
                        throw new ArgumentException(nameof(orientation));
                    }
                }
            }
        }

        public class BlockSGhost : BlockGhost
        {
            public override BlockType BlockType => BlockType.SGhost;

            public static ConsoleColor Color => ConsoleColor.DarkGreen;

            public BlockSGhost(Matrix matrix) : base(matrix)
            {
            }

            public static string GetSubBlockString(Orientation orientation, int subBlock, int line)
            {
                Trace.Assert(subBlock >= 1 && subBlock <= 4);
                Trace.Assert(line >= 1 && line <= 2);

                switch (orientation)
                {
                    case Orientation.North:
                    {
                        return new[,]
                        {
                            { " ┌──", "───┐", " ┌──", "─┘ │" },
                            { " │ ┌", "───┘", " └──", "───┘" }
                        }[line - 1, subBlock - 1];
                    }

                    case Orientation.East:
                    {
                        return new[,]
                        {
                            { "───┐", " │ │", " ┌─┐", " │ └" },
                            { "─┐ │", " └─┘", " │ │", " └──" }
                        }[line - 1, subBlock - 1];
                    }

                    case Orientation.South:
                    {
                        return new[,]
                        {
                            { "─┘ │", " ┌──", "───┐", " ┌──" },
                            { "───┘", " └──", "───┘", " │ ┌" }
                        }[line - 1, subBlock - 1];
                    }

                    case Orientation.West:
                    {
                        return new[,]
                        {
                            { " │ └", " ┌─┐", " │ │", "───┐" },
                            { " └──", " │ │", " └─┘", "─┐ │" }
                        }[line - 1, subBlock - 1];
                    }

                    default:
                    {
                        throw new ArgumentException(nameof(orientation));
                    }
                }
            }
        }

        public class BlockTGhost : BlockGhost
        {
            public override BlockType BlockType => BlockType.TGhost;

            public static ConsoleColor Color => ConsoleColor.DarkMagenta;

            public BlockTGhost(Matrix matrix) : base(matrix)
            {
            }

            public static string GetSubBlockString(Orientation orientation, int subBlock, int line)
            {
                Trace.Assert(subBlock >= 1 && subBlock <= 4);
                Trace.Assert(line >= 1 && line <= 2);

                switch (orientation)
                {
                    case Orientation.North:
                    {
                        return new[,]
                        {
                            { " ┌─┐", " ┌──", "─┘ └", "───┐" },
                            { " │ │", " └──", "────", "───┘" }
                        }[line - 1, subBlock - 1];
                    }

                    case Orientation.East:
                    {
                        return new[,]
                        {
                            { "───┐", " ┌─┐", " │ └", " │ │" },
                            { "───┘", " │ │", " │ ┌", " └─┘" }
                        }[line - 1, subBlock - 1];
                    }

                    case Orientation.South:
                    {
                        return new[,]
                        {
                            { " │ │", "───┐", "────", " ┌──" },
                            { " └─┘", "───┘", "─┐ ┌", " └──" }
                        }[line - 1, subBlock - 1];
                    }

                    case Orientation.West:
                    {
                        return new[,]
                        {
                            { " ┌──", " │ │", "─┘ │", " ┌─┐" },
                            { " └──", " └─┘", "─┐ │", " │ │" }
                        }[line - 1, subBlock - 1];
                    }

                    default:
                    {
                        throw new ArgumentException(nameof(orientation));
                    }
                }
            }
        }

        public class BlockZGhost : BlockGhost
        {
            public override BlockType BlockType => BlockType.ZGhost;

            public static ConsoleColor Color => ConsoleColor.DarkRed;

            public BlockZGhost(Matrix matrix) : base(matrix)
            {
            }

            public static string GetSubBlockString(Orientation orientation, int subBlock, int line)
            {
                Trace.Assert(subBlock >= 1 && subBlock <= 4);
                Trace.Assert(line >= 1 && line <= 2);

                switch (orientation)
                {
                    case Orientation.North:
                    {
                        return new[,]
                        {
                            { " ┌──", "───┐", " │ └", "───┐" },
                            { " └──", "─┐ │", " └──", "───┘" }
                        }[line - 1, subBlock - 1];
                    }

                    case Orientation.East:
                    {
                        return new[,]
                        {
                            { " ┌─┐", "─┘ │", " ┌──", " │ │" },
                            { " │ │", "───┘", " │ ┌", " └─┘" }
                        }[line - 1, subBlock - 1];
                    }

                    case Orientation.South:
                    {
                        return new[,]
                        {
                            { "───┐", " │ └", "───┐", " ┌──" },
                            { "───┘", " └──", "─┐ │", " └──" }
                        }[line - 1, subBlock - 1];
                    }

                    case Orientation.West:
                    {
                        return new[,]
                        {
                            { " │ │", " ┌──", "─┘ │", " ┌─┐" },
                            { " └─┘", " │ ┌", "───┘", " │ │" }
                        }[line - 1, subBlock - 1];
                    }

                    default:
                    {
                        throw new ArgumentException(nameof(orientation));
                    }
                }
            }
        }

        public class State
        {
            public bool Start { get; set; }

            public bool Pause { get; set; }

            public bool Exit { get; set; }
        }

        // Initially the Hold box starts empty; if you press C the spawned block (as long as it is not locked and therefore also in prelock)
        // goes into Hold and the block in Next box is spawned;
        // You cannot press C again until a new block is spawned;
        // If you can re-press C the spawned block goes into Hold and the block in Hold is re-spawned.
        public class Hold
        {
            public int Width { get; }
            public int Height { get; }

            public BlockType Index { get; set; }

            private Matrix _matrix;
            private Blocks _blocks;

            public Hold(int xOffset, int yOffset)
            {
                _matrix = new(xOffset, yOffset, maxRows: 2, maxColumns: 4);
                _blocks = new(_matrix);

                Width = _matrix.Width;
                Height = _matrix.Height;

                Index = BlockType.Empty;
            }

            public void ResetAndPrintUpdate()
            {
                _matrix.Reset();

                Index = BlockType.Empty;

                _matrix.PrintUpdate();
            }

            public void PrintInit()
            {
                _matrix.PrintInit(caption: "Hold");
            }

            public void PrintUpdate()
            {
                if (Index == BlockType.Empty)
                {
                    return;
                }

                _matrix.Reset();

                _blocks[Index].TrySpawn(slot: 0);

                _matrix.PrintUpdate();
            }
        }

        public class Stats
        {
            public const int Width = 19;
            public const int Height = 8;

            public int Score { get; private set; }
            public int Level { get; private set; }
            public int Lines { get; private set; }

            public int InitLevel
            {
                get => _initLevel;
                set { _initLevel = value < 1 ? 15 : value > 15 ? 1 : value; Level = _initLevel; }
            }

            private int _initLevel;

            private int _xOffset;
            private int _yOffset;

            public Stats(int xOffset, int yOffset, int initLevel)
            {
                Trace.Assert(initLevel >= 1 && initLevel <= 15);

                Level = initLevel;

                _initLevel = initLevel;

                _xOffset = xOffset;
                _yOffset = yOffset;
            }

            public void Reset()
            {
                Score = 0;
                Level = Math.Clamp(Level, 1, 15);
                Lines = 0;

                _initLevel = Level;
            }

            public int GetFrameTime()
            {
                int level = Math.Clamp(Level, 1, 15);

                return new[] { 1000, 793, 618, 473, 355, 262, 190, 135, 94, 64, 43, 28, 18, 11, 7 }[level - 1]; //! Cannot contain the value Block.MaxLockDelayShortTerm.
            }

            public void SoftDrop()
            {
                Score += 1;
            }

            public void HardDrop()
            {
                Score += 2;
            }

            public void LineClear(LineClearType lineClearType)
            {
                switch (lineClearType)
                {
                    case LineClearType.Single:
                    {
                        Lines += 1;
                        Level = Lines / 10 + _initLevel;
                        Score += 100 * Level;

                        break;
                    }

                    case LineClearType.Double:
                    {
                        Lines += 2;
                        Level = Lines / 10 + _initLevel;
                        Score += 300 * Level;

                        break;
                    }

                    case LineClearType.Triple:
                    {
                        Lines += 3;
                        Level = Lines / 10 + _initLevel;
                        Score += 500 * Level;

                        break;
                    }

                    case LineClearType.Tetris:
                    {
                        Lines += 4;
                        Level = Lines / 10 + _initLevel;
                        Score += 800 * Level;

                        break;
                    }
                }
            }

            public void PrintInit()
            {
                const ConsoleColor WallColor = ConsoleColor.Gray;

                Write("╔ Stats ══════════╗", _xOffset, _yOffset,     WallColor);
                Write("║                 ║", _xOffset, _yOffset + 1, WallColor);
                Write("║                 ║", _xOffset, _yOffset + 2, WallColor);
                Write("║                 ║", _xOffset, _yOffset + 3, WallColor);
                Write("║                 ║", _xOffset, _yOffset + 4, WallColor);
                Write("║                 ║", _xOffset, _yOffset + 5, WallColor);
                Write("║                 ║", _xOffset, _yOffset + 6, WallColor);
                Write("╚═════════════════╝", _xOffset, _yOffset + 7, WallColor);

                Write(" ─── SCORE ─── ", _xOffset + 2, _yOffset + 1, ConsoleColor.DarkGray);
                Write("  ── LEVEL ──  ", _xOffset + 2, _yOffset + 3, ConsoleColor.DarkGray);
                Write("   ─ LINES ─   ", _xOffset + 2, _yOffset + 5, ConsoleColor.DarkGray);
            }

            public void PrintUpdate()
            {
                string scoreString = Score.ToString();
                string levelString = Level.ToString();
                string linesString = Lines.ToString();

                int scoreXOffset = (Width - 4 - scoreString.Length) / 2;
                int levelXOffset = (Width - 4 - levelString.Length) / 2;
                int linesXOffset = (Width - 4 - linesString.Length) / 2;

                Write(new String(' ', Width - 4), _xOffset + 2, _yOffset + 2);
                Write(new String(' ', Width - 4), _xOffset + 2, _yOffset + 4);
                Write(new String(' ', Width - 4), _xOffset + 2, _yOffset + 6);

                Write(scoreString, _xOffset + 2 + scoreXOffset, _yOffset + 2, ConsoleColor.Black, ConsoleColor.Gray); // Inverted colours.
                Write(levelString, _xOffset + 2 + levelXOffset, _yOffset + 4, ConsoleColor.Black, ConsoleColor.Gray);
                Write(linesString, _xOffset + 2 + linesXOffset, _yOffset + 6, ConsoleColor.Black, ConsoleColor.Gray);
            }
        }

        public class Controls
        {
            public const int Width = 19;
            public const int Height = 12;

            private int _xOffset;
            private int _yOffset;

            private State _state;

            public Controls(int xOffset, int yOffset, State state)
            {
                _xOffset = xOffset;
                _yOffset = yOffset;

                _state = state;
            }

            public void PrintInit()
            {
                const ConsoleColor WallColor = ConsoleColor.Gray;

                Write("╔ Controls ═══════╗", _xOffset, _yOffset,      WallColor);
                Write("║                 ║", _xOffset, _yOffset + 1,  WallColor);
                Write("║                 ║", _xOffset, _yOffset + 2,  WallColor);
                Write("║                 ║", _xOffset, _yOffset + 3,  WallColor);
                Write("║                 ║", _xOffset, _yOffset + 4,  WallColor);
                Write("║                 ║", _xOffset, _yOffset + 5,  WallColor);
                Write("║                 ║", _xOffset, _yOffset + 6,  WallColor);
                Write("║                 ║", _xOffset, _yOffset + 7,  WallColor);
                Write("║                 ║", _xOffset, _yOffset + 8,  WallColor);
                Write("║                 ║", _xOffset, _yOffset + 9,  WallColor);
                Write("║                 ║", _xOffset, _yOffset + 10, WallColor);
                Write("╚═════════════════╝", _xOffset, _yOffset + 11, WallColor);
            }

            public void PrintUpdate()
            {
                switch (_state)
                {
                    case State { Pause: true }:
                    {
                        // Pause.
                        Write("Esc   │ Main   ", _xOffset + 2, _yOffset + 1,  ConsoleColor.DarkGray);
                        Write("──────┴────────", _xOffset + 2, _yOffset + 2,  ConsoleColor.DarkGray);
                        Write("               ", _xOffset + 2, _yOffset + 3,  ConsoleColor.DarkGray);
                        Write("               ", _xOffset + 2, _yOffset + 4,  ConsoleColor.DarkGray);
                        Write("               ", _xOffset + 2, _yOffset + 5,  ConsoleColor.DarkGray);
                        Write("               ", _xOffset + 2, _yOffset + 6,  ConsoleColor.DarkGray);
                        Write("               ", _xOffset + 2, _yOffset + 7,  ConsoleColor.DarkGray);
                        Write("               ", _xOffset + 2, _yOffset + 8,  ConsoleColor.DarkGray);
                        Write("──────┬────────", _xOffset + 2, _yOffset + 9,  ConsoleColor.DarkGray);
                        Write("Enter │ Resume ", _xOffset + 2, _yOffset + 10, ConsoleColor.DarkGray);

                        break;
                    }

                    case State { Start: true }:
                    {
                        // Ingame.
                        Write("→ │ Move Right ", _xOffset + 2, _yOffset + 1,  ConsoleColor.DarkGray);
                        Write("← │ Move Left  ", _xOffset + 2, _yOffset + 2,  ConsoleColor.DarkGray);
                        Write("↑ │ Rot. Right ", _xOffset + 2, _yOffset + 3,  ConsoleColor.DarkGray);
                        Write("Z │ Rot. Left  ", _xOffset + 2, _yOffset + 4,  ConsoleColor.DarkGray);
                        Write("──┴┬───────────", _xOffset + 2, _yOffset + 5,  ConsoleColor.DarkGray);
                        Write("↓  │ Soft Drop ", _xOffset + 2, _yOffset + 6,  ConsoleColor.DarkGray);
                        Write("SB │ Hard Drop ", _xOffset + 2, _yOffset + 7,  ConsoleColor.DarkGray);
                        Write("C  │ Hold      ", _xOffset + 2, _yOffset + 8,  ConsoleColor.DarkGray);
                        Write("───┴┬──────────", _xOffset + 2, _yOffset + 9,  ConsoleColor.DarkGray);
                        Write("Esc │ Pause    ", _xOffset + 2, _yOffset + 10, ConsoleColor.DarkGray);

                        break;
                    }

                    case State { }:
                    {
                        // Main.
                        Write("Esc   │ Exit   ", _xOffset + 2, _yOffset + 1,  ConsoleColor.DarkGray);
                        Write("L     │ Level  ", _xOffset + 2, _yOffset + 2,  ConsoleColor.DarkGray);
                        Write("──────┴────────", _xOffset + 2, _yOffset + 3,  ConsoleColor.DarkGray);
                        Write("               ", _xOffset + 2, _yOffset + 4,  ConsoleColor.DarkGray);
                        Write("               ", _xOffset + 2, _yOffset + 5,  ConsoleColor.DarkGray);
                        Write("               ", _xOffset + 2, _yOffset + 6,  ConsoleColor.DarkGray);
                        Write("               ", _xOffset + 2, _yOffset + 7,  ConsoleColor.DarkGray);
                        Write("               ", _xOffset + 2, _yOffset + 8,  ConsoleColor.DarkGray);
                        Write("──────┬────────", _xOffset + 2, _yOffset + 9,  ConsoleColor.DarkGray);
                        Write("Enter │ Start  ", _xOffset + 2, _yOffset + 10, ConsoleColor.DarkGray);

                        break;
                    }
                }
            }
        }

        public class Matrix
        {
            public int Id { get; set; }

            public int MaxRows { get; }
            public int MaxColumns { get; }

            public int Width { get; }
            public int Height { get; }

            public CellInfo this[int rows, int columns] { get => _matrix[rows][columns]; set => _matrix[rows][columns] = value; }

            private List<List<CellInfo>> _matrix;

            private int _xOffset;
            private int _yOffset;

            public Matrix(int xOffset, int yOffset, int maxRows, int maxColumns)
            {
                MaxRows = maxRows;
                MaxColumns = maxColumns;

                Width = (maxColumns * 4 + 1) + 2; // cells+padding + wall.
                Height = (maxRows * 2) + 2; // cells + wall.

                _matrix = new();

                _xOffset = xOffset;
                _yOffset = yOffset;

                lock (_matrix)
                {
                    for (int rows = 0; rows < maxRows; rows++)
                    {
                        List<CellInfo> row = new();

                        for (int columns = 0; columns < maxColumns; columns++)
                        {
                            row.Add(new());
                        }

                        _matrix.Add(row);
                    }
                }
            }

            public void PrintCaption(string caption = null)
            {
                const ConsoleColor WallColor = ConsoleColor.Gray;

                caption = caption == null ? String.Empty : $" {caption} ";

                Write("╔" + caption + new String('═', Width - 2 - caption.Length) + "╗", _xOffset, _yOffset, WallColor);
            }

            public void PrintInit(string caption = null)
            {
                const ConsoleColor WallColor = ConsoleColor.Gray;

                PrintCaption(caption);

                for (int rows = 1; rows <= MaxRows * 2; rows++)
                {
                    Write("║" + new String(' ', Width - 2) + "║", _xOffset, _yOffset + rows, WallColor);
                }

                Write(    "╚" + new String('═', Width - 2) + "╝", _xOffset, _yOffset + MaxRows * 2 + 1, WallColor);
            }

            public void PrintUpdate(bool isLockDelay = false, bool grid = false)
            {
                for (int rows = 0; rows < MaxRows; rows++)
                {
                    for (int columns = 0; columns < MaxColumns; columns++)
                    {
                        CellInfo cellInfo = _matrix[rows][columns];

                        switch (cellInfo.BlockType)
                        {
                            case BlockType.LineClear:
                            {
                                Trace.Assert(columns == 0);

                                switch ((LineClearType)cellInfo.SubBlock)
                                {
                                    case LineClearType.Single:
                                    {
                                        Write(BlockLineClear.GetBlockString(1, MaxColumns), _xOffset + 1, _yOffset + rows * 2 + 1, BlockLineClear.Color);
                                        Write(BlockLineClear.GetBlockString(2, MaxColumns), _xOffset + 1, _yOffset + rows * 2 + 2, BlockLineClear.Color);

                                        break;
                                    }

                                    case LineClearType.Double:
                                    {
                                        Write(BlockLineClear.GetBlockString(1, MaxColumns), _xOffset + 1, _yOffset + rows * 2 + 1, BlockLineClear.Color);
                                        Write(BlockLineClear.GetBlockString(3, MaxColumns), _xOffset + 1, _yOffset + rows * 2 + 2, BlockLineClear.Color);
                                        rows++;
                                        Write(BlockLineClear.GetBlockString(3, MaxColumns), _xOffset + 1, _yOffset + rows * 2 + 1, BlockLineClear.Color);
                                        Write(BlockLineClear.GetBlockString(2, MaxColumns), _xOffset + 1, _yOffset + rows * 2 + 2, BlockLineClear.Color);

                                        break;
                                    }

                                    case LineClearType.Triple:
                                    {
                                        Write(BlockLineClear.GetBlockString(1, MaxColumns), _xOffset + 1, _yOffset + rows * 2 + 1, BlockLineClear.Color);
                                        Write(BlockLineClear.GetBlockString(3, MaxColumns), _xOffset + 1, _yOffset + rows * 2 + 2, BlockLineClear.Color);
                                        rows++;
                                        Write(BlockLineClear.GetBlockString(3, MaxColumns), _xOffset + 1, _yOffset + rows * 2 + 1, BlockLineClear.Color);
                                        Write(BlockLineClear.GetBlockString(3, MaxColumns), _xOffset + 1, _yOffset + rows * 2 + 2, BlockLineClear.Color);
                                        rows++;
                                        Write(BlockLineClear.GetBlockString(3, MaxColumns), _xOffset + 1, _yOffset + rows * 2 + 1, BlockLineClear.Color);
                                        Write(BlockLineClear.GetBlockString(2, MaxColumns), _xOffset + 1, _yOffset + rows * 2 + 2, BlockLineClear.Color);

                                        break;
                                    }

                                    case LineClearType.Tetris:
                                    {
                                        Write(BlockLineClear.GetBlockString(1, MaxColumns), _xOffset + 1, _yOffset + rows * 2 + 1, BlockLineClear.Color);
                                        Write(BlockLineClear.GetBlockString(3, MaxColumns), _xOffset + 1, _yOffset + rows * 2 + 2, BlockLineClear.Color);
                                        rows++;
                                        Write(BlockLineClear.GetBlockString(3, MaxColumns), _xOffset + 1, _yOffset + rows * 2 + 1, BlockLineClear.Color);
                                        Write(BlockLineClear.GetBlockString(3, MaxColumns), _xOffset + 1, _yOffset + rows * 2 + 2, BlockLineClear.Color);
                                        rows++;
                                        Write(BlockLineClear.GetBlockString(3, MaxColumns), _xOffset + 1, _yOffset + rows * 2 + 1, BlockLineClear.Color);
                                        Write(BlockLineClear.GetBlockString(3, MaxColumns), _xOffset + 1, _yOffset + rows * 2 + 2, BlockLineClear.Color);
                                        rows++;
                                        Write(BlockLineClear.GetBlockString(3, MaxColumns), _xOffset + 1, _yOffset + rows * 2 + 1, BlockLineClear.Color);
                                        Write(BlockLineClear.GetBlockString(2, MaxColumns), _xOffset + 1, _yOffset + rows * 2 + 2, BlockLineClear.Color);

                                        break;
                                    }
                                }

                                columns += MaxColumns - 1;

                                break;
                            }

                            case BlockType.Empty:
                            {
                                Write(BlockEmpty.GetBlockString(1), _xOffset + columns * 4 + 1, _yOffset + rows * 2 + 1, grid ? BlockEmpty.Color1 : BlockEmpty.Color2);
                                Write(BlockEmpty.GetBlockString(2), _xOffset + columns * 4 + 1, _yOffset + rows * 2 + 2, grid ? BlockEmpty.Color1 : BlockEmpty.Color2);

                                break;
                            }

                            case BlockType.I:
                            {
                                int missingSubBlockMask = GetMissingSubBlockMask(cellInfo.Id);

                                ConsoleColor color = !(cellInfo.Id == Id && isLockDelay) ? BlockI.Color1 : BlockI.Color2;

                                Write(BlockI.GetSubBlockString(cellInfo.Orientation, cellInfo.SubBlock, 1, missingSubBlockMask), _xOffset + columns * 4 + 1, _yOffset + rows * 2 + 1, color);
                                Write(BlockI.GetSubBlockString(cellInfo.Orientation, cellInfo.SubBlock, 2, missingSubBlockMask), _xOffset + columns * 4 + 1, _yOffset + rows * 2 + 2, color);

                                break;
                            }

                            case BlockType.J:
                            {
                                int missingSubBlockMask = GetMissingSubBlockMask(cellInfo.Id);

                                ConsoleColor color = !(cellInfo.Id == Id && isLockDelay) ? BlockJ.Color1 : BlockJ.Color2;

                                Write(BlockJ.GetSubBlockString(cellInfo.Orientation, cellInfo.SubBlock, 1, missingSubBlockMask), _xOffset + columns * 4 + 1, _yOffset + rows * 2 + 1, color);
                                Write(BlockJ.GetSubBlockString(cellInfo.Orientation, cellInfo.SubBlock, 2, missingSubBlockMask), _xOffset + columns * 4 + 1, _yOffset + rows * 2 + 2, color);

                                break;
                            }

                            case BlockType.L:
                            {
                                int missingSubBlockMask = GetMissingSubBlockMask(cellInfo.Id);

                                ConsoleColor color = !(cellInfo.Id == Id && isLockDelay) ? BlockL.Color1 : BlockL.Color2;

                                Write(BlockL.GetSubBlockString(cellInfo.Orientation, cellInfo.SubBlock, 1, missingSubBlockMask), _xOffset + columns * 4 + 1, _yOffset + rows * 2 + 1, color);
                                Write(BlockL.GetSubBlockString(cellInfo.Orientation, cellInfo.SubBlock, 2, missingSubBlockMask), _xOffset + columns * 4 + 1, _yOffset + rows * 2 + 2, color);

                                break;
                            }

                            case BlockType.O:
                            {
                                int missingSubBlockMask = GetMissingSubBlockMask(cellInfo.Id);

                                ConsoleColor color = !(cellInfo.Id == Id && isLockDelay) ? BlockO.Color1 : BlockO.Color2;

                                Write(BlockO.GetSubBlockString(cellInfo.Orientation, cellInfo.SubBlock, 1, missingSubBlockMask), _xOffset + columns * 4 + 1, _yOffset + rows * 2 + 1, color);
                                Write(BlockO.GetSubBlockString(cellInfo.Orientation, cellInfo.SubBlock, 2, missingSubBlockMask), _xOffset + columns * 4 + 1, _yOffset + rows * 2 + 2, color);

                                break;
                            }

                            case BlockType.S:
                            {
                                int missingSubBlockMask = GetMissingSubBlockMask(cellInfo.Id);

                                ConsoleColor color = !(cellInfo.Id == Id && isLockDelay) ? BlockS.Color1 : BlockS.Color2;

                                Write(BlockS.GetSubBlockString(cellInfo.Orientation, cellInfo.SubBlock, 1, missingSubBlockMask), _xOffset + columns * 4 + 1, _yOffset + rows * 2 + 1, color);
                                Write(BlockS.GetSubBlockString(cellInfo.Orientation, cellInfo.SubBlock, 2, missingSubBlockMask), _xOffset + columns * 4 + 1, _yOffset + rows * 2 + 2, color);

                                break;
                            }

                            case BlockType.T:
                            {
                                int missingSubBlockMask = GetMissingSubBlockMask(cellInfo.Id);

                                ConsoleColor color = !(cellInfo.Id == Id && isLockDelay) ? BlockT.Color1 : BlockT.Color2;

                                Write(BlockT.GetSubBlockString(cellInfo.Orientation, cellInfo.SubBlock, 1, missingSubBlockMask), _xOffset + columns * 4 + 1, _yOffset + rows * 2 + 1, color);
                                Write(BlockT.GetSubBlockString(cellInfo.Orientation, cellInfo.SubBlock, 2, missingSubBlockMask), _xOffset + columns * 4 + 1, _yOffset + rows * 2 + 2, color);

                                break;
                            }

                            case BlockType.Z:
                            {
                                int missingSubBlockMask = GetMissingSubBlockMask(cellInfo.Id);

                                ConsoleColor color = !(cellInfo.Id == Id && isLockDelay) ? BlockZ.Color1 : BlockZ.Color2;

                                Write(BlockZ.GetSubBlockString(cellInfo.Orientation, cellInfo.SubBlock, 1, missingSubBlockMask), _xOffset + columns * 4 + 1, _yOffset + rows * 2 + 1, color);
                                Write(BlockZ.GetSubBlockString(cellInfo.Orientation, cellInfo.SubBlock, 2, missingSubBlockMask), _xOffset + columns * 4 + 1, _yOffset + rows * 2 + 2, color);

                                break;
                            }

                            case BlockType.IGhost:
                            {
                                Write(BlockIGhost.GetSubBlockString(cellInfo.Orientation, cellInfo.SubBlock, 1), _xOffset + columns * 4 + 1, _yOffset + rows * 2 + 1, BlockIGhost.Color);
                                Write(BlockIGhost.GetSubBlockString(cellInfo.Orientation, cellInfo.SubBlock, 2), _xOffset + columns * 4 + 1, _yOffset + rows * 2 + 2, BlockIGhost.Color);

                                break;
                            }

                            case BlockType.JGhost:
                            {
                                Write(BlockJGhost.GetSubBlockString(cellInfo.Orientation, cellInfo.SubBlock, 1), _xOffset + columns * 4 + 1, _yOffset + rows * 2 + 1, BlockJGhost.Color);
                                Write(BlockJGhost.GetSubBlockString(cellInfo.Orientation, cellInfo.SubBlock, 2), _xOffset + columns * 4 + 1, _yOffset + rows * 2 + 2, BlockJGhost.Color);

                                break;
                            }

                            case BlockType.LGhost:
                            {
                                Write(BlockLGhost.GetSubBlockString(cellInfo.Orientation, cellInfo.SubBlock, 1), _xOffset + columns * 4 + 1, _yOffset + rows * 2 + 1, BlockLGhost.Color);
                                Write(BlockLGhost.GetSubBlockString(cellInfo.Orientation, cellInfo.SubBlock, 2), _xOffset + columns * 4 + 1, _yOffset + rows * 2 + 2, BlockLGhost.Color);

                                break;
                            }

                            case BlockType.OGhost:
                            {
                                Write(BlockOGhost.GetSubBlockString(cellInfo.Orientation, cellInfo.SubBlock, 1), _xOffset + columns * 4 + 1, _yOffset + rows * 2 + 1, BlockOGhost.Color);
                                Write(BlockOGhost.GetSubBlockString(cellInfo.Orientation, cellInfo.SubBlock, 2), _xOffset + columns * 4 + 1, _yOffset + rows * 2 + 2, BlockOGhost.Color);

                                break;
                            }

                            case BlockType.SGhost:
                            {
                                Write(BlockSGhost.GetSubBlockString(cellInfo.Orientation, cellInfo.SubBlock, 1), _xOffset + columns * 4 + 1, _yOffset + rows * 2 + 1, BlockSGhost.Color);
                                Write(BlockSGhost.GetSubBlockString(cellInfo.Orientation, cellInfo.SubBlock, 2), _xOffset + columns * 4 + 1, _yOffset + rows * 2 + 2, BlockSGhost.Color);

                                break;
                            }

                            case BlockType.TGhost:
                            {
                                Write(BlockTGhost.GetSubBlockString(cellInfo.Orientation, cellInfo.SubBlock, 1), _xOffset + columns * 4 + 1, _yOffset + rows * 2 + 1, BlockTGhost.Color);
                                Write(BlockTGhost.GetSubBlockString(cellInfo.Orientation, cellInfo.SubBlock, 2), _xOffset + columns * 4 + 1, _yOffset + rows * 2 + 2, BlockTGhost.Color);

                                break;
                            }

                            case BlockType.ZGhost:
                            {
                                Write(BlockZGhost.GetSubBlockString(cellInfo.Orientation, cellInfo.SubBlock, 1), _xOffset + columns * 4 + 1, _yOffset + rows * 2 + 1, BlockZGhost.Color);
                                Write(BlockZGhost.GetSubBlockString(cellInfo.Orientation, cellInfo.SubBlock, 2), _xOffset + columns * 4 + 1, _yOffset + rows * 2 + 2, BlockZGhost.Color);

                                break;
                            }
                        }
                    }
                }
            }

            // 0b0000: No sub-block is missing.
            // 0b1000: Sub-block 1 is missing.
            // 0b0001: Sub-block 4 is missing.
            // 0b1001: Sub-blocks 1 and 4 are missing.
            private int GetMissingSubBlockMask(int id)
            {
                int notMissingSubBlockMask = 0b0000;

                for (int rows = 0; rows < MaxRows; rows++)
                {
                    for (int columns = 0; columns < MaxColumns; columns++)
                    {
                        CellInfo cellInfo = _matrix[rows][columns];

                        if (cellInfo.Id == id)
                        {
                            notMissingSubBlockMask |= 1 << (4 - cellInfo.SubBlock);
                        }
                    }
                }

                return ~notMissingSubBlockMask & 0b1111;
            }

            public bool TryFindLineClears(out List<LineClearType> lineClears)
            {
                lineClears = new();
                int count = 0;

                for (int rows = 0; rows < MaxRows; rows++)
                {
                    if (IsRowAllNotEmpty(rows))
                    {
                        if (rows + 1 < MaxRows && IsRowAllNotEmpty(rows + 1))
                        {
                            if (rows + 2 < MaxRows && IsRowAllNotEmpty(rows + 2))
                            {
                                if (rows + 3 < MaxRows && IsRowAllNotEmpty(rows + 3))
                                {
                                    SetRowAllLineClear(  rows, LineClearType.Tetris);
                                    SetRowAllLineClear(++rows, LineClearType.Tetris);
                                    SetRowAllLineClear(++rows, LineClearType.Tetris);
                                    SetRowAllLineClear(++rows, LineClearType.Tetris);

                                    lineClears.Add(LineClearType.Tetris);
                                    count += 4;
                                }
                                else
                                {
                                    SetRowAllLineClear(  rows, LineClearType.Triple);
                                    SetRowAllLineClear(++rows, LineClearType.Triple);
                                    SetRowAllLineClear(++rows, LineClearType.Triple);

                                    lineClears.Add(LineClearType.Triple);
                                    count += 3;
                                }
                            }
                            else
                            {
                                SetRowAllLineClear(  rows, LineClearType.Double);
                                SetRowAllLineClear(++rows, LineClearType.Double);

                                lineClears.Add(LineClearType.Double);
                                count += 2;
                            }
                        }
                        else
                        {
                            SetRowAllLineClear(rows, LineClearType.Single);

                            lineClears.Add(LineClearType.Single);
                            count += 1;
                        }
                    }
                }

                Trace.Assert(lineClears.Count <= 2);
                Debug.Assert(count <= 4);

                return lineClears.Count != 0;
            }

            public void RemoveLineClears()
            {
                lock (_matrix)
                {
                    for (int rows = MaxRows - 1; rows >= 0; )
                    {
                        if (IsRowAllLineClear(rows))
                        {
                            _matrix.RemoveAt(rows);

                            List<CellInfo> row = new();

                            for (int columns = 0; columns < MaxColumns; columns++)
                            {
                                row.Add(new());
                            }

                            _matrix.Insert(0, row);
                        }
                        else
                        {
                            rows--;
                        }
                    }
                }
            }

            private bool IsRowAllNotEmpty(int rows)
            {
                Trace.Assert(rows >= 0 && rows < MaxRows);

                int count = 0;

                for (int columns = 0; columns < MaxColumns; columns++)
                {
                    if (_matrix[rows][columns].BlockType > BlockType.Empty)
                    {
                        count++;
                    }
                }

                return count == MaxColumns;
            }

            private bool IsRowAllLineClear(int rows)
            {
                Trace.Assert(rows >= 0 && rows < MaxRows);

                int count = 0;

                for (int columns = 0; columns < MaxColumns; columns++)
                {
                    if (_matrix[rows][columns].BlockType == BlockType.LineClear)
                    {
                        count++;
                    }
                }

                return count == MaxColumns;
            }

            private void SetRowAllLineClear(int rows, LineClearType lineClearType)
            {
                Trace.Assert(rows >= 0 && rows < MaxRows);

                for (int columns = 0; columns < MaxColumns; columns++)
                {
                    _matrix[rows][columns].Set(BlockType.LineClear, subBlock: (int)lineClearType);
                }
            }

            public void Reset()
            {
                Id = 0;

                for (int rows = 0; rows < MaxRows; rows++)
                {
                    for (int columns = 0; columns < MaxColumns; columns++)
                    {
                        _matrix[rows][columns].Set();
                    }
                }
            }
        }

        public class Next
        {
            public int PreviewCount { get; }

            public int Width { get; }
            public int Height { get; }

            public List<BlockType> PreviewIndexes { get; set; }

            private Matrix _matrix;
            private Blocks _blocks;

            public Next(int xOffset, int yOffset, int previewCount)
            {
                Trace.Assert(previewCount >= 1 && previewCount <= 6);

                _matrix = new(xOffset, yOffset, maxRows: previewCount * 2, maxColumns: 4);
                _blocks = new(_matrix);

                PreviewCount = previewCount;

                Width = _matrix.Width;
                Height = _matrix.Height;
            }

            public void ResetAndPrintUpdate()
            {
                _matrix.Reset();

                PreviewIndexes = null;

                _matrix.PrintUpdate();
            }

            public void PrintInit()
            {
                _matrix.PrintInit(caption: "Next");
            }

            public void PrintUpdate()
            {
                if (PreviewIndexes == null)
                {
                    return;
                }

                _matrix.Reset();

                int slot = 0;

                foreach (BlockType previewIndex in PreviewIndexes)
                {
                    _blocks[previewIndex].TrySpawn(slot++);
                }

                _matrix.PrintUpdate();
            }
        }

        public class Blocks
        {
            private List<Block> _blocks;

            public Blocks(Matrix matrix)
            {
                _blocks = new();

                _blocks.Add(new BlockI(matrix));
                _blocks.Add(new BlockJ(matrix));
                _blocks.Add(new BlockL(matrix));
                _blocks.Add(new BlockO(matrix));
                _blocks.Add(new BlockS(matrix));
                _blocks.Add(new BlockT(matrix));
                _blocks.Add(new BlockZ(matrix));
            }

            public Block this[BlockType blockType] => _blocks[(int)blockType - 1];
        }

        public class BlocksGhost
        {
            private List<BlockGhost> _blocksGhost;

            public BlocksGhost(Matrix matrix)
            {
                _blocksGhost = new();

                _blocksGhost.Add(new BlockIGhost(matrix));
                _blocksGhost.Add(new BlockJGhost(matrix));
                _blocksGhost.Add(new BlockLGhost(matrix));
                _blocksGhost.Add(new BlockOGhost(matrix));
                _blocksGhost.Add(new BlockSGhost(matrix));
                _blocksGhost.Add(new BlockTGhost(matrix));
                _blocksGhost.Add(new BlockZGhost(matrix));
            }

            public BlockGhost this[BlockType blockType] => _blocksGhost[(int)blockType - 1];
        }

        public class Bag
        {
            public const int Capacity = 7;
            private const int Seed = 0;

            private List<BlockType> _bag;
            private Random _rnd;

            private BlockType _holdIndex;

            private Next _next;

            public BlockType Index { get; private set; }

            public Bag(Next next)
            {
                _bag = new(Capacity + next.PreviewCount);
                _rnd = new(/*Seed*/);

                _holdIndex = BlockType.Empty;

                _next = next;
            }

            public void Reset()
            {
                _bag.Clear();
                _rnd = new(/*Seed*/);

                _holdIndex = BlockType.Empty;

                Generate();

                _next.PreviewIndexes = _bag.GetRange(0, _next.PreviewCount);
            }

            public void NextIndex()
            {
                if (_holdIndex != BlockType.Empty)
                {
                    Index = _holdIndex;

                    _holdIndex = BlockType.Empty;

                    return;
                }

                if (_bag.Count <= _next.PreviewCount)
                {
                    Generate();
                }

                Index = _bag[0];
                _bag.RemoveAt(0);

                _next.PreviewIndexes = _bag.GetRange(0, _next.PreviewCount);
            }

            private void Generate()
            {
                List<BlockType> bag = new();

                for (int cnt = 1; cnt <= Capacity; cnt++)
                {
                    BlockType blockType;

                    do
                    {
                        blockType = (BlockType)_rnd.Next(1, Capacity + 1);
                    }
                    while (bag.Contains(blockType));

                    bag.Add(blockType);
                }

                _bag.AddRange(bag);
            }

            public void SetHoldIndex(BlockType blockType)
            {
                if (blockType != BlockType.Empty)
                {
                    _holdIndex = blockType;
                }
            }
        }

        private static void UnhandledException(Exception ex, bool isTerminating) // Asserts will not be logged in.
        {
            Console.OutputEncoding = System.Text.Encoding.Default;
            Console.CursorVisible = true;

            Console.TreatControlCAsInput = false;

            Console.Clear();
            Console.ResetColor();

            string path = @"TetrisC.log";
            string contents = $"{DateTime.Now} | Unhandled exception caught: {ex.Message}{Environment.NewLine}" +
                              $"{ex.StackTrace}{Environment.NewLine}{Environment.NewLine}";

            System.IO.File.AppendAllText(path, contents);
        }

        private static void PrintInit(Hold hold, Stats stats, Controls controls, Matrix matrix, Next next)
        {
            hold.    PrintInit();
            stats.   PrintInit();
            controls.PrintInit();
            matrix.  PrintInit();
            next.    PrintInit();
        }

        private static void PrintUpdate(Hold hold, Stats stats, Controls controls, Matrix matrix, Next next, bool isLockDelay = false)
        {
            hold.    PrintUpdate();
            stats.   PrintUpdate();
            controls.PrintUpdate();
            matrix.  PrintUpdate(isLockDelay, grid: true);
            next.    PrintUpdate();
        }

        private static void Write(
            string str,
            int left, int top,
            ConsoleColor foregroundColor = ConsoleColor.Gray,
            ConsoleColor backgroundColor = ConsoleColor.Black)
        {
            Console.ForegroundColor = foregroundColor;
            Console.BackgroundColor = backgroundColor;

            Console.SetCursorPosition(left, top);
            Console.Write(str);

            Console.ResetColor();
        }

        private static void BeepAsync(int frequencyA = 800, int frequencyB = 0, int duration = 200, bool ramp = false, int steps = 4)
        {
            ThreadPool.QueueUserWorkItem((_) =>
            {
                if (!ramp)
                {
                    if (frequencyA != 0)
                    {
                        Console.Beep(frequencyA, frequencyB == 0 ? duration : duration / 2);
                    }

                    if (frequencyB != 0)
                    {
                        Console.Beep(frequencyB, frequencyA == 0 ? duration : duration / 2);
                    }
                }
                else
                {
                    int frequencyStep = (frequencyA - frequencyB) / (steps - 1);
                    duration /= steps;

                    for (int i = 1; i <= steps; i++)
                    {
                        if (frequencyA != 0)
                        {
                            Console.Beep(frequencyA, duration);
                        }

                        frequencyA -= frequencyStep;
                    }
                }
            });
        }

        private static int SleepAsync(int timeLeft, params Func<bool>[] conds)
        {
            if (timeLeft <= 0)
            {
                return 0;
            }

            Stopwatch sW = Stopwatch.StartNew();

            while (true)
            {
                foreach (var cond in conds)
                {
                    if (!cond())
                    {
                        sW.Stop();

                        return Math.Max(0, timeLeft - (int)sW.ElapsedMilliseconds);
                    }
                }

                if (timeLeft > (int)sW.ElapsedMilliseconds)
                {
                    Thread.Sleep(1); //! Not for release.
                }
                else
                {
                    sW.Stop();

                    return 0;
                }
            }
        }

        public static void InputThread(object data)
        {
            var (state, hold, stats, controls, matrix, next, bag, blocks, blocksGhost) = ((State, Hold, Stats, Controls, Matrix, Next, Bag, Blocks, BlocksGhost))data;

            Stopwatch sW = new();

            while (!state.Exit)
            {
                int frameTime = 7; // ms.

                sW.Restart();

                if (state.Start && Console.KeyAvailable)
                {
                    ConsoleKey cK = Console.ReadKey(true).Key;

                    ThreadPool.QueueUserWorkItem((_) => { while (Console.KeyAvailable) Console.ReadKey(true); });

                    lock (_lock)
                    {
                        switch (cK)
                        {
                            case ConsoleKey.LeftArrow:
                            {
                                if (blocks[bag.Index].IsLocked || blocks[bag.Index].IsHardDrop || blocks[bag.Index].LockDelayLongTerm >= Block.MaxLockDelayLongTerm)
                                {
                                    break;
                                }

                                if (blocks[bag.Index].TryTranslate(Translation.ShiftLeft))
                                {
                                    if (blocks[bag.Index].IsPreLocked && ++blocks[bag.Index].LockDelayLongTerm <= Block.MaxLockDelayLongTerm)
                                    {
                                        blocks[bag.Index].IsPreLocked = false;
                                    }

                                    blocksGhost[bag.Index].TryGhost(blocks[bag.Index]);

                                    BeepAsync(frequencyA: 800, duration: 1);
                                }

                                break;
                            }

                            case ConsoleKey.RightArrow:
                            {
                                if (blocks[bag.Index].IsLocked || blocks[bag.Index].IsHardDrop || blocks[bag.Index].LockDelayLongTerm >= Block.MaxLockDelayLongTerm)
                                {
                                    break;
                                }

                                if (blocks[bag.Index].TryTranslate(Translation.ShiftRight))
                                {
                                    if (blocks[bag.Index].IsPreLocked && ++blocks[bag.Index].LockDelayLongTerm <= Block.MaxLockDelayLongTerm)
                                    {
                                        blocks[bag.Index].IsPreLocked = false;
                                    }

                                    blocksGhost[bag.Index].TryGhost(blocks[bag.Index]);

                                    BeepAsync(frequencyA: 800, duration: 1);
                                }

                                break;
                            }

                            case ConsoleKey.UpArrow:
                            {
                                if (blocks[bag.Index].IsLocked || blocks[bag.Index].IsHardDrop || blocks[bag.Index].LockDelayLongTerm >= Block.MaxLockDelayLongTerm)
                                {
                                    break;
                                }

                                if (blocks[bag.Index].TryRotate(Rotation.Clockwise))
                                {
                                    if (blocks[bag.Index].IsPreLocked && ++blocks[bag.Index].LockDelayLongTerm <= Block.MaxLockDelayLongTerm)
                                    {
                                        blocks[bag.Index].IsPreLocked = false;
                                    }

                                    blocksGhost[bag.Index].TryGhost(blocks[bag.Index]);

                                    BeepAsync(frequencyA: 800, frequencyB: 800, duration: 2);
                                }

                                break;
                            }

                            case ConsoleKey.Z:
                            {
                                if (blocks[bag.Index].IsLocked || blocks[bag.Index].IsHardDrop || blocks[bag.Index].LockDelayLongTerm >= Block.MaxLockDelayLongTerm)
                                {
                                    break;
                                }

                                if (blocks[bag.Index].TryRotate(Rotation.AntiClockwise))
                                {
                                    if (blocks[bag.Index].IsPreLocked && ++blocks[bag.Index].LockDelayLongTerm <= Block.MaxLockDelayLongTerm)
                                    {
                                        blocks[bag.Index].IsPreLocked = false;
                                    }

                                    blocksGhost[bag.Index].TryGhost(blocks[bag.Index]);

                                    BeepAsync(frequencyA: 800, frequencyB: 800, duration: 2);
                                }

                                break;
                            }

                            case ConsoleKey.DownArrow: // Soft Drop.
                            {
                                if (blocks[bag.Index].IsLocked)
                                {
                                    break;
                                }

                                if (blocks[bag.Index].TryTranslate(Translation.ShiftDown))
                                {
                                    if (blocks[bag.Index].HasLanded())
                                    {
                                        //spawnedBlock.IsPreLocked = true; // TODO: .
                                    }
                                    else
                                    {
                                        blocks[bag.Index].IsPreLocked = false;
                                    }

                                    blocksGhost[bag.Index].TryGhost(blocks[bag.Index]);

                                    stats.SoftDrop();

                                    BeepAsync(frequencyA: 800, duration: 1);
                                }

                                break;
                            }

                            case ConsoleKey.Spacebar: // Hard Drop.
                            {
                                if (blocks[bag.Index].IsLocked)
                                {
                                    break;
                                }

                                while (true)
                                {
                                    if (blocks[bag.Index].TryTranslate(Translation.ShiftDown))
                                    {
                                        if (blocks[bag.Index].HasLanded())
                                        {
                                            blocks[bag.Index].IsPreLocked = true; // TODO: .
                                        }
                                        else
                                        {
                                            blocks[bag.Index].IsPreLocked = false;
                                        }

                                        blocksGhost[bag.Index].TryGhost(blocks[bag.Index]);

                                        stats.HardDrop();

                                        BeepAsync(frequencyA: 800, duration: 1);
                                    }
                                    else
                                    {
                                        blocks[bag.Index].IsHardDrop = true;

                                        break;
                                    }
                                }

                                break;
                            }

                            case ConsoleKey.C: // Hold.
                            {
                                if (blocks[bag.Index].IsLocked || !blocks[bag.Index].CanHold)
                                {
                                    break;
                                }

                                blocks[bag.Index].IsHold = true;

                                BeepAsync(frequencyA: 600, duration: 200);

                                break;
                            }

                            case ConsoleKey.Escape:
                            {
                                state.Pause = true;

                                matrix.PrintCaption($"[ PAUSE ]");

                                controls.PrintUpdate();

                                Stopwatch sW2 = new();

                                while (state.Pause)
                                {
                                    int frameTime2 = 100; // ms.

                                    sW2.Restart();

                                    if (Console.KeyAvailable)
                                    {
                                        ConsoleKey cK2 = Console.ReadKey(true).Key;

                                        ThreadPool.QueueUserWorkItem((_) => { while (Console.KeyAvailable) Console.ReadKey(true); });

                                        switch (cK2)
                                        {
                                            case ConsoleKey.Enter:
                                            {
                                                for (int i = 3; i >= 1; i--)
                                                {
                                                    matrix.PrintCaption($"[ RESUMING: {i} ]");
                                                    BeepAsync(frequencyA: 900, duration: 200);

                                                    Thread.Sleep(1000);
                                                }
                                                matrix.PrintCaption();
                                                BeepAsync(frequencyA: 900, duration: 600);

                                                state.Pause = false;

                                                break;
                                            }

                                            case ConsoleKey.Escape:
                                            {
                                                state.Start = false;

                                                matrix.PrintCaption("[ GAME OVER ]");

                                                blocks[bag.Index].IsLocked = true;
                                                blocks[bag.Index].IsHardDrop = false;

                                                blocks[bag.Index].IsPreLocked = false;
                                                blocks[bag.Index].LockDelayLongTerm = 0;

                                                blocks[bag.Index].Clear();
                                                blocksGhost[bag.Index].Clear();

                                                hold.ResetAndPrintUpdate();
                                                next.ResetAndPrintUpdate();

                                                BeepAsync(frequencyA: 700, duration: 600);

                                                state.Pause = false;

                                                frameTime2 = 0;

                                                break;
                                            }
                                        }
                                    }

                                    sW2.Stop();

                                    int frameTimeLeft2 = frameTime2 - (int)sW.ElapsedMilliseconds;

                                    SleepAsync(frameTimeLeft2);
                                }

                                frameTime = 0;

                                break;
                            }
                        }

                        PrintUpdate(hold, stats, controls, matrix, next, blocks[bag.Index].IsLockDelay());
                    }
                }

                sW.Stop();

                int frameTimeLeft = frameTime - (int)sW.ElapsedMilliseconds;

                SleepAsync(frameTimeLeft);
            }
        }

        //! A raster font (e.g. Terminal 10x18) is recommended.
        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += (object sender, UnhandledExceptionEventArgs e) => UnhandledException(e.ExceptionObject as Exception, e.IsTerminating);

            State state = new();

            Hold     hold =     new(xOffset: 1,                             yOffset: 1);
            Stats    stats =    new(xOffset: 1,                             yOffset: hold.Height + 1,                initLevel: 1);
            Controls controls = new(xOffset: 1,                             yOffset: hold.Height + Stats.Height + 1, state);
            Matrix   matrix =   new(xOffset: hold.Width + 2,                yOffset: 1,                              maxRows: 20, maxColumns: 10);
            Next     next =     new(xOffset: hold.Width + matrix.Width + 3, yOffset: 1,                              previewCount: 3);

            Bag bag = new(next);

            Blocks blocks = new(matrix);
            BlocksGhost blocksGhost = new(matrix);

            Console.Title = "TetrisC";

            Console.SetWindowPosition(0, 0);

            Console.WindowWidth = hold.Width + matrix.Width + next.Width + 4; // margin.
            Console.WindowHeight = Math.Max(Math.Max(hold.Height + Stats.Height + Controls.Height + 2, matrix.Height + 2), next.Height + 2); // margin.

            Console.BufferWidth = Console.WindowWidth;
            Console.BufferHeight = Console.WindowHeight;

            Console.OutputEncoding = System.Text.Encoding.Unicode;
            Console.CursorVisible = false;

            Console.TreatControlCAsInput = true;

            Console.Clear();
            Console.ResetColor();

            PrintInit(hold, stats, controls, matrix, next);

            Logo.Print(xOffset: hold.Width + 4, yOffset: 3);

            for (int i = 3; i >= 1; i--)
            {
                matrix.PrintCaption($"[ LOADING: {i} ]");
                BeepAsync(frequencyA: 900, duration: 200);

                Thread.Sleep(1000);
            }
            matrix.PrintCaption();
            BeepAsync(frequencyA: 900, duration: 600);

            PrintUpdate(hold, stats, controls, matrix, next); // stats, controls, matrix.

            Thread inputThread = new(InputThread);
            inputThread.IsBackground = true;
            inputThread.Start((state, hold, stats, controls, matrix, next, bag, blocks, blocksGhost));

            while (!state.Exit)
            {
                Stopwatch sW = new();

                while (!state.Exit && !state.Start)
                {
                    int frameTime = 100; // ms.

                    sW.Restart();

                    if (Console.KeyAvailable)
                    {
                        ConsoleKey cK = Console.ReadKey(true).Key;

                        ThreadPool.QueueUserWorkItem((_) => { while (Console.KeyAvailable) Console.ReadKey(true); });

                        switch (cK)
                        {
                            case ConsoleKey.Enter:
                            {
                                stats.Reset();
                                matrix.Reset();
                                bag.Reset();

                                PrintUpdate(hold, stats, controls, matrix, next); // stats, matrix, next.

                                for (int i = 3; i >= 1; i--)
                                {
                                    matrix.PrintCaption($"[ STARTING: {i} ]");
                                    BeepAsync(frequencyA: 900, duration: 200);

                                    Thread.Sleep(1000);
                                }
                                matrix.PrintCaption();
                                BeepAsync(frequencyA: 900, duration: 600);

                                bag.NextIndex();

                                if (blocks[bag.Index].TrySpawn())
                                {
                                    blocks[bag.Index].CanHold = true;

                                    blocks[bag.Index].IsLocked = false;

                                    blocksGhost[bag.Index].TryGhost(blocks[bag.Index]);
                                }

                                PrintUpdate(hold, stats, controls, matrix, next); // controls, matrix, next.

                                state.Start = true;

                                Thread.Sleep(stats.GetFrameTime());

                                break;
                            }

                            case ConsoleKey.L:
                            {
                                stats.Reset();

                                stats.InitLevel++;

                                stats.PrintUpdate();

                                break;
                            }

                            case ConsoleKey.Escape:
                            {
                                state.Exit = true;

                                inputThread.Join();

                                frameTime = 0;

                                break;
                            }
                        }
                    }

                    sW.Stop();

                    int frameTimeLeft = frameTime - (int)sW.ElapsedMilliseconds;

                    SleepAsync(frameTimeLeft);
                }

                /**/

                while (state.Start)
                {
                    int frameTime = stats.GetFrameTime();

                    sW.Restart();

                    lock (_lock)
                    {
                        if (!blocks[bag.Index].IsLocked)
                        {
                            if (!blocks[bag.Index].IsHold)
                            {
                                if (blocks[bag.Index].TryTranslate(Translation.ShiftDown))
                                {
                                    if (blocks[bag.Index].HasLanded())
                                    {
                                        blocks[bag.Index].IsPreLocked = true;

                                        frameTime = Block.MaxLockDelayShortTerm;
                                    }
                                    else
                                    {
                                        blocks[bag.Index].IsPreLocked = false;
                                    }

                                    blocksGhost[bag.Index].TryGhost(blocks[bag.Index]);
                                }
                                else
                                {
                                    if (blocks[bag.Index].IsPreLocked)
                                    {
                                        blocks[bag.Index].IsLocked = true;
                                        blocks[bag.Index].IsHardDrop = false;

                                        blocks[bag.Index].IsPreLocked = false;
                                        blocks[bag.Index].LockDelayLongTerm = 0;

                                        frameTime = 0;
                                    }
                                    else
                                    {
                                        blocks[bag.Index].IsPreLocked = true;

                                        frameTime = Block.MaxLockDelayShortTerm;
                                    }
                                }

                                if (blocks[bag.Index].IsLocked)
                                {
                                    Debug.Assert(blocksGhost[bag.Index].Positions.Count == 0);

                                    if (matrix.TryFindLineClears(out List<LineClearType> lineClears))
                                    {
                                        foreach (LineClearType lineClear in lineClears)
                                        {
                                            stats.LineClear(lineClear);

                                            BeepAsync(frequencyA: 800 + (int)lineClear * 100, duration: 200);
                                        }

                                        PrintUpdate(hold, stats, controls, matrix, next, blocks[bag.Index].IsLockDelay()); // stats, matrix (with isLockDelay).

                                        sW.Stop();
                                        Thread.Sleep(500); // TODO: const.
                                        sW.Start();

                                        matrix.RemoveLineClears();
                                    }
                                    else
                                    {
                                        BeepAsync(frequencyA: 800, duration: 200);
                                    }
                                }
                            }
                            else
                            {
                                bag.SetHoldIndex(hold.Index);

                                hold.Index = blocks[bag.Index].BlockType;

                                blocks[bag.Index].IsLocked = true;
                                blocks[bag.Index].IsHardDrop = false;

                                blocks[bag.Index].IsPreLocked = false;
                                blocks[bag.Index].LockDelayLongTerm = 0;

                                blocks[bag.Index].Clear();
                                blocksGhost[bag.Index].Clear();

                                frameTime = 0;
                            }
                        }
                        else
                        {
                            bool canHold = true;

                            if (blocks[bag.Index].IsHold)
                            {
                                blocks[bag.Index].IsHold = false;

                                canHold = false;
                            }

                            bag.NextIndex();

                            if (blocks[bag.Index].TrySpawn())
                            {
                                blocks[bag.Index].CanHold = canHold;

                                blocks[bag.Index].IsLocked = false;

                                blocksGhost[bag.Index].TryGhost(blocks[bag.Index]);
                            }
                            else
                            {
                                state.Start = false;

                                matrix.PrintCaption("[ GAME OVER ]");

                                hold.ResetAndPrintUpdate();
                                next.ResetAndPrintUpdate();

                                BeepAsync(frequencyA: 700, duration: 600);

                                frameTime = 0;
                            }
                        }

                        PrintUpdate(hold, stats, controls, matrix, next, blocks[bag.Index].IsLockDelay());
                    }

                    sW.Stop();

                    if (frameTime != Block.MaxLockDelayShortTerm) // frameTime == stats.GetFrameTime() or frameTime == 0.
                    {
                        int frameTimeLeft = frameTime - (int)sW.ElapsedMilliseconds;

                        frameTimeLeft = SleepAsync(frameTimeLeft, () => !state.Pause, () => !blocks[bag.Index].IsHold, () => !blocks[bag.Index].HasLanded());

                        while (state.Pause)
                        {
                            SleepAsync(int.MaxValue, () => state.Pause);

                            if (state.Start)
                            {
                                frameTimeLeft = SleepAsync(frameTimeLeft, () => !state.Pause, () => !blocks[bag.Index].IsHold, () => !blocks[bag.Index].HasLanded());
                            }
                        }
                    }
                    else if (stats.GetFrameTime() < Block.MaxLockDelayShortTerm) // 7 < 500.
                    {
                        int frameTimeLeft = stats.GetFrameTime() - (int)sW.ElapsedMilliseconds;

                        frameTimeLeft = SleepAsync(frameTimeLeft, () => !state.Pause, () => !blocks[bag.Index].IsHold);

                        while (state.Pause)
                        {
                            SleepAsync(int.MaxValue, () => state.Pause);

                            if (state.Start)
                            {
                                frameTimeLeft = SleepAsync(frameTimeLeft, () => !state.Pause, () => !blocks[bag.Index].IsHold);
                            }
                        }

                        /**/

                        int frameTimeLeft2 = Block.MaxLockDelayShortTerm - Math.Max(stats.GetFrameTime(), (int)sW.ElapsedMilliseconds);

                        frameTimeLeft2 = SleepAsync(frameTimeLeft2, () => !state.Pause, () => !blocks[bag.Index].IsHold, () => blocks[bag.Index].HasLanded());

                        while (state.Pause)
                        {
                            SleepAsync(int.MaxValue, () => state.Pause);

                            if (state.Start)
                            {
                                frameTimeLeft2 = SleepAsync(frameTimeLeft2, () => !state.Pause, () => !blocks[bag.Index].IsHold, () => blocks[bag.Index].HasLanded());
                            }
                        }
                    }
                    else if (stats.GetFrameTime() > Block.MaxLockDelayShortTerm) // 1000 > 500.
                    {
                        int frameTimeLeft = Block.MaxLockDelayShortTerm - (int)sW.ElapsedMilliseconds;

                        frameTimeLeft = SleepAsync(frameTimeLeft, () => !state.Pause, () => !blocks[bag.Index].IsHold);

                        while (state.Pause)
                        {
                            SleepAsync(int.MaxValue, () => state.Pause);

                            if (state.Start)
                            {
                                frameTimeLeft = SleepAsync(frameTimeLeft, () => !state.Pause, () => !blocks[bag.Index].IsHold);
                            }
                        }

                        /**/

                        int frameTimeLeft2 = stats.GetFrameTime() - Block.MaxLockDelayShortTerm;

                        frameTimeLeft2 = SleepAsync(frameTimeLeft2, () => !state.Pause, () => !blocks[bag.Index].IsHold, () => !blocks[bag.Index].HasLanded());

                        while (state.Pause)
                        {
                            SleepAsync(int.MaxValue, () => state.Pause);

                            if (state.Start)
                            {
                                frameTimeLeft2 = SleepAsync(frameTimeLeft2, () => !state.Pause, () => !blocks[bag.Index].IsHold, () => !blocks[bag.Index].HasLanded());
                            }
                        }
                    }
                }
            }

            Console.OutputEncoding = System.Text.Encoding.Default;
            Console.CursorVisible = true;

            Console.TreatControlCAsInput = false;

            Console.Clear();
            Console.ResetColor();
        }
    }
}
