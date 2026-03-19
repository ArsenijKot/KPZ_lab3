using System;
using System.Collections.Generic;
using System.Text;

namespace LightHtmlFlyweightDemo
{
    public enum DisplayType
    {
        Block,
        Inline
    }

    public enum ClosingType
    {
        SelfClosing,
        WithClosingTag
    }

    public sealed class HtmlElementStyle
    {
        public string TagName { get; }
        public DisplayType DisplayType { get; }
        public ClosingType ClosingType { get; }
        public string[] CssClasses { get; }

        public HtmlElementStyle(string tagName, DisplayType displayType, ClosingType closingType, string[] cssClasses)
        {
            TagName = tagName;
            DisplayType = displayType;
            ClosingType = closingType;
            CssClasses = cssClasses ?? Array.Empty<string>();
        }
    }

    public sealed class HtmlElementStyleFactory
    {
        private readonly Dictionary<string, HtmlElementStyle> _cache = new Dictionary<string, HtmlElementStyle>();

        public HtmlElementStyle GetStyle(string tagName, DisplayType displayType, ClosingType closingType, params string[] cssClasses)
        {
            string key = $"{tagName}|{displayType}|{closingType}|{string.Join(".", cssClasses ?? Array.Empty<string>())}";

            if (!_cache.TryGetValue(key, out var style))
            {
                style = new HtmlElementStyle(tagName, displayType, closingType, cssClasses);
                _cache[key] = style;
            }

            return style;
        }

        public int CachedStylesCount => _cache.Count;
    }

    public abstract class LightNode
    {
        public abstract string OuterHTML();
        public abstract string InnerHTML();
        public abstract void Print(int indent = 0);
    }

    public sealed class LightTextNode : LightNode
    {
        public string Text { get; }

        public LightTextNode(string text)
        {
            Text = text;
        }

        public override string OuterHTML() => Text;
        public override string InnerHTML() => Text;

        public override void Print(int indent = 0)
        {
            Console.WriteLine(new string(' ', indent) + Text);
        }
    }

    public sealed class LightElementNode : LightNode
    {
        private readonly List<LightNode> _children = new List<LightNode>();

        public HtmlElementStyle Style { get; }

        public LightElementNode(HtmlElementStyle style)
        {
            Style = style;
        }

        public void AddChild(LightNode child)
        {
            _children.Add(child);
        }

        public int ChildrenCount => _children.Count;

        public override string InnerHTML()
        {
            var sb = new StringBuilder();
            foreach (var child in _children)
            {
                sb.Append(child.OuterHTML());
            }
            return sb.ToString();
        }

        public override string OuterHTML()
        {
            string classes = Style.CssClasses.Length > 0
                ? $" class=\"{string.Join(" ", Style.CssClasses)}\""
                : string.Empty;

            if (Style.ClosingType == ClosingType.SelfClosing)
            {
                return $"<{Style.TagName}{classes} />";
            }

            return $"<{Style.TagName}{classes}>{InnerHTML()}</{Style.TagName}>";
        }

        public override void Print(int indent = 0)
        {
            string spaces = new string(' ', indent);

            if (Style.ClosingType == ClosingType.SelfClosing)
            {
                Console.WriteLine(spaces + OuterHTML());
                return;
            }

            string classes = Style.CssClasses.Length > 0
                ? $" class=\"{string.Join(" ", Style.CssClasses)}\""
                : string.Empty;

            Console.WriteLine($"{spaces}<{Style.TagName}{classes}>");
            foreach (var child in _children)
            {
                child.Print(indent + 2);
            }
            Console.WriteLine($"{spaces}</{Style.TagName}>");
        }
    }

    public static class BookToHtmlConverter
    {
        public static LightElementNode Convert(string[] lines, HtmlElementStyleFactory factory)
        {
            var root = new LightElementNode(
                factory.GetStyle("div", DisplayType.Block, ClosingType.WithClosingTag, "book")
            );

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                LightElementNode element;

                if (i == 0)
                {
                    element = new LightElementNode(
                        factory.GetStyle("h1", DisplayType.Block, ClosingType.WithClosingTag, "title")
                    );
                }
                else if (!string.IsNullOrEmpty(line) && char.IsWhiteSpace(line[0]))
                {
                    element = new LightElementNode(
                        factory.GetStyle("blockquote", DisplayType.Block, ClosingType.WithClosingTag, "quote")
                    );
                }
                else if (line.Length < 20)
                {
                    element = new LightElementNode(
                        factory.GetStyle("h2", DisplayType.Block, ClosingType.WithClosingTag, "subtitle")
                    );
                }
                else
                {
                    element = new LightElementNode(
                        factory.GetStyle("p", DisplayType.Block, ClosingType.WithClosingTag, "paragraph")
                    );
                }

                element.AddChild(new LightTextNode(line));
                root.AddChild(element);
            }

            return root;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            string[] bookLines =
            {
                "The Little Prince",
                "A short line",
                "  Once when I was six years old",
                "The airplane landed in the desert after a long flight.",
                "  The fox said that what is essential is invisible to the eye.",
                "Chapter Two"
            };

            var factory = new HtmlElementStyleFactory();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long memoryBefore = GC.GetTotalMemory(true);

            LightElementNode document = BookToHtmlConverter.Convert(bookLines, factory);

            long memoryAfter = GC.GetTotalMemory(true);

            Console.WriteLine("=== HTML ===");
            document.Print();

            Console.WriteLine();
            Console.WriteLine("=== OuterHTML ===");
            Console.WriteLine(document.OuterHTML());

            Console.WriteLine();
            Console.WriteLine("=== Memory info ===");
            Console.WriteLine($"Managed memory before building tree: {memoryBefore:N0} bytes");
            Console.WriteLine($"Managed memory after building tree:  {memoryAfter:N0} bytes");
            Console.WriteLine($"Approximate memory used by tree:     {memoryAfter - memoryBefore:N0} bytes");

            Console.WriteLine();
            Console.WriteLine("=== Flyweight info ===");
            Console.WriteLine($"Unique shared styles in cache: {factory.CachedStylesCount}");

            GC.KeepAlive(document);
        }
    }
}