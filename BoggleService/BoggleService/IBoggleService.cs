using System.Collections.Generic;
using System.IO;
using System.ServiceModel;
using System.ServiceModel.Web;

namespace Boggle
{
    [ServiceContract]
    public interface IBoggleService
    {
        /// <summary>
        /// Sends back index.html as the response body.
        /// </summary>
        [WebGet(UriTemplate = "/api")]
        Stream API();

        /// <summary>
        /// Returns the nth word from dictionary.txt.  If there is
        /// no nth word, responds with code 403. This is a demo;
        /// you can delete it.
        /// </summary>
        [WebGet(UriTemplate = "/word?index={n}")]
        string WordAtIndex(int n);

        /// <summary>
        /// Create a new user.
        ///If Nickname is null, or is empty when trimmed, responds with status 403 (Forbidden).
        ///Otherwise, creates a new user with a unique UserToken and the trimmed Nickname.
        ///The returned UserToken should be used to identify the user in subsequent requests.
        ///Responds with status 201 (Created).
        /// </summary>
        /// <param name="item"></param>
        /// <returns> string of the userToken </returns>
        [WebInvoke(Method = "POST", UriTemplate = "/users")]
        string CreateUser(CreateUserData userData);

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
        [WebInvoke(Method = "POST", UriTemplate = "/games")]
        int JoinGame(JoinGameData userData);

        /// <summary>
        /// Cancel a pending request to join a game.
        /// 
        /// If UserToken is invalid or is not a player in the pending game, responds with status 403 (Forbidden).
        /// Otherwise, removes UserToken from the pending game and responds with status 200 (OK).
        /// </summary>
        /// <param name="user"></param>
        [WebInvoke(Method = "PUT", UriTemplate = "/games")]
        void CancelJoinRequest(CancelJoinData userData);

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
        [WebInvoke(Method = "PUT", UriTemplate = "/games/{GameID}")]
        int PlayWord(PlayWordData userData);


        [WebGet(UriTemplate = "/games/{GameID}")]
        string GameStatus(GameStatusData game);

]
        [WebGet(UriTemplate = "/?=brief=yes")]
        string GameStatusBYes(GameStatusData game);

        //[WebGet(UriTemplate = "/GetAllItems?completed={completedOnly}&user={userID}")]
        //[WebGet(UriTemplate = "/GetAllItems?completed={completedOnly}&user={userID}")]
        //[WebGet(UriTemplate = "/GetAllItems?completed={completedOnly}&user={userID}")]

    }
}
