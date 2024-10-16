﻿#nullable disable
namespace DataBaseApp
{
    using System;
    using System.Data.SQLite;
    
    public class DataBase
    {
        private static DataBase _instance = null!;
        private SQLiteConnection _connection = null!;
        private static readonly string _defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MusicLibraryEditor", "database.db");

        // constructor for the program
        private DataBase(string dbPath) => Initialize(dbPath);

        // constructor for the test
        private DataBase() => Initialize(":memory:");

        //Initialize the database
        private void Initialize(string dbPath)
        {
            string directoryPath = Path.GetDirectoryName(dbPath);
            if (dbPath != ":memory:" && directoryPath != null && !Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
                Console.WriteLine($"Directory '{directoryPath}' created.");
            }

            bool dbExists = File.Exists(dbPath);
            string connectionString = $"Data Source={dbPath};Version=3;";

            _connection = new SQLiteConnection(connectionString);
            _connection.Open();
            Console.WriteLine("Database connection open");

            if (!dbExists) CreateTables();
        }

        //singleton
        public static DataBase Instance()
        {
            if (_instance == null) 
                _instance = new DataBase(_defaultPath);
            return _instance;
        }

        // Singleton for test
        public static DataBase TestInstance()
        {
            if (_instance == null)
                _instance = new DataBase();
            return _instance;
        }

        // Make tables if data base does not exist
        private void CreateTables()
        {
            string createTablesQuery = @"
                CREATE TABLE types (
                    id_type INTEGER PRIMARY KEY,
                    description TEXT
                );

                INSERT INTO types VALUES(0, 'Person');
                INSERT INTO types VALUES(1, 'Group');
                INSERT INTO types VALUES(2, 'Unknown');

                CREATE TABLE performers (
                    id_performer INTEGER PRIMARY KEY,
                    id_type INTEGER,
                    name TEXT,
                    FOREIGN KEY (id_type) REFERENCES types(id_type)
                );

                CREATE TABLE persons (
                    id_person INTEGER PRIMARY KEY,
                    stage_name TEXT,
                    real_name TEXT,
                    birth_date TEXT,
                    death_date TEXT
                );

                CREATE TABLE groups (
                    id_group INTEGER PRIMARY KEY,
                    name TEXT,
                    start_date TEXT,
                    end_date TEXT
                );

                CREATE TABLE in_group (
                    id_person INTEGER,
                    id_group INTEGER,
                    PRIMARY KEY (id_person, id_group),
                    FOREIGN KEY (id_person) REFERENCES persons(id_person),
                    FOREIGN KEY (id_group) REFERENCES groups(id_group)
                );

                CREATE TABLE albums (
                    id_album INTEGER PRIMARY KEY,
                    path TEXT,
                    name TEXT,
                    year INTEGER
                );

                CREATE TABLE rolas (
                    id_rola INTEGER PRIMARY KEY,
                    id_performer INTEGER,
                    id_album INTEGER,
                    path TEXT,
                    title TEXT,
                    track INTEGER,
                    year INTEGER,
                    genre TEXT,
                    FOREIGN KEY (id_performer) REFERENCES performers(id_performer),
                    FOREIGN KEY (id_album) REFERENCES albums(id_album)
                );
            ";
            using (var command = new SQLiteCommand(createTablesQuery, _connection))
                command.ExecuteNonQuery();
            Console.WriteLine("Data base created successfully");
        }

        //close connection
        public void Disconnect()
        {
            if (_connection != null && _connection.State == System.Data.ConnectionState.Open)
            {
                _connection.Close();
                Console.WriteLine("Data base connection closed");
            }
        }

        //check if the database is empty
        public bool IsRolasTableEmpty()
        {
            string query = "SELECT COUNT(*) FROM rolas";
            using (SQLiteCommand command = new SQLiteCommand(query, _connection))
            {
                int count = Convert.ToInt32(command.ExecuteScalar());
                return count == 0;
            }
        }

