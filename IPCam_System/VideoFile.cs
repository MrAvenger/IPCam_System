using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCam_System
{
    public class VideoFiles
    {
        public List<VideoFile> VideoFilesList { get; set; } = new List<VideoFile>();
    }
    public class VideoFile
    {
        public string Name { get; set; }
        public string VideoFormat { get; set; }
        public string TimeVideo { get; set; }
        public string FullName { get; set; }

        public VideoFile() { }

        public VideoFile(string Name, string VideoFormat, string TimeVideo,string FullName)
        {
            this.Name = Name;
            this.VideoFormat = VideoFormat;
            this.TimeVideo = TimeVideo;
            this.FullName = FullName;
        }
    }
}
