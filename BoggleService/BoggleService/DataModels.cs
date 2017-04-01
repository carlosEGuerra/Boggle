﻿using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace Boggle
{

    /// <summary>
    /// The literal API input for Create User
    /// </summary>
    public class CreateUserData
    {
        public string Nickname { get; set; }
    }

    public class CreateUserResponse
    {
        public string UserToken { get; set; }
    }

    /// <summary>
    /// The literal API input for Join Game
    /// </summary>
    public class JoinGameData
    {
        public string UserToken { get; set; }
        public int TimeLimit { get; set; }
    }

    public class JoinGameResponse
    {
        public string GameID { get; set; }
    }

    /// <summary>
    /// The literal API input for Cancel Join
    /// </summary>
    public class CancelJoinData
    {
        public string UserToken { get; set; }

    }

    /// <summary>
    /// The literal API input for Play Word
    /// </summary>
    public class PlayWordData
    {
        public string UserToken { get; set; }
        public string Word { get; set; }
    }

    public class PlayWordResponse
    {
        public int Score { get; set; }
    }

    /// <summary>
    /// Literal API optional input for a game.
    /// </summary>
    public class GameStatusData
    {
        public string Brief { get; set; }
    }

    /// <summary>
    /// The literal API output for Game
    /// </summary>
    [DataContract]
    public class StatusResponse
    {
        [DataMember]
        public string GameState { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string Board { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int TimeLimit { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public int TimeLeft { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public player Player1 { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public player Player2 { get; set; }

    }

    [DataContract]
    public class player
    {
        [DataMember(EmitDefaultValue = false)]
        public string Nickname { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public int Score { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public object[] WordsPlayed = new object[100];
        //public List<List<object>> WordsPlayed = new List<List<object>>();
        //public List<WordItem> WordsPlayed = new List<WordItem>();
        //public Dictionary<string, WordItem> WordsPlayed = new Dictionary<string, WordItem>();
       
    }

    public class User
    {
        /// <summary>
        /// This is the userToken
        /// </summary>
        public string UserId { get; set;}
        /// <summary>
        /// Nickname given by the user
        /// </summary>
        public string Nickname { get; set; }

        /// <summary>
        /// Time limit requested by the user.
        /// </summary>
        public int GivenTimeLimit { get; set; }

        /// <summary>
        /// Lets us know if the current user is in a pending game already.
        /// </summary>
        public bool HasPendingGame = false;

        /// <summary>
        /// The current game ID of the user.
        /// </summary>
        public int CurrentGameID = -1;

        /// <summary>
        /// The current total score of the player
        /// </summary>
        public int CurrentTotalScore = 0;

        /// <summary>
        /// All words played by the user.
        /// </summary>
        public Dictionary<string, int> WordsPlayed = new Dictionary<string, int>();

    }

    public class Game
    {
        /// <summary>
        /// The first player to enter a game.
        /// </summary>
        public string Player1 { get; set; }

        /// <summary>
        /// Second player to enter a game.
        /// </summary>
        public string Player2 { get; set; }

        /// <summary>
        /// The current game ID.
        /// </summary>
        public int GameID { get; set; }
        
        /// <summary>
        ///The 16-letter string representation of the current game board.
        /// </summary>
        public string Board { get; set; }

        public BoggleBoard BogBoard { get; set;}

        /// <summary>
        /// Time limit given by the server.
        /// </summary>
        public int TimeLimit { get; set; }

        /// <summary>
        /// Time the game started.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Holds the game status: pending, active, or completed.
        /// </summary>
        public string GameStatus { get; set; }

    }

    [DataContract]
    public class WordItem
    {
        [DataMember(EmitDefaultValue = false)]
        public string Word { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int Score { get; set; }
    }

    /// <summary>
    /// Representation of all of the words in the game.
    /// </summary>
    public class Words
    {
        /// <summary>
        /// Id of the word??
        /// </summary>
   //     public int Id { get; set; }

        /// <summary>
        /// String reprsentation of the word played.
        /// </summary>
        public string Word { get; set; }

        /// <summary>
        /// ID of the current game.
        /// </summary>
        public int GameId { get; set; }

        /// <summary>
        /// Player who played the current word.
        /// </summary>
        public string Player { get; set; } 

        /// <summary>
        /// Score of the individual word.
        /// </summary>
        public int Score { get; set; }
    }

}