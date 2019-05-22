using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrickyGameJolt;

namespace Test {
    class Program {

        const string testgame = "336383";
        const string testprivate = "e8a4b4be97e11da42183a5751cef877b";

        static void Main(string[] args) {
            Console.Write("User Name: ");
            var user = Console.ReadLine();
            Console.Write("Token: ");
            var token = Console.ReadLine();

            var gju = new GJUser(testgame, testprivate, user, token);
            gju.AwardTrophy("107249");
        }
    }
}
