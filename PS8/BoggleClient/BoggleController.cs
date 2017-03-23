using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace BoggleClient
{
    class BoggleController: IBoggleClient
    {
        //The window being controlled.
        private IBoggleClient window;

        public event Action CloseGameEvent;
        public event Action<string> CreateUserEvent;
        public event Action JoinGameEvent;
        public event Action CancelJoinRequestEvent;
        public event Action<string> PlayWordEvent;
        public event Action<string> GameStatusEvent;
        private string localUserToken;
        private BoggleView view;

        /// <summary>
        /// Creates the controller for the provided view
        /// </summary>
        /// <param name="view"></param>
        public BoggleController(BoggleView view)
        {
            this.view = view;
            this.userToken = "0";
            view.RegisterPressed += Register;

        }

        public async void Register(string name, string domain)
        {
            try
            {
                view.EnableControls(false);
                using (HttpClient client = CreateClient(domain))
                {
                    string url = String.Format(domain);
                    StringContent content = null;

                }
            }
            catch (TaskCanceledException)
            {
                //Does Nothing when caught
            }
            finally
            {
                view.EnableControls(true);
            }
        }

        private HttpClient CreateClient(string domain)
        {
            //creates the client with base address given via domain
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(domain);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            return client;

        }

        public string userToken
        {
            get
            {
                return localUserToken;
            }

            set
            {
                localUserToken = value;
            }
        }

        private int localGameTimeLimit = 0;
        public int gameTimeLimit
        {
            get
            {
                return localGameTimeLimit;
            }

            set
            {
                localGameTimeLimit = value;
            }
        }


        private int localPlayTimeLimit;
        public int playTimeLimit
        {
            get
            {
                return localPlayTimeLimit;
            }

            set
            {
                localPlayTimeLimit = value;
            }
        }

        /*
        public BoggleController(IBoggleClient window)
        {
            this.window = window;
            window.CreateUserEvent += HandleCreateUser;
            window.JoinGameEvent += HandleJoinGame;
            window.CancelJoinRequestEvent += HandleCancelJoinRequest;
            window.PlayWordEvent += HandlePlayWord;
            //WE MIGHT NEED TO CHANGE THIS
            window.GameStatusEvent += HandleGameStatus;

        }
        */

        /// <summary>
        /// Create a new user.
        /// 
        /// If Nickname is null, or is empty when trimmed, responds with status 403 (Forbidden).
        /// Otherwise, creates a new user with a unique UserToken and the trimmed Nickname.The 
        /// returned UserToken should be used to identify the user in subsequent 
        /// requests.Responds with status 201 (Created).
        /// </summary>
        /// <param name="nickname"></param>
        private void HandleCreateUser(string nickname)
        {


        }

        /// <summary>
        /// Join a game.
        /// 
        /// If UserToken is invalid, TimeLimit <5, or TimeLimit> 120, responds with status 403 (Forbidden).
        /// Otherwise, if UserToken is already a player in the pending game, responds with status 409 (Conflict).
        /// Otherwise, if there is already one player in the pending game, adds UserToken as the second player. 
        /// The pending game becomes active and a new pending game with no players is created. The active game's
        /// time limit is the integer average of the time limits requested by the two players. Returns the new 
        /// active game's GameID (which should be the same as the old pending game's GameID). Responds
        /// with status 201 (Created).
        /// Otherwise, adds UserToken as the first player of the pending game, and the TimeLimit as the pending
        /// game's requested time limit. Returns the pending game's GameID. Responds with status 202 (Accepted).
        /// </summary>
        private void HandleJoinGame()
        {

        }

        /// <summary>
        /// Cancel a pending request to join a game.
        /// 
        /// If UserToken is invalid or is not a player in the pending game, responds with status 403 (Forbidden).
        /// Otherwise, removes UserToken from the pending game and responds with status 200 (OK).
        /// </summary>
        private void HandleCancelJoinRequest()
        {

        }

        /// <summary>
        /// Play a word in a game.
        /// If Word is null or empty when trimmed, or if GameID or UserToken is missing or invalid, 
        /// or if UserToken is not a player in the game identified by GameID, responds with response code 403 (Forbidden)
        /// Otherwise, if the game state is anything other than "active", responds with response code 409 (Conflict).
        /// Otherwise, records the trimmed Word as being played by UserToken in the game identified by GameID.
        /// Returns the score for Word in the context of the game (e.g. if Word has been played before 
        /// the score is zero). Responds with status 200 (OK). Note: The word is not case sensitive.
        /// </summary>
        /// <param name="word"></param>
        private void HandlePlayWord(string word)
        {

        }

        /// <summary>
        /// If GameID is invalid, responds with status 403 (Forbidden). Otherwise, returns 
        /// information about the game named by GameID as illustrated below.Note that the information returned depends on 
        /// whether "Brief=yes" was included as a parameter as well as on the state of the game. Responds 
        /// with status code 200 (OK). Note: The Board and Words are not case sensitive.
        /// </summary>
        /// <param name="brief"></param>
        private void HandleGameStatus(string brief)
        {

        }

    }
}
