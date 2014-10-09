using SharpBot.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpBot
{
    public static class GameState
    {
        public static Dictionary<BoardLocation, List<BoardLocation>> myPossibleToLocations = new Dictionary<BoardLocation,List<BoardLocation>>();
        public static Dictionary<BoardLocation, List<BoardLocation>> myPossibleFromLocations = new Dictionary<BoardLocation, List<BoardLocation>>();
        public static Dictionary<BoardLocation, List<BoardLocation>> myAttackMoves = new Dictionary<BoardLocation, List<BoardLocation>>();
        public static List<BoardLocation> myLocations = new List<BoardLocation>();
        public static Dictionary<Stone, int> myMaxStoneHeight = new Dictionary<Stone, int>{{Stone.boulder,0},{Stone.pebble,0},{Stone.rock,0}};
        public static int myHighest = 0;

        public static Dictionary<BoardLocation, List<BoardLocation>> hisPossibleToLocations = new Dictionary<BoardLocation, List<BoardLocation>>();
        public static Dictionary<BoardLocation, List<BoardLocation>> hisPossibleFromLocations = new Dictionary<BoardLocation, List<BoardLocation>>();
        public static Dictionary<BoardLocation, List<BoardLocation>> hisAttackMoves = new Dictionary<BoardLocation, List<BoardLocation>>();
        public static List<BoardLocation> hisLocations = new List<BoardLocation>();
        public static Dictionary<Stone, int> hisMaxStoneheight = new Dictionary<Stone, int> { { Stone.boulder, 0 }, { Stone.pebble, 0 }, { Stone.rock, 0 } };


        public static void CalculateGameState(Board board, Player me)
        {
            myPossibleToLocations.Clear();
            myPossibleFromLocations.Clear();
            myAttackMoves.Clear();
            myLocations.Clear();
            myMaxStoneHeight[Stone.boulder] = 0;
            myMaxStoneHeight[Stone.pebble] = 0;
            myMaxStoneHeight[Stone.rock] = 0;
            hisMaxStoneheight[Stone.boulder] = 0;
            hisMaxStoneheight[Stone.pebble] = 0;
            hisMaxStoneheight[Stone.rock] = 0;
            myHighest = 0;
            hisAttackMoves.Clear();
            hisLocations.Clear();
            hisPossibleFromLocations.Clear();
            hisPossibleToLocations.Clear();

            for (int y = 0; y < 9; y++)
            {
                for (int x = 0; x < 9; x++)
                {
                    if (BoardLocation.IsLegal(x, y))
                    {
                        var location = new BoardLocation(x, y);
                        Stone stone = board.GetStone(location);
                        if (stone != Stone.None)
                        {
                            int height = board.GetHeight(location);

                            if (board.GetOwner(location) == me)
                            {
                                myLocations.Add(location);
                                myPossibleToLocations.Add(location, new List<BoardLocation>());
                                myPossibleFromLocations.Add(location, new List<BoardLocation>());
                                myAttackMoves.Add(location, new List<BoardLocation>());
                                if (height > myMaxStoneHeight[stone])
                                {
                                    myMaxStoneHeight[stone] = height;
                                }
                                if (height > myHighest)
                                {
                                    myHighest = height;
                                }
                            }
                            else
                            {
                                hisLocations.Add(location);
                                hisPossibleToLocations.Add(location, new List<BoardLocation>());
                                hisPossibleFromLocations.Add(location, new List<BoardLocation>());
                                hisAttackMoves.Add(location, new List<BoardLocation>());
                                if (height > hisMaxStoneheight[stone])
                                {
                                    hisMaxStoneheight[stone] = height;
                                }
                            }
                            GetPossibleToLocations(board, location, me);
                            GetPossibleFromLocations(board, location, me);
                        }
                    }
                }
            }
        }

        private static void GetPossibleToLocations(Board board, BoardLocation fromLocation, Player me)
        {
            List<BoardLocation> possibleToLocations = new List<BoardLocation>();

            BoardLocation north = GetFirstNonEmptyInDirection(board, fromLocation, 0, -1);
            if (north != null && IsValidMove(board, fromLocation, north))
            {
                AddToToList(board, fromLocation, me, north);
            }

            BoardLocation south = GetFirstNonEmptyInDirection(board, fromLocation, 0, 1);
            if (south != null && IsValidMove(board, fromLocation, south))
            {
                AddToToList(board, fromLocation, me, south);
            }

            BoardLocation east = GetFirstNonEmptyInDirection(board, fromLocation, 1, 0);
            if (east != null && IsValidMove(board, fromLocation, east))
            {
                AddToToList(board, fromLocation, me, east);
            }

            BoardLocation west = GetFirstNonEmptyInDirection(board, fromLocation, -1, 0);
            if (west != null && IsValidMove(board, fromLocation, west))
            {
                AddToToList(board, fromLocation, me, west);
            }

            BoardLocation northWest = GetFirstNonEmptyInDirection(board, fromLocation, -1, -1);
            if (northWest != null && IsValidMove(board, fromLocation, northWest))
            {
                AddToToList(board, fromLocation, me, northWest);
            }

            BoardLocation southEast = GetFirstNonEmptyInDirection(board, fromLocation, 1, 1);
            if (southEast != null && IsValidMove(board, fromLocation, southEast))
            {
                AddToToList(board, fromLocation, me, southEast);
            }
        }

        private static void AddToToList(Board board, BoardLocation fromLocation, Player me, BoardLocation toLocation)
        {
            if (board.GetOwner(fromLocation) == me)
            {
                myPossibleToLocations[fromLocation].Add(toLocation);
                if (board.GetOwner(toLocation) != me)
                {
                    myAttackMoves[fromLocation].Add(toLocation);
                }
            }
            else
            {
                hisPossibleToLocations[fromLocation].Add(toLocation);
                if (board.GetOwner(toLocation) == me)
                {
                    hisAttackMoves[fromLocation].Add(toLocation);
                }
            }
        }

        private static void GetPossibleFromLocations(Board board, BoardLocation toLocation, Player me)
        {

            BoardLocation north = GetFirstNonEmptyInDirection(board, toLocation, 0, -1);
            if (north != null && IsValidMove(board, north, toLocation,true))
            {
                if (board.GetOwner(toLocation) == me)
                {
                    myPossibleFromLocations[toLocation].Add(north);
                }
                else
                {
                    hisPossibleFromLocations[toLocation].Add(north);
                }
            }

            BoardLocation south = GetFirstNonEmptyInDirection(board, toLocation, 0, 1);
            if (south != null && IsValidMove(board, south, toLocation, true))
            {
                if (board.GetOwner(toLocation) == me)
                {
                    myPossibleFromLocations[toLocation].Add(south);
                }
                else
                {
                    hisPossibleFromLocations[toLocation].Add(south);
                }
            }

            BoardLocation east = GetFirstNonEmptyInDirection(board, toLocation, 1, 0);
            if (east != null && IsValidMove(board, east, toLocation, true))
            {
                if (board.GetOwner(toLocation) == me)
                {
                    myPossibleFromLocations[toLocation].Add(east);
                }
                else
                {
                    hisPossibleFromLocations[toLocation].Add(east);
                }
            }

            BoardLocation west = GetFirstNonEmptyInDirection(board, toLocation, -1, 0);
            if (west != null && IsValidMove(board, west, toLocation, true))
            {
                if (board.GetOwner(toLocation) == me)
                {
                    myPossibleFromLocations[toLocation].Add(west);
                }
                else
                {
                    hisPossibleFromLocations[toLocation].Add(west);
                }
            }

            BoardLocation northWest = GetFirstNonEmptyInDirection(board, toLocation, -1, -1);
            if (northWest != null && IsValidMove(board, northWest, toLocation, true))
            {
                if (board.GetOwner(toLocation) == me)
                {
                    myPossibleFromLocations[toLocation].Add(northWest);
                }
                else
                {
                    hisPossibleFromLocations[toLocation].Add(northWest);
                }
            }

            BoardLocation southEast = GetFirstNonEmptyInDirection(board, toLocation, 1, 1);
            if (southEast != null && IsValidMove(board, southEast, toLocation, true))
            {
                if (board.GetOwner(toLocation) == me)
                {
                    myPossibleFromLocations[toLocation].Add(southEast);
                }
                else
                {
                    hisPossibleFromLocations[toLocation].Add(southEast);
                }
            }
        }

        private static BoardLocation GetFirstNonEmptyInDirection(Board board, BoardLocation location, int directionX, int directionY)
        {
            int x = location.X;
            int y = location.Y;

            do
            {
                x += directionX;
                y += directionY;
            } while (BoardLocation.IsLegal(x, y) && (board.GetOwner(new BoardLocation(x, y)) == Player.None));

            if (!BoardLocation.IsLegal(x, y))
            {
                return null;
            }

            BoardLocation newLocation = new BoardLocation(x, y);
            if (newLocation == location ||
                board.GetOwner(newLocation) == Player.None)
            {
                return null;
            }
            else
            {
                return newLocation;
            }
        }

        private static bool IsValidMove(Board board, BoardLocation from, BoardLocation to, bool onlyAttack = false)
        {
            Player fromOwner = board.GetOwner(from);
            Player toOwner = board.GetOwner(to);
            int fromHeight = board.GetHeight(from);
            int toHeight = board.GetHeight(to);
            if (onlyAttack)
            {
                return (fromOwner != Player.None) && (toOwner != Player.None) && (fromOwner != toOwner) && (fromHeight >= toHeight);
            }
            else
            {
                return (fromOwner != Player.None) &&
                    (toOwner != Player.None) &&
                    (fromOwner == toOwner || fromHeight >= toHeight);
            }
        }

        private static IEnumerable<BoardLocation> AllLegalBoardLocations()
        {
            for (int y = 0; y < 9; y++)
            {
                for (int x = 0; x < 9; x++)
                {
                    if (BoardLocation.IsLegal(x, y))
                    {
                        yield return new BoardLocation(x, y);
                    }
                }
            }
        }
    }
}
