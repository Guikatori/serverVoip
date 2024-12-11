namespace Services.RecordingFinder;

public static class RecordingsFinder
{
    public static string GetRecordingPath()
    {
        var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        var recordingPath = Path.Combine(desktopPath, "Recordings");

        if (!Directory.Exists(recordingPath))
        {
            recordingPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Recordings");
        }
        return recordingPath;
    }

    public static FileInfo? GetLastAudioFile(string path)
    {

        if (string.IsNullOrEmpty(path))
        {
            Console.WriteLine("Recording Path is Null");
            return null;
        }


        return new DirectoryInfo(path).GetFiles("*.mp3")
            .OrderByDescending(f => f.CreationTime)
            .FirstOrDefault();
    }
}