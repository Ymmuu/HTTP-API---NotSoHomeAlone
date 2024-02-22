using System.Net;
using Npgsql;
namespace real_time_horror_group3;

public class GameMessage()
{
    public static string Help(HttpListenerResponse response)
    {
        string message = @"
List of possible commands and paths:

Create player: 
curl -d ""<player name>"" localhost:3000/player

Ready:
curl -X PATCH localhost:3000/<player name>/ready

Start game: 
curl -X PATCH localhost:3000/<player name>/start

Move to room: 
curl -X PATCH -d ""<room name>"" localhost:3000/<player name>/move

Check status of windows in current room: 
curl localhost:3000/<player name>/windows

Check status of doors in current room: 
curl localhost:3000/<player name>/doors

Check room for danger:
curl localhost:3000/<player name>/room

Lock window: 
curl -X PATCH -d ""<window name>"" localhost:3000/<player name>/windows

Lock door: 
curl -X PATCH -d ""<door name>"" localhost:3000/<player name>/doors

Remove danger:
curl -X PATCH localhost:3000/<player name>/room

Write on whiteboard:
curl -d ""<message>"" localhost:3000/<player name>/whiteboard

Read posts on whiteboard:
curl localhost:3000/<player name>/whiteboard

See time elapsed since start
curl localhost:3000/<player name>/time

Restart the game
curl -X PATCH localhost:3000/<player name>/restart

Room names: 'kitchen', 'hallway', 'living room'.
Each room has 1 door 'A' and two windows 'A' & 'B'.
"; 


        response.StatusCode = (int)HttpStatusCode.OK;
        return message;
    }

    DateTime then = new(2020, 05, 06);
    DateTime now = DateTime.Now;
    public static string NotFound(HttpListenerResponse response)
    {
        string message = "Invalid path or command.\nFor list of available commands: curl localhost:3000/help";
        response.StatusCode = (int)HttpStatusCode.NotFound;
        return message;
    }
    public static string PrintGameOverScreen(NpgsqlDataSource db, HttpListenerRequest request, HttpListenerResponse response)
    {

        var selectHighscore = db.CreateCommand(@"
      SELECT * FROM public.highscore
           ORDER BY ""time"" DESC 
           LIMIT 10;        
");


        string message = "GAMEOVER\n\nhighscore: \n";

        using var reader = selectHighscore.ExecuteReader();
        while (reader.Read())
        {
            message += $"{reader.GetString(1)} - {reader.GetString(2)}\n";
        }


        return message;

    }
    public static string Story(HttpListenerResponse respons)
    {
        string message = @"
You stand tired in your kitchen reheating some leftover noodles from yesterday.
Suddenly you hear some strange banging noises coming from outside of the house
Looking out the kitchen window you see two dark masked figures trying to break your garage door.
OMG! They'll sure be going for the house next!
You need to keep it secure as long as you can by checking
every room ('kitchen', 'hallway', 'living room') for any open entry point.
But be careful about your steps along the way, strange things could happen..";
        return message;
    }
}