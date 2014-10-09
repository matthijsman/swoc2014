using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpBot.Protocol;

namespace SharpBot
{
    public class Bot : IBot
    {
        private Player myColor;
        private static int BoardCount = 0;
        double pebbleValueEnemy;
        double rockValueEnemy;
        double boulderValueEnemy;

        double pebbleValue;
        double rockValue;
        double boulderValue;

        List<BoardLocation> attackableStones = new List<BoardLocation>();

        MoveType[] m_AllowedMoves;

        public void HandleInitiate(Protocol.InitiateRequest request)
        {
            myColor = request.Color;
        }

        public Protocol.Move HandleMove(Protocol.MoveRequest request)
        {
            var start = DateTime.Now;
            Move firstOption = HandleMoveOldStyle(request);
            //Move secondOption = HandleMoveNewStyle(request);
            Console.Error.WriteLine((DateTime.Now - start).TotalMilliseconds + " total board count: " + BoardCount);
            BoardCount = 0;
            //EVERYTHING IS BROKEN
            return firstOption;

        }

        private Move HandleMoveNewStyle(MoveRequest request)
        {
            if (request.AllowedMoves.Count() == 1)
            {
                List<Move> doneMoves = new List<Move>();
                List<Tuple<Move, Move, double>> monsterStuff = new List<Tuple<Move, Move, double>>();
                foreach (var location in GetMyLocations(request.Board))
                {
                    foreach (var move in GetPossibleToLocations(request.Board, location, true).Select<BoardLocation, Move>(l => CreateMove(request.Board, location, l)))
                    {
                        //.Where(m => !doneMoves.Contains(m))
                        var newBoard = CopyBoard(request.Board);
                        newBoard.ApplyMove(move);
                        foreach (var nextLocation in GetMyLocations(newBoard))
                        {
                            foreach (var secondMove in GetPossibleToLocations(newBoard, nextLocation).Where(l => l != nextLocation).Select<BoardLocation, Move>(l => CreateMove(newBoard, nextLocation, l)).Where(m => !doneMoves.Contains(m)))
                            {
                                var evenNewerBoard = CopyBoard(newBoard);
                                evenNewerBoard.ApplyMove(secondMove);
                                monsterStuff.Add(new Tuple<Move, Move, double>(move, secondMove, GetBoardDanger(evenNewerBoard)));
                                doneMoves.Add(move);
                            }
                        }
                    }
                }
                Console.Error.Write("SIZE: " + monsterStuff.Count + " ");
                var orderedMonster = monsterStuff.OrderBy(t => t.Item3);
                return orderedMonster.First().Item1;
            }
            else
            {
                return HandleMoveOldStyle(request);
            }
        }

        private Move HandleMoveOldStyle(Protocol.MoveRequest request)
        {
            var stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();
            m_AllowedMoves = request.AllowedMoves;
            DateTime start = DateTime.Now;
            var validMoves = new List<Move>();
            foreach (var location in GetMyLocations(request.Board))
            {
                validMoves.AddRange(GetPossibleToLocations(request.Board, location).Select<BoardLocation, Move>(l => CreateMove(request.Board, location, l)).Where(m => m.From != m.To).Where(m => request.AllowedMoves.Contains(m.Type)));
            }
            int amountOfMoves = validMoves.Count();
            SetEnemyValues(request.Board);
            //.AsParallel()
            var printableValidMoves = validMoves.Select(m => GetPrintableMoveWithDanger(request.Board, m, (request.AllowedMoves.Count() == 1))).ToList().OrderBy(m => m.Danger - m.Value).ToList();
            var MovesForSecondStep = printableValidMoves.Take(14).ToList();
            var combinedMoves = new List<PrintableMove>();
            if (request.AllowedMoves.Count() == 1)
            {
                // var listForNextStep = printableValidMoves.Take(20);
                foreach (var firstMove in printableValidMoves)
                {
                    if (stopWatch.Elapsed > TimeSpan.FromSeconds(2))
                    {
                        break;
                    }
                    //Board resultBoard = GetNewBoard(request.Board, firstMove.InnerMove);
                    CalculateSecondMove(combinedMoves, firstMove);
                }
            }
            int milliseconds = (int)Math.Ceiling((DateTime.Now - start).TotalMilliseconds);
            Console.Error.WriteLine("moves: " + amountOfMoves.ToString() + " Time(milliseconds): " + milliseconds.ToString());
            if (request.AllowedMoves.Count() == 1)
            {
                combinedMoves = combinedMoves.OrderBy(m => m.Danger - m.Value).ToList();
                return combinedMoves.First().InnerMove;
            }
            else
            {
                return printableValidMoves.First().InnerMove;
            }
        }