        // add a performer in the database
        public bool InsertPerformer(Performer performer)
        {
            bool isAdded = false;
            Performer existingPerformer = GetPerformerByName(performer.GetName());
            if (existingPerformer != null)
            {
                Console.WriteLine($"Performer '{performer.GetName()}' already exists with ID: {existingPerformer.GetIdPerformer()}");
                return isAdded;
            }
            string query = "INSERT INTO performers (id_type, name) VALUES (@id_type, @name)";
            using (SQLiteCommand command = new SQLiteCommand(query, _connection))
            {
                command.Parameters.AddWithValue("@id_type", performer.GetIdType());
                command.Parameters.AddWithValue("@name", performer.GetName());
                int rowsAffected = command.ExecuteNonQuery();
                isAdded = rowsAffected > 0;
                if (isAdded)
                {
                    performer.SetIdPerformer((int)_connection.LastInsertRowId);
                    Console.WriteLine($"Performer '{performer.GetName()}' added with ID: {performer.GetIdPerformer()}");
                }
                else 
                    Console.WriteLine("Performer not added");
            }
            return isAdded;
        }

        // add a rola in the database
        public bool InsertRola(Rola rola)
        {   
            bool isAdded = false;
            Rola existingRola = GetRolaByTitleAndPath(rola.GetTitle(), rola.GetPath());
            if (existingRola != null && existingRola.GetPath() == rola.GetPath())
            {
                Console.WriteLine($"Rola '{rola.GetTitle()}' already exists with ID: {existingRola.GetIdRola()}");
                return isAdded;
            }
            string query = "INSERT INTO rolas (id_performer, id_album, path, title, track, year, genre) " +
                              "VALUES (@id_performer, @id_album, @path, @title, @track, @year, @genre)";
            using (SQLiteCommand command = new SQLiteCommand(query, _connection))
            {
                command.Parameters.AddWithValue("@id_performer", rola.GetIdPerformer());
                command.Parameters.AddWithValue("@id_album", rola.GetIdAlbum());
                command.Parameters.AddWithValue("@path", rola.GetPath());
                command.Parameters.AddWithValue("@title", rola.GetTitle());
                command.Parameters.AddWithValue("@track", rola.GetTrack());
                command.Parameters.AddWithValue("@year", rola.GetYear());
                command.Parameters.AddWithValue("@genre", rola.GetGenre());
                int rowsAffected = command.ExecuteNonQuery();
                isAdded = rowsAffected > 0;
                if (isAdded)
                {
                    rola.SetIdRola((int)_connection.LastInsertRowId);
                    Console.WriteLine($"Rola '{rola.GetTitle()}' added with ID: {rola.GetIdRola()}");
                }
                else 
                    Console.WriteLine("Rola not added");
            }
            return isAdded;
        }
        
        // add a person in the database
        public bool InsertPerson(Person person)
        {
            bool isAdded = false;
            Person existingPerson = GetPersonByStageName(person.GetStageName());
            if (existingPerson != null)
            {
                Console.WriteLine($"Person '{person.GetStageName()}' already exists with ID: {existingPerson.GetIdPerson()}");
                return isAdded;
            }
            string query = "INSERT INTO persons (stage_name, real_name, birth_date, death_date) VALUES (@stage_name, @real_name, @birth_date, @death_date)";
            using (SQLiteCommand command = new SQLiteCommand(query, _connection))
            {
                command.Parameters.AddWithValue("@stage_name", person.GetStageName());
                command.Parameters.AddWithValue("@real_name", person.GetRealName());
                command.Parameters.AddWithValue("@birth_date", person.GetBirthDate());
                command.Parameters.AddWithValue("@death_date", person.GetDeathDate());
                int rowsAffected = command.ExecuteNonQuery();
                isAdded = rowsAffected > 0;
                if (isAdded)
                {
                    person.SetIdPerson((int)_connection.LastInsertRowId);
                    Console.WriteLine($"Person '{person.GetStageName()}' added with ID: {person.GetIdPerson()}");
                }
                else 
                    Console.WriteLine("Person not added");
            }
            return isAdded;
        }

