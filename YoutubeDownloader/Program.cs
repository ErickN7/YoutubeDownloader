using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Videos.Streams;

class Program
{
    static string path = @"YourPath";

    static async Task Main(string[] args)
    {
        var pathPlaylist = "PlayListId";
        await DownloadPlaylist(pathPlaylist);
    }

    public async static Task DownloadPlaylist(string url)
    {
        var youtube = new YoutubeClient();
        var videos = await youtube.Playlists.GetVideosAsync(url);
        var playlist = await youtube.Playlists.GetAsync(url);
        var folder = playlist.Title.Replace("|", "");
        folder = folder.Replace(".", "");
        folder = folder.Replace(":", "");
        folder = folder.Replace("/", "");
        var optionParallelism = new ParallelOptions { MaxDegreeOfParallelism = 10 };

        if (videos != null && videos.Any())
        {
            path += folder + "\\";
            var existsFolder = System.IO.Directory.Exists(path);

            if (!existsFolder)
                System.IO.Directory.CreateDirectory(path);

            await Parallel.ForEachAsync(videos, optionParallelism, async (video, _) =>
            {
                await DownloadIndividualVideo(video.Url);
            });
        }
    }

    public async static Task DownloadIndividualVideo(string url, int retry = 0)
    {
        var title = "";
        retry++;

        try
        {
            var youtube = new YoutubeClient();
            var video = await youtube.Videos.GetAsync(url);
            title = video.Title.Replace("|", "");
            title = title.Replace(".", "");
            title = title.Replace(":", "");
            title = title.Replace("/", "");
            var streamManifest = await youtube.Videos.Streams.GetManifestAsync(url);
            var bestVideoAudioQuality = streamManifest.GetMuxedStreams().Where(s => s.Container == Container.Mp4).GetWithHighestVideoQuality();
            var pathFile = $"{path}{title}.{bestVideoAudioQuality.Container}";

            if (!File.Exists(pathFile))
            {
                await youtube.Videos.Streams.DownloadAsync(bestVideoAudioQuality, pathFile);
                Console.WriteLine($"Downloaded video: {title}.{bestVideoAudioQuality.Container}"); 
            }
            else
                Console.WriteLine($"Video already exists: {title}.{bestVideoAudioQuality.Container}");
        }
        catch (Exception e)
        {
            if (retry < 5)
            {
                await Task.Delay(5000);
                await DownloadIndividualVideo(url, retry);
            }
            else
                Console.WriteLine($"Download error video: {(!string.IsNullOrEmpty(title) ? title : url)}");
        }
    }
}