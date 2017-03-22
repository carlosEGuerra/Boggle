using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoggleClient
{
    interface IBoggleClient
    {
        string userToken { get; set; }

        /// <summary>
        /// The time limit of the whole game
        /// </summary>
        int gameTimeLimit { get; set; }

        int playTimeLimit { get; set; }

        event Action CloseGameEvent;

        /// <summary>
        /// Create user takes in a string nickname.
        /// </summary>
        event Action<string> CreateUserEvent;

        event Action JoinGameEvent;

        event Action CancelJoinRequestEvent;

        event Action<string> PlayWordEvent;

        event Action<string> GameStatusEvent;

        
    }
} 
