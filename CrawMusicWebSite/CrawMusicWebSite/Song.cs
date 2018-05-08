using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrawMusicWebSite
{
    public class Song
    {
        public Guid Guid { get; set; }
        public Guid AlbumGuid { get; set; }
        public string AlbumSlug { get; set; }
        public Guid ArtistGuid { get; set; }
        public string ArtistSlug { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Thumbnail { get; set; }
        public string MediaUrl { get; set; }
        public string Lyrics { get; set; }
        public Nullable<int> Status { get; set; }
        public string Url { get; set; }

    }

    public class Album
    {
        public string Title { get; set; }
        public string ArtistName { get; set; }
        public string Description { get; set; }
        public string Thumbnail { get; set; }
        public string ReleaseDate { get; set; }
        public string Slug { get; set; }
        public List<Song> Songs { get; set; }
    }

    public class Artist
    {
        public Guid Guid { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
    }
}
