using Npgsql;
using System.Net;

namespace real_time_horror_group3;

public class Check()
{
    public static string Room(NpgsqlDataSource db, HttpListenerRequest request, HttpListenerResponse response)
    {
        string eventMessage = GameEvent.RandomTrigger(db);
        string? message = string.Empty;

        int roomId = PlayerPosition(db, request, response);

        NpgsqlCommand checkRoom = db.CreateCommand(@"
            SELECT COUNT(id)
            FROM room
            WHERE id = $1 AND has_danger = true
            ");
        checkRoom.Parameters.AddWithValue(roomId);
        using var reader = checkRoom.ExecuteReader();

        if (reader.Read())
        {
            if (reader.GetInt32(0) != 0)
            {
                message = $"You found a dangerous object in this room, be careful!{eventMessage}";
            }
            else
            {
                message = $"This room is safe, no dangers to be found.{eventMessage}";
            }
        }
        reader.Close();
        response.StatusCode = (int)HttpStatusCode.OK;
        return message;
    }
    public static string Windows(NpgsqlDataSource db, HttpListenerRequest request, HttpListenerResponse response)
    {
        int roomId = PlayerPosition(db, request, response);
        bool hasDanger = Check.RoomHasDanger(db, request, response);
        string message = string.Empty;
        string eventMessage = GameEvent.RandomTrigger(db);

        if (!hasDanger)
        {
            var windows = db.CreateCommand(@"
                        SELECT name, is_locked
                        FROM entry_point
                        WHERE room_id = $1 AND type = 'Window';
                        ");
            windows.Parameters.AddWithValue(roomId);
            using var reader2 = windows.ExecuteReader();

            while (reader2.Read())
            {
                switch (reader2.GetBoolean(1))
                {
                    case true:
                        message += $"Window {reader2.GetString(0)} is locked.\n";
                        break;
                    case false:
                        message += $"Window {reader2.GetString(0)} is unlocked.\n";
                        break;
                }
            }

            reader2.Close();
            message += eventMessage;

            response.StatusCode = (int)HttpStatusCode.OK;
            return message;
        }
        else
        {
            response.StatusCode = (int)HttpStatusCode.OK;
            message = "You forgot to clear the room of dangers and you are now dead.";
            return message;
        }

    }
    public static string Doors(NpgsqlDataSource db, HttpListenerRequest request, HttpListenerResponse response)
    {
        int roomId = PlayerPosition(db, request, response);
        string message = string.Empty;
        bool hasDanger = Check.RoomHasDanger(db, request, response);
        string eventMessage = GameEvent.RandomTrigger(db);

        if (!hasDanger)
        {
            var windows = db.CreateCommand(@"
                        SELECT name, is_locked
                        FROM entry_point
                        WHERE room_id = $1 AND type = 'Door';
                        ");
            windows.Parameters.AddWithValue(roomId);
            using var reader2 = windows.ExecuteReader();

            while (reader2.Read())
            {
                switch (reader2.GetBoolean(1))
                {
                    case true:
                        message += $"Door {reader2.GetString(0)} is locked.\n";
                        break;
                    case false:
                        message += $"Door {reader2.GetString(0)} is unlocked.\n";
                        break;
                }
            }
            message += eventMessage;
            response.StatusCode = (int)HttpStatusCode.OK;
            reader2.Close();
            return message;
        }
        else
        {
            GameEvent.RandomTrigger(db);
            response.StatusCode = (int)HttpStatusCode.OK;
            message = "You forgot to clear the room of dangers and you are now dead.";
            return message;
        }
    }

    public static string EntryPoints(NpgsqlDataSource db, HttpListenerResponse response, string playerName)
    {
        string message = string.Empty;

        var playerPos = db.CreateCommand(@"
            SELECT p.location, r.name
            FROM public.player p
            JOIN public.room r ON r.id = p.location
            WHERE p.name = $1;
            ");

        playerPos.Parameters.AddWithValue(playerName ?? string.Empty);
        using var reader1 = playerPos.ExecuteReader();

        int roomId = 0;
        string roomName = string.Empty;
        if (reader1.Read())
        {
            roomId = reader1.GetInt32(0);
            roomName = reader1.GetString(1);
        }

        int doors = CountEntries(db, roomId, "Door");
        int windows = CountEntries(db, roomId, "Window");
        response.StatusCode = (int)HttpStatusCode.OK;

        if (roomId == 1)
        {
            message = $"You are in the {roomName}. \nThere is {doors} door(s) and {windows} window(s).\nYou also see a whiteboard with a marker on the fridge door.";
        }
        else
        {
            message = $"You are in the {roomName}. \nThere is {doors} door(s) and {windows} window(s).";
        }

        reader1.Close();
        return message;
    }

    private static int CountEntries(NpgsqlDataSource db, int roomId, string type)
    {
        var entryPoints = db.CreateCommand(@"
            SELECT COUNT(type) 
            FROM entry_point
            WHERE room_id = $1 AND type = $2;
            ");
        entryPoints.Parameters.AddWithValue(roomId);
        entryPoints.Parameters.AddWithValue(type);
        using var reader2 = entryPoints.ExecuteReader();

        int count = 0;
        while (reader2.Read())
        {
            count = reader2.GetInt32(0);
        }

        reader2.Close();
        return count;
    }
    public static int PlayerPosition(NpgsqlDataSource db, HttpListenerRequest request, HttpListenerResponse response)
    {
        var playerPos = db.CreateCommand(@"
                        SELECT location
                        FROM public.player
                        WHERE name = $1
                        ");
        playerPos.Parameters.AddWithValue(Check.VerifyPlayer(db, request));

        using var reader = playerPos.ExecuteReader();
        int roomId = 0;

        if (reader.Read())
        {
            roomId = reader.GetInt32(0);
        }

        reader.Close();
        return roomId;
    }
    public static bool IfGameOver(NpgsqlDataSource db, HttpListenerRequest request, HttpListenerResponse response)
    {
        var cmd = db.CreateCommand(@"
      SELECT COUNT(*) 
      FROM public.player
      WHERE is_dead = true
        ");

        var cmd2 = db.CreateCommand(@"
        SELECT COUNT(*) 
        FROM public.player;
        ");


        using var reader = cmd.ExecuteReader();
        int deadPlayer = 0;
        int totalPlayers = 0;


        if (reader.Read())
        {
            deadPlayer = reader.GetInt32(0);
        }

        using var reader2 = cmd2.ExecuteReader();
        if (reader2.Read())
        {
            totalPlayers = reader2.GetInt32(0);
        }

        reader.Close();
        reader2.Close();

        response.StatusCode = (int)HttpStatusCode.OK;
        if (deadPlayer == totalPlayers && totalPlayers != 0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    public static bool RoomHasDanger(NpgsqlDataSource db, HttpListenerRequest request, HttpListenerResponse response)
    {
        var cmd = db.CreateCommand(@"
        SELECT has_danger
        FROM public.room
        WHERE id = $1;
    ");
        cmd.Parameters.AddWithValue(Check.PlayerPosition(db, request, response));

        using var reader = cmd.ExecuteReader();

        bool hasDanger = false;
        if (reader.Read())
        {
            hasDanger = reader.GetBoolean(0);
        }
        reader.Close();

        if (hasDanger)
        {
            var killPlayer = db.CreateCommand(@"
                UPDATE public.player
                SET is_dead = true
                WHERE name = $1
                ");
            killPlayer.Parameters.AddWithValue(Check.VerifyPlayer(db, request));
            killPlayer.ExecuteNonQuery();
        }
        return hasDanger;
    }

    public static string VerifyPlayer(NpgsqlDataSource db, HttpListenerRequest request)
    {
        string? path = request.Url?.AbsolutePath;
        string? name = path?.Split('/')[1] ?? string.Empty;
        string username = string.Empty;

        var cmd = db.CreateCommand(@"
            SELECT (name)
            FROM public.player
            WHERE name = $1
            ");
        cmd.Parameters.AddWithValue(name ?? string.Empty);
        using var reader = cmd.ExecuteReader();

        if (reader.Read())
        {
            username = reader.GetString(0);
        }

        reader.Close();
        return username;
    }

    public static bool AllPlayersReady(NpgsqlDataSource db, HttpListenerResponse response)
    {
        using var cmd = db.CreateCommand(@"
        SELECT COUNT(*)
        FROM public.player
        ");

        using var reader1 = cmd.ExecuteReader();

        int totalPlayers = 0;
        if (reader1.Read())
        {
            totalPlayers = reader1.GetInt32(0);
        }

        reader1.Close();

        var cmd1 = db.CreateCommand(@"
        SELECT COUNT(*) 
        FROM public.player 
        WHERE is_ready = true
        ");

        using var reader2 = cmd1.ExecuteReader();

        int readyPlayers = 0;
        if (reader2.Read())
        {
            readyPlayers = reader2.GetInt32(0);
        }
        response.StatusCode = (int)HttpStatusCode.OK;

        reader2.Close();
        if (totalPlayers == readyPlayers)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public static bool IfDead(NpgsqlDataSource db, string playerName)
    {
        var cmd = db.CreateCommand(@"
        SELECT name, is_dead
        FROM public.player
        WHERE name = $1 AND is_dead = true;
        ");
        cmd.Parameters.AddWithValue(playerName);

        using var reader = cmd.ExecuteReader();
        bool playerDeath = false;

        if (reader.Read())
        {
            playerDeath = reader.GetBoolean(1);
        }
        reader.Close();
        return playerDeath;
    }
    public static bool EntryPointTimer(NpgsqlDataSource db)
    {
        bool gameOver = false;
        var cmd = db.CreateCommand(@"
        SELECT to_char(""time"", 'HH24:MI:SS')
        FROM public.entry_point
        WHERE time is not null;
        ");

        using var reader = cmd.ExecuteReader();
        TimeOnly currentTime = TimeOnly.FromDateTime(DateTime.Now);
        String sessionStart = string.Empty;

        while (reader.Read())
        {
            sessionStart = reader.GetString(0);
            var split = sessionStart.Split(":");

            TimeOnly startTime = new(int.Parse(split[0]), int.Parse(split[1]), int.Parse(split[2]));

            TimeSpan timeElapsed = currentTime - startTime;
            if ((timeElapsed.TotalSeconds > 240))
            {
                gameOver = true;
                break;
            }
            else
            {
                gameOver = false;
            }
        }
        reader.Close();
        return gameOver;
    }
}