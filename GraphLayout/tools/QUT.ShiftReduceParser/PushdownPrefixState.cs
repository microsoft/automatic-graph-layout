// Decompiled with JetBrains decompiler
// Type: QUT.Gppg.PushdownPrefixState`1
// Assembly: QUT.ShiftReduceParser, Version=1.4.1.0, Culture=neutral, PublicKeyToken=402396ef6102baec
// MVID: 454DBF7C-E638-4FF4-BE67-5E822302962D
// Assembly location: C:\Repos\automatic-graph-layout\GraphLayout\tools\Dot2Graph\gp\QUT.ShiftReduceParser.dll

using System;

namespace QUT.Gppg
{
  public class PushdownPrefixState<T>
  {
    private T[] array = new T[8];
    private int tos = 0;

    public T this[int index]
    {
      get
      {
        return this.array[index];
      }
    }

    public int Depth
    {
      get
      {
        return this.tos;
      }
    }

    internal void Push(T value)
    {
      if (this.tos >= this.array.Length)
      {
        T[] objArray = new T[this.array.Length * 2];
        Array.Copy((Array) this.array, (Array) objArray, this.tos);
        this.array = objArray;
      }
      this.array[this.tos++] = value;
    }

    internal T Pop()
    {
      T obj = this.array[--this.tos];
      this.array[this.tos] = default (T);
      return obj;
    }

    internal T TopElement()
    {
      return this.array[this.tos - 1];
    }

    internal bool IsEmpty()
    {
      return this.tos == 0;
    }
  }
}
