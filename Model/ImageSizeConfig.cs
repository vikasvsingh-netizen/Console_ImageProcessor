using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcessor.Model
{
    public class ImageSizeConfig
    {
        public List<ImageSize> Thumbnails { get; set; }
        public List<ImageSize> Grid { get; set; }
        public List<ImageSize> FullPage { get; set; }
    }

}
