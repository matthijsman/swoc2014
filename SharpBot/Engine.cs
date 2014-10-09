using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpBot.Protocol;
using Newtonsoft.Json;
using System.IO;

namespace SharpBot
{
    public class Engine
    {
        private readonly IBot bot;

        private Player botColor;

        public Engine(IBot bot)
        {
            this.bot = bot;
        }

        public void run()
        {
            try
            {
                DoInitiateRequest();

                Player winner = DoFirstRound();
                while (winner == Player.None)
                {
                    winner = DoNormalRound();
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Exception. Bailing out.");
                Console.Error.WriteLine(ex.StackTrace);
            }
        }

        private void DoInitiateRequest()
        {
            InitiateRequest initRequest = readMessage<InitiateRequest>();
            if (initRequest == null)
            {
                throw new InvalidMessageException("Unexpected message received. Expected InitiateRequest.");
            }

            botColor = initRequest.Color;

            bot.HandleInitiate(initRequest);
        }

        private Player DoFirstRound()
        {
            Player winner;
            if (botColor == Player.White)
            {
                // Do the first move
                HandleMoveRequest();
                // and wait for the engine to acknowledge
                winner = HandleProcessedMove();
                if (winner != Player.None)
                {
                    return winner;
                }

                // Wait for first two moves of black
                winner = HandleProcessedMove();
                if (winner != Player.None)
                {
                    return winner;
                }
                winner = HandleProcessedMove();
            }
            else
            {
                // Wait for first white move
                winner = HandleProcessedMove();
            }
            return winner;
        }

        private Player DoNormalRound()
        {
            Player winner;

            HandleMoveRequest();
            winner = HandleProcessedMove();
            if (winner != Player.None)
            {
                return winner;
            }

            HandleMoveRequest();
            winner = HandleProcessedMove();
            if (winner != Player.None)
            {
                return winner;
            }

            winner = HandleProcessedMove();
            if (winner != Player.None)
            {
                return winner;
            }

            winner = HandleProcessedMove();

            return winner;
        }

        private void HandleMoveRequest()
        {
            // process first move
            MoveRequest moveRequest = readMessage<MoveRequest>();
            if (moveRequest == null)
            {
                throw new InvalidMessageException("Unexpected message received. Expected MoveRequest.");
            }
            var start = DateTime.Now;
            Move move = bot.HandleMove(moveRequest);
            //Console.Error.WriteLine(DateTime.Now - start);
            writeMessage(move);
        }

        private Player HandleProcessedMove() 
        {
            ProcessedMove processedMove = readMessage<ProcessedMove>();
            if (processedMove == null)
            {
                throw new InvalidMessageException("Unexpected message received. Expected ProcessedMove.");
            }
            
            bot.HandleProcessedMove(processedMove);
            return processedMove.Winner;
        }

        private T readMessage<T>()
        {
            //ReadLine();//
            string line = Console.In.ReadLine();
            if (string.IsNullOrEmpty(line))
            {
                return default(T);
            }
            //Console.Error.WriteLine(line);
            return JsonConvert.DeserializeObject<T>(line);
        }

        const int READLINE_BUFFER_SIZE = 300;
        private static string ReadLine()
        { 
            Stream inputStream = Console.OpenStandardInput(READLINE_BUFFER_SIZE);
            byte[] bytes = new byte[READLINE_BUFFER_SIZE];
            int outputLength = inputStream.Read(bytes, 0, READLINE_BUFFER_SIZE);
            //Console.WriteLine(outputLength);
            char[] chars = Encoding.UTF8.GetChars(bytes, 0, outputLength);
            return new string(chars);
        }

        private void writeMessage<T>(T message)
        {
            Console.Out.WriteLine(JsonConvert.SerializeObject(message));
        }
    }
}