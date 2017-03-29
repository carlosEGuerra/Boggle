using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Boggle
{
    public class Users
    {
        /// <summary>
        /// This is the userToken
        /// </summary>
        public string UserId { get; set;}
        /// <summary>
        /// Nickname given by the user
        /// </summary>
        public string Nickname { get; set; }

    }

    public class Games
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
        ///The 16-letter string representation of the current game board.
        /// </summary>
        public string Board { get; set; }

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
    public class Words
    {
        /// <summary>
        /// Id of the word??
        /// </summary>
        public int Id { get; set; }

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