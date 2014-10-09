using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpBot.Protocol
{
    public class PrintableMove : Move
    {

        private static string[] X = new string[] { "A", "B", "C", "D", "E", "F", "G", "H", "I" };
        private static string[] Y = new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9" };

        public double Danger;
        public double Value;
        public Move InnerMove;
        public Board board;

        public double ValueOfAttackedStone;

        public PrintableMove(Move move, double danger, double value, Board board, double valueOfAttackedStone) : base(move.Type,move.From,move.To)
        {
            Danger = danger;
            Value = value;
            InnerMove = move;
            this.board = board;
            this.ValueOfAttackedStone = valueOfAttackedStone;

        }

        public override string ToString()
        {
            if (base.Type == MoveType.Pass)
            {
                return "Pass";
            }
            return "Type: " + base.Type.ToString() + " From: " + GetString(base.From.X, base.From.Y) + " To: " + GetString(base.To.X, base.To.Y);
        }

        public static string GetString(int x, int y)
        {
            if (x > 4) y--;
            if (x > 5) y--;
            if (x > 6) y--;
            if (x > 7) y--;
            return X[x] + Y[y];
        }
    }

    public class MoveWithValue : Move
    {
        public double Value;
        public double ValueWithOneRemoved;
        public Stone stone;
        public Move InnerMove;
        public MoveWithValue(Move move, Board board) : base(move.Type,move.From,move.To)
        {
            InnerMove = move;
            stone = board.GetStone(move.To);
            Player owner = board.GetOwner(move.To);
            var nrleft = board.GetTotalCount(owner, stone);
            var height = board.GetHeight(move.To) ;
            ValueWithOneRemoved = (double)height / (double)((nrleft-1) * (nrleft-1));
            Value = (double)height / ((double)nrleft * (double)nrleft);
        }

        public double GetValue(Stone removedStone)
        {
            if (stone == removedStone)
            {
                return ValueWithOneRemoved;
            }
            else
            {
                return Value;
            }
        }

    }
}
