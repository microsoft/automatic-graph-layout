// Decompiled with JetBrains decompiler
// Type: QUT.Gppg.ShiftReduceParser`2
// Assembly: QUT.ShiftReduceParser, Version=1.4.1.0, Culture=neutral, PublicKeyToken=402396ef6102baec
// MVID: 454DBF7C-E638-4FF4-BE67-5E822302962D
// Assembly location: C:\Repos\automatic-graph-layout\GraphLayout\tools\Dot2Graph\gp\QUT.ShiftReduceParser.dll

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.Serialization;
using System.Text;

namespace QUT.Gppg
{
  public abstract class ShiftReduceParser<TValue, TSpan> where TSpan : IMerge<TSpan>, new()
  {
    private PushdownPrefixState<State> StateStack = new PushdownPrefixState<State>();
    private PushdownPrefixState<TValue> valueStack = new PushdownPrefixState<TValue>();
    private PushdownPrefixState<TSpan> locationStack = new PushdownPrefixState<TSpan>();
    private AbstractScanner<TValue, TSpan> scanner;
    [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
    protected TValue CurrentSemanticValue;
    [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
    protected TSpan CurrentLocationSpan;
    private TSpan LastSpan;
    private int NextToken;
    private State FsaState;
    private bool recovering;
    private int tokensSinceLastError;
    private int errorToken;
    private int endOfFileToken;
    private string[] nonTerminals;
    private State[] states;
    private Rule[] rules;

    protected AbstractScanner<TValue, TSpan> Scanner
    {
      get
      {
        return this.scanner;
      }
      set
      {
        this.scanner = value;
      }
    }

    protected ShiftReduceParser(AbstractScanner<TValue, TSpan> scanner)
    {
      this.scanner = scanner;
    }

    protected PushdownPrefixState<TValue> ValueStack
    {
      get
      {
        return this.valueStack;
      }
    }

    protected PushdownPrefixState<TSpan> LocationStack
    {
      get
      {
        return this.locationStack;
      }
    }

    protected void InitRules(Rule[] rules)
    {
      this.rules = rules;
    }

    protected void InitStates(State[] states)
    {
      this.states = states;
    }

    protected void InitStateTable(int size)
    {
      this.states = new State[size];
    }

    protected void InitSpecialTokens(int err, int end)
    {
      this.errorToken = err;
      this.endOfFileToken = end;
    }

    protected void InitNonTerminals(string[] names)
    {
      this.nonTerminals = names;
    }

    protected static void YYAccept()
    {
      throw new ShiftReduceParser<TValue, TSpan>.AcceptException();
    }

    protected static void YYAbort()
    {
      throw new ShiftReduceParser<TValue, TSpan>.AbortException();
    }

    protected static void YYError()
    {
      throw new ShiftReduceParser<TValue, TSpan>.ErrorException();
    }

    protected bool YYRecovering
    {
      get
      {
        return this.recovering;
      }
    }

    protected abstract void Initialize();

    public bool Parse()
    {
      this.Initialize();
      this.NextToken = 0;
      this.FsaState = this.states[0];
      this.StateStack.Push(this.FsaState);
      this.valueStack.Push(this.CurrentSemanticValue);
      this.LocationStack.Push(this.CurrentLocationSpan);
      while (true)
      {
        int defaultAction = this.FsaState.defaultAction;
        if (this.FsaState.ParserTable != null)
        {
          if (this.NextToken == 0)
          {
            this.LastSpan = this.scanner.yylloc;
            this.NextToken = this.scanner.yylex();
          }
          if (this.FsaState.ParserTable.ContainsKey(this.NextToken))
            defaultAction = this.FsaState.ParserTable[this.NextToken];
        }
        if (defaultAction > 0)
          this.Shift(defaultAction);
        else if (defaultAction < 0)
        {
          try
          {
            this.Reduce(-defaultAction);
            if (defaultAction == -1)
              return true;
          }
          catch (Exception ex)
          {
            int num;
            switch (ex)
            {
              case ShiftReduceParser<TValue, TSpan>.AbortException _:
                return false;
              case ShiftReduceParser<TValue, TSpan>.AcceptException _:
                return true;
              case ShiftReduceParser<TValue, TSpan>.ErrorException _:
                num = this.ErrorRecovery() ? 1 : 0;
                break;
              default:
                num = 1;
                break;
            }
            if (num == 0)
              return false;
            throw;
          }
        }
        else if (defaultAction == 0 && !this.ErrorRecovery())
          break;
      }
      return false;
    }

    private void Shift(int stateIndex)
    {
      this.FsaState = this.states[stateIndex];
      this.valueStack.Push(this.scanner.yylval);
      this.StateStack.Push(this.FsaState);
      this.LocationStack.Push(this.scanner.yylloc);
      if (this.recovering)
      {
        if (this.NextToken != this.errorToken)
          ++this.tokensSinceLastError;
        if (this.tokensSinceLastError > 5)
          this.recovering = false;
      }
      if (this.NextToken == this.endOfFileToken)
        return;
      this.NextToken = 0;
    }

    private void Reduce(int ruleNumber)
    {
      Rule rule = this.rules[ruleNumber];
      if (rule.RightHandSide.Length == 1)
      {
        this.CurrentSemanticValue = this.valueStack.TopElement();
        this.CurrentLocationSpan = this.LocationStack.TopElement();
      }
      else if (rule.RightHandSide.Length == 0)
      {
        this.CurrentSemanticValue = default (TValue);
        this.CurrentLocationSpan = (object) this.scanner.yylloc == null || (object) this.LastSpan == null ? default (TSpan) : this.scanner.yylloc.Merge(this.LastSpan);
      }
      else
      {
        this.CurrentSemanticValue = this.valueStack.TopElement();
        TSpan location1 = this.LocationStack[this.LocationStack.Depth - rule.RightHandSide.Length];
        TSpan location2 = this.LocationStack[this.LocationStack.Depth - 1];
        this.CurrentLocationSpan = (object) location1 == null || (object) location2 == null ? default (TSpan) : location1.Merge(location2);
      }
      this.DoAction(ruleNumber);
      for (int index = 0; index < rule.RightHandSide.Length; ++index)
      {
        this.StateStack.Pop();
        this.valueStack.Pop();
        this.LocationStack.Pop();
      }
      this.FsaState = this.StateStack.TopElement();
      if (this.FsaState.Goto.ContainsKey(rule.LeftHandSide))
        this.FsaState = this.states[this.FsaState.Goto[rule.LeftHandSide]];
      this.StateStack.Push(this.FsaState);
      this.valueStack.Push(this.CurrentSemanticValue);
      this.LocationStack.Push(this.CurrentLocationSpan);
    }

    protected abstract void DoAction(int actionNumber);

    private bool ErrorRecovery()
    {
      if (!this.recovering)
        this.ReportError();
      if (!this.FindErrorRecoveryState())
        return false;
      this.ShiftErrorToken();
      bool flag = this.DiscardInvalidTokens();
      this.recovering = true;
      this.tokensSinceLastError = 0;
      return flag;
    }

    private void ReportError()
    {
      StringBuilder stringBuilder = new StringBuilder();
      stringBuilder.AppendFormat("Syntax error, unexpected {0}", (object) this.TerminalToString(this.NextToken));
      if (this.FsaState.ParserTable.Count < 7)
      {
        bool flag = true;
        foreach (int key in this.FsaState.ParserTable.Keys)
        {
          if (flag)
            stringBuilder.Append(", expecting ");
          else
            stringBuilder.Append(", or ");
          stringBuilder.Append(this.TerminalToString(key));
          flag = false;
        }
      }
      this.scanner.yyerror(stringBuilder.ToString());
    }

    private void ShiftErrorToken()
    {
      int nextToken = this.NextToken;
      this.NextToken = this.errorToken;
      this.Shift(this.FsaState.ParserTable[this.NextToken]);
      this.NextToken = nextToken;
    }

    private bool FindErrorRecoveryState()
    {
      while (true)
      {
        if (this.FsaState.ParserTable == null || !this.FsaState.ParserTable.ContainsKey(this.errorToken) || this.FsaState.ParserTable[this.errorToken] <= 0)
        {
          this.StateStack.Pop();
          this.valueStack.Pop();
          this.LocationStack.Pop();
          if (!this.StateStack.IsEmpty())
            this.FsaState = this.StateStack.TopElement();
          else
            goto label_3;
        }
        else
          break;
      }
      return true;
label_3:
      return false;
    }

    private bool DiscardInvalidTokens()
    {
      int defaultAction = this.FsaState.defaultAction;
      if (this.FsaState.ParserTable != null)
      {
        while (true)
        {
          if (this.NextToken == 0)
            this.NextToken = this.scanner.yylex();
          if (this.NextToken != this.endOfFileToken)
          {
            if (this.FsaState.ParserTable.ContainsKey(this.NextToken))
              defaultAction = this.FsaState.ParserTable[this.NextToken];
            if (defaultAction == 0)
              this.NextToken = 0;
            else
              goto label_7;
          }
          else
            break;
        }
        return false;
label_7:
        return true;
      }
      if (!this.recovering || this.tokensSinceLastError != 0)
        return true;
      if (this.NextToken == this.endOfFileToken)
        return false;
      this.NextToken = 0;
      return true;
    }

    [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "yyclearin")]
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "yyclearin")]
    protected void yyclearin()
    {
      this.NextToken = 0;
    }

    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "yyerrok")]
    [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "yyerrok")]
    protected void yyerrok()
    {
      this.recovering = false;
    }

    protected void AddState(int stateNumber, State state)
    {
      this.states[stateNumber] = state;
      state.number = stateNumber;
    }

    private void DisplayStack()
    {
      Console.Error.Write("State stack is now:");
      for (int index = 0; index < this.StateStack.Depth; ++index)
        Console.Error.Write(" {0}", (object) this.StateStack[index].number);
      Console.Error.WriteLine();
    }

    private void DisplayRule(int ruleNumber)
    {
      Console.Error.Write("Reducing stack by rule {0}, ", (object) ruleNumber);
      this.DisplayProduction(this.rules[ruleNumber]);
    }

    private void DisplayProduction(Rule rule)
    {
      if (rule.RightHandSide.Length == 0)
      {
        Console.Error.Write("/* empty */ ");
      }
      else
      {
        foreach (int symbol in rule.RightHandSide)
          Console.Error.Write("{0} ", (object) this.SymbolToString(symbol));
      }
      Console.Error.WriteLine("-> {0}", (object) this.SymbolToString(rule.LeftHandSide));
    }

    protected abstract string TerminalToString(int terminal);

    private string SymbolToString(int symbol)
    {
      return symbol < 0 ? this.nonTerminals[-symbol - 1] : this.TerminalToString(symbol);
    }

    protected static string CharToString(char input)
    {
      switch (input)
      {
        case char.MinValue:
          return "'\\0'";
        case '\a':
          return "'\\a'";
        case '\b':
          return "'\\b'";
        case '\t':
          return "'\\t'";
        case '\n':
          return "'\\n'";
        case '\v':
          return "'\\v'";
        case '\f':
          return "'\\f'";
        case '\r':
          return "'\\r'";
        default:
          return string.Format((IFormatProvider) CultureInfo.InvariantCulture, "'{0}'", (object) input);
      }
    }

    [SuppressMessage("Microsoft.Design", "CA1064:ExceptionsShouldBePublic")]
    [Serializable]
    private class AcceptException : Exception
    {
      internal AcceptException()
      {
      }

      protected AcceptException(SerializationInfo i, StreamingContext c)
        : base(i, c)
      {
      }
    }

    [SuppressMessage("Microsoft.Design", "CA1064:ExceptionsShouldBePublic")]
    [Serializable]
    private class AbortException : Exception
    {
      internal AbortException()
      {
      }

      protected AbortException(SerializationInfo i, StreamingContext c)
        : base(i, c)
      {
      }
    }

    [SuppressMessage("Microsoft.Design", "CA1064:ExceptionsShouldBePublic")]
    [Serializable]
    private class ErrorException : Exception
    {
      internal ErrorException()
      {
      }

      protected ErrorException(SerializationInfo i, StreamingContext c)
        : base(i, c)
      {
      }
    }
  }
}
