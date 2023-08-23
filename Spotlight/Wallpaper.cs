using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spotlight
{
    internal class Wallpaper
    {
        public string title;
        public string copyright;
        public string landscapeUrl;
        public string landscapeSha256;
        public string portraitUrl;
        public string portraitSha256;

        public Wallpaper() { }

        public Wallpaper(string title, string copyRight, string landscapeUrl, string landscapeSha256, string portraitUrl, string portraitSha256)
        {
            this.title = title;
            this.copyright = copyRight;
            this.landscapeUrl = landscapeUrl;
            this.landscapeSha256 = landscapeSha256;
            this.portraitUrl = portraitUrl;
            this.portraitSha256 = portraitSha256;
        }
    }
}