        // add a group in the database
        public bool InsertGroup(Group group)
        {
            bool isAdded = false;
            Group existingGroup = GetGroupByName(group.GetName());
            if (existingGroup != null) 
            {
                Console.WriteLine($"Group '{group.GetName()}' already exists with ID: {existingGroup.GetIdGroup()}");
                return isAdded;
            }
            string query = "INSERT INTO groups (name, start_date, end_date) " +
                            "VALUES (@name, @start_date, @end_date)";
            using (SQLiteCommand command = new SQLiteCommand(query, _connection))
            {
                command.Parameters.AddWithValue("@name", group.GetName());
                command.Parameters.AddWithValue("@start_date", group.GetStartDate());
                command.Parameters.AddWithValue("@end_date", group.GetEndDate());
                int rowsAffected = command.ExecuteNonQuery();
                isAdded = rowsAffected > 0;
                if (isAdded)
                {
                    group.SetIdGroup((int)_connection.LastInsertRowId);
                    Console.WriteLine($"Group '{group.GetName()}' added with ID: {group.GetIdGroup()}");
                }
                else 
                    Console.WriteLine("Group not added");
            }
            return isAdded;
        }

        // add an album in the database
        public bool InsertAlbum(Album album)
        {
            bool isAdded = false;            
            Album existingAlbum = GetAlbumByNameAndPath(album.GetName(), album.GetPath());
            if (existingAlbum != null && existingAlbum.GetPath() == album.GetPath())
            {
                Console.WriteLine($"Album '{album.GetName()}' already exists with ID: {existingAlbum.GetIdAlbum()}");
                return isAdded;
            }
            string query = "INSERT INTO albums (path, name, year) VALUES (@path, @name, @year)";
            using (SQLiteCommand command = new SQLiteCommand(query, _connection))
            {
                command.Parameters.AddWithValue("@path", album.GetPath());
                command.Parameters.AddWithValue("@name", album.GetName());
                command.Parameters.AddWithValue("@year", album.GetYear());
                int rowsAffected = command.ExecuteNonQuery();
                isAdded = rowsAffected > 0;
                if (isAdded)
                {
                    album.SetIdAlbum((int)_connection.LastInsertRowId);
                    Console.WriteLine($"Album '{album.GetName()}' added with ID: {album.GetIdAlbum()}");
                }
                else 
                    Console.WriteLine("Album not added");
            }
            return isAdded;
        }

