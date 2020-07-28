// Decompiled with JetBrains decompiler
// Type: QUT.Gppg.LexLocation
// Assembly: QUT.ShiftReduceParser, Version=1.4.1.0, Culture=neutral, PublicKeyToken=402396ef6102baec
// MVID: 454DBF7C-E638-4FF4-BE67-5E822302962D
// Assembly location: C:\Repos\automatic-graph-layout\GraphLayout\tools\Dot2Graph\gp\QUT.ShiftReduceParser.dll

namespace QUT.Gppg
{
  public class LexLocation : IMerge<LexLocation>
  {
    private int startLine;
    private int startColumn;
    private int endLine;
    private int endColumn;

    public int StartLine
    {
      get
      {
        return this.startLine;
      }
    }

    public int StartColumn
    {
      get
      {
        return this.startColumn;
      }
    }

    public int EndLine
    {
      get
      {
        return this.endLine;
      }
    }

    public int EndColumn
    {
      get
      {
        return this.endColumn;
      }
    }

    public LexLocation()
    {
    }

    public LexLocation(int sl, int sc, int el, int ec)
    {
      this.startLine = sl;
      this.startColumn = sc;
      this.endLine = el;
      this.endColumn = ec;
    }

    public LexLocation Merge(LexLocation last)
    {
      return new LexLocation(this.startLine, this.startColumn, last.endLine, last.endColumn);
    }
  }
}
