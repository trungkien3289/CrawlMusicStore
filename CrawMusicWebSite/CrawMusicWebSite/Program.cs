using Jurassic.Library;
using Newtonsoft.Json;
using ScrapySharp.Extensions;
using ScrapySharp.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CrawMusicWebSite
{
    class Program
    {
        static void Main(string[] args)
        {
            var link = "http://www.cristiana.fm/mewithoutyou/a-b-life/";
            ScrapingAlbum(link);
        }

        public static string TOPDOMAIN = "cristiana.fm";
        public static string CDN_DOMAIN = "http://cdn.cristiana.fm/";
        public static string DOMAIN = "http://cristiana.fm/";
        public static string folderAlbumImagePath = @"D:\CrawlData\Album_Images";
        public static string folderArtistImagePath = @"D:\CrawlData\Artist_Images";
        public static string folderSongPath = @"D:\CrawlData\Album_Songs";

        public static Album ScrapingAlbum(string link)
        {

            ScrapingBrowser Browser = new ScrapingBrowser();
            Browser.AllowAutoRedirect = true; // Browser has settings you can access in setup
            Browser.AllowMetaRedirect = true;
            Browser.Encoding = Encoding.UTF8;

            WebPage PageResult = Browser.NavigateToPage(new Uri(link));

            var scriptString = PageResult.Html.CssSelect("#music>script").FirstOrDefault();

            var engine = new Jurassic.ScriptEngine();
            var result = engine.Evaluate("(function() { var MN = {};MN.m_page= {};MN.m_page.songlist = {};MN.m_page.songlist.artists = {};MN.m_page.songlist.songs = {};MN.m_page.songlist.sid = {};" + scriptString.InnerHtml + " return MN.m_page.songlist; })()");
            var json = JSONObject.Stringify(engine, result);
            songlist data = JsonConvert.DeserializeObject<songlist>(json);

            // Get list song data of album
            List<Guid>  listSongGuid = new List<Guid>(data.songs.Keys);
            List<Song> listSongs = new List<Song>();
            for (int i = 0; i < listSongGuid.Count; i++)
            {
                listSongs.Add(FetchSong(Browser, data, listSongGuid[i]));
            }

            // Crawl album
            Album album = new Album();
            album.Title = PageResult.Html.CssSelect("#artist-info>article>header>h1").FirstOrDefault().InnerText.Trim();
            album.ReleaseDate = PageResult.Html.CssSelect("#artist-info>article>header>h1>time").FirstOrDefault().InnerText.TrimStart('-').Trim();
            album.ArtistName = PageResult.Html.CssSelect("#artist-info>article>header>h2>a").FirstOrDefault().InnerText.Trim();
            album.Thumbnail = PageResult.Html.CssSelect("#artist-info>article>figure>img").FirstOrDefault().GetAttributeValue("src");
            album.Slug = listSongs.Count > 0 ? listSongs[0].AlbumSlug: "";
            SaveImage(folderAlbumImagePath, album.Thumbnail);

            // Crawl artist
            List<Artist> artist = new List<Artist>();
            List<Guid> listArtistIds = new List<Guid>(data.artists.Keys);
            for (int i = 0; i < listArtistIds.Count; i++)
            {
                var newArtist = new Artist()
                {
                    Guid = listArtistIds[i],
                    Name = data.artists[listArtistIds[i]].artist,
                    Slug = data.artists[listArtistIds[i]].slug
                };
                artist.Add(newArtist);

                var artistImageUrl = GetArtistImage(newArtist.Slug, 2);
                SaveImage(folderArtistImagePath, artistImageUrl);
            }


            // Save mp3 files

            for (int i = 0; i < listSongs.Count; i++)
            {
                var mp3FullPath = Path.Combine(folderSongPath, Path.GetFileName(listSongs[i].MediaUrl));
                var success = FileDownloader.DownloadFile(listSongs[i].MediaUrl, mp3FullPath, 120000);
                Console.WriteLine("Done  - success: " + success);
            }

            return null;
        }

        public static Song FetchSong(ScrapingBrowser Browser, songlist albumData,Guid songGuid)
        {
            Song song = new Song();
            var title = albumData.songs[songGuid].song;
            var slug = albumData.songs[songGuid].slug;
            var artistGuid = albumData.songs[songGuid].artistId;
            var endPathSongUrl = albumData.songs[songGuid].url;
            var songSId = albumData.sid[songGuid];
            var subDomain = (Convert.ToInt32(songSId, 16) - 100) / 7;
            var songUrl = "http://mus" + subDomain + "." + TOPDOMAIN + endPathSongUrl;
            var artistRouteName = albumData.artists[artistGuid].slug;
            var albumRouteName = albumData.songs[songGuid].albumSlug;
            var displaySongImage = "";
            if (albumData.songs[songGuid].haveAlbumImage == "True")
            {
                displaySongImage = GetAlbumImage(artistRouteName, albumRouteName, 2);
            }
            else
            {
                displaySongImage = GetArtistImage(artistRouteName, 2);
            }

            // Get lyric
            var lyricUrl = DOMAIN + String.Format("ajax/song?t=1&songId={0}", songGuid);
            WebPage PageResult = Browser.NavigateToPage(new Uri(lyricUrl));
            string lyrics = "";
            ajax_response response = JsonConvert.DeserializeObject<ajax_response>(PageResult.ToString());
            if(response.code == 0 && response.data != null && response.data.Count>0)
            {
                lyrics = response.data[0].lyrics;
            }

            song.Guid = songGuid;
            song.Title = title;
            song.ArtistGuid = artistGuid;
            song.MediaUrl = songUrl;
            song.Thumbnail = displaySongImage;
            song.Url = slug;
            song.Lyrics = lyrics;
            song.AlbumSlug = albumRouteName;
            song.ArtistSlug = artistRouteName;


            // Save resources
            //SaveImage(folderImagePath, songUrl);
            //SaveImage(folderSongPath, songUrl);

            return song;
        }

        private static string GetAlbumImage(string artistRouteName, string albumRouteName, int key)
        {
            return CDN_DOMAIN + "artists/" + artistRouteName[0] + "/" + artistRouteName + "/albums/" + albumRouteName + "-" + key + ".jpg";
        }

        private static string GetArtistImage(string artistRouteName,int key)
        {
            return CDN_DOMAIN + "artists/" + artistRouteName[0] + "/" + artistRouteName + "/" + artistRouteName + "-" + key + ".jpg";
        }

        private static void SaveImage(string folderPath, string imageUrl)
        {
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            string coverImageLocalPath = ImageDownloader.DownloadImageDirect(folderPath, imageUrl, new WebClient());
        }
    }


    public class songlist
    {
        public Dictionary<Guid, cr_artists> artists { get; set; }
        public Dictionary<Guid, string> sid { get; set; }
        public Dictionary<Guid, cr_song> songs { get; set; }

    }

    public class cr_artists
    {
        public string artist { get; set; }
        public int counter { get; set; }
        public string slug { get; set; }
    }

    public class cr_song
    {
        public string albumSlug { get; set; }
        public Guid artistId { get; set; }
        public string haveAlbumImage { get; set; }
        public Guid id { get; set; }
        public string slug { get; set; }
        public string song { get; set; }
        public string url { get; set; }
    }

    public class ajax_response
    {
        public int code { get; set; }
        public string message { get; set; }
        public List<cr_song_detail> data { get; set; }
    }

    public class cr_song_detail
    {
        public Guid id { get; set; }
        public string song { get; set; }
        public string artist { get; set; }
        public string lyrics { get; set; }
        public string slug { get; set; }
        public string artistSlug { get; set; }
    }
}