        private void CalculateSecondMove(List<PrintableMove> combinedMoves, PrintableMove firstMove)
        {
            var resultBoard = firstMove.board;
            var validNextMoves = new List<Move>();
            foreach (var location in GetMyLocations(resultBoard))
            {
                validNextMoves.AddRange(GetPossibleToLocations(resultBoard, location).Select<BoardLocation, Move>(l => CreateMove(resultBoard, location, l)).Where(m => m.From != m.To));
            }
            var Moves = validNextMoves.Select(m => GetPrintableMoveWithDanger(resultBoard, m)).OrderBy(m => m.Danger - m.Value);
            var bestMove = Moves.First();
            firstMove.Danger = bestMove.Danger;
            firstMove.Value = bestMove.Value;
            combinedMoves.Add(firstMove);
        }

        private PrintableMove GetPrintableMoveWithDanger(Board oldBoard, Move m, bool nextMove = false)
        {
            //ideas:
            /*
             * Add danger for when the enemy can make me not move anymore, so given a board, add the risk 
             * is the stone value correct?
             */
            double hisOrigValue = getHisTotalValue(oldBoard);
            double stoneValue = CalculateStoneValue(oldBoard, m.To);

            Board board = GetNewBoard(oldBoard, m);
            double thisValue = getHisTotalValue(board);
            double totalValue = 1.3*(hisOrigValue - thisValue);
            double totalDanger = 0;
            if (m.Type == MoveType.Strengthen)
            {
                totalValue  += 0.5-stoneValue;
            }

            int stonesThatCanAttack = 0;
            foreach (BoardLocation location in AllLegalBoardLocations().Where(l => board.GetOwner(l) == myColor))
            {
                var danger = CalculateDanger(board, location);
                if (CanAttack(board, location, true))
                {
                    stonesThatCanAttack++;
                }
                totalDanger += danger;
            }
            if (stonesThatCanAttack < 3)
            {
                if (stonesThatCanAttack == 0)
                {
                  //  totalDanger += 10;
                }
                else
                {
                    //totalDanger = Math.Min(100, totalDanger + (Math.Max(1, (4 - stonesThatCanAttack))));
                }
            }

            //totalDanger += GetBoardDanger(board);
            int hisAmountOfAttacks = 0;

            if (m_AllowedMoves.Count() > 2)
            {
                foreach (var location in AllLegalBoardLocations().Where(l => board.GetOwner(l) != myColor))
                {
                    if (CanAttack(board, location))
                    {
                        hisAmountOfAttacks++;
                    }
                }
                if (hisAmountOfAttacks == 0)
                {
                    totalValue = int.MaxValue;
                }

            }
            int disAttStones = attackableStones.Distinct().Count();
            if (disAttStones < 4)
            {
                totalDanger += 5 * (4 - disAttStones);
            }
            attackableStones.Clear();


            totalDanger = ApplyLoseConditions(board, totalDanger);

            totalValue = ApplyWinConditions(board, totalValue);


            if (false)
            {
                List<Move> nextMoves = new List<Move>();
                foreach (var location in GetMyLocations(board))
                {
                    nextMoves.AddRange(GetPossibleToLocations(board, location).Select<BoardLocation, Move>(l => CreateMove(board, location, l)).Where(move => move.From != move.To));
                }
                var printableValidMoves = nextMoves.Select(move => GetPrintableMoveWithDanger(board, move)).OrderBy(move => (move.Danger) - move.Value).ToList();
                if (printableValidMoves.Any())
                {
                    totalDanger = printableValidMoves.First().Danger;
                    totalValue = printableValidMoves.First().Value;
                }
            }
            return new PrintableMove(m, totalDanger, totalValue, board);
        }

