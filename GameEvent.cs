using Npgsql;
using System.Net;

namespace real_time_horror_group3;
public class GameEvent()
{
    public static string SpawnDangerousObject(NpgsqlDataSource db)
    {
        Random random = new();
        int randomRoom = random.Next(1, 4);

        var spawnObject = db.CreateCommand(@"
            UPDATE public.room
            SET has_danger = true
            WHERE id = $1;
            ");
        spawnObject.Parameters.AddWithValue(randomRoom);
        spawnObject.ExecuteNonQuery();

        switch (randomRoom)
        {
            case 1:
                return "Kitchen";
            case 2:
                return "Hallway";
            case 3:
                return "Living room";
            default:
                return "Error finding room for spawning object";
        }
    }
    public static string UnlockEntry(NpgsqlDataSource db)
    {
        var entryCount = db.CreateCommand(@"
            SELECT COUNT(id)
            FROM public.entry_point;
            ");
        using var reader1 = entryCount.ExecuteReader();
        int totalEntry = 0;
        if (reader1.Read())
        {
            totalEntry = reader1.GetInt32(0);
        }

        reader1.Close();
        Random random = new Random();
        int randomEntry = random.Next(1, totalEntry);

        var lockEntry = db.CreateCommand(@"
            UPDATE public.entry_point
            SET is_locked = false, time = CURRENT_TIMESTAMP 
            WHERE id = $1 AND is_locked = true;
            ");
        lockEntry.Parameters.AddWithValue(randomEntry);
        lockEntry.ExecuteNonQuery();

        var getRoomId = db.CreateCommand(@"
            SELECT room_id
            FROM public.entry_point
            WHERE id = $1
            ");
        getRoomId.Parameters.AddWithValue(randomEntry);
        using var reader = getRoomId.ExecuteReader();

        int roomId = 0;
        if (reader.Read())
        {
            roomId = reader.GetInt32(0);
        }
        switch (roomId)
        {
            case 1:
                return "Kitchen";
            case 2:
                return "Hallway";
            case 3:
                return "Living room";
            default:
                return "Error for finding room for unlocking window";

        }
    }

    public static string RandomTrigger(NpgsqlDataSource db)
    {
        TimeSpan timeElapsed = Session.ElapsedTime(db);

        double baseProbability = 0.2;
        double exponentialRate = 0.1;
        double timeInterval = timeElapsed.TotalMinutes;

        double probability = baseProbability * Math.Exp(exponentialRate * timeInterval);

        Random random = new Random();
        double randomValue = random.NextDouble();

        if (randomValue <= probability)
        {
            int randomEvent = random.Next(1, 3);
            if (randomEvent == 1)
            {
                return $"\n\n{RandomEventMessage("unlock")}{GameEvent.UnlockEntry(db)}";
            }
            else
            {
                return $"\n\n{RandomEventMessage("object")}{GameEvent.SpawnDangerousObject(db)}";
            }
        }
        else
        {
            return string.Empty;
        }
    }

    public static string RandomEventMessage(string eventType)

    {
        List<string> entryMessages = new List<string>();
        entryMessages.Add("You hear a scraping sound from the ");
        entryMessages.Add("There's a loud bang coming from the ");
        entryMessages.Add("The sound of faint whispering echoes from the ");
        entryMessages.Add("A rhythmic tapping resonates from the ");
        entryMessages.Add("Something scratches at the ");
        entryMessages.Add("The faint jingle of keys outside the ");
        entryMessages.Add("Heavy breathing can be heard near the ");
        entryMessages.Add("Footsteps approach closer to the ");
        entryMessages.Add("A low, guttural growl emanates from the ");
        entryMessages.Add("The sound of fingernails dragging along the ");
        entryMessages.Add("The rustling of leaves masks the footsteps nearing the ");
        entryMessages.Add("A soft, eerie humming fills the air around the ");
        entryMessages.Add("Branches scrape against the windows of the ");
        entryMessages.Add("Shadows dance menacingly outside the ");
        entryMessages.Add("The faint sound of a child's laughter drifts from the ");
        entryMessages.Add("An unsettling scratching noise comes from the ");
        entryMessages.Add("A distant, mournful howl pierces the silence near the ");
        entryMessages.Add("The unsettling sound of chains rattling outside the ");
        entryMessages.Add("A cold breeze whistles through the cracks of the ");
        entryMessages.Add("The sound of something dragging across the ground grows louder outside the ");
        entryMessages.Add("Unearthly moans echo through the darkness, seeming to originate from the ");

        List<string> objectMessages = new List<string>();
        objectMessages.Add("The sound of glass shattering echoes from the ");
        objectMessages.Add("A sudden crash reverberates from the ");
        objectMessages.Add("The unmistakable click of a bear trap being set emanates from the ");
        objectMessages.Add("An eerie whisper seems to come from the darkest corner of the ");
        objectMessages.Add("The rustling of papers suggests movement inside the ");
        objectMessages.Add("A door slams shut with a forceful bang inside the ");
        objectMessages.Add("The faint sound of footsteps can be heard pacing within the ");
        objectMessages.Add("The muffled sound of stifled cries echoes from the ");
        objectMessages.Add("The rattling of chains emanates from the depths of the ");
        objectMessages.Add("A chilling laughter echoes from the hidden recesses of the ");
        objectMessages.Add("The unsettling sound of a child's toy activating echoes from the ");
        objectMessages.Add("A sinister melody plays from a music box within the ");
        objectMessages.Add("The creaking of floorboards suggests movement within the ");
        objectMessages.Add("An otherworldly glow emanates from the ");
        objectMessages.Add("The sound of a door unlocking sends shivers down your spine from the ");
        objectMessages.Add("The scratching of claws against wood can be heard within the ");
        objectMessages.Add("The flickering of lights casts eerie shadows within the ");
        objectMessages.Add("A sudden, chilling silence falls upon the ");
        objectMessages.Add("The sound of heavy breathing can be heard coming from the ");
        objectMessages.Add("Unsettling whispers seem to emanate from behind the walls of the ");
        objectMessages.Add("The unsettling sound of something dragging across the floor echoes from the ");
        objectMessages.Add("A bloodcurdling scream pierces the silence from within the ");
        objectMessages.Add("The eerie ticking of a clock echoes through the halls of the ");

        Random random = new Random();
        int randomMessage = 0;
        switch (eventType)
        {
            case "unlock":
                randomMessage = random.Next(0, entryMessages.Count);
                return entryMessages[randomMessage];
            case "object":
                randomMessage = random.Next(0, objectMessages.Count);
                return objectMessages[randomMessage];
            default:
                return "Error in choosing Event Message";
        }
    }
    public static void AddScore(NpgsqlDataSource db, HttpListenerRequest request, HttpListenerResponse response)
    {

        var selectName = db.CreateCommand(@"
        SELECT name 
        FROM public.player;
        ");

        using var reader = selectName.ExecuteReader();

        string playerNames = string.Empty;

        while (reader.Read())
        {
            playerNames += $"{reader.GetString(0)}, ";
        }
        playerNames = playerNames.Substring(0, playerNames.Length - 2);
        reader.Close();

        string time = Session.FormattedTime(db);
        var highscore = db.CreateCommand(@"
        INSERT INTO public.highscore(player_names, ""time"")
        VALUES ($1, $2);
    ");
        highscore.Parameters.AddWithValue(playerNames);
        highscore.Parameters.AddWithValue(time);
        highscore.ExecuteNonQuery();
    }
}