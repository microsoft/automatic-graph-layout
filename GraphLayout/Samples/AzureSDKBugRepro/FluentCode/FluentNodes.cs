using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FluentVisualizer.Models
{
    [JsonConverter(typeof(FluentNodeConverter))]
    public abstract class FluentNode
    {
        private class FluentNodeConverter : JsonCreationConverter<FluentNode>
        {
            protected override FluentNode Create(Type objectType,
              Newtonsoft.Json.Linq.JObject jObject)
            {
                if ("interface".Equals(jObject.Value<string>("Type")))
                {
                    return new FluentInterface("default", "default");
                }
                return new FluentMethod("default", "default", "default");
            }
        }

        public string Id { get; set; }

        public ISet<string> ParentsIds { get; set; }

        public ISet<string> ChildrenIds { get; set; }

        public FluentNode(string id)
        {
            this.Id = id;
            this.ParentsIds = new HashSet<string>();
            this.ChildrenIds = new HashSet<string>();
        }

    }

    public abstract class FluentUINode : FluentNode
    {
        public string Name { get; set; }

        public string Documentation { get; set; }

        public double FX { get; set; }

        public double FY { get; set; }

        public abstract string Type { get; }

        private readonly string docLink = "<br /><a href=\"https://github.com/Azure/azure-sdk-for-java#azure-management-libraries-for-java\" target=\"_blank\">See Java Docs</a>";

        public FluentUINode(string id, string name, string description)
            : base(id)
        {
            this.Name = name;
            this.Documentation = ((string.IsNullOrWhiteSpace(description)) ? "[No Documentation Found]" : description) + this.docLink;

        }
    }

    public class FluentInterface : FluentUINode
    {
        public override string Type { get { return "interface"; } }

        public FluentInterface(string id, string name)
            : this(id, name, null)
        {

        }

        public FluentInterface(string id, string name, string description)
            : base(id, name, description)
        {
        }
    }

    public class FluentMethod : FluentUINode
    {
        public override string Type { get { return "method"; } }

        public IList<MethodParameter> Parameters { get; }

        public IList<FluentMethod> Overloads { get; }

        public FluentMethod(string id, string name, string description)
            :base (id, name, description)
        {
            this.Parameters = new List<MethodParameter>();
            this.Overloads = new List<FluentMethod>();
        }
        

        public void AddParameter(string name, string type)
        {
            this.Parameters.Add(new MethodParameter(name, type));
        }

        public bool IsCycled
        {
            get
            {
                if (this.ParentsIds.Count == 1 && this.ChildrenIds.Count == 1 &&
                    this.ParentsIds.ElementAt(0).Equals(this.ChildrenIds.ElementAt(0), StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                return false;
            }
        }
    }
}
