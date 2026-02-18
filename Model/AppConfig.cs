using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcessor.Model
{
    public class AppConfig
    {
        public LibrariesEnum Library { get; set; }
        public string InputFolder { get; set; }
        public string OutputFolder { get; set; }
        public string KrakenApiKey { get; set; }
        public string KrakenApiSecret { get; set; }
        public ImageSizeConfig SizeConfig { get; set; }
    }

}
