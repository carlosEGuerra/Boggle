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

        private int player2Score, player1Score;

        /// <summary>
        /// To help us keep track of which side player names, words, and scores should appear.
        /// </summary>
        private bool weAreP1 = false;

        /// <summary>
        /// Holds the nickname the user put in.
        /// </summary>
        private string ourName;

        private System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();

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
            view.PlayWord += WordPlayed;
            view.CancelJoinRequest += HandleCancelJoinRequest;
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

                ourName = name;
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
        private async void HandleCancelJoinRequest()
        {
            try
            {
                view.EnableControls(false);
                using (HttpClient client = CreateClient())
                {
                    dynamic userData = new ExpandoObject();
                    userData.UserToken = userToken.UserToken;

                    string getStatus = "games";
                    StringContent content = new StringContent(JsonConvert.SerializeObject(userData), Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await client.PutAsync(getStatus, content);

                    //If the status is successful
                    if (response.IsSuccessStatusCode)
                    {
                        //Get the letters of the boggle board.
                        string result = response.Content.ReadAsStringAsync().Result;
                        dynamic token = JsonConvert.DeserializeObject(result);
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
        /// If GameID is invalid, responds with status 403 (Forbidden). Otherwise, returns 
        /// information about the game named by GameID as illustrated below.Note that the information returned depends on 
        /// whether "Brief=yes" was included as a parameter as well as on the state of the game. Responds 
        /// with status code 200 (OK). Note: The Board and Words are not case sensitive.
        /// </summary>
        /// <param name="brief"></param>
        private void HandleGameStatus(string brief)
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
                        string result = response.Content.ReadAsStringAsync().Result;
                        dynamic token = JsonConvert.DeserializeObject(result);

                        string gameState = (string)token.GameState;
                        board = token.Board;
                        player1 = (string)token.Player2.Nickname;
                        player2 = (string)token.Player1.Nickname;


                        if (player1 == ourName)
                        {
                            view.setP1 = player1;
                            view.setP2 = player2;
                            weAreP1 = true;
                        }

                        if(player2 == ourName)
                        {
                            view.setP1 = player2;
                            view.setP1 = player1;
                        }

                       
                        if (gameState == "active")
                        {
                            //MessageBox.Show("active");
                            if (weAreP1)
                            {
                                view.setPlayer1Score = token.Player1.Score;
                                view.setPlayer2ScoreBox = token.Player2.Score;
                            }

                            else
                            {
                                view.setPlayer1Score = token.Player2.Score;
                                view.setPlayer2ScoreBox = token.Player1.Score;
                            }
              
                            view.setTimeLeftBox = token.TimeLeft;
                        }
                        else if(gameState == "completed")
                        {
                          
                          
                            view.setTimeLeftBox = "0";
                            timer.Stop();
                            MessageBox.Show("Game has ended!");
                            int.TryParse(token.Player2.Score, out player2Score);//Not parsing correctly.
                            int.TryParse(token.Player1.Score, out player1Score);
                            if (player1Score > player2Score)
                            {
                                MessageBox.Show("Player1 has won!");
                            }
                            else if (player1Score < player2Score)
                            {
                                MessageBox.Show("Player2 has won!");
                            }
                            else
                            {
                                MessageBox.Show("It's a tie");
                            }
                        }
                        else if(gameState == "pending")
                        {
                            MessageBox.Show("Pending Show");
                        }

                        //Put each letter in the view.
                        AddLetters();
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

                        //Put each letter in the view.
                        AddLetters();
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
            finally
            {
                timer.Start();
                timer.Interval = 500;
                timer.Tick += updateBoard;
            }
        }

        /// <summary>
        /// Updates the boards timer and the board every 1000 ms

        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void updateBoard(object sender, EventArgs e)
        {
            HandleGameStatus((string)gameID);
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

        /// <summary>
        /// Handles when a word is played.
        /// </summary>
        /// <param name="word"></param>
        private async void WordPlayed(string word)
        {
            try
            {
                view.EnableControls(false);
                using (HttpClient client = CreateClient())
                {
                 
                    // Create the parameter
                    dynamic userData = new ExpandoObject();
                    userData.UserToken = userToken.UserToken;
                    userData.Word = word;

                    StringContent content = new StringContent(JsonConvert.SerializeObject(userData), Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PutAsync("games/" + gameID, content);

                    //If the status is successful
                    if (response.IsSuccessStatusCode)
                    {
                        //Get the score
                        String result = response.Content.ReadAsStringAsync().Result;
                        dynamic token = JsonConvert.DeserializeObject(result);

                        
                        if (weAreP1)
                        {
                            view.player1Words += word + '\n';
                        }

                        if (!weAreP1)
                        {
                            view.player2Words += word + '\n';
                        }

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
    }
}