        private double GetBoardDanger(Board board)
        {
            var hisStones = GetHisLocations(board);
            List<MoveWithValue> hisPossibleMoves = new List<MoveWithValue>();
            foreach (var stone in hisStones)
            {
                hisPossibleMoves.AddRange(GetPossibleToLocations(board,stone,true).Select(l => new MoveWithValue(new Move(MoveType.Attack,stone,l),board)));
            }
            double maxDanger = 0;
            foreach (var move in hisPossibleMoves)
            {
                try
                {
                    Stone removedStone = board.GetStone(move.To);
                    var newBoard = GetNewBoard(board, move);
                    var maxDangerSecond = GetPossibleToLocations(newBoard, move.To, true).Max(l => new MoveWithValue(new Move(MoveType.Attack, move.From, l), newBoard).Value);
                    var maxDangerOtherFirst = hisPossibleMoves.Where(m => m.From != move.From).Max(m => m.Value);
                    var curDanger = move.Value + Math.Max(maxDangerSecond, maxDangerOtherFirst);
                    maxDanger = Math.Max(curDanger, maxDanger);
                    BoardCount++;
                }
                catch { }
            }
            
            return maxDanger;
        }

        

        private double getHisTotalValue(Board board)
        {
            return ((double)GetHisNumberofStones(board, Stone.pebble) * pebbleValueEnemy) + ((double)GetHisNumberofStones(board, Stone.rock) * rockValueEnemy) + ((double)GetHisNumberofStones(board, Stone.boulder) * boulderValueEnemy);
        }

        private double getMyTotalValue(Board board)
        {
            return ((double)GetMyNumberofStones(board, Stone.pebble) * pebbleValueEnemy) + ((double)GetMyNumberofStones(board, Stone.rock) * rockValueEnemy) + ((double)GetMyNumberofStones(board, Stone.boulder) * boulderValueEnemy);
        }

        private double ApplyWinConditions(Board board, double totalValue)
        {
            if (GetHisNumberofStones(board, Stone.boulder) == 0)
            {
                totalValue = int.MaxValue;
            }
            if (GetHisNumberofStones(board, Stone.rock) == 0)
            {
                totalValue = int.MaxValue;
            }
            if (GetHisNumberofStones(board, Stone.pebble) == 0)
            {
                totalValue = int.MaxValue;
            }
            //  \0/
            //   |
            //  / \
            return totalValue;
        }

        public void SetAllowedMoves(MoveType[] moves)
        {
            m_AllowedMoves = moves;
        }

