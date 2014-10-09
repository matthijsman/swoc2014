using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpBot.Protocol
{
    public class BoardLocation
    {
        public BoardLocation(int x, int y)
        {
            if (!IsLegal(x, y))
            {
                throw new ArgumentException("not a legal board location");
            }

            X = x;
            Y = y;
        }

        public readonly int X;
        public readonly int Y;

        public static bool IsLegal(int x, int y)
        {
            return x >= 0 && x < 9 && y >= 0 && y < 9 &&
                    (x - y) < 5 && (y - x) < 5 && (x != 4 || y != 4);
        }

        public override string ToString()
        {
            return "(" + X.ToString() + ", " + Y.ToString() + ")";
        }

        public override int GetHashCode()
        {
            return (X * 10 + Y).ToString().GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            if (this == null)
            {
                return false;
            }
            if (obj is BoardLocation)
            {
                BoardLocation b2 = obj as BoardLocation;
                return b2.X == X && b2.Y == Y;
            }
            return base.Equals(obj);
        }

        public static bool operator ==(BoardLocation b1, BoardLocation b2)
        {
            if ((object)b1 == null || ((object)b2 == null))
            {
                return Object.Equals(b1,b2);
            }
            return b1.Equals(b2);
        }

        public static bool operator !=(BoardLocation b1, BoardLocation b2)
        {
            if ((object)b1 == null || ((object)b2 == null))
            {
                return ! Object.Equals(b1, b2);
            }
            return !(b1.Equals(b2));
        }
    }
}
