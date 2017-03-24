using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BoggleClient
{
    class BoggleController
    {
        //The window being controlled.
        private BoggleView view;

        /// <summary>
        /// For canceling the current operation
        /// </summary>
        private CancellationTokenSource tokenSource;

        /// <summary>
        /// Holds the value of the user
        /// </summary>
        private dynamic userToken;

        private dynamic gameID;

        /*
                public event Action CloseGameEvent;
                public event Action<string> CreateUserEvent;
                public event Action JoinGameEvent;
                public event Action CancelJoinRequestEvent;
                public event Action<string> PlayWordEvent;
                public event Action<string> GameStatusEvent;


        */
        /// <summary>
        /// Creates the controller for the provided view
        /// </summary>
        /// <param name="view"></param>
        public BoggleController(BoggleView view)
        {
            this.view = view;
            this.userToken = "0";
            view.RegisterPressed += Register;
            view.CancelPressed += Cancel;
            view.JoinGamePressed += JoinPressed;

        }

        public void Cancel()
        {
            tokenSource.Cancel();
        }

        public async void Register(string name, string domain)
        {
            try
            {
                //disables the controls
                view.EnableControls(false);

                //creates the HTTP client via user input domain
                using (HttpClient client = CreateClient())
                {
                    //creating the user parameter
                    dynamic userData = new ExpandoObject();
                    userData.Nickname = name;

                    tokenSource = new CancellationTokenSource();

                    //Compose and send the request
                    tokenSource = new CancellationTokenSource();
                    StringContent content = new StringContent(JsonConvert.SerializeObject(userData), Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await client.PostAsync("users", content, tokenSource.Token);
                    if (response.IsSuccessStatusCode)
                    {
                        //Get the user token
                        String result = response.Content.ReadAsStringAsync().Result;
                        userToken = JsonConvert.DeserializeObject(result);
                        view.userRegistered = true;
                        view.setUserID = userToken.UserToken;
                        MessageBox.Show("You are registered! :D");
                    }
                    else
                    {
                        MessageBox.Show("Sorry but we have an error registering your username" + response.StatusCode + "\n" + response.ReasonPhrase);
                    }

                }
            }
            catch (TaskCanceledException)
            {
                //Does Nothing when caught
            }
            finally
            {
                view.EnableControls(true);
                //Join game here


            }
        }


        /*
        private HttpClient CreateClient(string domain)
        {
            //creates the client with base address given via domain
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(domain);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            return client;

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
        /// If UserToken is invalid, TimeLimit <5, or TimeLimit > 120, responds with status 403 (Forbidden).
        /// Otherwise, if UserToken is already a player in the pending game, responds with status 409 (Conflict).
        /// Otherwise, if there is already one player in the pending game, adds UserToken as the second player. 
        /// The pending game becomes active and a new pending game with no players is created. The active game's
        /// time limit is the integer average of the time limits requested by the two players. Returns the new 
        /// active game's GameID (which should be the same as the old pending game's GameID). Responds
        /// with status 201 (Created).
        /// Otherwise, adds UserToken as the first player of the pending game, and the TimeLimit as the pending
        /// game's requested time limit. Returns the pending game's GameID. Responds with status 202 (Accepted).
        /// </summary>
        private async void JoinPressed(string dTimeLimit)
        {
            try
            {
                view.EnableControls(false);
                using (HttpClient client = CreateClient())
                {
                    int dTL;
                    int.TryParse(dTimeLimit, out dTL);
                    // Create the parameter
                    dynamic userData = new ExpandoObject();
                    userData.UserID = userToken.UserToken;
                    userData.TimeLimit = dTL;

                    StringContent content = new StringContent(JsonConvert.SerializeObject(userData), Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await client.PostAsync("games", content, tokenSource.Token);

                    //If the status is successful
                    if (response.IsSuccessStatusCode)
                    {
                        //Get the gameID
                        String result = response.Content.ReadAsStringAsync().Result;
                        dynamic token = JsonConvert.DeserializeObject(result);
                        
                        gameID = token.GameID;
                        view.setGameID = gameID;
                        
                    }
                    else
                    {
                        Console.WriteLine("Error submitting: " + response.StatusCode);
                        Console.WriteLine(response.ReasonPhrase);
                    }
                }
 
            }
            finally
            {
                view.EnableControls(true);
            }
        }

        /// <summary>
        /// Cancel a pending request to join a game.
        /// 
        /// If UserToken is invalid or is not a player in the pending game, responds with status 403 (Forbidden).
        /// Otherwise, removes UserToken from the pending game and responds with status 200 (OK).
        /// </summary>
        private void HandleCancelJoinRequest()
        {
            tokenSource.Cancel();
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

        /// <summary>
        /// Creates an HttpClient for communicating with the server.
        /// </summary>
        private static HttpClient CreateClient()
        {
            // Create a client whose base address is BoggleService
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri("http://cs3500-boggle-s17.azurewebsites.net/BoggleService.svc/");

            // Tell the server that the client will accept this particular type of response data
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("Accept", "application/json");

            // There is more client configuration to do, depending on the request.
            return client;
        }
    }
}
