using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Msagl.Drawing
{
    public class SerializableAttribute : Attribute
    {
    }
}

namespace System.Collections
{
    public class ArrayList : List<object>
    {
        public object[] ToArray(Type t)
        {
            return ToArray();
        }
    }

    public class Hashtable : Dictionary<object, object>
    {
    }
}