        public double ApplyLoseConditions(Board board, double totalDanger)
        {
            if (GetAmountOfSimilar(board, Stone.boulder) == 0)
            {
                totalDanger = int.MaxValue;
            }
            if (GetAmountOfSimilar(board, Stone.rock) == 0)
            {
                totalDanger = int.MaxValue;
            }
            if (GetAmountOfSimilar(board, Stone.pebble) == 0)
            {
                totalDanger = int.MaxValue;
            }
            //difficult bit: what if the enemy can slay my two last stones (oh noes!)
            //only valid if we can only attack i.e. when the enemy has two moves coming up
            if (m_AllowedMoves.Count() > 1)
            {
                //does the orderby work?
                var thing = (from l in GetMyLocations(board) where GetAmountOfSimilar(board, board.GetStone(l)) == 2 select l).OrderBy(l => board.GetStone(l)).ToList();
                while (thing.Count > 0)
                {
                    var stonePair = thing.Take(2).ToList();
                    var enemyStonesAttacking1 = GetPossibleFromLocations(board, stonePair[0])
                        .Where(l => board.GetOwner(l) != myColor && board.GetHeight(l) >= board.GetHeight(stonePair[0])).ToList();
                    var enemyStonesAttacking2 = GetPossibleFromLocations(board, stonePair[1])
                        .Where(l => board.GetOwner(l) != myColor && board.GetHeight(l) >= board.GetHeight(stonePair[1])).ToList();
                    if (enemyStonesAttacking1.Count() > 0 || enemyStonesAttacking2.Count() > 0)
                    {
                        var height1 = board.GetHeight(stonePair[0]);
                        var height2 = board.GetHeight(stonePair[1]);
                        if (GetPossibleToLocations(board, stonePair[0]).Contains(stonePair[1]))
                        {
                            var somes = enemyStonesAttacking1.Union(enemyStonesAttacking2).Max(s => board.GetHeight(s));
                            if (somes > Math.Max(height1, height2))
                            {
                                return int.MaxValue;
                            }
                        }
                        else
                        {
                            if (enemyStonesAttacking1.Count() != 0 && enemyStonesAttacking2.Count() != 0)
                            {
                                //the distinct may not be needed
                                var unionSet = enemyStonesAttacking1.Union(enemyStonesAttacking2).Distinct();
                                if (unionSet.Count() > 1)
                                {
                                    return int.MaxValue;
                                }
                            }
                        }
                    }
                    thing = thing.Skip(2).ToList();
                }
            }
            return totalDanger;
        }

        private bool CanAttack(Board board, BoardLocation location, bool storeToLocations = false)
        {

            if (storeToLocations)
            {
                var possibleToLocations = GetPossibleToLocations(board, location, true);
                attackableStones.AddRange(possibleToLocations);
                return possibleToLocations.Count > 0;
            }
            else
            {
                return GetPossibleToLocations(board, location, true).Any();
            }
           
        }

        private double CalculateDanger(Board board, BoardLocation position)
        {
            double stoneValue = CalculateStoneValue(board, position);
            double risk = CalculateRisk(board, position);
            double danger = stoneValue * risk;
            return danger;
        }

        private double CalculateStoneValue(Board board, BoardLocation position)
        {
            var currentStone = board.GetStone(position);
            if (currentStone == Stone.None)
            {
                return 0;
            }
            int numberOfSimilar = GetAmountOfSimilar(board, currentStone);
            double stoneValue = 1.0 / (double)(numberOfSimilar * numberOfSimilar);
            stoneValue *= board.GetHeight(position);
            return stoneValue;
        }

        private double CalculateRisk(Board board, BoardLocation position)
        {
            double risk = GetPossibleFromLocations(board, position).Where(l => board.GetOwner(l) != myColor && board.GetHeight(l) >= board.GetHeight(position)).Count();
            //second step: 
            foreach (var location in GetPossibleToLocations(board, position).Where(p => board.GetOwner(p) == myColor))
            {
                //elke dude die mij kan slaan, en de dude op position
                risk += GetPossibleFromLocations(board, location).Where(l => board.GetOwner(l) != myColor && board.GetHeight(l) >= board.GetHeight(location) && board.GetHeight(l) >= board.GetHeight(position)).Count();
            }
            //'the move out of the way one'
            //foreach enemy stone that border me, but cannot attack (the rest is included in the risk)                          AND can move out of the way
            var thing = GetPossibleToLocations(board, position, true).Where(s => board.GetOwner(s) != myColor).Where(s => board.GetHeight(s) < board.GetHeight(position)).Where(s => GetPossibleToLocations(board, s, true).Where(e => e != position).Count() > 0).Where(s => GetPossibleToLocations(board, s).Any(p => board.GetHeight(p) >= board.GetHeight(position) && IsInAStraightLine(board, position, s, p))).Count();
            return risk+thing;
        }

