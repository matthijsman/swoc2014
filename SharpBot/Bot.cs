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
        bool debug = false;
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

            Move firstOption = HandleMoveOldStyle(request);


            return firstOption;

        }


        private Move HandleMoveOldStyle(Protocol.MoveRequest request)
        {
            bool onlyAttackMove = request.AllowedMoves.Count() == 1;
            var stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();
            m_AllowedMoves = request.AllowedMoves;
            DateTime start = DateTime.Now;
            GameState.CalculateGameState(request.Board, myColor);

            var validMoves = new List<Move>();
            foreach (var location in GameState.myLocations)
            {
                validMoves.AddRange(GameState.myPossibleToLocations[location].Select<BoardLocation, Move>(l => CreateMove(request.Board, location, l)).Where(m => m.From != m.To).Where(m => request.AllowedMoves.Contains(m.Type)));
            }
            SetEnemyValues(request.Board);
            if (!onlyAttackMove)
            {
                validMoves.Add(new Move(MoveType.Pass, null, null));
            }
            //.AsParallel()  
            if (debug)
            {
                Console.WriteLine("\nfirstmoves: ");
            }
            var printableValidMoves = validMoves.Select(m => GetPrintableMoveWithDanger(request.Board, m, request.AllowedMoves)).ToList();
            if (!onlyAttackMove)
            {
                printableValidMoves = printableValidMoves.OrderBy(m => m.Danger - m.Value).ToList();
            }
            else
            {
                printableValidMoves = printableValidMoves.OrderBy(m => m.Value).ToList();
            }
            var combinedMoves = new List<PrintableMove>();
            if (onlyAttackMove)
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
            
            if (request.AllowedMoves.Count() == 1)
            {
                Console.Error.WriteLine("moves: " + combinedMoves.Count.ToString() + " Time(milliseconds): " + milliseconds.ToString());
                combinedMoves = combinedMoves.OrderBy(m => m.Danger - m.Value).ToList();
                return combinedMoves.First().InnerMove;
            }
            else
            {
                try
                {
                    var firstMove =  printableValidMoves.First();
                    var valueFirstMove = firstMove.Danger - firstMove.Value;
                    var randomList = printableValidMoves.TakeWhile(m => (m.Danger - m.Value) == valueFirstMove).ToList();
                    return randomList.OrderBy(x => Guid.NewGuid()).First().InnerMove;
                   // return printableValidMoves.First().InnerMove;
                }
                catch
                {
                    return new Move(MoveType.Pass, null, null);
                }
            }
        }

        private void CalculateSecondMove(List<PrintableMove> combinedMoves, PrintableMove firstMove)
        {
            var resultBoard = firstMove.board;
            GameState.CalculateGameState(resultBoard, myColor);
            var validNextMoves = new List<Move>();
            foreach (var location in GameState.myLocations)
            {
                validNextMoves.AddRange(GameState.myPossibleToLocations[location].Select<BoardLocation, Move>(l => CreateMove(resultBoard, location, l)).Where(m => m.From != m.To));
            }
            if (debug)
            {
                Console.WriteLine("\nSecond Moves for location: " + firstMove.ToString());
            }
            validNextMoves.Add(new Move(MoveType.Pass, null, null));
            var Moves = validNextMoves.Select(m => GetPrintableMoveWithDanger(resultBoard, m, new MoveType[] { MoveType.Attack, MoveType.Pass, MoveType.Strengthen })).OrderBy(m => m.Danger - m.Value);
            var bestMove = Moves.First();
            firstMove.Danger = bestMove.Danger;
            firstMove.Value = bestMove.Value + firstMove.ValueOfAttackedStone;
            combinedMoves.Add(firstMove);
        }

        public PrintableMove GetPrintableMoveWithDanger(Board oldBoard, Move m, MoveType[] allowedMoves, bool nextMove = false)
        {
            if (debug)
            {
                Console.WriteLine(new PrintableMove(m,0,0,oldBoard,0).ToString());
            }
            Board board = GetNewBoard(oldBoard, m);

            double totalValue = 0;
            if (m.Type == MoveType.Attack)
            {
                var attackedStoneType = oldBoard.GetStone(m.To);
                switch (attackedStoneType)
                {
                    case Stone.None:
                        break;
                    case Stone.pebble:
                        totalValue = pebbleValueEnemy;
                        break;
                    case Stone.rock:
                        totalValue = rockValueEnemy;
                        break;
                    case Stone.boulder:
                        totalValue = boulderValueEnemy;
                        break;
                    default:
                        break;
                }
                totalValue *= oldBoard.GetHeight(m.To);
            }
            double valueOfAttackedStone = totalValue;

            if (allowedMoves.Count() == 1)
            {
                return new PrintableMove(m, 0, totalValue, board, valueOfAttackedStone);
            }
            GameState.CalculateGameState(board, myColor);

            //84b51
            //if (m.From.X == 8 && m.From.Y == 4 && m.To.X == 5 && m.To.Y == 1)
            //{
            //    Console.WriteLine("dees");
            //}

            
            
            double totalDanger = 0;
            
            if (m.Type == MoveType.Strengthen)
            {
                var toHeight = oldBoard.GetHeight(m.To);
                var toType = oldBoard.GetStone(m.To);
                var fromType = oldBoard.GetStone(m.From);
                double removedValue;
                switch (toType)
                {
                    case Stone.None:
                        removedValue = 0;
                        break;
                    case Stone.pebble:
                        removedValue = pebbleValue * toHeight;
                        break;
                    case Stone.rock:
                        removedValue = rockValue * toHeight;
                        break;
                    case Stone.boulder:
                        removedValue = boulderValue * toHeight;
                        break;
                    default:
                        removedValue = 0;
                        break;
                }
                double addedValue;
                switch (fromType)
                {
                    case Stone.None:
                        addedValue = 0;
                        break;
                    case Stone.pebble:
                        addedValue = pebbleValue * toHeight;
                        break;
                    case Stone.rock:
                        addedValue = rockValue * toHeight;
                        break;
                    case Stone.boulder:
                        addedValue = boulderValue * toHeight;
                        break;
                    default:
                        addedValue = 0;
                        break;
                }
                if (debug)
                {
                    Console.WriteLine("value for strengthen: -" + removedValue + " +" + addedValue );
                }
                //addedValue = GameState.myMaxStoneHeight[fromType] <= GameState.hisMaxStoneheight ? addedValue : 0;
                totalValue += 0.5 - removedValue;
            }

            foreach (BoardLocation location in GameState.myLocations)
            {
                var danger = CalculateDanger(board, location);
                if (debug)
                {
                    Console.WriteLine("danger on: "+PrintableMove.GetString(location.X,location.Y) + " = " + danger.ToString());
                }
                totalDanger += danger;
            }

            int hisAmountOfAttacks = 0;

            if (allowedMoves.Count() > 2)
            {
                foreach (var location in GameState.hisLocations)
                {
                    if (CanAttack(board, location))
                    {
                        hisAmountOfAttacks++;
                    }
                }
                if (hisAmountOfAttacks == 0)
                {
                    totalValue += 1000;
                }

            }

            int attackableStones = GameState.hisLocations.Where(s => GameState.myLocations.Where(l => GameState.myAttackMoves[l].Contains(s)).Any()).Count();
            if (attackableStones < 4)
            {
                var noAttackDanger = (4 - attackableStones);
                totalDanger += noAttackDanger;
                if (debug)
                {
                    Console.WriteLine("danger because of attackableStones: " + noAttackDanger.ToString());
                }
            }


            totalDanger = ApplyLoseConditions(board, totalDanger, allowedMoves);

            totalValue = ApplyWinConditions(board, totalValue);
            if (debug)
            {
                Console.WriteLine("totalDanger: "+totalDanger.ToString());
                Console.WriteLine("totalValue: " + totalValue.ToString() + "\n\n");
            }
            return new PrintableMove(m, totalDanger, totalValue, board, valueOfAttackedStone);
        }

        private double GetBoardDanger(Board board)
        {
            //this does not work, and is too slow when i fix it.
            var hisStones = GameState.hisLocations;
            List<MoveWithValue> hisPossibleMoves = new List<MoveWithValue>();
            foreach (var stone in hisStones)
            {
                hisPossibleMoves.AddRange(GameState.hisAttackMoves[stone].Select(l => new MoveWithValue(new Move(MoveType.Attack, stone, l), board)));
            }
            hisPossibleMoves = hisPossibleMoves.OrderByDescending(m => m.Value).ToList();
            double maxDanger = 0;
            foreach (var move in hisPossibleMoves)
            {
                try
                {
                    Stone removedStone = board.GetStone(move.To);
                    var newBoard = GetNewBoard(board, move.InnerMove);
                    var stl = GetPossibleToLocations(newBoard, move.To, true);
                    var secondMoves = stl.Select(l => new MoveWithValue(new Move(MoveType.Attack, move.From, l),newBoard)).OrderBy(m => m.Value);
                    var secondMove = secondMoves.First(); 
                    var maxDangerSecond = secondMove.Value;
                    var maxDangerOtherFirst = hisPossibleMoves.Where(m => m.From != move.From).Max(m => m.GetValue(newBoard.GetStone(secondMove.To))) ;
                    var curDanger = move.Value + Math.Max(maxDangerSecond, maxDangerOtherFirst);
                    maxDanger = Math.Max(curDanger, maxDanger);
                    BoardCount++;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex.Message);
                }
            }

            return maxDanger;
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

        public double ApplyLoseConditions(Board board, double totalDanger, MoveType[] allowedMoves)
        {
            if (GetAmountOfSimilar(board, Stone.boulder) == 0)
            {
                return int.MaxValue;
            }
            if (GetAmountOfSimilar(board, Stone.rock) == 0)
            {
                return int.MaxValue;
            }
            if (GetAmountOfSimilar(board, Stone.pebble) == 0)
            {
                return int.MaxValue;
            }
            //difficult bit: what if the enemy can slay my two last stones (oh noes!)
            //only valid if we can only attack i.e. when the enemy has two moves coming up
            if (allowedMoves.Count() > 1)
            {
                //does the orderby work?
                var thing = (from l in GameState.myLocations where GetAmountOfSimilar(board, board.GetStone(l)) == 2 select l).OrderBy(l => board.GetStone(l)).ToList();
                while (thing.Count > 0)
                {
                    var stonePair = thing.Take(2).ToList();
                    var enemyStonesAttacking1 = GameState.myPossibleFromLocations[stonePair[0]]
                        .Where(l => board.GetOwner(l) != myColor && board.GetHeight(l) >= board.GetHeight(stonePair[0])).ToList();
                    var enemyStonesAttacking2 = GameState.myPossibleFromLocations[stonePair[1]]
                        .Where(l => board.GetOwner(l) != myColor && board.GetHeight(l) >= board.GetHeight(stonePair[1])).ToList();
                    if (enemyStonesAttacking1.Count() > 0 || enemyStonesAttacking2.Count() > 0)
                    {
                        var height1 = board.GetHeight(stonePair[0]);
                        var height2 = board.GetHeight(stonePair[1]);
                        if ( GameState.myPossibleToLocations[stonePair[0]].Contains(stonePair[1]) || AreOnTheSameLine(board, stonePair[0], stonePair[1], enemyStonesAttacking1, enemyStonesAttacking2))
                        {
                            var somes = enemyStonesAttacking1.Union(enemyStonesAttacking2).Max(s => board.GetHeight(s));
                            if (somes > Math.Max(height1, height2))
                            {
                                totalDanger += 1000;
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
                                    totalDanger += 1000;
                                }
                            }
                        }
                    }
                    thing = thing.Skip(2).ToList();
                }
            }
            return totalDanger;
        }

        private bool AreOnTheSameLine(Board board, BoardLocation boardLocation1, BoardLocation boardLocation2, List<BoardLocation> enemyStonesAttacking1, List<BoardLocation> enemyStonesAttacking2)
        {
            var thePossibleDudeInTheMiddle = enemyStonesAttacking1.Union(enemyStonesAttacking2).Distinct();
            if (thePossibleDudeInTheMiddle.Count() == 1)
            {
                return IsInAStraightLine(board, boardLocation1, thePossibleDudeInTheMiddle.ToList()[0], boardLocation2);
            }
            else
            {
                return GameState.myPossibleToLocations[boardLocation1].Contains(boardLocation2);
            }
        }


        private bool CanAttack(Board board, BoardLocation location, bool storeToLocations = false)
        {
            if (board.GetOwner(location) == myColor)
            {
                return GameState.myAttackMoves[location].Count > 0;
            }
            else
            {
                return GameState.hisAttackMoves[location].Count > 0;
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
            var height = board.GetHeight(position);
            stoneValue *= height + (stoneValue*(height-1));
            return stoneValue;
        }

        private double CalculateRisk(Board board, BoardLocation position)
        {
            double risk = GameState.myPossibleFromLocations[position].Any() ? 1 : 0;
            //second step: 
            if (risk == 0)
            {
                foreach (var location in GameState.myPossibleToLocations[position].Where(p => board.GetOwner(p) == myColor))
                {
                    //elke dude die mij kan slaan, en de dude op position
                    risk = GameState.myPossibleFromLocations[location].Where(l => board.GetOwner(l) != myColor && board.GetHeight(l) >= board.GetHeight(location) && board.GetHeight(l) >= board.GetHeight(position)).Any() ? 1 : 0;
                    if (risk == 1)
                    {
                        break;
                    }
                }
            }

            //'the move out of the way one'
            //foreach enemy stone that border me, but cannot attack (the rest is included in the risk)                          AND can move out of the way
            if (risk == 0)
            {
                risk = GameState.myAttackMoves[position].Where(s => board.GetHeight(s) < board.GetHeight(position)).Where(s => GameState.hisAttackMoves[s].Where(e => e != position).Count() > 0).Where(s => GameState.hisAttackMoves.ContainsKey(s) && GameState.hisPossibleToLocations[s].Any(p => board.GetHeight(p) >= board.GetHeight(position) && IsInAStraightLine(board, position, s, p))).Any() ? 1 : 0;
            }
            if (risk != 0)
            {
                if (GetAmountOfSimilar(board, board.GetStone(position)) == 1)
                {
                    risk = 1000;
                }
                else
                {
                    risk = 1;
                }
            }
            return risk;
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
            firstFound = GetFirstNonEmptyInDirection(board, me, 1, 0);
            secondFound = GetFirstNonEmptyInDirection(board, coward, 1, 0);
            if (firstFound != null && secondFound != null && firstFound.Equals(coward) && secondFound.Equals(scary))
            {
                return true;
            }
            //-1  0
            firstFound = GetFirstNonEmptyInDirection(board, me, -1, 0);
            secondFound = GetFirstNonEmptyInDirection(board, coward, -1, 0);
            if (firstFound != null && secondFound != null && firstFound.Equals(coward) && secondFound.Equals(scary))
            {
                return true;
            }
            //-1 -1
            firstFound = GetFirstNonEmptyInDirection(board, me, -1, -1);
            secondFound = GetFirstNonEmptyInDirection(board, coward, -1, -1);
            if (firstFound != null && secondFound != null && firstFound.Equals(coward) && secondFound.Equals(scary))
            {
                return true;
            }
            // 1  1
            firstFound = GetFirstNonEmptyInDirection(board, me, 1, 1);
            secondFound = GetFirstNonEmptyInDirection(board, coward, 1, 1);
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
            var nrOfPebbles = (double)GetHisNumberofStones(board, Stone.pebble);
            var nrOfRocks = (double)GetHisNumberofStones(board, Stone.rock);
            var nrOfBoulders = (double)GetHisNumberofStones(board, Stone.boulder);
            pebbleValueEnemy = (double)1 / (nrOfPebbles*nrOfPebbles);
            rockValueEnemy = (double)1 / (nrOfRocks*nrOfRocks);
            boulderValueEnemy = (double)1 / (nrOfBoulders*nrOfBoulders);
            if (GameState.hisMaxStoneheight[Stone.boulder] > GameState.myHighest+1)
            {
                boulderValueEnemy /= 4;
            }
            if (GameState.hisMaxStoneheight[Stone.rock] > GameState.myHighest+1)
            {
                rockValueEnemy /= 4;
            }
            if (GameState.hisMaxStoneheight[Stone.pebble] > GameState.myHighest+1)
            {
                pebbleValueEnemy /= 4;
            }
            var myPebbles = (double)GetMyNumberofStones(board, Stone.pebble);
            var myRocks = (double)GetMyNumberofStones(board, Stone.rock);
            var myBoulders = (double)GetMyNumberofStones(board, Stone.boulder);
            pebbleValue = (double)1 / (myPebbles * myPebbles);
            rockValue = (double)1 / (myRocks * myRocks);
            boulderValue = (double)1 / (myBoulders * myBoulders);

        }

        private Board GetNewBoard(Board oldBoard, Move move)
        {
            if (move.Type == MoveType.Pass)
            {
                return oldBoard;
            }
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
            int numberOfSimilar = GameState.myLocations.Count(l => board.GetStone(l) == stone);
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
            return GameState.hisLocations.Where(l => board.GetStone(l) == stone).Count();
        }

        int GetMyNumberofStones(Board board, Stone stone)
        {
            return GameState.myLocations.Where(l => board.GetStone(l) == stone).Count();
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

        private static List<BoardLocation> GetPossibleFromLocations(Board board, BoardLocation toLocation, bool onlyattack = false)
        {
            List<BoardLocation> possibleFromLocations = new List<BoardLocation>();

            // always possible to pass
            //possibleFromLocations.Add(toLocation);

            BoardLocation north = GetFirstNonEmptyInDirection(board, toLocation, 0, -1);
            if (north != null && IsValidMove(board, north, toLocation, onlyattack)) possibleFromLocations.Add(north);

            BoardLocation south = GetFirstNonEmptyInDirection(board, toLocation, 0, 1);
            if (south != null && IsValidMove(board, south, toLocation, onlyattack)) possibleFromLocations.Add(south);

            BoardLocation east = GetFirstNonEmptyInDirection(board, toLocation, 1, 0);
            if (east != null && IsValidMove(board, east, toLocation, onlyattack)) possibleFromLocations.Add(east);

            BoardLocation west = GetFirstNonEmptyInDirection(board, toLocation, -1, 0);
            if (west != null && IsValidMove(board, west, toLocation, onlyattack)) possibleFromLocations.Add(west);

            BoardLocation northWest = GetFirstNonEmptyInDirection(board, toLocation, -1, -1);
            if (northWest != null && IsValidMove(board, northWest, toLocation, onlyattack)) possibleFromLocations.Add(northWest);

            BoardLocation southEast = GetFirstNonEmptyInDirection(board, toLocation, 1, 1);
            if (southEast != null && IsValidMove(board, southEast, toLocation, onlyattack)) possibleFromLocations.Add(southEast);
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
