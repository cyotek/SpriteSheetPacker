using System;
using System.Collections.Generic;
using System.Drawing;
using Cyotek.Spriter.QuickPack.Packing;

namespace Cyotek.Spriter.QuickPack
{
  internal sealed class LayoutPacker
  {
    #region Fields

    private List<string> _files;

    private IDictionary<string, Rectangle> _imagePlacement;

    private IDictionary<string, SpriteImage> _images;

    private IDictionary<string, Size> _imageSizes;

    private int _increment;

    private int _outputHeight;

    private int _outputWidth;

    #endregion

    #region Properties

    public int Increment
    {
      get { return _increment; }
      set
      {
        if (value < 1)
        {
          throw new ArgumentException("Value must be a non-zero positive number.", "value");
        }

        _increment = value;
      }
    }

    public Size MaximumSize { get; set; }

    public Size MinimumSize { get; set; }

    public bool SortImages { get; set; }

    #endregion

    #region Methods

    public Size LayoutImages(IEnumerable<SpriteImage> images)
    {
      _imageSizes = new Dictionary<string, Size>();
      _files = new List<string>();
      _images = new Dictionary<string, SpriteImage>();
      _imagePlacement = new Dictionary<string, Rectangle>();

      _outputWidth = this.MaximumSize.Width;
      _outputHeight = this.MaximumSize.Height;

      foreach (SpriteImage image in images)
      {
        _files.Add(image.FileName);
        _images.Add(image.FileName, image);
        _imageSizes.Add(image.FileName, image.Size);
      }

      if (_files.Count != 0)
      {
        // sort our files by image size so we place large sprites first
        if (this.SortImages)
        {
          _files.Sort((x, y) =>
                      {
                        Size lhs;
                        Size rhs;
                        int compare;

                        lhs = _imageSizes[x];
                        rhs = _imageSizes[y];

                        compare = -lhs.Width.CompareTo(rhs.Width);
                        if (compare == 0)
                        {
                          compare = -lhs.Height.CompareTo(rhs.Height);
                        }

                        return compare;
                      });
        }

        // try to pack the images
        this.LayoutImageRectangles();

        // update the collection
        foreach (KeyValuePair<string, Rectangle> pair in _imagePlacement)
        {
          SpriteImage image;

          image = _images[pair.Key];
          if (image != null)
          {
            image.Location = pair.Value.Location;
          }
        }
      }
      else
      {
        _outputWidth = this.MinimumSize.Width;
        _outputHeight = this.MinimumSize.Height;
      }

      return new Size(_outputWidth, _outputHeight);
    }

    private void LayoutImageRectangles()
    {
      Dictionary<string, Rectangle> testImagePlacement;
      bool smallestSizeReached;
      bool workAreaIncreased;
      int workAreaWidth;
      int workAreaHeight;

      smallestSizeReached = false;
      workAreaIncreased = false;
      workAreaWidth = _outputWidth;
      workAreaHeight = _outputHeight;
      testImagePlacement = new Dictionary<string, Rectangle>();

      while (!smallestSizeReached)
      {
        bool areImagesPacked;

        // try to pack the images into our current test size
        areImagesPacked = this.TestPackingRectangles(workAreaWidth, workAreaHeight, testImagePlacement);
        if (!areImagesPacked)
        {
          // if that failed...

          // if we have no images in imagePlacement, i.e. we've never succeeded at PackImages,
          // show an error and return false since there is no way to fit the images into our
          // maximum size texture
          if (testImagePlacement.Count == 0)
          {
            throw new OutOfSpaceException("Unable to fit any images");
          }

          // otherwise increase the size of the canvas
          workAreaIncreased = true;
          workAreaHeight += this.Increment;
          workAreaWidth = Math.Max(workAreaWidth, workAreaHeight);
        }
        else
        {
          if (!workAreaIncreased)
          {
            int newWidth;
            int newHeight;

            // figure out the smallest bitmap that will hold all the images
            newWidth = 0;
            newHeight = 0;
            foreach (KeyValuePair<string, Rectangle> pair in testImagePlacement)
            {
              newWidth = Math.Max(newWidth, pair.Value.Right);
              newHeight = Math.Max(newHeight, pair.Value.Bottom);
            }

            newWidth = Math.Max(newWidth, this.MinimumSize.Width);
            newHeight = Math.Max(newHeight, this.MinimumSize.Height);

            if (newWidth == workAreaWidth && newHeight == workAreaHeight)
            {
              smallestSizeReached = true;
            }

            workAreaWidth = newWidth;
            workAreaHeight = newHeight;
          }
          else
          {
            smallestSizeReached = true;
          }
        }
      }

      // clear the imagePlacement dictionary and add our test results in
      _imagePlacement = new Dictionary<string, Rectangle>(testImagePlacement);
      _outputWidth = workAreaWidth;
      _outputHeight = workAreaHeight;
    }

    private bool TestPackingRectangles(int testWidth, int testHeight, IDictionary<string, Rectangle> placementStorage)
    {
      RectanglePacker rectanglePacker;
      bool result;

      result = true;

      rectanglePacker = new ArevaloRectanglePacker(testWidth, testHeight);

      // reset the results
      placementStorage.Clear();

      foreach (string image in _files)
      {
        Size size;
        Point origin;

        // get the bitmap for this file
        size = _imageSizes[image];

        // pack the image
        result = rectanglePacker.TryPack(size.Width, size.Height, out origin);

        if (result)
        {
          placementStorage.Add(image, new Rectangle(origin.X, origin.Y, size.Width, size.Height));
        }
        else
        {
          break;
        }
      }

      return result;
    }

    #endregion
  }
}
