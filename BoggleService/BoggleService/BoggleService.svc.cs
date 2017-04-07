﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.ServiceModel.Web;
using static System.Net.HttpStatusCode;
using System.Configuration;
using System.Data.SqlClient;
using Newtonsoft.Json;
using System.Dynamic;
using System.Net.Http;
using System.Diagnostics;

namespace Boggle
{
    public class BoggleService : IBoggleService
    {
        //Last try.
        //Connection string for the DB
        private static string BoggleDB;
        /// <summary>
        /// Saves the connection string 
        /// </summary>
        static BoggleService()
        {
            BoggleDB = ConfigurationManager.ConnectionStrings["BoggleDB"].ConnectionString;
        }

        // This amounts to a "poor man's" database.  The state of the service is
        // maintained in users and items.  The sync object is used
        // for synchronization (because multiple threads can be running
        // simultaneously in the service).  The entire state is lost each time
        // the service shuts down, so eventually we'll need to port this to
        // a proper database.
        private readonly static Dictionary<int, Game> games = new Dictionary<int, Game>();    //mapped via gameID
        private readonly static Dictionary<String, User> users = new Dictionary<String, User>();  //maped via userID
        private static readonly object sync = new object();
        private static HashSet<string> dictionary = new HashSet<string>();
        private static bool dictionaryLoaded = false;
        private static string ourGameStatus = null;
        private static int PendingTimeLimit = 0;
        private static string lastGameID = "";
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

            //Checks the data the user entered for null names, names with length of 0, or an empty nickname
            if (userData.Nickname == null || userData.Nickname.Trim().Length == 0 || userData.Nickname.Trim() == null)
            {
                SetStatus(Forbidden);
                return null;
            }

