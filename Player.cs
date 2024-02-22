using Npgsql;
using System.Net;
namespace real_time_horror_group3;

public class Player()
{
    public static string Create(NpgsqlDataSource db, HttpListenerRequest request, HttpListenerResponse response)
    {
        StreamReader reader = new(request.InputStream, request.ContentEncoding);
        string checkIfNameExists = reader.ReadToEnd();
        
        var selectChoice = db.CreateCommand(@$"
            SELECT COUNT(*)
            FROM public.player
            WHERE name = $1
            ");
        selectChoice.Parameters.AddWithValue(checkIfNameExists);

        using var checkName = selectChoice.ExecuteReader();

        int validChoice = 0;
        if (checkName.Read())
        {
            validChoice = checkName.GetInt32(0);
        }
        reader.Close();

        if (validChoice > 0)
        {
            return "Player with this name already exists.";
        }

        string name = checkIfNameExists.ToLower();

        var cmd = db.CreateCommand(@"
                    INSERT INTO public.player
                    (name, location)
                    VALUES($1, 1);
                    ");
        cmd.Parameters.AddWithValue(name);
        cmd.ExecuteNonQuery();

        string message = $"Player '{name}' has been created. Type /ready when you are ready. Game can only start when all players are ready.\nAnd you will start in 'Kitchen'";
        response.StatusCode = (int)HttpStatusCode.Created;

        return message;
    }

    public static string Ready(NpgsqlDataSource db, HttpListenerRequest request, HttpListenerResponse response)
    {
        string playerName = Check.VerifyPlayer(db, request);

        var cmd = db.CreateCommand(@"
        UPDATE public.player
        SET is_ready = true
        WHERE name = $1;
    ");
        cmd.Parameters.AddWithValue(playerName);

        cmd.ExecuteNonQuery();
        response.StatusCode = (int)HttpStatusCode.OK;

        string message = $"{playerName}, you are ready!";
        return message;
    }


    public static string Move(NpgsqlDataSource db, HttpListenerRequest request, HttpListenerResponse response)

    {
        string message;
        StreamReader reader = new(request.InputStream, request.ContentEncoding);
        string roomName = reader.ReadToEnd();
        int roomId = 0;

        switch (roomName.ToLower())
        {
            case "kitchen":
                roomId = 1;
                break;
            case "hallway":
                roomId = 2;
                break;
            case "living room":
                roomId = 3;
                break;
        }

        if (roomId != 0)
        {
            string playerName = Check.VerifyPlayer(db, request);

            bool hasDanger = Check.RoomHasDanger(db, request, response);

            var cmd = db.CreateCommand(@"
                        UPDATE public.player
                        SET location = $1
                        WHERE name = $2;
                        ");
            cmd.Parameters.AddWithValue(roomId);
            cmd.Parameters.AddWithValue(playerName);

            cmd.ExecuteNonQuery();

            string eventMessage = GameEvent.RandomTrigger(db);
            
            if (hasDanger)
            {
                response.StatusCode = (int)HttpStatusCode.OK;
                message = "You forgot to check for dangers, you are now dead!";
                return message;
            }
            else
            {
                response.StatusCode = (int)HttpStatusCode.OK;
                message = $"{Check.EntryPoints(db, response, playerName)}{eventMessage}";
                return message;
            }
        }
        else
        {
            response.StatusCode = (int)HttpStatusCode.BadRequest;
            message = "Invalid room name";
            return message;
        }
    }

    public static string Lock(NpgsqlDataSource db, string type, HttpListenerRequest request, HttpListenerResponse response)
    {
        string message;
        StreamReader reader = new(request.InputStream, request.ContentEncoding);
        string lockName = reader.ReadToEnd();

        var selectChoice = db.CreateCommand(@$"
            SELECT COUNT(*) 
            FROM public.entry_point     
            WHERE name = $1
            ");
        selectChoice.Parameters.AddWithValue(lockName);
        selectChoice.ExecuteNonQuery();

        using var reader1 = selectChoice.ExecuteReader();

        int validChoice = 0;
        if (reader1.Read())
        {
            validChoice = reader1.GetInt32(0);
        }

        reader1.Close();

        bool hasDanger = Check.RoomHasDanger(db, request, response);
        if (validChoice > 0)
        {
            if (!hasDanger)
            {
                var cmd = db.CreateCommand(@$"

                UPDATE entry_point
                SET is_locked = true, ""time"" = null
                WHERE name = $1 AND room_id = $2 AND type = $3;");

                cmd.Parameters.AddWithValue(lockName);
                cmd.Parameters.AddWithValue(Check.PlayerPosition(db, request, response));
                cmd.Parameters.AddWithValue(type);
                cmd.ExecuteNonQuery();

                string eventMessage = GameEvent.RandomTrigger(db);
                message = $"{type} {lockName} is now locked{eventMessage}";
                response.StatusCode = (int)HttpStatusCode.OK;
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
        else
        {
            message = "Not a valid choice";
            return message;
        }

    }
    public static string RemoveDanger(NpgsqlDataSource db, HttpListenerRequest request, HttpListenerResponse response)
    {
        int roomId = Check.PlayerPosition(db, request, response);

        NpgsqlCommand removeDanger = db.CreateCommand(@"
            UPDATE public.room
            SET has_danger = false
            WHERE id = $1;
            ");
        removeDanger.Parameters.AddWithValue(roomId);
        string eventMessage = GameEvent.RandomTrigger(db);
        removeDanger.ExecuteNonQuery();

        string message = $"You cleared the room of dangerous objects, it's safe now.{eventMessage}";
        return message;
    }

    public static string ReadWhiteboard(NpgsqlDataSource db, HttpListenerRequest request, HttpListenerResponse response)
    {
        string message = string.Empty;
        if (!Check.RoomHasDanger(db, request, response))
        {
            if (Check.PlayerPosition(db, request, response) == 1)
            {
                var readWhiteboard = db.CreateCommand(@"
                SELECT *
                FROM public.whiteboard
                ORDER BY id DESC LIMIT 5
                ");
                using var reader = readWhiteboard.ExecuteReader();
                while (reader.Read())
                {
                    message = $"{reader.GetString(1)}\n\n{message}";
                }

                message = $"Messages on whiteboard:\n\n\n{message}{GameEvent.RandomTrigger(db)}";
                response.StatusCode = (int)HttpStatusCode.OK;
                return message;
            }
            else
            {
                message = $"The whiteboard is not here, it's in the kitchen.{GameEvent.RandomTrigger(db)}";
                response.StatusCode = (int)HttpStatusCode.OK;
                return message;
            }
        }
        else
        {
            response.StatusCode = (int)HttpStatusCode.OK;
            message = "You forgot to clear the room of dangers and you are now dead.";
            return message;
        }
    }

    public static string WriteOnWhiteBoard(NpgsqlDataSource db, HttpListenerRequest request, HttpListenerResponse response)
    {
        string message = string.Empty;
        if (!Check.RoomHasDanger(db, request, response))
        {
            if (Check.PlayerPosition(db, request, response) == 1)
            {
                StreamReader reader = new(request.InputStream, request.ContentEncoding);
                string post = reader.ReadToEnd();

                var postMessage = db.CreateCommand(@"
                INSERT INTO public.whiteboard(message)
                VALUES($1);
                ");
                postMessage.Parameters.AddWithValue(post);
                postMessage.ExecuteNonQuery();

                message = $"'{post}' added to the whiteboard.{GameEvent.RandomTrigger(db)}";
                response.StatusCode = (int)HttpStatusCode.Created;
                return message;
            }
            else
            {
                message = $"The whiteboard is not here, it's in the kitchen.{GameEvent.RandomTrigger(db)}";
                response.StatusCode = (int)HttpStatusCode.OK;
                return message;
            }
        }
        else
        {
            response.StatusCode = (int)HttpStatusCode.OK;
            message = "You forgot to clear the room of dangers and you are now dead.";
            return message;
        }
    }
}
