using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Cyotek.Spriter.QuickPack
{
  internal sealed class Program
  {
    #region Constants

    private readonly CommandLineTokenizer _commandLine;

    #endregion

    #region Fields

    private string _baseClassName;

    private string _cssFileName;

    private string[] _files;

    private SpriteImage[] _images;

    private string[] _masks;

    private string _path;

    private string _spriteSheetFileName;

    private Size _spriteSheetSize;

    #endregion

    #region Constructors

    internal Program(IEnumerable<string> args)
    {
      _commandLine = new CommandLineTokenizer(args);
    }

    #endregion

    #region Static Methods

    private static int Main(string[] args)
    {
      int exitCode;

      exitCode = new Program(args).Run();

#if DEBUG
      Console.WriteLine("(Press any key to exit)");
      Console.ReadKey(true);
#endif

      return exitCode;
    }

    #endregion

    #region Methods

    internal int Run()
    {
      int exitCode;

      exitCode = 1;

      this.WriteHeader();

      this.LoadArguments();

      _files = this.FindFiles();

      if (_files.Length == 0)
      {
        Console.WriteLine("Nothing to do.");
      }
      else
      {
        try
        {
          _images = this.LoadImages();
          _spriteSheetSize = this.LayoutImages();
          this.SaveSpriteSheet();
          this.SaveCss();

          Console.WriteLine();
          Console.WriteLine(string.Concat("Complete. ", _files.Length.ToString(), " files processed."));

          exitCode = 0;
        }
        catch (Exception ex)
        {
          Console.Error.Write(ex.ToString());
        }
      }

      return exitCode;
    }

    private string[] FindFiles()
    {
      HashSet<string> results;

      results = new HashSet<string>();

      this.PerformAction(() =>
                         {
                           // ReSharper disable LoopCanBePartlyConvertedToQuery
                           foreach (string mask in _masks)
                           {
                             foreach (string fileName in Directory.GetFiles(_path, mask))
                             // ReSharper restore LoopCanBePartlyConvertedToQuery
                             {
                               if (!string.Equals(fileName, _spriteSheetFileName, StringComparison.InvariantCultureIgnoreCase))
                               {
                                 results.Add(fileName);
                               }
                             }
                           }
                         }, "Building file list");

      return results.ToArray();
    }

    private string GetClassNameFromFileName(string fileName)
    {
      // ReSharper disable once PossibleNullReferenceException
      return Path.GetFileNameWithoutExtension(fileName).ToLowerInvariant().Replace(" ", "-");
    }

    private Size GetMaximumSpriteSize()
    {
      int maxWidth;
      int maxHeight;

      maxWidth = 0;
      maxHeight = 0;

      foreach (SpriteImage image in _images)
      {
        maxWidth = Math.Max(maxWidth, image.Size.Width);
        maxHeight = Math.Max(maxHeight, image.Size.Height);
      }

      return new Size(maxWidth, maxHeight);
    }

    private Size GetMinimumSpriteSize()
    {
      int minWidth;
      int minHeight;
      Size maxSize;

      maxSize = this.GetMaximumSpriteSize();
      minWidth = maxSize.Width;
      minHeight = maxSize.Height;

      foreach (SpriteImage image in _images)
      {
        minWidth = Math.Min(minHeight, image.Size.Width);
        minHeight = Math.Min(minHeight, image.Size.Height);
      }

      return new Size(minWidth, minHeight);
    }

    private string GetPixelValue(int value)
    {
      return value != 0 ? string.Concat(value.ToString(), "px") : "0";
    }

    private Size LayoutImages()
    {
      Size size;

      size = Size.Empty;

      this.PerformAction(() =>
                         {
                           LayoutPacker packer;
                           Size maximum;
                           Size minimum;

                           minimum = this.GetMinimumSpriteSize();
                           maximum = this.GetMaximumSpriteSize();

                           packer = new LayoutPacker
                                    {
                                      MinimumSize = minimum,
                                      MaximumSize = maximum,
                                      Increment = minimum.Height,
                                      SortImages = true
                                    };

                           size = packer.LayoutImages(_images);
                         }, "Packing images");

      return size;
    }

    private void LoadArguments()
    {
      _path = _commandLine.GetString("path", Directory.GetCurrentDirectory());
      _masks = _commandLine.ContainsKey("mask") ? _commandLine.GetStringList("mask") : new[]
                                                                                       {
                                                                                         "*.png"
                                                                                       };
      _spriteSheetFileName = _commandLine.GetString("out", Path.Combine(_path, "sheet.png"));
      _cssFileName = _commandLine.GetString("css", null);
      _baseClassName = _commandLine.GetString("class", "icon");
    }

    private SpriteImage[] LoadImages()
    {
      List<SpriteImage> results;

      results = new List<SpriteImage>();

      foreach (string fileName in _files)
      {
        Image image;

        image = Image.FromFile(fileName);

        results.Add(new SpriteImage
                    {
                      FileName = fileName,
                      Size = image.Size
                    });
      }

      return results.ToArray();
    }

    private void PerformAction(Action action, string message)
    {
      Console.Write(message);
      Console.Write("... ");

      try
      {
        action();
        Console.ForegroundColor = ConsoleColor.Green;
        ;
        Console.WriteLine("Done");
      }
      catch (Exception ex)
      {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Failed");
        Console.WriteLine(ex.ToString());
      }
      finally
      {
        Console.ResetColor();
      }
    }

    private void SaveCss()
    {
      if (!string.IsNullOrEmpty(_cssFileName))
      {
        this.PerformAction(() =>
                           {
                             using (Stream stream = File.Create(_cssFileName))
                             {
                               using (TextWriter writer = new StreamWriter(stream, Encoding.UTF8))
                               {
                                 writer.WriteLine(".{0} {{ background-image: url(\"{1}\"); background-repeat: no-repeat; }}", _baseClassName, Path.GetFileName(_spriteSheetFileName));

                                 foreach (SpriteImage image in _images)
                                 {
                                   writer.WriteLine(".{0} {{ background-position: {1} {2}; width: {3}; height: {4}; }}", this.GetClassNameFromFileName(image.FileName), this.GetPixelValue(-image.Location.X), this.GetPixelValue(-image.Location.Y), this.GetPixelValue(image.Size.Width), this.GetPixelValue(image.Size.Height));
                                 }
                               }
                             }
                           }, "Saving CSS");
      }
    }

    private void SaveSpriteSheet()
    {
      this.PerformAction(() =>
                         {
                           using (Bitmap bitmap = new Bitmap(_spriteSheetSize.Width, _spriteSheetSize.Height, PixelFormat.Format32bppArgb))
                           {
                             using (Graphics graphics = Graphics.FromImage(bitmap))
                             {
                               graphics.Clear(Color.Transparent);

                               foreach (SpriteImage image in _images)
                               {
                                 using (Image sprite = Image.FromFile(image.FileName))
                                 {
                                   graphics.DrawImage(sprite, image.Bounds, new Rectangle(Point.Empty, image.Size), GraphicsUnit.Pixel);
                                 }
                               }
                             }

                             if (File.Exists(_spriteSheetFileName))
                             {
                               File.Delete(_spriteSheetFileName);
                             }
                             bitmap.Save(_spriteSheetFileName, ImageFormat.Png);
                           }
                         }, "Saving sprite sheet");
    }

    private void WriteHeader()
    {
      Assembly assembly;
      FileVersionInfo fileVersionInfo;

      assembly = Assembly.GetEntryAssembly();
      fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);

      Console.ForegroundColor = ConsoleColor.White;
      Console.WriteLine(fileVersionInfo.FileDescription);
      Console.WriteLine(string.Concat("Version ", fileVersionInfo.ProductMajorPart.ToString(), ".", fileVersionInfo.ProductMinorPart.ToString()));
      Console.ResetColor();
      Console.WriteLine(fileVersionInfo.LegalCopyright);
      Console.WriteLine();
    }

    #endregion
  }
}
