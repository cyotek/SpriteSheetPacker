using System.Drawing;

namespace Cyotek.Spriter.QuickPack
{
  internal sealed class SpriteImage
  {
    #region Properties

    public Rectangle Bounds
    {
      get { return new Rectangle(this.Location, this.Size); }
      set
      {
        this.Location = value.Location;
        this.Size = value.Size;
      }
    }

    public string FileName { get; set; }

    public Image Image { get; set; }

    public Point Location { get; set; }

    public Size Size { get; set; }

    #endregion
  }
}