        private bool IsInAStraightLine(Board board, BoardLocation me, BoardLocation coward, BoardLocation scary)
        {
            if (coward == scary)
            {
                return false;
            }
            //directions:

            // 0 -1
            var firstFound = GetFirstNonEmptyInDirection(board, me, 0, -1);
            var secondFound = GetFirstNonEmptyInDirection(board, coward, 0, -1);
            if (firstFound != null && secondFound != null && firstFound.Equals(coward) && secondFound.Equals(scary))
            {
                return true;
            }
            // 0  1 
            firstFound = GetFirstNonEmptyInDirection(board, me, 0, 1);
            secondFound = GetFirstNonEmptyInDirection(board, coward, 0, 1);
            if (firstFound != null && secondFound != null && firstFound.Equals(coward) && secondFound.Equals(scary))
            {
                return true;
            }
            // 1  0 
            firstFound = GetFirstNonEmptyInDirection(board, me,1,0);
            secondFound = GetFirstNonEmptyInDirection(board, coward, 1,0);
            if (firstFound != null && secondFound != null && firstFound.Equals(coward) && secondFound.Equals(scary))
            {
                return true;
            }
            //-1  0
            firstFound = GetFirstNonEmptyInDirection(board, me, -1,0);
            secondFound = GetFirstNonEmptyInDirection(board, coward, -1,0);
            if (firstFound != null && secondFound != null && firstFound.Equals(coward) && secondFound.Equals(scary))
            {
                return true;
            }
            //-1 -1
            firstFound = GetFirstNonEmptyInDirection(board, me, -1,-1);
            secondFound = GetFirstNonEmptyInDirection(board, coward,-1,-1);
            if (firstFound != null && secondFound != null && firstFound.Equals(coward) && secondFound.Equals(scary))
            {
                return true;
            }
            // 1  1
            firstFound = GetFirstNonEmptyInDirection(board, me,1,1);
            secondFound = GetFirstNonEmptyInDirection(board, coward, 1,1);
            if (firstFound != null && secondFound != null && firstFound.Equals(coward) && secondFound.Equals(scary))
            {
                return true;
            }

            return false;
        }

        private Move CreateMove(Board board, BoardLocation from, BoardLocation to)
        {
            if (board.GetOwner(to) == myColor)
            {
                return new Move(MoveType.Strengthen, from, to);
            }
            else
            {
                return new Move(MoveType.Attack, from, to);
            }
        }

        private void SetEnemyValues(Board board)
        {
            pebbleValueEnemy = (double)1 / (double)GetHisNumberofStones(board, Stone.pebble);
            rockValueEnemy = (double)1 / (double)GetHisNumberofStones(board, Stone.rock);
            boulderValueEnemy = (double)1 / (double)GetHisNumberofStones(board, Stone.boulder);

        }

        private Board GetNewBoard(Board oldBoard, Move move)
        {
            Board newBoard = CopyBoard(oldBoard);
            newBoard.ApplyMove(move);
            return newBoard;
        }

        private Board CopyBoard(Board oldBoard)
        {
            int[][] state = new int[9][];
            for (int i = 0; i < 9; i++)
            {
                int[] row = new int[9];

                for (int j = 0; j < 9; j++)
                {
                    row[j] = oldBoard.state[i][j];
                }
                state[i] = row;
            }

            // Board board = new Board(oldBoard);
            return new Board(state);
        }

        private int GetAmountOfSimilar(Board board, Stone stone)
        {
            int numberOfSimilar = GetMyLocations(board).Count(l => board.GetStone(l) == stone);
            return numberOfSimilar;
        }

        private IEnumerable<BoardLocation> GetMyLocations(Board board)
        {
            var mystones = AllLegalBoardLocations().Where(l => board.GetOwner(l) == myColor);
            return mystones;
        }