            using (SqlConnection conn = new SqlConnection(BoggleDB))
            {
                conn.Open();
                using (SqlTransaction trans = conn.BeginTransaction())
                {
                    using (SqlCommand command = new SqlCommand("insert into Users (UserToken, Nickname) values(@UserToken, @Nickname)", conn, trans))
                    {
                        //Generates the User ID
                        string userToken = Guid.NewGuid().ToString();

                        //Replaces the UserID and Nickname from the SQL command
                        command.Parameters.AddWithValue("@UserToken", userToken);
                        command.Parameters.AddWithValue("@Nickname", userData.Nickname);

                        //creates the object that will need to be changed into a Json Object
                        CreateUserResponse Response = new CreateUserResponse();
                        Response.UserToken = userToken;

                        command.ExecuteNonQuery();
                        SetStatus(Created);

                        trans.Commit();
                        return Response;
                    }
                }
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
            //Console.WriteLine("Running Join Game");
            //Debug.WriteLine("Running bitches!");
            //Checks the time limits whether they are correct
            if (userData.TimeLimit < 5 || userData.TimeLimit > 120)
            {
                SetStatus(Forbidden);
                return null;
            }
            //if the time limits are correct it will move into this else statement
            else
            {
                using (SqlConnection conn = new SqlConnection(BoggleDB)) //REUSE ME
                {
                    conn.Open();
                    using (SqlTransaction trans = conn.BeginTransaction())
                    {
                        //checks to see if the user token exists in the users table
                        using (SqlCommand command = new SqlCommand("select UserToken from Users where UserToken = @UserToken", conn, trans))
                        {
                            command.Parameters.AddWithValue("@UserToken", userData.UserToken);

                            // This executes a query (i.e. a select statement).  The result is an
                            // SqlDataReader that you can use to iterate through the rows in the response.
                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                //If we don't have a valid UserToken
                                if (!reader.HasRows)
                                {
                                    SetStatus(Forbidden);
                                    trans.Commit();
                                    return null;
                                }
                            }
                        }


                        //First we need to see if the player is already in a game.
                        string cmd = "select * from Games where Player1 = @UserToken";
                        using (SqlCommand command = new SqlCommand(cmd, conn, trans))
                        {
                            command.Parameters.AddWithValue("@UserToken", userData.UserToken);

                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                //If we don't have a valid UserToken
                                if (reader.HasRows)
                                {
                                    SetStatus(Conflict);
                                    return null;
                                }
                            }

                        }

                        //First we need to see if the player is already in a game.
                        cmd = "select * from Games where Player2 = @UserToken";
                        using (SqlCommand command = new SqlCommand(cmd, conn, trans))
                        {
                            command.Parameters.AddWithValue("@UserToken", userData.UserToken);

                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                //If we don't have a valid UserToken
                                if (reader.HasRows)
                                {
                                    SetStatus(Conflict);
                                    trans.Commit();
                                    return null;
                                }
                            }

                        }

                        JoinGameResponse response = new JoinGameResponse();
                        if (string.IsNullOrEmpty(ourGameStatus))
                        {
                            //If we don't have a player 1 in the game.
                            //cmd = "update Games(Player1, TimeLimit, GameState) output inserted.GameID values(@Player1, @TimeLimit, @GameState) where Player1 = @isNull";   //Incorrect syntax near the keyword 'where'.
                            cmd = "insert into Games (Player1, TimeLimit, GameState, Board) output inserted.GameID values(@Player1, @TimeLimit, @GameState, @Board)";
                            using (SqlCommand CreateGameCommand = new SqlCommand(cmd, conn, trans))
                            {
                                //Console.WriteLine("Addihg player 1");
                                BoggleBoard board = new BoggleBoard();
                                CreateGameCommand.Parameters.AddWithValue("@Board", board.ToString());
                                CreateGameCommand.Parameters.AddWithValue("@Player1", userData.UserToken);
                                CreateGameCommand.Parameters.AddWithValue("@TimeLimit", userData.TimeLimit);
                                CreateGameCommand.Parameters.AddWithValue("@GameState", "pending");
                                ourGameStatus = "pending";
                                PendingTimeLimit = userData.TimeLimit;

                                string GameID = CreateGameCommand.ExecuteScalar().ToString();
                                lastGameID = GameID;
                                response.GameID = GameID;

                                CreateGameCommand.ExecuteNonQuery();
                                SetStatus(Accepted); //Player 1 only

                                trans.Commit();
                                return response;
                            }
                        }



                        /**************************ALL FOR PLAYER 1 DONE************************************/

                        if (ourGameStatus.Equals("pending"))
                        {
                            //If we have a player 1 but not a player 2.
                            //cmd = "insert into Games(Player2, Board, TimeLimit, GameState, StartTime) output inserted.GameID values(@Player2, @Board, @TimeLimit, @GameState, @StartTime) where Player1 != @isNull";
                            cmd = "update Games set Player2=@Player2, TimeLimit=@TimeLimit, GameState=@GameState, StartTime=@StartTime where GameID =@LastGameID";
                            using (SqlCommand command = new SqlCommand(cmd, conn, trans))
                            {
                                //Console.WriteLine("Addihg player 2");
                                BoggleBoard board = new BoggleBoard();
                                command.Parameters.AddWithValue("@LastGameID", lastGameID);
                                command.Parameters.AddWithValue("@Player2", userData.UserToken);
                                command.Parameters.AddWithValue("@Board", board.ToString());
                                command.Parameters.AddWithValue("@TimeLimit", (PendingTimeLimit + userData.TimeLimit) / 2);
                                command.Parameters.AddWithValue("@GameState", "active");
                                command.Parameters.AddWithValue("@StartTime", DateTime.Now);
                                SetStatus(Created);//For the second player only;

                                ourGameStatus = "active";
                                response.GameID = lastGameID;

                                command.ExecuteNonQuery();
                                trans.Commit();
                                return response;
                            }
                        }
                    }
                }
                return null;
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
            if ((userData.UserToken == null))
            {
                SetStatus(Forbidden);
                return;
            }

