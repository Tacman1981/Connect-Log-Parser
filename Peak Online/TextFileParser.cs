using System.IO;

public class TextFileParser
{
    public class LogEvent
    {
        public DateTime Timestamp { get; set; }
        public bool IsConnectEvent { get; set; }
    }

    public class DailyPeak
    {
        public DateTime Date { get; set; }
        public int PeakPlayers { get; set; }
    }

    public (int peakPlayers, List<DailyPeak> dailyPeaks) ParseLogFile(string filePath)
    {
        int peakPlayers = 0;
        List<LogEvent> logEvents = new List<LogEvent>();

        if (File.Exists(filePath))
        {
            string[] lines = File.ReadAllLines(filePath);

            foreach (string line in lines)
            {
                string timestampString = line.Substring(0, line.IndexOf("]")).Trim('[', ']');
                DateTime timestamp;

                if (DateTime.TryParseExact(timestampString, "MM/dd/yyyy HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out timestamp))
                {
                    bool isConnectEvent = line.Contains("[CONNECT]");
                    logEvents.Add(new LogEvent { Timestamp = timestamp, IsConnectEvent = isConnectEvent });
                }
                else
                {
                    Console.WriteLine($"Error parsing timestamp: {timestampString}");
                }
            }

            logEvents.Sort((x, y) => x.Timestamp.CompareTo(y.Timestamp));

            // Calculate daily peaks
            var dailyPeaks = CalculateDailyPeaks(logEvents, ref peakPlayers);
            return (peakPlayers, dailyPeaks);
        }
        else
        {
            peakPlayers = -1; // File not found
        }

        return (peakPlayers, new List<DailyPeak>());
    }

    public List<DailyPeak> CalculateDailyPeaks(List<LogEvent> logEvents, ref int overallPeak)
    {
        var dailyPeaks = new Dictionary<DateTime, int>();
        int currentPlayers = 0;

        foreach (var logEvent in logEvents)
        {
            if (logEvent.IsConnectEvent)
            {
                currentPlayers++;
            }
            else
            {
                // Ignore disconnect events after midnight if no previous connect
                if (logEvent.Timestamp.TimeOfDay == TimeSpan.Zero && currentPlayers == 0)
                    continue;

                currentPlayers--;
            }

            DateTime eventDate = logEvent.Timestamp.Date;

            if (!dailyPeaks.ContainsKey(eventDate))
            {
                dailyPeaks[eventDate] = currentPlayers;
            }
            else
            {
                dailyPeaks[eventDate] = Math.Max(dailyPeaks[eventDate], currentPlayers);
            }

            if (currentPlayers > overallPeak)
            {
                overallPeak = currentPlayers;
            }
        }

        return dailyPeaks.Select(dp => new DailyPeak { Date = dp.Key, PeakPlayers = dp.Value }).ToList();
    }
}
