using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentHubConsole
{
    public static class DescriptionBuilder
    {
        // "<ul>\n\t<li>lion</li>\n\t<li>headshot</li>\n</ul>\n"
        public static string BuildBulletString(params string[] descriptionText)
        {
            var builder = new StringBuilder();
            builder.AppendLine("<ul>");
            Array.ForEach(descriptionText, d => builder.AppendLine($"\t<li>{d}</li>"));
            builder.AppendLine("</ul>");
            return builder.ToString();
        }
    }
}