            using (SqlConnection conn = new SqlConnection(BoggleDB))
            {
                conn.Open();
                using (SqlTransaction trans = conn.BeginTransaction())
                {

                    string cmd = "delete from Games where  Player1 = @Player1";
                    using (SqlCommand command = new SqlCommand(cmd, conn, trans))
                    {
                        command.Parameters.AddWithValue("@Player1", userData.UserToken);

                        if (command.ExecuteNonQuery() == 0)
                        {
                            SetStatus(Forbidden);

                        }
                        else
                        {
                            SetStatus(OK);

                        }
                        ourGameStatus = "";
                        trans.Commit();

                        return;

                    }
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
            PlayWordResponse response = new PlayWordResponse();
            string trimmedString = userData.Word.Trim().ToUpper();

            if (String.IsNullOrEmpty(trimmedString) || String.IsNullOrEmpty(GameID))
            {
                SetStatus(Forbidden);
                return null;
            }

            using (SqlConnection conn = new SqlConnection(BoggleDB))
            {
                conn.Open();
                using (SqlTransaction trans = conn.BeginTransaction())
                {
                    //checks to see if the user token exists in the users table
                    using (SqlCommand command = new SqlCommand("select UserToken from Users where UserToken = @UserToken", conn, trans))
                    {
                        command.Parameters.AddWithValue("@UserToken", userData.UserToken);

                        // This executes a query (i.e. a select statement).  The result is an
                        // SqlDataReader that you can use to iterate through the rows in the response.
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            //If we don't have a valid UserToken
                            if (!reader.HasRows)
                            {
                                SetStatus(Forbidden);
                                trans.Commit();
                                return null;
                            }
                        }
                    }
                    //Check to see if the gameID is valid
                    string cmd = "select GameID from Games where GameID = @GameID";
                    using (SqlCommand command = new SqlCommand(cmd, conn, trans))
                    {
                        command.Parameters.AddWithValue("@UserToken", userData.UserToken);
                        command.Parameters.AddWithValue("@GameStatus", "active");
                        command.Parameters.AddWithValue("@GameID", GameID);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            //If we don't have a valid UserToken
                            if (!reader.HasRows)
                            {
                                SetStatus(Forbidden);
                                trans.Commit();
                                return null;
                            }
                        }
                    }
                    //See if the UserToken is not a player in an active game
                    cmd = "select GameID from Games where ((Player1 = @UserToken OR Player2 = @UserToken) AND GameState = @GameStatus AND GameID = @GameID)";
                    using (SqlCommand command = new SqlCommand(cmd, conn, trans))
                    {
                        command.Parameters.AddWithValue("@UserToken", userData.UserToken);
                        command.Parameters.AddWithValue("@GameStatus", "active");
                        command.Parameters.AddWithValue("@GameID", GameID);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            //If we don't have a valid UserToken
                            if (!reader.HasRows)
                            {
                                SetStatus(Conflict);
                                trans.Commit();
                                return null;
                            }
                        }

                    }

                    cmd = "insert into Words(Word, GameID, Player, Score) values(@Word, @GameID, @Player, @Score)";
                    using (SqlCommand command = new SqlCommand(cmd, conn, trans))
                    {
                        int score = ScoreWord(trimmedString, userData.UserToken, GameID);
                        command.Parameters.AddWithValue("@Word", trimmedString);
                        command.Parameters.AddWithValue("@Player", userData.UserToken);
                        command.Parameters.AddWithValue("@GameID", GameID);
                        command.Parameters.AddWithValue("@Score", score);

                        response.Score = score;
                        command.ExecuteNonQuery();
                        trans.Commit();
                    }
                }
                return response;

