using Npgsql;
namespace real_time_horror_group3;

public class Database(NpgsqlDataSource db)
{
    public async Task Create()
    {
        await using var cmd = db.CreateCommand(@"
ALTER DATABASE notsohomealone
SET TIMEZONE TO +01;
DROP TABLE IF EXISTS public.room, public.entry_point, public.player, public.session, public.whiteboard;
CREATE TABLE IF NOT EXISTS public.room
(
    id serial,
    name text NOT NULL,
    has_danger boolean NOT NULL DEFAULT false,
    PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS public.entry_point
(
    id serial,
    name text NOT NULL,
    type text NOT NULL,
    is_locked boolean NOT NULL DEFAULT false,
    room_id integer NOT NULL,
    ""time"" timestamp with time zone,
    PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS public.player
(
    id serial,
    name text NOT NULL,
    location integer NOT NULL,
    is_dead boolean NOT NULL DEFAULT false,
    is_ready boolean NOT NULL DEFAULT false,
    PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS public.session
(
    id serial,
    ""time"" timestamp with time zone NOT NULL,
    PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS public.highscore
(
    id serial,
    player_names text NOT NULL,
    ""time"" text NOT NULL,
    PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS public.whiteboard
(
    id serial,
    message text,
    PRIMARY KEY (id)
);

ALTER TABLE IF EXISTS public.entry_point
    ADD CONSTRAINT room_id FOREIGN KEY (room_id)
    REFERENCES public.room (id);

ALTER TABLE IF EXISTS public.player
    ADD CONSTRAINT location FOREIGN KEY (location)
    REFERENCES public.room (id);

INSERT INTO public.room(
	name)
	VALUES 
	('Kitchen'),
	('Hallway'),
	('Living room');

INSERT INTO public.entry_point
(name, type, room_id)
VALUES
	('A', 'Door', 1),
	('A', 'Window', 1),
	('B', 'Window', 1),
	('A', 'Door', 2),
	('A', 'Window', 2),
	('B', 'Window', 2),
	('A', 'Door', 3),
	('A', 'Window', 3),
	('B', 'Window', 3);

");
        await cmd.ExecuteNonQueryAsync();
        Console.WriteLine("Created and populated database");
    }
}