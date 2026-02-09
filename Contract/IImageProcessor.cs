using ImageProcessor.Model;
using System.Collections.Generic;
namespace ImageProcessor.Contract
{
    public interface IImageProcessor
    {
        /// <summary>
        /// Process a single image file and output multiple sizes in a folder hierarchy
        /// </summary>
        /// <param name="inputFilePath">Original image path</param>
        /// <param name="outputRoot">Root output folder</param>
        void ProcessImage(string inputFilePath, string outputRoot);

        /// <summary>
        /// Set the size configurations (thumbnails, grid, fullpage)
        /// </summary>
        void SetSizeConfigs(ImageSizeConfig config);
    }
}