                //END OF CONN
            }

            /*int thisgameID;
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
*/
            return null;

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

            StatusResponse response = new StatusResponse();

            //nt TimeLeft = 0;

            using (SqlConnection conn = new SqlConnection(BoggleDB)) //REUSE ME
            {
                conn.Open();
                using (SqlTransaction trans = conn.BeginTransaction())
                {

                    string cmd = "select GameID from Games where GameID = @GameID";
                    using (SqlCommand command = new SqlCommand(cmd, conn, trans))
                    {
                        command.Parameters.AddWithValue("@GameID", GameID);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (!reader.HasRows)
                            {
                                SetStatus(Forbidden);
                                return null;
                            }
                        }
                    }

                    string curStatus = "";
                    //If we don't have have Brief
                    if (Brief == "no" || Brief == null)
                    {
                        cmd = "select GameState from Games where GameID = @GameID";
                        using (SqlCommand command = new SqlCommand(cmd, conn, trans))
                        {
                            command.Parameters.AddWithValue("@GameID", GameID);
                            string GameState = command.ExecuteScalar().ToString();
                            if (GameState == "pending")
                            {
                                response.GameState = "pending";
                            }
                        }

                        //Get start time.
                        DateTime curStartTime;
                        cmd = "select StartTime from Games where GameID = @GameID";
                        using (SqlCommand command = new SqlCommand(cmd, conn, trans))
                        {
                            command.Parameters.AddWithValue("@GameID", GameID);
                            curStartTime = (DateTime)command.ExecuteScalar();
                        }
                        //Get time limit.
                        int curTimeLimit = 0;
                        cmd = "select TimeLimit from Games where GameID = @GameID";
                        using (SqlCommand command = new SqlCommand(cmd, conn, trans))
                        {
                            command.Parameters.AddWithValue("@GameID", GameID);
                            curTimeLimit = (int)command.ExecuteScalar();
                            response.TimeLimit = curTimeLimit;
                        }
                        //Generate the time left.
                        DateTime now = DateTime.UtcNow;
                        TimeSpan difference = now.Subtract(curStartTime);
                        int timeLeft = curTimeLimit - difference.Seconds;

                        string localGameStatus = "";

                        //Change the status
                        if (timeLeft <= 0)
                        {
                            localGameStatus = "completed";
                        }

                        if (timeLeft > 0)
                        {
                            localGameStatus = "active";
                        }

                        response.GameState = localGameStatus;

                        if (localGameStatus == "active")
                        {
                            //cmd = "update Games set Player2=@Player2, TimeLimit=@TimeLimit, GameState=@GameState, StartTime=@StartTime where GameID =@LastGameID";
                            cmd = "update Games set GameStatus=@GameStatus where GameID = @GameID  ";
                            using (SqlCommand command = new SqlCommand(cmd, conn, trans))
                            {
                                command.Parameters.AddWithValue("@GameStatus", localGameStatus);
                            }
                            cmd = "select Board from Games where GameID = @GameID";
                            using (SqlCommand command = new SqlCommand(cmd, conn, trans))
                            {
                                command.Parameters.AddWithValue("@GameID", GameID);
                                response.Board = command.ExecuteScalar().ToString();
                            }

                            //Get the player tokens
                            response.Player1 = new player();
                            string Player1Token = "";
                            cmd = "select Player1 from Games where GameID = @GameID";
                            using (SqlCommand command = new SqlCommand(cmd, conn, trans))
                            {
                                command.Parameters.AddWithValue("@GameID", GameID);
                                Player1Token = command.ExecuteScalar().ToString();
                            }
                            response.Player2 = new player();
                            string Player2Token = "";
                            cmd = "select Player2 from Games where GameID = @GameID";
                            using (SqlCommand command = new SqlCommand(cmd, conn, trans))
                            {
                                command.Parameters.AddWithValue("@GameID", GameID);
                                Player2Token = command.ExecuteScalar().ToString();
                            }

                            //Get the players individual scores
                            cmd = "select Score from Words where Player = @Player1 AND GameID = @GameID";
                            using (SqlCommand command = new SqlCommand(cmd, conn, trans))
                            {
                                command.Parameters.AddWithValue("@GameID", GameID);
                                command.Parameters.AddWithValue("@Player1", Player1Token);
                                int finalScore = 0;

                                using (SqlDataReader reader = command.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        int temp;
                                        int.TryParse(reader["Score"].ToString(), out temp);

                                        finalScore += temp;

                                    }
                                }

                                response.Player1.Score = finalScore;
                            }
                            //fOR PLAYER 2'S SCORE
                            cmd = "select Score from Words where Player = @Player2 AND GameID = @GameID";
                            using (SqlCommand command = new SqlCommand(cmd, conn, trans))
                            {
                                command.Parameters.AddWithValue("@GameID", GameID);
                                command.Parameters.AddWithValue("@Player2", Player2Token);
                                int finalScore = 0;

                                using (SqlDataReader reader = command.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        int temp;
                                        int.TryParse(reader["Score"].ToString(), out temp);

                                        finalScore += temp;

                                    }
                                }

                                response.Player2.Score = finalScore;
                            }

                        }//end of activeS

                        if (curStatus == "completed")
                        {

                        }
                    }




                }//END TRANSACTION
            }//END Conn

            /*
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
                        } */
            return response;
        }

        /// <summary>
        /// Scores a word based on the rules of Boggle.
        /// </summary>
        /// <param name="word"></param>
        /// <param name="token"></param>
        /// <returns>the integer score that </returns>
        private int ScoreWord(string word, string token, string gID)
        {
            // If a string has fewer than three characters, it scores zero points.
            //Otherwise, if a string has a duplicate that occurs earlier in the list, it scores zero points.
            if (word.Length < 3)
            {
                return 0;
            }

            BoggleBoard curBoard = null;
            //Otherwise, if a string is legal (it appears in the dictionary and occurs on the board),
            if (dictionary.Contains(word))
            {
                //&& games[users[token].CurrentGameID].BogBoard.CanBeFormed(word))

                using (SqlConnection conn = new SqlConnection(BoggleDB)) //REUSE ME
                {
                    conn.Open();
                    using (SqlTransaction trans = conn.BeginTransaction())
                    {
                        //Check to see if the boggle board can be formed.
                        string cmd = "select Board from Games where GameID = @GameID";
                        using (SqlCommand command = new SqlCommand(cmd, conn, trans))
                        {
                            command.Parameters.AddWithValue("@GameID", gID);
                            string gotBoard = command.ExecuteScalar().ToString();

                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                //If we don't have a valid UserToken
                                if (reader.HasRows)
                                {
                                    curBoard = new BoggleBoard(gotBoard);
                                }
                            }
                        }
                    }
                }
            }

            if (curBoard != null && curBoard.CanBeFormed(word))
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

