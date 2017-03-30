using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.ServiceModel.Web;
using static System.Net.HttpStatusCode;

namespace Boggle
{
    public class BoggleService : IBoggleService
    {
        // This amounts to a "poor man's" database.  The state of the service is
        // maintained in users and items.  The sync object is used
        // for synchronization (because multiple threads can be running
        // simultaneously in the service).  The entire state is lost each time
        // the service shuts down, so eventually we'll need to port this to
        // a proper database.
        private readonly static Dictionary<String, Games> games = new Dictionary<String, Games>();
        private readonly static Dictionary<String, Users> users = new Dictionary<String, Users>();
        private readonly static Dictionary<String, Words> words = new Dictionary<String, Words>();

        /// <summary>
        /// The most recent call to SetStatus determines the response code used when
        /// an http response is sent.
        /// </summary>
        /// <param name="status"></param>
        private static void SetStatus(HttpStatusCode status)
        {
            WebOperationContext.Current.OutgoingResponse.StatusCode = status;
        }

        /// <summary>
        /// Returns a Stream version of index.html.
        /// </summary>
        /// <returns></returns>
        public Stream API()
        {
            SetStatus(OK);
            WebOperationContext.Current.OutgoingResponse.ContentType = "text/html";
            return File.OpenRead(AppDomain.CurrentDomain.BaseDirectory + "index.html");
        }

        public void CancelJoinRequest(Users user)
        {
            throw new NotImplementedException();
        }

        public string CreateUser(Users user)
        {
            //Checks if the nickname entered by the user is valid
            if (user.Nickname.Trim() == null || user.Nickname.Trim().Length == 0)
            {
                SetStatus(Forbidden);
                return null;
            }
            else
            {
                string userID = Guid.NewGuid().ToString();
                users.Add(userID, user);
                SetStatus(Created);
                return userID;
            }
        }

        public string GameStatus(Games game)
        {
            throw new NotImplementedException();
        }

        public int JoinGame(Users user, Games game)
        {
            throw new NotImplementedException();
        }

        public int PlayWord(Users user, Words word)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Demo.  You can delete this.
        /// </summary>
        public string WordAtIndex(int n)
        {
            if (n < 0)
            {
                SetStatus(Forbidden);
                return null;
            }

            string line;
            using (StreamReader file = new System.IO.StreamReader(AppDomain.CurrentDomain.BaseDirectory + "dictionary.txt"))
            {
                while ((line = file.ReadLine()) != null)
                {
                    if (n == 0) break;
                    n--;
                }
            }

            if (n == 0)
            {
                SetStatus(OK);
                return line;
            }
            else
            {
                SetStatus(Forbidden);
                return null;
            }
        }
    }
}
