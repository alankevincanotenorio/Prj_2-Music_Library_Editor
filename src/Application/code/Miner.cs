using System;
using System.Collections.Generic;
using System.IO;
using TagLib;

public class Miner
{
    private string _path;
    public List<Rola> _rolas = new List<Rola>();

    public Miner(string path)
    {
        _path = path;
    }

    //mining method?
    public bool BrowsePaths(string path)
    {
        try
        {
            var mp3Files = Directory.GetFiles(path, "*.mp3", SearchOption.AllDirectories); //ToDo: check if the path is denied
            foreach (var file in mp3Files)
            {
                if (IsValidFile(file))
                {
                    Rola? rola = GetMetadata(file);
                    if (rola != null)
                    {
                        _rolas.Add(rola);
                        Console.WriteLine($"Rola added: {rola.Title}");
                    }
                }
            }
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Mining error: " + ex.Message);
            return false;
        }
    }

    //check if is .mp3 file but maybe erase?
    public bool IsValidFile(string filePath)
    {
        return Path.GetExtension(filePath).Equals(".mp3", StringComparison.OrdinalIgnoreCase);
    }

    //mine metadata
    public Rola? GetMetadata(string rolaStr)
    {
        Rola? rola = null;
        try
        {
            var file = TagLib.File.Create(rolaStr);
            // TPE1 - performer
            string performer = file.Tag.FirstPerformer ?? "Unknown";
            // TIT2 - title
            string title = file.Tag.Title ?? "Unknown";
            // TALB - Álbum
            string album = file.Tag.Album ?? "Unknown";
            // TDRC - year
            uint year = file.Tag.Year != 0 ? file.Tag.Year : (uint)System.IO.File.GetCreationTime(rolaStr).Year;
            // TCON - genre
            string genre = file.Tag.FirstGenre ?? "Unknown";
            // TRCK - track number
            uint track = file.Tag.Track != 0 ? file.Tag.Track : 0;
            uint totalTracks = file.Tag.TrackCount;
            string trackInfo = totalTracks > 0 ? $"{track} de {totalTracks}" : $"{track}";
            rola = new Rola(9, 0, 0, rolaStr, title, (int)track, (int)year, genre);
                    Console.WriteLine($"Artista: {performer}");
                    Console.WriteLine($"Título: {title}");
                    Console.WriteLine($"Álbum: {album}");
                    Console.WriteLine($"Año: {year}");
                    Console.WriteLine($"Pista: {trackInfo}");
                    Console.WriteLine($"genero: {genre}");
            return rola;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error mining metadata: " + ex.Message);
        }
        return rola;
    }

}