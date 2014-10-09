using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpBot.Protocol
{
    public class Move
    {
        public Move(MoveType type, BoardLocation from, BoardLocation to)
        {
            this.type = type;
            this.from = from;
            this.to = to;
            if (type == MoveType.Pass)
            {
                this.from = null;
                this.to = null;
            }
        }

        private readonly MoveType type;
        public MoveType Type { get { return type; } }

        private readonly BoardLocation from;
        public BoardLocation From { get { return from; } }

        private readonly BoardLocation to;
        public BoardLocation To { get { return to; } }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            if (obj is Move)
            {
                var move = obj as Move;
                return Equals(move);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return (To.X*1000 + to.Y*100 + From.X * 10 + from.Y).GetHashCode();
        }

        public bool Equals(Move move)
        {
            if (move == null)
            {
                return false;
            }
            return move.To.Equals(To) && move.From.Equals(From);
        }

        public static bool operator ==(Move move1, Move move2)
        {
            if ((object)move1 == null || ((object)move2 == null))
            {
                return Object.Equals(move1, move2);
            }
            return (move1.Equals(move2));
        }

        public static bool operator !=(Move move1, Move move2)
        {
            if ((object)move1 == null || ((object)move2 == null))
            {
                return !Object.Equals(move1, move2);
            }
            return !(move1.Equals(move2));
        }

    }
}
