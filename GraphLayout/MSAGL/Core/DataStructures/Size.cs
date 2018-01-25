namespace Microsoft.Msagl.Core.DataStructures
{


  /// <summary>
  /// Size structure
  /// </summary>
  public struct Size
  {
    double width;
    /// <summary>
    /// width
    /// </summary>
    public double Width
    {
      get { return width; }
      set { width = value; }
    }
    double height;
    /// <summary>
    /// Height
    /// </summary>
    public double Height
    {
      get { return height; }
      set { height = value; }
    }

    /// <summary>
    /// constructor
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    public Size(double width, double height)
    {
      this.width = width;


      this.height = height;

    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="s"></param>
    /// <param name="d"></param>
    /// <returns></returns>
    public static Size operator /(Size s, double d) { return new Size(s.Width / d, s.Height / d); }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="s"></param>
    /// <param name="d"></param>
    /// <returns></returns>
    public static Size operator *(Size s, double d) { return new Size(s.Width * d, s.Height * d); }


      /// <summary>
      /// padding the size ( from both sides!)
      /// </summary>
      /// <param name="padding"></param>
      public void Pad(double padding) {
          width += 2*padding;
          height += 2*padding;
      }
  }


}