        private IEnumerable<BoardLocation> GetHisLocations(Board board)
        {
            var hisStones = AllLegalBoardLocations().Where(l => board.GetOwner(l) != myColor);
            return hisStones;
        }

        int GetHisNumberofStones(Board board, Stone stone)
        {
            return GetHisLocations(board).Where(l => board.GetStone(l) == stone).Count();
        }

        int GetMyNumberofStones(Board board, Stone stone)
        {
            return GetMyLocations(board).Where(l => board.GetStone(l) == stone).Count();
        }

        public void HandleProcessedMove(Protocol.ProcessedMove move)
        {
            // Ignore what the other did
        }

        private static List<BoardLocation> GetPossibleToLocations(Board board, BoardLocation fromLocation, bool onlyAttack = false)
        {
            List<BoardLocation> possibleToLocations = new List<BoardLocation>();

            // always possible to pass
            if (!onlyAttack)
            {
                possibleToLocations.Add(fromLocation);
            }
            BoardLocation north = GetFirstNonEmptyInDirection(board, fromLocation, 0, -1);
            if (north != null && IsValidMove(board, fromLocation, north, onlyAttack)) possibleToLocations.Add(north);

            BoardLocation south = GetFirstNonEmptyInDirection(board, fromLocation, 0, 1);
            if (south != null && IsValidMove(board, fromLocation, south, onlyAttack)) possibleToLocations.Add(south);

            BoardLocation east = GetFirstNonEmptyInDirection(board, fromLocation, 1, 0);
            if (east != null && IsValidMove(board, fromLocation, east, onlyAttack)) possibleToLocations.Add(east);

            BoardLocation west = GetFirstNonEmptyInDirection(board, fromLocation, -1, 0);
            if (west != null && IsValidMove(board, fromLocation, west, onlyAttack)) possibleToLocations.Add(west);

            BoardLocation northWest = GetFirstNonEmptyInDirection(board, fromLocation, -1, -1);
            if (northWest != null && IsValidMove(board, fromLocation, northWest, onlyAttack)) possibleToLocations.Add(northWest);

            BoardLocation southEast = GetFirstNonEmptyInDirection(board, fromLocation, 1, 1);
            if (southEast != null && IsValidMove(board, fromLocation, southEast, onlyAttack)) possibleToLocations.Add(southEast);
            return possibleToLocations;
        }

        private static List<BoardLocation> GetPossibleFromLocations(Board board, BoardLocation toLocation)
        {
            List<BoardLocation> possibleFromLocations = new List<BoardLocation>();

            // always possible to pass
            //possibleFromLocations.Add(toLocation);

            BoardLocation north = GetFirstNonEmptyInDirection(board, toLocation, 0, -1);
            if (north != null && IsValidMove(board, north, toLocation)) possibleFromLocations.Add(north);

            BoardLocation south = GetFirstNonEmptyInDirection(board, toLocation, 0, 1);
            if (south != null && IsValidMove(board, south, toLocation)) possibleFromLocations.Add(south);

            BoardLocation east = GetFirstNonEmptyInDirection(board, toLocation, 1, 0);
            if (east != null && IsValidMove(board, east, toLocation)) possibleFromLocations.Add(east);

            BoardLocation west = GetFirstNonEmptyInDirection(board, toLocation, -1, 0);
            if (west != null && IsValidMove(board, west, toLocation)) possibleFromLocations.Add(west);

            BoardLocation northWest = GetFirstNonEmptyInDirection(board, toLocation, -1, -1);
            if (northWest != null && IsValidMove(board, northWest, toLocation)) possibleFromLocations.Add(northWest);

            BoardLocation southEast = GetFirstNonEmptyInDirection(board, toLocation, 1, 1);
            if (southEast != null && IsValidMove(board, southEast, toLocation)) possibleFromLocations.Add(southEast);
            return possibleFromLocations;
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
