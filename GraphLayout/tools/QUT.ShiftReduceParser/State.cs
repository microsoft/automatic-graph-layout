// Decompiled with JetBrains decompiler
// Type: QUT.Gppg.State
// Assembly: QUT.ShiftReduceParser, Version=1.4.1.0, Culture=neutral, PublicKeyToken=402396ef6102baec
// MVID: 454DBF7C-E638-4FF4-BE67-5E822302962D
// Assembly location: C:\Repos\automatic-graph-layout\GraphLayout\tools\Dot2Graph\gp\QUT.ShiftReduceParser.dll

using System.Collections.Generic;

namespace QUT.Gppg
{
  public class State
  {
    public int number;
    internal Dictionary<int, int> ParserTable;
    internal Dictionary<int, int> Goto;
    internal int defaultAction;

    public State(int[] actions, int[] goToList)
      : this(actions)
    {
      this.Goto = new Dictionary<int, int>();
      for (int index = 0; index < goToList.Length; index += 2)
        this.Goto.Add(goToList[index], goToList[index + 1]);
    }

    public State(int[] actions)
    {
      this.ParserTable = new Dictionary<int, int>();
      for (int index = 0; index < actions.Length; index += 2)
        this.ParserTable.Add(actions[index], actions[index + 1]);
    }

    public State(int defaultAction)
    {
      this.defaultAction = defaultAction;
    }

    public State(int defaultAction, int[] goToList)
      : this(defaultAction)
    {
      this.Goto = new Dictionary<int, int>();
      for (int index = 0; index < goToList.Length; index += 2)
        this.Goto.Add(goToList[index], goToList[index + 1]);
    }
  }
}
