using System;
using System.Linq;
using System.Collections.Generic;

namespace FluentVisualizer.Models
{
    public class FluentDataPoints
    {
        public string Interface { get; set; }

        public IList<Method> Methods { get; set; }
    }


    public class Method
    {
        public string Name { get; set; }
        public IList<MethodParameter> Parameters { get; set; }

        public string ReturnType { get; set; }

        public string DefiningType { get; set; }

        public string Description { get; set; }
    }

    public class MethodParameter
    {
        public string Name { get; set; }
        public string Type { get; set; }

        public MethodParameter(string name, string type)
        {
            this.Name = name;
            this.Type = type;
        }
    }
}
