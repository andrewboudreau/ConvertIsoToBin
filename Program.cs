// https://chat.openai.com/share/2f2f981d-8533-4d91-9397-20d0a36ee090

class Program
{
    static void Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: dotnet run <path/to/iso/file> OR <path/to/directory>");
            return;
        }

        string inputPath = args[0];

        if (File.Exists(inputPath))
        {
            // Single ISO file provided
            ConvertIsoToBin(inputPath);
        }
        else if (Directory.Exists(inputPath))
        {
            // Directory provided
            foreach (var subfolder in Directory.GetDirectories(inputPath))
            {
                var isoFiles = Directory.GetFiles(subfolder, "*.iso");
                foreach (var isoFile in isoFiles)
                {
                    ConvertIsoToBin(isoFile);
                }
            }
        }
        else
        {
            Console.WriteLine("Invalid path provided.");
        }
    }
    static void ConvertIsoToBin(string isoFilePath)
    {
        // Existing logic for single ISO file conversion

        // Validate ISO file exists
        if (string.IsNullOrEmpty(isoFilePath) || !File.Exists(isoFilePath))
        {
            Console.WriteLine("Invalid ISO file path.");
            return;
        }

        // Look for associated CUE file in the same directory
        string directoryPath = Path.GetDirectoryName(isoFilePath)!;
        string cueFileName = Path.GetFileNameWithoutExtension(isoFilePath) + ".cue";
        string cueFilePath = Path.Combine(directoryPath, cueFileName);

        if (!File.Exists(cueFilePath))
        {
            Console.WriteLine("Associated CUE file not found.");
            return;
        }

        try
        {
            // Read ISO file
            byte[] isoData = ReadIsoFile(isoFilePath);

            // Parse CUE file
            CueInfo cueInfo = ParseCueFile(cueFilePath);

            // Extract Tracks
            TrackInfo[] tracks = ExtractTracks(isoData, cueInfo);

            // Create New BIN File
            string newBinFilePath = CreateBinFile(tracks, $"_{Path.GetFileNameWithoutExtension(isoFilePath)}");

            // Create New CUE Sheet
            string newCueFilePath = CreateCueSheet(newBinFilePath, tracks);

            Console.WriteLine($"Conversion successful. New BIN File: {newBinFilePath}, New CUE File: {newCueFilePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }

    static byte[] ReadIsoFile(string isoFilePath)
    {
        try
        {
            return File.ReadAllBytes(isoFilePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while reading the ISO file: {ex.Message}");
            throw;
        }
    }

    static CueInfo ParseCueFile(string cueFilePath)
    {
        CueInfo cueInfo = new();

        try
        {
            string[] lines = File.ReadAllLines(cueFilePath);
            int currentTrackNumber = 0;
            string currentTrackType = string.Empty;
            string currentPregapTime = string.Empty;

            foreach (var line in lines.Select(x => x.Trim()))
            {
                if (line.StartsWith("TRACK"))
                {
                    currentTrackNumber++;
                    currentTrackType = line.Split(' ')[1];
                }
                else if (line.StartsWith("PREGAP"))
                {
                    currentPregapTime = line.Split(' ')[1];
                }
                else if (line.StartsWith("INDEX 01"))
                {
                    string startingTime = line.Split(' ')[2];
                    cueInfo.Tracks.Add(new TrackInfo(currentTrackNumber, currentTrackType, startingTime, default, currentPregapTime));
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while parsing the CUE file: {ex.Message}");
            throw;
        }

        return cueInfo;
    }

    static TrackInfo[] ExtractTracks(byte[] isoData, CueInfo cueInfo)
    {
        List<TrackInfo> extractedTracks = [];
        try
        {
            foreach (var track in cueInfo.Tracks)
            {
                // Calculate the starting offset based on the StartingTime property of the track
                // NOTE: This would require converting the time format to an actual byte offset
                int startOffset = CalculateStartOffset(track.StartingTime);

                // Determine the length of the track
                // NOTE: Typically, this would be the start offset of the next track minus the start offset of the current track
                // For the last track, this would be the end of the isoData array
                int trackLength = CalculateTrackLength(track, cueInfo, isoData.Length);

                // Use ArraySegment to point to the slice of isoData for this track
                ArraySegment<byte> trackData = new(isoData, startOffset, trackLength);

                TrackInfo newTrack = track with { Data = trackData };

                extractedTracks.Add(newTrack);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while extracting tracks: {ex.Message}");
            throw;
        }

        return [.. extractedTracks];
    }

    static int CalculateStartOffset(string startingTime)
    {
        try
        {
            // Parse the mm:ss:ff format into individual components
            string[] timeComponents = startingTime.Split(':');
            int minutes = int.Parse(timeComponents[0]);
            int seconds = int.Parse(timeComponents[1]);
            int frames = int.Parse(timeComponents[2]);

            // Calculate the byte offset
            int totalFrames = (minutes * 60 * 75) + (seconds * 75) + frames;
            int byteOffset = totalFrames * 2352;  // Assuming 2352 bytes per frame

            return byteOffset;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while calculating the start offset: {ex.Message}");
            throw;
        }
    }

    static int CalculateTrackLength(TrackInfo currentTrack, CueInfo cueInfo, int isoLength)
    {
        try
        {
            int currentIndex = cueInfo.Tracks.IndexOf(currentTrack);

            // If it's the last track, the length is up to the end of the ISO data
            if (currentIndex == cueInfo.Tracks.Count - 1)
            {
                return isoLength - CalculateStartOffset(currentTrack.StartingTime);
            }

            // Otherwise, find the next track's start offset
            TrackInfo nextTrack = cueInfo.Tracks[currentIndex + 1];
            int nextTrackStartOffset = CalculateStartOffset(nextTrack.StartingTime);

            // The length of the current track is the difference between the next and current start offsets
            return nextTrackStartOffset - CalculateStartOffset(currentTrack.StartingTime);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while calculating the track length: {ex.Message}");
            throw;
        }
    }

    static string CreateBinFile(TrackInfo[] tracks, string isoFileName)
    {
        try
        {
            string newBinFilePath = $"{isoFileName}.bin";
            using (FileStream fs = new(newBinFilePath, FileMode.Create, FileAccess.Write))
            {
                foreach (var track in tracks)
                {
                    // The Data property in TrackInfo is now of type ArraySegment<byte>
                    fs.Write(track.Data.Array!, track.Data.Offset, track.Data.Count);
                }
            }
            return newBinFilePath;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while creating the BIN file: {ex.Message}");
            throw;
        }
    }

    static string CreateCueSheet(string newBinFilePath, TrackInfo[] tracks)
    {
        try
        {
            string newCueFilePath = $"{Path.GetFileNameWithoutExtension(newBinFilePath)}.cue";
            using (StreamWriter sw = new(newCueFilePath))
            {
                sw.WriteLine($"FILE \"{newBinFilePath}\" BINARY");
                foreach (var track in tracks)
                {
                    sw.WriteLine($"  TRACK {track.TrackNumber:D2} {track.TrackType}");
                    if (!string.IsNullOrEmpty(track.Pregap))
                    {
                        sw.WriteLine($"    PREGAP {track.Pregap}");
                    }
                    sw.WriteLine($"    INDEX 01 {track.StartingTime}");
                }
            }
            return newCueFilePath;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while creating the CUE file: {ex.Message}");
            throw;
        }
    }

    record CueInfo(List<TrackInfo> Tracks)
    {
        public CueInfo() : this([]) { }
    }

    record TrackInfo(int TrackNumber, string TrackType, string StartingTime, ArraySegment<byte> Data, string? Pregap);
}
