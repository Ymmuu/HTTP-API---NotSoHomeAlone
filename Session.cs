using Npgsql;

namespace real_time_horror_group3;

public class Session()
{
    public static void Start(NpgsqlDataSource db)
    {
        var select = db.CreateCommand(@"
                        SELECT COUNT(id)
                        FROM public.session
                        ");

        using var reader = select.ExecuteReader();
        int count = 1;
        if (reader.Read())
        {
            count = reader.GetInt32(0);
        }
        reader.Close();
        if (count is 0)
        {
            var insert = db.CreateCommand(@"
                            INSERT INTO public.session(
	                        time)
	                        VALUES (
                            CURRENT_TIMESTAMP);
                            UPDATE entry_point
                            SET time = CURRENT_TIMESTAMP;
                            ");
            insert.ExecuteNonQuery();
        }
    }

    public static TimeSpan ElapsedTime(NpgsqlDataSource db)
    {
        var sessionStart = db.CreateCommand(@"
            SELECT to_char(""time"", 'HH24:MI:SS')
            FROM public.session 
            WHERE time is not null;
            ");
        using var reader = sessionStart.ExecuteReader();

        TimeOnly currentTime = TimeOnly.FromDateTime(DateTime.Now);
        TimeOnly startTime = currentTime;
        while (reader.Read())
        {
            string[] time = reader.GetString(0).Split(":");
            startTime = new(int.Parse(time[0]), int.Parse(time[1]), int.Parse(time[2]));
        }

        reader.Close();
        TimeSpan interval = currentTime - startTime;
        return interval;
    }
    public static string FormattedTime(NpgsqlDataSource db)
    {
        TimeSpan elapsedTime = ElapsedTime(db);
        return elapsedTime.ToString(@"hh\:mm\:ss");
    }

    public static async void ResetDBForNewSession(NpgsqlDataSource db)
    {

        Database database = new(db);
        await database.Create();
    }
}