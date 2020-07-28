// Decompiled with JetBrains decompiler
// Type: QUT.Gppg.AbstractScanner`2
// Assembly: QUT.ShiftReduceParser, Version=1.4.1.0, Culture=neutral, PublicKeyToken=402396ef6102baec
// MVID: 454DBF7C-E638-4FF4-BE67-5E822302962D
// Assembly location: C:\Repos\automatic-graph-layout\GraphLayout\tools\Dot2Graph\gp\QUT.ShiftReduceParser.dll

using System.Diagnostics.CodeAnalysis;

namespace QUT.Gppg
{
  public abstract class AbstractScanner<TValue, TSpan> where TSpan : IMerge<TSpan>
  {
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "yylval")]
    [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
    [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "yylval")]
    public TValue yylval;

    [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "yylloc")]
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "yylloc")]
    public virtual TSpan yylloc
    {
      get
      {
        return default (TSpan);
      }
      set
      {
      }
    }

    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "yylex")]
    [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "yylex")]
    public abstract int yylex();

    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "yyerror")]
    [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "yyerror")]
    public virtual void yyerror(string format, params object[] args)
    {
    }
  }
}
