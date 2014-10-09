using Newtonsoft.Json;
using NUnit.Framework;
using SharpBot;
using SharpBot.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotTest
{
    [TestFixture]
    public class BotTest
    {
        [Test]
        public void TestLoseConditions()
        {
            Bot bot = new Bot();
            MoveRequest request = readMessage<MoveRequest>("{'Board':{'state':[[0,0,0,0,-10,0,0,0,0],[0,15,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0],[0,0,-27,0,0,0,0,0,-5],[0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,11,0,5],[0,0,0,0,0,0,0,-13,5],[0,0,0,0,-6,18,0,-5,-5]]},'AllowedMoves':['0','1','2']}");
            double danger = 0;
            var allowedMoves = new MoveType[] { MoveType.Attack, MoveType.Pass, MoveType.Strengthen };
            bot.SetAllowedMoves(allowedMoves);
            danger = bot.ApplyLoseConditions(request.Board, danger,allowedMoves);
            Assert.AreEqual(int.MaxValue, danger);
            Assert.DoesNotThrow(() => bot.GetPrintableMoveWithDanger(request.Board, new Move(MoveType.Pass, null, null), allowedMoves));
        }

        [Test]
        public void PassingWorks()
        {
            Bot bot = new Bot();
            MoveRequest request = readMessage<MoveRequest>("{'Board':{'state':[[0,0,0,0,0,0,0,0,0],[0,15,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0],[0,0,-27,0,0,-7,9,0,0],[0,0,0,0,0,0,-6,0,-18],[0,0,0,0,-5,0,0,14,-5]]},'AllowedMoves':['0','1','2']}");
            var allowedMoves = new MoveType[] { MoveType.Attack, MoveType.Pass, MoveType.Strengthen };
            PrintableMove result = null;
            Assert.DoesNotThrow(() => result = bot.GetPrintableMoveWithDanger(request.Board, new Move(MoveType.Pass, null, null), allowedMoves));
            Console.WriteLine("poep" + result.ToString());
        }

        private T readMessage<T>(string text)
        {
            //ReadLine();//
            string line = text;
            if (string.IsNullOrEmpty(line))
            {
                return default(T);
            }
            //Console.Error.WriteLine(line);
            return JsonConvert.DeserializeObject<T>(line);
        }
    }
}
