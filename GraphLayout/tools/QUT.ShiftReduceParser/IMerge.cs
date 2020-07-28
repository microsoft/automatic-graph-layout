// Decompiled with JetBrains decompiler
// Type: QUT.Gppg.IMerge`1
// Assembly: QUT.ShiftReduceParser, Version=1.4.1.0, Culture=neutral, PublicKeyToken=402396ef6102baec
// MVID: 454DBF7C-E638-4FF4-BE67-5E822302962D
// Assembly location: C:\Repos\automatic-graph-layout\GraphLayout\tools\Dot2Graph\gp\QUT.ShiftReduceParser.dll

namespace QUT.Gppg
{
  public interface IMerge<TSpan>
  {
    TSpan Merge(TSpan last);
  }
}
