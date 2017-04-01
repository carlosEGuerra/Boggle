using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static System.Net.HttpStatusCode;
using System.Diagnostics;
using Newtonsoft.Json;
using System.Dynamic;

namespace Boggle
{
    /// <summary>
    /// Provides a way to start and stop the IIS web server from within the test
    /// cases.  If something prevents the test cases from stopping the web server,
    /// subsequent tests may not work properly until the stray process is killed
    /// manually.
    /// </summary>
    public static class IISAgent
    {
        // Reference to the running process
        private static Process process = null;

        /// <summary>
        /// Starts IIS
        /// </summary>
        public static void Start(string arguments)
        {
            if (process == null)
            {
                ProcessStartInfo info = new ProcessStartInfo(Properties.Resources.IIS_EXECUTABLE, arguments);
                info.WindowStyle = ProcessWindowStyle.Minimized;
                info.UseShellExecute = false;
                process = Process.Start(info);
            }
        }

        /// <summary>
        ///  Stops IIS
        /// </summary>
        public static void Stop()
        {
            if (process != null)
            {
                process.Kill();
            }
        }
    }
    [TestClass]
    public class BoggleTests
    {
        private string testerUserToken = "";
        /// <summary>
        /// This is automatically run prior to all the tests to start the server
        /// </summary>
        [ClassInitialize()]
        public static void StartIIS(TestContext testContext)
        {
            IISAgent.Start(@"/site:""BoggleService"" /apppool:""Clr4IntegratedAppPool"" /config:""..\..\..\.vs\config\applicationhost.config""");

        }

        /// <summary>
        /// This is automatically run when all tests have completed to stop the server
        /// </summary>
        [ClassCleanup()]
        public static void StopIIS()
        {
            IISAgent.Stop();
        }

        private RestTestClient client = new RestTestClient("http://localhost:60000/BoggleService.svc/");

        /// <summary>
        /// Note that DoGetAsync (and the other similar methods) returns a Response object, which contains
        /// the response Stats and the deserialized JSON response (if any).  See RestTestClient.cs
        /// for details.
        /// </summary>
        [TestMethod]
        public void TestMethod1()
        {
            Response r = client.DoGetAsync("word?index={0}", "-5").Result;
            Assert.AreEqual(Forbidden, r.Status);

            r = client.DoGetAsync("word?index={0}", "5").Result;
            Assert.AreEqual(OK, r.Status);

            string word = (string) r.Data;
            Assert.AreEqual("AAL", word);
        }

        /**************************************************** CREATE USER TESTS ***********************************************/

        /// <summary>
        /// Checks to see if a user can be added normally, then give back a 16-character user token.
        /// </summary>
        [TestMethod]
        public void TestCreateUser1()
        {
            dynamic CreateUserData = new ExpandoObject();
            CreateUserData.Nickname = "Nate";
            Response r = client.DoPostAsync("users", CreateUserData).Result;
            Assert.AreEqual(Created, r.Status);
            Assert.IsTrue(r.Data.Length > 0);
        }
        
        /// <summary>
        /// Makes sure user with the same nicknames can be created successfully.
        /// </summary>
        [TestMethod]
        public void TestCreateUser2()
        {
            dynamic CreateUserData = new ExpandoObject();
            CreateUserData.Nickname = "Nate";
            Response r = client.DoPostAsync("users", CreateUserData).Result;
            Assert.AreEqual(Created, r.Status);
            Assert.IsTrue(r.Data.UserToken.Length == 16);
            testerUserToken = r.Data.UserToken;
        }
        
        /// <summary>
        /// Make sure status comes up as 403 with empty nickname.
        /// </summary>
        [TestMethod]
        public void TestCreateUser3()
        {
            dynamic CreateUserData = new ExpandoObject();
            CreateUserData.Nickname = "     ";
            Response r = client.DoPostAsync("users", CreateUserData).Result;
            Assert.AreEqual(Forbidden, r.Status);
            Assert.IsTrue(r.Data == null);
        }
        
        /// <summary>
        /// Make sure status comes up as 403 with null username.
        /// </summary>
        [TestMethod]
        public void TestCreateUser4()
        {
            Response r = client.DoPostAsync("users", null).Result;
            Assert.AreEqual(Forbidden, r.Status);
            Assert.IsTrue(r.Data == null);
        }
        

        /*********************************************** JOIN GAME TESTS *************************************************/

        /// <summary>
        /// If UserToken is invalid, responds with status 403 (Forbidden).
        /// </summary>
        [TestMethod]
        public void TestJoinGame1()
        {
            dynamic JoinGameData = new ExpandoObject();
            JoinGameData.UserToken = null;
            JoinGameData.TimeLimit = 20;
            Response r = client.DoPostAsync("games", JoinGameData).Result;
            Assert.AreEqual(Forbidden, r.Status);
            Assert.IsTrue(r.Data == null);
        }

        /// <summary>
        /// TimeLimit < 5 responds with status 403 (Forbidden).
        /// </summary>
        [TestMethod]
        public void TestJoinGame2()
        {
            dynamic JoinGameData = new ExpandoObject();
            JoinGameData.UserToken = testerUserToken;
            JoinGameData.TimeLimit = 4;
            Response r = client.DoPostAsync("games", JoinGameData).Result;
            Assert.AreEqual(Forbidden, r.Status);
            Assert.IsTrue(r.Data == null);
        }

        /// <summary>
        /// TimeLimit > 120, responds with status 403 (Forbidden).
        /// </summary>
        [TestMethod]
        public void TestJoinGame3()
        {
            dynamic JoinGameData = new ExpandoObject();
            JoinGameData.UserToken = testerUserToken;
            JoinGameData.TimeLimit = 200;
            Response r = client.DoPostAsync("games", JoinGameData).Result;
            Assert.AreEqual(Forbidden, r.Status);
            Assert.IsTrue(r.Data == null);
        }


        /// <summary>
        /// TimeLimit > 120, responds with status 403 (Forbidden).
        /// </summary>
        [TestMethod]
        public void TestJoinGame4()
        {
            dynamic JoinGameData = new ExpandoObject();
            JoinGameData.UserToken = testerUserToken;
            JoinGameData.TimeLimit = 4;
            Response r = client.DoPostAsync("games", JoinGameData).Result;
            Assert.AreEqual(Forbidden, r.Status);
            Assert.IsTrue(r.Data == null);
        }
    }
}
