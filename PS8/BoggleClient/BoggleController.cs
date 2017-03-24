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

        /// <summary>
        /// Holds the game ID from the server
        /// </summary>
        private dynamic gameID;

        /// <summary>
        /// Holds the letters of the gameboard.
        /// </summary>
        private dynamic board;

        /// <summary>
        /// Holds name of player 1.
        /// </summary>
        private dynamic player1;

        /// <summary>
        /// Holds name of player 2.
        /// </summary>
        private dynamic player2;

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
            view.CancelJoinRequest += HandleCancelJoinRequest;
        }

        public void Cancel()
        {
            tokenSource.Cancel();
            new BoggleController(new BoggleView());
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
                    userData.UserToken = userToken.UserToken;
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


                        //Put all of the Boggle letters into the view.
                        SetUpBoggleBoard();

                        //  HttpResponseMessage getResponse = await client.GetAsync("games/{+" + gameID.GameID + "}");

                    }
                    else
                    {
                        Console.WriteLine("Error submitting: " + response.StatusCode);
                        Console.WriteLine(response.ReasonPhrase);
                    }
                }
                
            }
            catch
            {
                //Do nothing.
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

        /// <summary>
        /// Adds characters from the server to the boggle board.
        /// </summary>
        private void SetUpBoggleBoard()
        {
            try
            {
                view.EnableControls(false);
                using (HttpClient client = CreateClient())
                {
                    string getStatus = "games/" + gameID;
                    HttpResponseMessage response = client.GetAsync(getStatus).Result;

                    //If the status is successful
                    if (response.IsSuccessStatusCode)
                    {
                        //Get the letters of the boggle board.
                        String result = response.Content.ReadAsStringAsync().Result;
                        dynamic token = JsonConvert.DeserializeObject(result);

                        this.board = token.Board; 
                        //Put each letter in the boxes
                        AddLetters();

                        this.player1 = token.Player2.Nickname;
                        view.setP1 = player1;

                        /*
                        this.player2 = token.Player1.Nickname;
                        view.setP2 = player2;
                        */
                    }
                    else
                    {
                        Console.WriteLine("Error submitting: " + response.StatusCode);
                        Console.WriteLine(response.ReasonPhrase);
                    }
                }
            }

            catch
            {

            }
        }

        /// <summary>
        /// Completes the work of actually adding letters to the view.
        /// </summary>
        private void AddLetters()
        {
            object aBoard = this.board.Value;
            string bBoard = aBoard.ToString();
            view.setButton1 = bBoard[0].ToString();
            view.setButton2 = bBoard[1].ToString();
            view.setButton3 = bBoard[2].ToString();
            view.setButton4 = bBoard[3].ToString();
            view.setButton5 = bBoard[4].ToString();
            view.setButton6 = bBoard[5].ToString();
            view.setButton7 = bBoard[6].ToString();
            view.setButton8 = bBoard[7].ToString();
            view.setButton9 = bBoard[8].ToString();
            view.setButton10 = bBoard[9].ToString();
            view.setButton11 = bBoard[10].ToString();
            view.setButton12 = bBoard[11].ToString();
            view.setButton13 = bBoard[12].ToString();
            view.setButton14 = bBoard[13].ToString();
            view.setButton15 = bBoard[14].ToString();
            view.setButton16 = bBoard[15].ToString();
        }
    }
}
