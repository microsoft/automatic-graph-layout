using System.Collections.Generic;

namespace SvgLayerSample.Svg {
    public class ComponentNode : LabeledNode {
        public ComponentNode(string id, string title = null, string technology = null, string description = null) 
            : base(id, new List<string>()) {

            if (technology is null) technology = "[component]";

            this.Labels.Add(new SvgLabel(title ?? id));
            if (technology != null) {
                this.Labels.Add(new SvgLabel($"[{technology}]", new System.Drawing.Font("Verdana", 8f, System.Drawing.FontStyle.Bold)));
            }
            if (description != null) {
                var descriptionLabel = new SvgLabel(description);
                descriptionLabel.PaddingTop = 10;
                this.Labels.Add(descriptionLabel);
            }

            this.SvgElement.BackgroundColor = Utils.backgroundColor;
            this.SvgElement.BorderColor = Utils.borderColor;
            this.SvgElement.FontColor = Utils.fontColor;

        }
    }
}
