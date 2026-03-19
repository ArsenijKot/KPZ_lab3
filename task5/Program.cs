using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LightHTMLDemo
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

    public abstract class LightNode
    {
        public abstract string OuterHTML();
        public abstract string InnerHTML();
        public abstract void Print(int indent = 0);
    }

    public class LightTextNode : LightNode
    {
        public string Text { get; }

        public LightTextNode(string text)
        {
            Text = text;
        }

        public override string OuterHTML()
        {
            return Text;
        }

        public override string InnerHTML()
        {
            return Text;
        }

        public override void Print(int indent = 0)
        {
            Console.WriteLine($"{new string(' ', indent)}{Text}");
        }
    }

    public class LightElementNode : LightNode
    {
        private readonly List<LightNode> _children = new List<LightNode>();
        private readonly List<string> _cssClasses = new List<string>();

        public string TagName { get; }
        public DisplayType DisplayType { get; }
        public ClosingType ClosingType { get; }

        public IReadOnlyList<string> CssClasses => _cssClasses;
        public int ChildrenCount => _children.Count;

        public LightElementNode(string tagName, DisplayType displayType, ClosingType closingType)
        {
            TagName = tagName;
            DisplayType = displayType;
            ClosingType = closingType;
        }

        public void AddChild(LightNode child)
        {
            _children.Add(child);
        }

        public void AddCssClass(string className)
        {
            if (!string.IsNullOrWhiteSpace(className))
            {
                _cssClasses.Add(className);
            }
        }

        public override string OuterHTML()
        {
            string classAttr = _cssClasses.Count > 0
                ? $" class=\"{string.Join(" ", _cssClasses)}\""
                : string.Empty;

            if (ClosingType == ClosingType.SelfClosing)
            {
                return $"<{TagName}{classAttr} />";
            }

            return $"<{TagName}{classAttr}>{InnerHTML()}</{TagName}>";
        }

        public override string InnerHTML()
        {
            var sb = new StringBuilder();

            foreach (var child in _children)
            {
                sb.Append(child.OuterHTML());
            }

            return sb.ToString();
        }

        public override void Print(int indent = 0)
        {
            string spaces = new string(' ', indent);

            if (ClosingType == ClosingType.SelfClosing)
            {
                Console.WriteLine($"{spaces}{OuterHTML()}");
                return;
            }

            Console.WriteLine($"{spaces}<{TagName}{GetClassAttribute()}>");

            foreach (var child in _children)
            {
                child.Print(indent + 2);
            }

            Console.WriteLine($"{spaces}</{TagName}>");
        }

        private string GetClassAttribute()
        {
            return _cssClasses.Count > 0
                ? $" class=\"{string.Join(" ", _cssClasses)}\""
                : string.Empty;
        }

        public void ShowInfo()
        {
            Console.WriteLine($"Tag name: {TagName}");
            Console.WriteLine($"Display type: {DisplayType}");
            Console.WriteLine($"Closing type: {ClosingType}");
            Console.WriteLine($"CSS classes: {(CssClasses.Count > 0 ? string.Join(", ", CssClasses) : "none")}");
            Console.WriteLine($"Children count: {ChildrenCount}");
            Console.WriteLine($"InnerHTML: {InnerHTML()}");
            Console.WriteLine($"OuterHTML: {OuterHTML()}");
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var page = new LightElementNode("div", DisplayType.Block, ClosingType.WithClosingTag);
            page.AddCssClass("container");

            var title = new LightElementNode("h1", DisplayType.Block, ClosingType.WithClosingTag);
            title.AddChild(new LightTextNode("LightHTML Demo"));

            var list = new LightElementNode("ul", DisplayType.Block, ClosingType.WithClosingTag);
            list.AddCssClass("menu");

            var item1 = new LightElementNode("li", DisplayType.Block, ClosingType.WithClosingTag);
            item1.AddChild(new LightTextNode("Home"));

            var item2 = new LightElementNode("li", DisplayType.Block, ClosingType.WithClosingTag);
            item2.AddChild(new LightTextNode("About"));

            var item3 = new LightElementNode("li", DisplayType.Block, ClosingType.WithClosingTag);
            item3.AddChild(new LightTextNode("Contacts"));

            list.AddChild(item1);
            list.AddChild(item2);
            list.AddChild(item3);

            var image = new LightElementNode("img", DisplayType.Inline, ClosingType.SelfClosing);
            image.AddCssClass("logo");

            page.AddChild(title);
            page.AddChild(list);
            page.AddChild(image);

            Console.WriteLine("=== Tree output ===");
            page.Print();

            Console.WriteLine();
            Console.WriteLine("=== Root element info ===");
            page.ShowInfo();

            Console.WriteLine();
            Console.WriteLine("=== Full OuterHTML ===");
            Console.WriteLine(page.OuterHTML());
        }
    }
}