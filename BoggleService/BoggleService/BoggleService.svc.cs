using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Net;
using System.Collections;
using System.ServiceModel.Web;
using System.Collections;
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
        private static int gameID = 0;
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
                    string line;
                    using (StreamReader file = new System.IO.StreamReader(AppDomain.CurrentDomain.BaseDirectory + "dictionary.txt"))
                    {
                        while ((line = file.ReadLine()) != null)
                        {
                            dictionary.Add(line);
                        }
                    }
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
                    games[gameID].BogBoard = new BoggleBoard();//Actual board with all methods.
                    games[gameID].Board = games[gameID].BogBoard.ToString();//sTRING 
                    SetStatus(Created);//For the second player only.
                    games[gameID].StartTime = DateTime.Now;
                    gameID++; //Create a new empty game.
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
        /// 
        /// Otherwise, if the game state is anything other than "active", responds with response code 409 (Conflict).
        /// 
        /// Otherwise, records the trimmed Word as being played by UserToken in the game identified by GameID.
        /// Returns the score for Word in the context of the game(e.g. if Word has been played 
        /// before the score is zero). Responds with status 200 (OK). Note: The word is not case sensitive.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="word"></param>
        /// <returns> returns the integer score of the current word. </returns>
        public PlayWordResponse PlayWord(PlayWordData userData, string GameID)
        {
            int thisgameID;
            int.TryParse(GameID, out thisgameID);
            if (!games[thisgameID].GameStatus.Equals("active")) //If our game isn't active.
            {
                SetStatus(Conflict);
                return null;
            }

            //Check for invalid
            if (String.IsNullOrEmpty(userData.Word) || userData.UserToken == null || userData.UserToken.Length == 0
                || !games.ContainsKey(users[userData.UserToken].CurrentGameID)
                || (games[users[userData.UserToken].CurrentGameID].Player1 != userData.UserToken && games[users[userData.UserToken].CurrentGameID].Player2 != userData.UserToken))
            {
                SetStatus(Forbidden);
                return null;

            }
            else
            {
                PlayWordResponse response = new PlayWordResponse();
                string trimmedWord = userData.Word.Trim();
                int score = 0;

                string token = userData.UserToken;

                score = ScoreWord(trimmedWord, token); //Score the word;
                users[token].WordsPlayed.Add(trimmedWord, score);//Add the word and its coinciding score to our database.
                users[token].CurrentTotalScore += score;//Add the word 

                response.Score = score;

                return response;
            }

        }

        public int TimeLeft(int gameID)
        {
            int thisTimeLimit = games[gameID].TimeLimit;
            DateTime now = DateTime.UtcNow;
            TimeSpan difference = now.Subtract(games[gameID].StartTime);
            return thisTimeLimit - (int)difference.Seconds;
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
        public StatusResponse GameStatus(string GameID, string Brief)
        {
            int gameID;
            int.TryParse(GameID, out gameID);
            StatusResponse response = new StatusResponse();
            if (!games.ContainsKey(gameID))
            {
                SetStatus(Forbidden);
                return null;
            }
            else if (Brief == "no" || Brief == null)
            {
                if (games[gameID].GameStatus == "pending")
                {
                    response.GameState = "pending";
                    return response;
                }

                //If the time left int the game is less than or equal to zero seconds.
                if (TimeLeft(gameID) <= 0)
                {
                    games[gameID].GameStatus = "completed";
                    response.TimeLeft = 0;
                }
                if (games[gameID].GameStatus == "active")
                {
                    response.GameState = "active";
                    response.Board = games[gameID].Board;
                    response.TimeLimit = games[gameID].TimeLimit;
                    response.TimeLeft = TimeLeft(gameID);
                    response.Player1 = new player();
                    response.Player2 = new player();
                    response.Player1.Nickname = users[games[gameID].Player1].Nickname;
                    response.Player1.Score = users[games[gameID].Player1].CurrentTotalScore;
                    response.Player2.Nickname = users[games[gameID].Player2].Nickname;
                    response.Player2.Score = users[games[gameID].Player2].CurrentTotalScore;
                }
                else if (games[gameID].GameStatus == "completed")
                {
                    response.GameState = "completed";
                    response.Board = games[gameID].Board;
                    response.TimeLimit = games[gameID].TimeLimit;
                    response.TimeLeft = 0;
                    response.Player1 = new player();
                    response.Player2 = new player();
                    response.Player1.Nickname = users[games[gameID].Player1].Nickname;
                    response.Player1.Score = users[games[gameID].Player1].CurrentTotalScore;
                    //PLAYER 1'S WORDS
                    foreach (KeyValuePair<string, int> p in users[games[gameID].Player1].WordsPlayed)
                    {
   
                        WordItem w = new WordItem();
                        w.Word = p.Key;
                        w.Score = p.Value;
                        response.Player1.WordsPlayed.Add(w);
                    }
                    response.Player2.Nickname = users[games[gameID].Player2].Nickname;
                    response.Player2.Score = users[games[gameID].Player2].CurrentTotalScore;

                    //PLAYER 2'S WORDS NEED THESE
                    foreach (KeyValuePair<string, int> p in users[games[gameID].Player2].WordsPlayed)
                    {

                        WordItem w = new WordItem();
                        w.Word = p.Key;
                        w.Score = p.Value;
                        response.Player2.WordsPlayed.Add(w);
                    }
                }

            }
            else if (Brief == "yes")
            {
                if (games[gameID].GameStatus == "pending")
                {
                    response.GameState = "pending";
                }

                if (TimeLeft(gameID) <= 0)
                {
                    games[gameID].GameStatus = "completed";
                    games[gameID].TimeLimit = 0;

                }
                else if (games[gameID].GameStatus == "active")
                {
                    response.GameState = "active";
                    response.TimeLeft = TimeLeft(gameID);
                    response.Player1 = new player();
                    response.Player2 = new player();
                    response.Player1.Nickname = users[games[gameID].Player1].Nickname;
                    response.Player1.Score = users[games[gameID].Player1].CurrentTotalScore;


                    response.Player2.Nickname = users[games[gameID].Player2].Nickname;
                    response.Player2.Score = users[games[gameID].Player2].CurrentTotalScore;
                }
                else if (games[gameID].GameStatus == "completed")
                {
                    response.GameState = "completed";
                    response.Board = games[gameID].Board;
                    response.TimeLimit = games[gameID].TimeLimit;
                    response.TimeLeft = 0;
                    response.Player1.Nickname = users[games[gameID].Player1].Nickname;
                    response.Player1.Score = users[games[gameID].Player1].CurrentTotalScore;
                    //PLAYER 1'S WORDS
                    foreach (KeyValuePair<string, int> p in users[games[gameID].Player1].WordsPlayed)
                    {

                        WordItem w = new WordItem();
                        w.Word = p.Key;
                        w.Score = p.Value;
                        response.Player1.WordsPlayed.Add(w);
                    }

                    response.Player2.Nickname = users[games[gameID].Player2].Nickname;
                    response.Player2.Score = users[games[gameID].Player2].CurrentTotalScore;
                    //Player 2's words NEED THIS.
                    foreach (KeyValuePair<string, int> p in users[games[gameID].Player2].WordsPlayed)
                    {

                        WordItem w = new WordItem();
                        w.Word = p.Key;
                        w.Score = p.Value;
                        response.Player2.WordsPlayed.Add(w);
                    }

                    
                }
            }
            return response;
        }

        /// <summary>
        /// Scores a word based on the rules of Boggle.
        /// </summary>
        /// <param name="word"></param>
        /// <param name="token"></param>
        /// <returns>the integer score that </returns>
        private int ScoreWord(string word, string token)
        {
            // If a string has fewer than three characters, it scores zero points.
            //Otherwise, if a string has a duplicate that occurs earlier in the list, it scores zero points.
            if (word.Length < 3 || users[token].WordsPlayed.ContainsKey(word))
            {
                return 0;
            }

            //Otherwise, if a string is legal (it appears in the dictionary and occurs on the board),
            if (dictionary.Contains(word) && games[users[token].CurrentGameID].BogBoard.CanBeFormed(word))
            {
                //it receives a score that depends on its length.
                //Three - and four - letter words are worth one point,
                if (word.Length == 3 || word.Length == 4)
                {
                    return 1;
                }
                //five - letter words are worth two points, 
                else if (word.Length == 5)
                {
                    return 2;
                }
                //six - letter words are worth three points,
                else if (word.Length == 6)
                {
                    return 3;
                }

                //seven - letter words are worth five points
                else if (word.Length == 7)
                {
                    return 5;
                }
                //and longer words are worth 11 points.
                else
                {
                    return 11;
                }
            }
            //Otherwise, the string scores negative one point.
            else
            {
                return -1;
            }
        }
    }
}
