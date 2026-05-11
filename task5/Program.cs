using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

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

    public class LightEvent
    {
        public string Type { get; }
        public LightElementNode Target { get; }
        public object? Data { get; }

        public LightEvent(string type, LightElementNode target, object? data = null)
        {
            Type = type;
            Target = target;
            Data = data;
        }
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

        private readonly Dictionary<string, List<Action<LightEvent>>> _eventListeners
            = new Dictionary<string, List<Action<LightEvent>>>(StringComparer.OrdinalIgnoreCase);

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

        public void AddEventListener(string eventName, Action<LightEvent> handler)
        {
            if (string.IsNullOrWhiteSpace(eventName) || handler == null) return;

            if (!_eventListeners.TryGetValue(eventName, out var list))
            {
                list = new List<Action<LightEvent>>();
                _eventListeners[eventName] = list;
            }

            list.Add(handler);
        }

        public bool RemoveEventListener(string eventName, Action<LightEvent> handler)
        {
            if (string.IsNullOrWhiteSpace(eventName) || handler == null) return false;

            if (_eventListeners.TryGetValue(eventName, out var list))
            {
                var removed = list.Remove(handler);
                if (list.Count == 0)
                {
                    _eventListeners.Remove(eventName);
                }
                return removed;
            }
            return false;
        }

        public void DispatchEvent(string eventName, object? data = null)
        {
            if (string.IsNullOrWhiteSpace(eventName)) return;

            var evt = new LightEvent(eventName, this, data);

            if (_eventListeners.TryGetValue(eventName, out var list))
            {
                var handlers = list.ToArray();
                foreach (var h in handlers)
                {
                    try
                    {
                        h.Invoke(evt);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Event handler error] {ex.Message}");
                    }
                }
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
            Console.WriteLine($"Registered events: {(_eventListeners.Count > 0 ? string.Join(", ", _eventListeners.Keys) : "none")}");
        }
        public IReadOnlyList<LightNode> GetChildren()
        {
            return _children.AsReadOnly();
        }
    }

    // =========================================
    // ITERATOR
    // =========================================

    public interface ILightNodeIterator
    {
        bool HasNext();
        LightNode Next();
    }

    // =========================================
    // DEPTH-FIRST ITERATOR (DFS)
    // =========================================

    public class DepthFirstIterator : ILightNodeIterator
    {
        private readonly Stack<LightNode> _stack = new Stack<LightNode>();

        public DepthFirstIterator(LightNode root)
        {
            _stack.Push(root);
        }

        public bool HasNext()
        {
            return _stack.Count > 0;
        }

        public LightNode Next()
        {
            if (!HasNext())
            {
                throw new InvalidOperationException("No more elements.");
            }

            var current = _stack.Pop();

            if (current is LightElementNode element)
            {
                var childrenField = typeof(LightElementNode)
                    .GetField("_children",
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Instance);

                var children = (List<LightNode>)childrenField.GetValue(element);

                for (int i = children.Count - 1; i >= 0; i--)
                {
                    _stack.Push(children[i]);
                }
            }

            return current;
        }
    }

    // =========================================
    // BREADTH-FIRST ITERATOR (BFS)
    // =========================================

    public class BreadthFirstIterator : ILightNodeIterator
    {
        private readonly Queue<LightNode> _queue = new Queue<LightNode>();

        public BreadthFirstIterator(LightNode root)
        {
            _queue.Enqueue(root);
        }

        public bool HasNext()
        {
            return _queue.Count > 0;
        }

        public LightNode Next()
        {
            if (!HasNext())
            {
                throw new InvalidOperationException("No more elements.");
            }

            var current = _queue.Dequeue();

            if (current is LightElementNode element)
            {
                var children = element.GetChildren();

                foreach (var child in children)
                {
                    _queue.Enqueue(child);
                }
            }

            return current;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;

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

            item1.AddEventListener("click", evt =>
            {
                Console.WriteLine($"[Event] '{evt.Type}' на елементі <{evt.Target.TagName}> з текстом: '{evt.Target.InnerHTML()}'");
            });

            item2.AddEventListener("click", evt =>
            {
                Console.WriteLine($"[Event] '{evt.Type}' на <{evt.Target.TagName}> — відкриваємо сторінку 'About'.");
            });

            list.AddEventListener("mouseover", evt =>
            {
                Console.WriteLine($"[Event] '{evt.Type}' на <{evt.Target.TagName}> — підсвічуємо меню.");
            });

            image.AddEventListener("click", evt =>
            {
                Console.WriteLine($"[Event] '{evt.Type}' на <{evt.Target.TagName}> — логотип натиснуто. Дані: {evt.Data ?? "none"}");
            });

            Console.WriteLine("=== Tree output ===");
            page.Print();

            Console.WriteLine();
            Console.WriteLine("=== Root element info ===");
            page.ShowInfo();

            Console.WriteLine();
            Console.WriteLine("=== Full OuterHTML ===");
            Console.WriteLine(page.OuterHTML());

            Console.WriteLine();
            Console.WriteLine("=== Симуляція подій ===");

            Console.WriteLine("-- Клік по першому пункту меню --");
            item1.DispatchEvent("click");

            Console.WriteLine();

            Console.WriteLine("-- Mouseover на списку --");
            list.DispatchEvent("mouseover");

            Console.WriteLine();

            Console.WriteLine("-- Клік по логотипу з додатковими даними --");
            image.DispatchEvent("click", new { href = "/", timestamp = DateTime.UtcNow });

            Console.WriteLine();

            Console.WriteLine();
            Console.WriteLine("=== DFS traversal (Depth-First) ===");

            ILightNodeIterator dfsIterator = new DepthFirstIterator(page);

            while (dfsIterator.HasNext())
            {
                var node = dfsIterator.Next();

                if (node is LightElementNode element)
                {
                    Console.WriteLine($"Element: <{element.TagName}>");
                }
                else if (node is LightTextNode text)
                {
                    Console.WriteLine($"Text: {text.Text}");
                }
            }

            Console.WriteLine();

            Console.WriteLine("=== BFS traversal (Breadth-First) ===");

            ILightNodeIterator bfsIterator = new BreadthFirstIterator(page);

            while (bfsIterator.HasNext())
            {
                var node = bfsIterator.Next();

                if (node is LightElementNode element)
                {
                    Console.WriteLine($"Element: <{element.TagName}>");
                }
                else if (node is LightTextNode text)
                {
                    Console.WriteLine($"Text: {text.Text}");
                }
            }

            Console.WriteLine("=== Кінець демонстрації подій ===");
        }
    }
}
