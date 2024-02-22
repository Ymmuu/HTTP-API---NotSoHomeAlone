using System.Net;
using System.Text;
using Npgsql;
using real_time_horror_group3;

string dbUri = "Host=localhost;Port=5455;Username=postgres;Password=postgres;Database=notsohomealone";
using var db = NpgsqlDataSource.Create(dbUri);

Database database = new(db);
await database.Create();

bool gameOver = false;
bool listen = true;
bool sessionStarted = false;
string? message = string.Empty;

Console.CancelKeyPress += delegate (object? sender, ConsoleCancelEventArgs e)
{
    Console.WriteLine("Interrupting cancel event");
    e.Cancel = true;
    listen = false;
};

int port = 3000;
HttpListener listener = new();
State state = new State(listener, db);

listener.Prefixes.Add($"http://localhost:{port}/");
listener.Start();
Console.WriteLine($"Server listening on port: {port}");

listener.BeginGetContext(new AsyncCallback(Router), state);
while (listen)
{

}

listener.Stop();
Console.WriteLine("Server stopped");

void Router(IAsyncResult result)
{
    if (result.AsyncState is State state)
    {
        HttpListener listener = state.listener;
        var db = state.db;

        HttpListenerContext context = listener.EndGetContext(result);
        HttpListenerResponse response = context.Response;
        HttpListenerRequest request = context.Request;

        response.ContentType = "text/plain";

        if (Check.EntryPointTimer(db) == false && Check.IfGameOver(db, request, response) == false)
        {
            if (Check.IfDead(db, Check.VerifyPlayer(db, request)) == false)
            {
                switch (request.Url?.AbsolutePath.ToLower())
                {
                    case (string path) when path == "/player":
                        if (request.HttpMethod is "POST")
                        {
                            message = Player.Create(db, request, response);
                        }
                        break;

                    case (string path) when path == $"/{Check.VerifyPlayer(db, request)}/ready" && path != $"//ready":
                        if (request.HttpMethod == "PATCH")
                        {
                            message = Player.Ready(db, request, response);
                        }
                        break;

                    case (string path) when path == $"/{Check.VerifyPlayer(db, request)}/start" && path != $"//start":
                        if (request.HttpMethod is "PATCH")
                        {
                            bool playersReady = Check.AllPlayersReady(db, response);
                            if (playersReady)
                            {
                                message = GameMessage.Story(response);
                                Session.Start(db);
                                sessionStarted = true;
                            }
                            else
                            {
                                message = "Not all players are ready. Please wait until all players are ready to start.";
                                response.StatusCode = (int)HttpStatusCode.OK;
                            }
                        }
                        break;

                    case (string path) when path == $"/{Check.VerifyPlayer(db, request)}/move" && path != $"//move":
                        if (sessionStarted)
                        {
                            if (request.HttpMethod is "PATCH")
                            {
                                message = Player.Move(db, request, response);
                            }
                        }
                        else
                        {
                            message = "You need to start game to play";
                            response.StatusCode = (int)HttpStatusCode.OK;
                        }
                        break;

                    case (string path) when path == $"/{Check.VerifyPlayer(db, request)}/windows" && path != $"//windows":
                        if (sessionStarted)
                        {
                            if (request.HttpMethod is "GET")
                            {
                                message = Check.Windows(db, request, response);
                            }
                            else if (request.HttpMethod is "PATCH")
                            {
                                message = Player.Lock(db, "Window", request, response);
                            }
                        }
                        else
                        {
                            message = "You need to start game to play";
                            response.StatusCode = (int)HttpStatusCode.OK;
                        }
                        break;

                    case (string path) when path == $"/{Check.VerifyPlayer(db, request)}/doors" && path != $"//doors":
                        if (sessionStarted)
                        {
                            if (request.HttpMethod is "GET")
                            {
                                message = Check.Doors(db, request, response);
                            }
                            else if (request.HttpMethod is "PATCH")
                            {
                                message = Player.Lock(db, "Door", request, response);
                            }
                        }
                        else
                        {
                            message = "You need to start game to play";
                            response.StatusCode = (int)HttpStatusCode.OK;
                        }
                        break;

                    case (string path) when path == $"/{Check.VerifyPlayer(db, request)}/room" && path != $"//room":
                        if (sessionStarted)
                        {
                            if (request.HttpMethod is "GET")
                            {
                                message = Check.Room(db, request, response);
                            }
                            else if (request.HttpMethod is "PATCH")
                            {
                                message = Player.RemoveDanger(db, request, response);
                            }
                        }
                        else
                        {
                            message = "You need to start game to play";
                            response.StatusCode = (int)HttpStatusCode.OK;
                        }
                        break;

                    case (string path) when path == $"/{Check.VerifyPlayer(db, request)}/whiteboard" && path != $"//whiteboard":
                        if (sessionStarted)
                        {
                            if (request.HttpMethod is "GET")
                            {
                                message = Player.ReadWhiteboard(db, request, response);
                            }
                            if (request.HttpMethod is "POST")
                            {
                                message = Player.WriteOnWhiteBoard(db, request, response);
                            }
                        }
                        else
                        {
                            message = "You need to start game to play";
                            response.StatusCode = (int)HttpStatusCode.OK;
                        }
                        break;

                    case (string path) when path == $"/{Check.VerifyPlayer(db, request)}/time" && path != $"//time":
                        if (sessionStarted)
                        {
                            if (request.HttpMethod is "GET")
                            {
                                message = Session.FormattedTime(db);
                            }
                        }
                        else
                        {
                            message = "You need to start game to play";
                            response.StatusCode = (int)HttpStatusCode.OK;
                        }
                        break;

                    case (string path) when path == "/help" || path == "/":
                        if (request.HttpMethod is "GET")
                        {
                            message = GameMessage.Help(response);
                        }                            
                        break;

                    default:
                        message = GameMessage.NotFound(response);
                        break;
                }
            }
            else
            {
                message = "Your player is dead";
            }
        }

        else
        {
            if (gameOver == false)
            {
                GameEvent.AddScore(db, request, response);
                gameOver = true;
                message = $"{GameMessage.PrintGameOverScreen(db, request, response)}\n\nTo restart the game use:\ncurl -X PATCH localhost:3000/<player name>/restart";
            }
            else if (request.Url?.AbsolutePath.ToLower() == $"/{Check.VerifyPlayer(db, request)}/restart" && request.Url?.AbsolutePath.ToLower() != "//restart" && gameOver)
            {
                if (request.HttpMethod == "PATCH")
                {
                    sessionStarted = false;
                    gameOver = false;
                    Session.ResetDBForNewSession(db);
                    message = "Game reset. Please create a new player to continue";
                }
            }
            else
            {
                message = GameMessage.PrintGameOverScreen(db, request, response);
            }
        }

        message = $"\n\n{message}\n\n";
        byte[] buffer = Encoding.UTF8.GetBytes(message);
        response.OutputStream.Write(buffer);
        response.OutputStream.Close();
        message = string.Empty;

        listener.BeginGetContext(new AsyncCallback(Router), state);
    }
}

record State(HttpListener listener, NpgsqlDataSource db);