        // get performer by name
        public Performer GetPerformerByName(string name)
        {
            Performer performer = null;
            string query = "SELECT * FROM performers WHERE name = @name";
            using (SQLiteCommand command = new SQLiteCommand(query, _connection))
            {
                command.Parameters.AddWithValue("@name", name);
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        int idPerformer = reader.GetInt32(0);
                        int idType = reader.GetInt32(1);
                        performer = new Performer(idPerformer, name, idType);
                    }
                }
            }
            return performer;
        }

        // get rola by name and path
        public Rola GetRolaByTitleAndPath(string title, string path)
        {
            Rola rola = null;
            string query = "SELECT * FROM rolas WHERE title = @title AND path = @path";
            using (SQLiteCommand command = new SQLiteCommand(query, _connection))
            {
                command.Parameters.AddWithValue("@title", title);
                command.Parameters.AddWithValue("@path", path);
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        int idRola = reader.GetInt32(0);
                        int idPerformer = reader.GetInt32(1);
                        int idAlbum = reader.GetInt32(2);
                        string rolaPath = reader.GetString(3);
                        string rolaTitle = reader.GetString(4);
                        int track = reader.GetInt32(5);
                        int year = reader.GetInt32(6);
                        string genre = reader.GetString(7);
                        rola = new Rola(idRola, idPerformer, idAlbum, rolaPath, rolaTitle, track, year, genre);
                    }
                }
            }
            return rola;
        }

        // get person by stage name
        public Person GetPersonByStageName(string stage_name)
        {
            Person person  = null;
            string query = "SELECT * FROM persons WHERE stage_name = @stage_name";
            using (SQLiteCommand command = new SQLiteCommand(query, _connection))
            {
                command.Parameters.AddWithValue("@stage_name", stage_name);
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        int idPerson = reader.GetInt32(0);
                        string stageName = reader.GetString(1);
                        string realName = reader.GetString(2);
                        string birthDate = reader.GetString(3);
                        string deathDate = reader.GetString(4);
                        person = new Person(idPerson, stageName, realName, birthDate, deathDate);
                    }
                }
            }
            return person;
        }

        // get group by name
        public Group GetGroupByName(string name)
        {
            Group group = null;
            string query = "SELECT * FROM groups WHERE name = @name";
            using (SQLiteCommand command = new SQLiteCommand(query, _connection))
            {
                command.Parameters.AddWithValue("@name", name);
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        int idGroup = reader.GetInt32(0);
                        string groupName = reader.GetString(1);
                        string startDate = reader.GetString(2);
                        string endDate = reader.GetString(3);
                        group = new Group(idGroup, groupName, startDate, endDate);
                    }
                }
            }
            return group;
        }

        // get album by name and path
        public Album GetAlbumByNameAndPath(string name, string path)
        {
            Album album = null;
            string query = "SELECT * FROM albums WHERE name = @name AND path = @path";
            using (SQLiteCommand command = new SQLiteCommand(query, _connection))
            {
                command.Parameters.AddWithValue("@name", name);
                command.Parameters.AddWithValue("@path", path);
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        int idAlbum = reader.GetInt32(0);
                        string albumPath = reader.GetString(1);
                        string albumName = reader.GetString(2);
                        int year = reader.GetInt32(3);
                        album = new Album(idAlbum, albumPath, albumName, year);
                    }
                }
            }
            return album;
        }

        // update performer
        public bool UpdatePerformer (Performer performer)
        {
            bool isUpdated = false;
            string query = "UPDATE performers SET id_type = @id_type, name = @name WHERE id_performer = @id_performer";
            using (SQLiteCommand command = new SQLiteCommand(query, _connection))
            {
                command.Parameters.AddWithValue("@id_type", performer.GetIdType());
                command.Parameters.AddWithValue("@name", performer.GetName());
                command.Parameters.AddWithValue("@id_performer", performer.GetIdPerformer());
                int rowsAffected = command.ExecuteNonQuery();
                isUpdated = rowsAffected > 0;
                if(isUpdated) Console.WriteLine("Performer updated successfully.");
                else Console.WriteLine("Performer not updated.");
            }
            return isUpdated;
        }

        // update rola
        public bool UpdateRola(Rola rola)
        {
            bool isUpdated = false;
            string query = "UPDATE rolas SET title = @title, id_performer = @id_performer, id_album = @id_album, " +
                            "track = @track, year = @year, genre = @genre WHERE id_rola = @id_rola";
            using (SQLiteCommand command = new SQLiteCommand(query, _connection))
            {
                command.Parameters.AddWithValue("@title", rola.GetTitle());
                command.Parameters.AddWithValue("@id_performer", rola.GetIdPerformer());
                command.Parameters.AddWithValue("@id_album", rola.GetIdAlbum());
                command.Parameters.AddWithValue("@track", rola.GetTrack());
                command.Parameters.AddWithValue("@year", rola.GetYear());
                command.Parameters.AddWithValue("@genre", rola.GetGenre());
                command.Parameters.AddWithValue("@id_rola", rola.GetIdRola());
                int rowsAffected = command.ExecuteNonQuery();
                isUpdated = rowsAffected > 0;
                if(isUpdated) 
                    Console.WriteLine("Rola updated successfully.");
                else 
                    Console.WriteLine("Rola not updated.");
            }
            return isUpdated;
        }

        // update person
        public bool UpdatePerson(Person person)
        {
            bool isUpdated = false;
            string query = "UPDATE persons SET stage_name = @stage_name, real_name = @real_name, birth_date = @birth_date, death_date = @death_date WHERE id_person = @id_person";
            
            using (SQLiteCommand command = new SQLiteCommand(query, _connection))
            {
                command.Parameters.AddWithValue("@stage_name", person.GetStageName());
                command.Parameters.AddWithValue("@real_name", person.GetRealName());
                command.Parameters.AddWithValue("@birth_date", person.GetBirthDate());
                command.Parameters.AddWithValue("@death_date", person.GetDeathDate());
                command.Parameters.AddWithValue("@id_person", person.GetIdPerson());

                int rowsAffected = command.ExecuteNonQuery();
                isUpdated = rowsAffected > 0;

                if (isUpdated) 
                    Console.WriteLine("Person updated successfully.");
                else 
                    Console.WriteLine("Person not updated.");
            }

            return isUpdated;
        }

        //update album
        public bool UpdateGroup(Group group)
        {
            bool isUpdated = false;
            string query = "UPDATE groups SET name = @name, start_date = @start_date, end_date = @end_date WHERE id_group = @id_group";
            
            using (SQLiteCommand command = new SQLiteCommand(query, _connection))
            {
                command.Parameters.AddWithValue("@name", group.GetName());
                command.Parameters.AddWithValue("@start_date", group.GetStartDate());
                command.Parameters.AddWithValue("@end_date", group.GetEndDate());
                command.Parameters.AddWithValue("@id_group", group.GetIdGroup());

                int rowsAffected = command.ExecuteNonQuery();
                isUpdated = rowsAffected > 0;

                if (isUpdated) 
                    Console.WriteLine("Group updated successfully.");
                else 
                    Console.WriteLine("Group not updated.");
            }

            return isUpdated;
        }

        // update album
        public bool UpdateAlbum(Album album)
        {
            bool isUpdated = false;
            string query = "UPDATE albums SET name = @name, year = @year WHERE id_album = @id_album";
            using (SQLiteCommand command = new SQLiteCommand(query, _connection))
            {
                command.Parameters.AddWithValue("@name", album.GetName());
                command.Parameters.AddWithValue("@year", album.GetYear());
                command.Parameters.AddWithValue("@id_album", album.GetIdAlbum());
                int rowsAffected = command.ExecuteNonQuery();
                isUpdated = rowsAffected > 0;
                if(isUpdated) 
                    Console.WriteLine("Album updated successfully.");
                else 
                    Console.WriteLine("Album not updated.");
            }
            string updateRolasQuery = "UPDATE rolas SET id_album = @id_album WHERE id_album = @id_album";
            using (SQLiteCommand command = new SQLiteCommand(updateRolasQuery, _connection))
            {
                command.Parameters.AddWithValue("@id_album", album.GetIdAlbum());
                int rowsAffected = command.ExecuteNonQuery();
                Console.WriteLine($"{rowsAffected} rolas updated with new album details.");
            }
            return isUpdated;
        }

        // get all performers from the performers table
        public List<Performer> GetAllPerformers()
        {
            List<Performer> performers = new List<Performer>();
            string query = "SELECT * FROM performers";
            
            using (SQLiteCommand command = new SQLiteCommand(query, _connection))
            {
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int idPerformer = reader.GetInt32(0);
                        int idType = reader.GetInt32(1);
                        string name = reader.GetString(2);
                        
                        Performer performer = new Performer(idPerformer, name, idType);
                        performers.Add(performer);
                    }
                }
            }
            
            return performers;
        }

        // get all rolas from the rolas table
        public List<Rola> GetAllRolas()
        {
            List<Rola> rolas = new List<Rola>();
            string query = "SELECT * FROM rolas";
            using(SQLiteCommand command = new SQLiteCommand(query, _connection))
            {
                using(SQLiteDataReader reader = command.ExecuteReader())
                {
                    while(reader.Read())
                    {
                        int idRola = reader.GetInt32(0);
                        int idPerformer = reader.GetInt32(1);
                        int idAlbum = reader.GetInt32(2);
                        string rolaPath = reader.GetString(3);
                        string rolaTitle = reader.GetString(4);
                        int track = reader.GetInt32(5);
                        int year = reader.GetInt32(6);
                        string genre = reader.GetString(7);
                        Rola rola = new Rola(idRola, idPerformer, idAlbum, rolaPath, rolaTitle, track, year, genre);
                        rolas.Add(rola);
                    }
                }
            }
            return rolas;
        }

        // get all persons from the person table
        public List<Person> GetAllPersons()
        {
            List<Person> persons = new List<Person>();
            string query = "SELECT * FROM persons";
            using (SQLiteCommand command = new SQLiteCommand(query, _connection))
            {
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int idPerson = reader.GetInt32(0);
                        string stageName = reader.GetString(1);
                        string realName = reader.GetString(2);
                        string birthDate = reader.GetString(3);
                        string deathDate = reader.GetString(4);
                        Person person = new Person(idPerson, stageName, realName, birthDate, deathDate);
                        persons.Add(person);
                    }
                }
            }
            return persons;
        }

        // get all groups from the groups table
        public List<Group> GetAllGroups()
        {
            List<Group> groups = new List<Group>();
            string query = "SELECT * FROM groups";
            using (SQLiteCommand command = new SQLiteCommand(query, _connection))
            {
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int idGroup = reader.GetInt32(0);
                        string groupName = reader.GetString(1);
                        string startDate = reader.GetString(2);
                        string endDate = reader.GetString(3);
                        Group group = new Group(idGroup, groupName, startDate, endDate);
                        groups.Add(group);
                    }
                }
            }
            return groups;
        }

        // get all albums from the albums table
        public List<Album> GetAllAlbums()
        {
            List<Album> albums = new List<Album>();
            string query = "SELECT * FROM albums";
            using (SQLiteCommand command = new SQLiteCommand(query, _connection))
            {
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int idAlbum = reader.GetInt32(0);
                        string path = reader.GetString(1);
                        string name = reader.GetString(2);
                        int year = reader.GetInt32(3);
                        
                        Album album = new Album(idAlbum, path, name, year);
                        albums.Add(album);
                    }
                }
            }
            return albums;
        }

        //delete a rola from the rolas table
        public bool DeleteRola(int idRola)
        {
            string query = "DELETE FROM rolas WHERE id_rola = @id_rola";
            using (SQLiteCommand command = new SQLiteCommand(query, _connection))
            {
                command.Parameters.AddWithValue("@id_rola", idRola);
                int rowsAffected = command.ExecuteNonQuery();
                return rowsAffected > 0;
            }
        }

        // insert a person in a group
        public bool AddInGroup(Person person, Group group)
        {
            if (CheckPersonInGroup(person, group))
            {
                Console.WriteLine("The person is already in the group.");
                return false;
            }
            string query = "INSERT INTO in_group (id_person, id_group) VALUES (@id_person, @id_group)";
            using (SQLiteCommand command = new SQLiteCommand(query, _connection))
            {
                command.Parameters.AddWithValue("@id_person", person.GetIdPerson());
                command.Parameters.AddWithValue("@id_group", group.GetIdGroup());
                int rowsAffected = command.ExecuteNonQuery();
                bool isInserted = rowsAffected > 0;
                if (isInserted) 
                    Console.WriteLine("Person successfully added to group.");
                else 
                    Console.WriteLine("Failed to add person to group.");
                return isInserted;
            }
        }
        
        // checks if a person is in a group
        public bool CheckPersonInGroup(Person person, Group group)
        {
            string checkQuery = "SELECT COUNT(*) FROM in_group WHERE id_person = @id_person AND id_group = @id_group";
            using (SQLiteCommand command = new SQLiteCommand(checkQuery, _connection))
            {
                command.Parameters.AddWithValue("@id_person", person.GetIdPerson());
                command.Parameters.AddWithValue("@id_group", group.GetIdGroup());
                long count = (long)command.ExecuteScalar();
                return count > 0;
            }
        }

        // get performer by query from compiler
        public List<Performer> GetPerformersByQuery(string sql, Dictionary<string, string> parameters)
        {
            List<Performer> performers = new List<Performer>();
            using (SQLiteCommand command = new SQLiteCommand(sql, _connection))
            {
                foreach (var param in parameters)
                {
                    command.Parameters.AddWithValue($"@{param.Key}", param.Value);
                }
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int idPerformer = reader.GetInt32(0);
                        string name = reader.GetString(2);
                        int idType = reader.GetInt32(1);
                        performers.Add(new Performer(idPerformer, name, idType));
                    }
                }
            }
            return performers;
        }

        // get rolas by query from the compiler
        public List<Rola> GetRolasByQuery(string sql, Dictionary<string, string> parameters)
        {
            List<Rola> rolas = new List<Rola>();
            using (SQLiteCommand command = new SQLiteCommand(sql, _connection))
            {
                foreach (var param in parameters)
                {
                    command.Parameters.AddWithValue($"@{param.Key}", param.Value);
                }
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int idRola = reader.GetInt32(0);
                        int idPerformer = reader.GetInt32(1);
                        int idAlbum = reader.GetInt32(2);
                        string rolaPath = reader.GetString(3);
                        string rolaTitle = reader.GetString(4);
                        int track = reader.GetInt32(5);
                        int year = reader.GetInt32(6);
                        string genre = reader.GetString(7);
                        Rola rola = new Rola(idRola, idPerformer, idAlbum, rolaPath, rolaTitle, track, year, genre);
                        rolas.Add(rola);
                    }
                }
            }
            return rolas;
        }

        // get albums by query from the compiler
        public List<Album> GetAlbumsByQuery(string sql, Dictionary<string, string> parameters)
        {
            List<Album> albums = new List<Album>();
            using (SQLiteCommand command = new SQLiteCommand(sql, _connection))
            {
                foreach (var param in parameters)
                {
                    command.Parameters.AddWithValue($"@{param.Key}", param.Value);
                }
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int idAlbum = reader.GetInt32(0);
                        string path = reader.GetString(1);
                        string name = reader.GetString(2);
                        int year = reader.GetInt32(3);
                        Album album = new Album(idAlbum, path, name, year);
                        albums.Add(album);
                    }
                }
            }
            return albums;
        }

        // get groups where a person belongs
        public List<Group> GetGroupsForPerson(Person person)
        {
            List<Group> groups = new List<Group>();
            string query = "SELECT g.* FROM in_group ig JOIN groups g ON ig.id_group = g.id_group WHERE ig.id_person = @id_person";
            using (SQLiteCommand command = new SQLiteCommand(query, _connection))
            {
                command.Parameters.AddWithValue("@id_person", person.GetIdPerson());
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Group group = new Group
                        (
                            reader["name"].ToString(),
                            reader["start_date"].ToString(),
                            reader["end_date"].ToString()
                        );
                        group.SetIdGroup(Convert.ToInt32(reader["id_group"]));
                        groups.Add(group);
                    }
                }
            }
            return groups;
        }

        // get the members of a group
        public List<Person> GetPersonsInGroup(Group group)
        {
            List<Person> personsInGroup = new List<Person>();
            string query = "SELECT persons.* FROM persons JOIN in_group ON persons.id_person = in_group.id_person " +
                            "WHERE in_group.id_group = @id_group";
            using (SQLiteCommand command = new SQLiteCommand(query, _connection))
            {
                command.Parameters.AddWithValue("@id_group", group.GetIdGroup());
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int idPerson = reader.GetInt32(0);
                        string stageName = reader.GetString(1);
                        string realName = reader.GetString(2);
                        string birthDate = reader.GetString(3);
                        string deathDate = reader.GetString(4);
                        Person person = new Person(idPerson, stageName, realName, birthDate, deathDate);
                        personsInGroup.Add(person);
                    }
                }
            }
            return personsInGroup;
        }

        // get rolas by performer id
        public List<Rola> GetRolasByPerformer(int idPerformer)
        {
            List<Rola> rolas = new List<Rola>();
            string query = "SELECT * FROM rolas WHERE id_performer = @id_performer";
            using (SQLiteCommand command = new SQLiteCommand(query, _connection))
            {
                command.Parameters.AddWithValue("@id_performer", idPerformer);
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int idRola = reader.GetInt32(0);
                        int idAlbum = reader.GetInt32(2);
                        string path = reader.GetString(3);
                        string title = reader.GetString(4);
                        int track = reader.GetInt32(5);
                        int year = reader.GetInt32(6);
                        string genre = reader.GetString(7);
                        Rola rola = new Rola(idRola, idPerformer, idAlbum, path, title, track, year, genre);
                        rolas.Add(rola);
                    }
                }
            }
            return rolas;
        }

        // get rolas by group id
        public List<Rola> GetRolasByGroup(int idGroup)
        {
            List<Rola> rolas = new List<Rola>();
            string query = "SELECT * FROM rolas WHERE id_performer = @id_group";
            using (SQLiteCommand command = new SQLiteCommand(query, _connection))
            {
                command.Parameters.AddWithValue("@id_group", idGroup);
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int idRola = reader.GetInt32(0);
                        int idPerformer = reader.GetInt32(1);
                        int idAlbum = reader.GetInt32(2);
                        string path = reader.GetString(3);
                        string title = reader.GetString(4);
                        int track = reader.GetInt32(5);
                        int year = reader.GetInt32(6);
                        string genre = reader.GetString(7);
                        Rola rola = new Rola(idRola, idPerformer, idAlbum, path, title, track, year, genre);
                        rolas.Add(rola);
                    }
                }
            }
            return rolas;
        }
    }
}