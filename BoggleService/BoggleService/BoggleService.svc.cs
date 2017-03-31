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
        private readonly static Dictionary<int, Game> games = new Dictionary<int, Game>();    //mapped via gameID
        private readonly static Dictionary<String, User> users = new Dictionary<String, User>();  //maped via userID
        private readonly static Dictionary<String, Words> words = new Dictionary<String, Words>();  //mapped via userID
        private static readonly object sync = new object();
        private static int gameID = 1;
        private HashSet<string> dictionary = new HashSet<string>();
        private bool dictionaryLoaded = false;
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

        /// <summary>
        /// Create a new user.
        ///If Nickname is null, or is empty when trimmed, responds with status 403 (Forbidden).
        ///Otherwise, creates a new user with a unique UserToken and the trimmed Nickname.
        ///The returned UserToken should be used to identify the user in subsequent requests.
        ///Responds with status 201 (Created).
        /// </summary>
        /// <param name="item"></param>
        /// <returns> string of the userToken </returns>
        public CreateUserResponse CreateUser(CreateUserData userData)
        {
            lock (sync)
            {

                if (userData.Nickname == null || userData.Nickname.Trim().Length == 0)
                {
                    SetStatus(Forbidden);
                    return null;
                }

                string userToken = Guid.NewGuid().ToString(); //Generate our userToken
                CreateUserResponse response = new CreateUserResponse();
                response.UserToken = userToken;
                users.Add(userToken, new User()); //Add it to our database; set the fields of our own User class.
                users[userToken].UserId = userToken;
                users[userToken].Nickname = userData.Nickname.Trim();
                SetStatus(Created);

                return response;
            }
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
        public JoinGameResponse JoinGame(JoinGameData userData)
        {
            lock (sync)
            {

                if (!users.ContainsKey(userData.UserToken) || userData.TimeLimit < 5 || userData.TimeLimit > 120)//If we don't have the current user in our database
                {
                    SetStatus(Forbidden);
                    return null;
                }

                //Otherwise, if UserToken is already a player in the pending game, responds with status 409(Conflict).
                if (users[userData.UserToken].HasPendingGame)
                {
                    SetStatus(Conflict);
                    return null;
                }

                //Store the dictionry only the first time this method is called.
                if (!dictionaryLoaded)
                {
                    string word;
                    StreamReader reader = new StreamReader("dictionary.txt");
                    while ((word = reader.ReadLine()) != null)
                    {
                        dictionary.Add(word);
                    }

                    reader.Close();
                    dictionaryLoaded = true;
                }

                //Take in the time limit given by the current player.
                users[userData.UserToken].GivenTimeLimit = userData.TimeLimit;
                users[userData.UserToken].HasPendingGame = true; //Player will now be put into a pending game no matter what.

                JoinGameResponse response = new JoinGameResponse();
                response.GameID = gameID.ToString();

                //If the game does not have the current game ID in it, 
                //add the game id to games and the new player to the game as player 1.
                if (!games.ContainsKey(gameID))
                {
                    games.Add(gameID, new Game());
                    games[gameID].GameID = gameID; //Store the current gameID to our database
                    games[gameID].GameStatus = "pending";
                    games[gameID].Player1 = userData.UserToken;
                    users[userData.UserToken].CurrentGameID = gameID;//Make sure the player has a gameID.
                    SetStatus(Accepted); //For the first player only.
                }
                //If the current game ID exists, it only has 1 player; add the user as a second player.
                else if (games.ContainsKey(gameID))
                {
                    int p1TimeLimit = users[games[gameID].Player1].GivenTimeLimit;//The time limit given by player 1.
                    games[gameID].Player2 = userData.UserToken;//Set player 2 to be the second player to the existing game.
                    games[gameID].TimeLimit = (userData.TimeLimit + p1TimeLimit) / 2; //The average time limit given by the two players.
                    users[userData.UserToken].CurrentGameID = gameID;//Make sure the player has a gameID.
                    games[gameID].GameStatus = "active"; //The game is now active.
                    games[gameID].Board = new BoggleBoard().ToString();
                    SetStatus(Created);//For the second player only.
                    gameID++; //Create a new empty game.
                    games[gameID].StartTime = DateTime.Now;
                }

                return response;
            }
        }

        /// <summary>
        /// Cancel a pending request to join a game.
        /// 
        /// If UserToken is invalid or is not a player in the pending game, responds with status 403 (Forbidden).
        /// Otherwise, removes UserToken from the pending game and responds with status 200 (OK).
        /// </summary>
        /// <param name="user"></param>
        public void CancelJoinRequest(CancelJoinData userData)
        {
            lock (sync)
            {
                if (userData.UserToken == null || (games[gameID].Player1 != userData.UserToken && !users[userData.UserToken].HasPendingGame))
                {
                    SetStatus(Forbidden);
                    return;
                }

                else
                {
                    int thisGameID = users[userData.UserToken].CurrentGameID;
                    users[userData.UserToken].HasPendingGame = false; //Make sure the user's game is no longer pending.

                    //Only player 1 can choose to quit the game. Clear the first player.
                    games[gameID].Player1 = null;
                    users[userData.UserToken].CurrentGameID = 0;//Make sure the player has no gameID.
                    games[gameID].TimeLimit = 0;

                    SetStatus(OK);
                }
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
        public PlayWordResponse PlayWord(PlayWordData userData)
        {
            if (String.IsNullOrEmpty(userData.Word) || userData.UserToken == null || userData.UserToken.Length == 0 
                || !games.ContainsKey(users[userData.UserToken].CurrentGameID)
                || (games[users[userData.UserToken].CurrentGameID].Player1 != userData.UserToken && games[users[userData.UserToken].CurrentGameID].Player2 != userData.UserToken))
            {
                SetStatus(Forbidden);
                return null;

            }

            int ourGameID = users[userData.UserToken].CurrentGameID;

            if (!games[ourGameID].GameStatus.Equals("active"))
            {
                SetStatus(Conflict);
                return null;
            }

            else
            {
                PlayWordResponse response = new PlayWordResponse();
                string trimmedWord = userData.Word;      
                //If player 1 played the word
                if(userData.UserToken == games[ourGameID].Player1)
                {
                    //Add the word to our database if we haven't already.
                    if (!users[userData.UserToken].WordsPlayed.ContainsKey(userData.Word))
                    {
                        //Check the word.
                        //Score the word.
                        //response.Score = 
                        //users[userData.UserToken].WordsPlayed.Add()
                    }
                   
             
                }
                //If player 2 played the word
                if (userData.UserToken == games[ourGameID].Player2)
                {
                    //Add the word to our database.
                    //Check the word.
                    //Score the word.
                    //response.Score = 
                }

                return response;
            }
         
        }

        /// <summary>
        /// Get game status information.
        /// If GameID is invalid, responds with status 403 (Forbidden).
        /// Otherwise, returns information about the game named by GameID as illustrated below.
        /// Note that the information returned depends on whether "Brief=yes" was included as a 
        /// parameter as well as on the state of the game. Responds with status code 200 (OK). 
        /// Note: The Board and Words are not case sensitive.
        /// </summary>
        /// <param name="game"></param>
        /// <returns></returns>
        public GameStatusResponse GameStatus(GameStatusData game, int GameID)
        {
            if (!games.ContainsKey(GameID))
            {
                SetStatus(Forbidden);
                return null;
            }
            else
            {
                GameStatusResponse response = new GameStatusResponse();
                string GameStat = games[GameID].GameStatus;
                if (GameStat == "pending")
                {
                    response.GameState = "pending";
                    return response;
                }
                else if (GameStat == "active")
                {
                    response.GameState = "active";
                    response.Board = games[GameID].Board;
                    response.TimeLimit = games[GameID].TimeLimit;
                    response.TimeLeft = (int) (games[GameID].TimeLimit - (DateTime.Now.Ticks -  games[GameID].StartTime.Ticks));
                    response.Player1 = new Player();
                    response.Player1.Nickname = users[games[GameID].Player1].Nickname;
                    response.Player1.Score = users[games[GameID].Player1].CurrentTotalScore;



        public GameStatusResponse GameStatus(GameStatusData game)
        {
            throw new NotImplementedException();
        }

        public GameStatusResponse GameStatusBYes(GameStatusData game, int GameID)
        {
            throw new NotImplementedException();
        }
    }
}
