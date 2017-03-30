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
        private int gameID = 0;

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

        /// <summary>
        /// Create a new user.
        ///If Nickname is null, or is empty when trimmed, responds with status 403 (Forbidden).
        ///Otherwise, creates a new user with a unique UserToken and the trimmed Nickname.
        ///The returned UserToken should be used to identify the user in subsequent requests.
        ///Responds with status 201 (Created).
        /// </summary>
        /// <param name="item"></param>
        /// <returns> string of the userToken </returns>
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
                //creates a unique ID for the user
                string userID = Guid.NewGuid().ToString();
                //adds it to the user dictionary
                users.Add(userID, user);
                SetStatus(Created);
                return userID;
            }
        }


        public string GameStatus(Games game)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// If UserToken is invalid, TimeLimit < 5, or TimeLimit > 120, responds with status 403 (Forbidden).
        /// Otherwise, if UserToken is already a player in the pending game, responds with status 409 (Conflict).
        /// Otherwise, if there is already one player in the pending game, adds UserToken as the second player.
        /// The pending game becomes active and a new pending game with no players is created.The active game's time
        /// limit is the integer average of the time limits requested by the two players. Returns the new active game's 
        /// GameID(which should be the same as the old pending game's GameID). Responds with status 201 (Created).
        /// Otherwise, adds UserToken as the first player of the pending game, and the TimeLimit as the pending game's 
        /// requested time limit. Returns the pending game's GameID. Responds with status 202 (Accepted).
        /// </summary>
        /// <param name="user"></param>
        /// <param name="game"></param>
        /// <returns> an integer GameID </returns>
        public int JoinGame(Users user, Games game)
        {
            //if the user token is invalid, timelimit is less than 5 and greater than 120 then responds with a 403 error
            if(user.UserId.Length == 0 || game.TimeLimit < 5 || game.TimeLimit > 120)
            {
                SetStatus(Forbidden);
                return 0;
            }
            //if the user token is already a player in the pending game, the game reponds with 409 conflict error
            else if((game.Player1 == user.UserId || game.Player2 == user.UserId) && game.GameStatus == "pending")
            {
                SetStatus(Conflict);
                return 0;
            }
            else if (!string.IsNullOrEmpty(game.Player1))
            {
                games.Add();
            }
        }


        /// <summary>
        /// Play a word in a game.
        /// If Word is null or empty when trimmed, or if GameID or UserToken is missing or invalid, or if UserToken 
        /// is not a player in the game identified by GameID, responds with response code 403 (Forbidden).
        /// Otherwise, if the game state is anything other than "active", responds with response code 409 (Conflict).
        /// Otherwise, records the trimmed Word as being played by UserToken in the game identified by GameID.
        /// Returns the score for Word in the context of the game(e.g. if Word has been played 
        /// before the score is zero). Responds with status 200 (OK). Note: The word is not case sensitive.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="word"></param>
        /// <returns> returns the integer score of the current word. </returns>
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
