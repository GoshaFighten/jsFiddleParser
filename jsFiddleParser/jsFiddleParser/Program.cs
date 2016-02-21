using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jsFiddleParser {
    class Program {
        private const string endFlag = "# end";
        static void Main(string[] args) {
            if (args.Length == 0)
                return;
            var path = args[0];
            var folder = Path.GetDirectoryName(path);
            string fileName = Path.GetFileNameWithoutExtension(path);
            HtmlDocument doc = new HtmlDocument();
            doc.Load(path);
            var detailsInfo = new DetailsInfo();
            detailsInfo.Name = GetName(doc);
            detailsInfo.Authors = GetAuthor(doc);
            detailsInfo.Resources = ProcessResources(doc);
            detailsInfo.Wrap = GetWrapMode(doc);
            var saveFolder = folder + "/" + fileName + "/";
            if (!Directory.Exists(saveFolder)) {
                Directory.CreateDirectory(saveFolder);
            }
            WriteDetails(detailsInfo, saveFolder, fileName);
            CreateHTML(doc, saveFolder, fileName);
            CreateJS(doc, saveFolder, fileName);
            CreateCSS(doc, saveFolder, fileName);
        }

        private static void CreateCSS(HtmlDocument doc, string folder, string name) {
            HtmlNode node = doc.DocumentNode.SelectSingleNode("//style[contains(text(), 'CSS')]");
            if (node == null)
                return;
            var fullPath = folder + name + ".css";
            string[] lines = node.InnerHtml.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            int endIndex = Array.FindIndex(lines, item => item.Contains(endFlag));
            File.WriteAllText(fullPath, string.Join("\r\n", lines.SubArray(1, endIndex - 1)));
        }

        private static void CreateJS(HtmlDocument doc, string folder, string name) {
            HtmlNode node = doc.DocumentNode.SelectSingleNode("//script[contains(text(), 'JavaScript')]");
            if (node == null)
                return;
            var fullPath = folder + name + ".js";
            string[] lines = node.InnerHtml.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            int endIndex = Array.FindIndex(lines, item => item.Contains(endFlag));
            File.WriteAllText(fullPath, string.Join("\r\n", lines.SubArray(1, endIndex - 1)));
        }

        private static void CreateHTML(HtmlDocument doc, string folder, string name) {
            HtmlNode node = doc.DocumentNode.SelectSingleNode("//body");
            if (node == null)
                return;
            var fullPath = folder + name + ".html";
            string[] lines = node.InnerHtml.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            File.WriteAllText(fullPath, string.Join("\r\n", lines));
        }

        private static void WriteDetails(DetailsInfo detailsInfo, string folder, string name) {
            var fullPath = folder + name + ".details";
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(string.Format("name: {0}", detailsInfo.Name));
            builder.AppendLine("authors:");
            builder.AppendLine(string.Format("  - {0}", detailsInfo.Authors));
            builder.AppendLine("resources:");
            foreach (string item in detailsInfo.Resources) {
                builder.AppendLine(string.Format("  - {0}", item));
            }
            builder.AppendLine(string.Format("wrap: {0}", detailsInfo.Wrap));
            File.WriteAllText(fullPath, builder.ToString());
        }

        private static string GetName(HtmlDocument doc) {
            HtmlNode node = doc.DocumentNode.SelectSingleNode("//title");
            if (node == null)
                return null;
            return node.InnerText;
        }

        private static string GetWrapMode(HtmlDocument doc) {
            HtmlNode node = doc.DocumentNode.SelectSingleNode("//script[contains(text(), 'JavaScript')]");
            if (node == null)
                return null;
            string searchString = "JavaScript:";
            string mode = node.InnerText.Substring(node.InnerText.IndexOf(searchString) + searchString.Length, 1);
            return mode;
        }

        private static List<string> ProcessResources(HtmlDocument doc) {
            HtmlNode node = doc.DocumentNode.SelectSingleNode("//comment()[contains(., 'Resources')]");
            if (node == null)
                return null;
            List<string> result = new List<string>();
            while (!node.NextSibling.InnerText.Contains(endFlag)) {
                node = node.NextSibling;
                if (node.Attributes["href"] != null) {
                    result.Add(node.Attributes["href"].Value);
                }
                if (node.Attributes["src"] != null) {
                    result.Add(node.Attributes["src"].Value);
                }
            }
            return result;
        }

        private static string GetAuthor(HtmlDocument doc) {
            HtmlNode mdnode = doc.DocumentNode.SelectSingleNode("//meta[@name='author']");
            if (mdnode != null) {
                HtmlAttribute desc;
                desc = mdnode.Attributes["content"];
                return desc.Value;
            }
            return string.Empty;
        }
    }

    public static class ArrayExtensions {
        public static T[] SubArray<T>(this T[] data, int index, int length) {
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }
    }
}
