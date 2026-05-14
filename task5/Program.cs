using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
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
            if (string.IsNullOrWhiteSpace(eventName) || handler == null)
                return;

            if (!_eventListeners.TryGetValue(eventName, out var list))
            {
                list = new List<Action<LightEvent>>();
                _eventListeners[eventName] = list;
            }

            list.Add(handler);
        }

        public bool RemoveEventListener(string eventName, Action<LightEvent> handler)
        {
            if (string.IsNullOrWhiteSpace(eventName) || handler == null)
                return false;

            if (_eventListeners.TryGetValue(eventName, out var list))
            {
                bool removed = list.Remove(handler);

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
            if (string.IsNullOrWhiteSpace(eventName))
                return;

            var evt = new LightEvent(eventName, this, data);

            if (_eventListeners.TryGetValue(eventName, out var list))
            {
                var handlers = list.ToArray();

                foreach (var handler in handlers)
                {
                    try
                    {
                        handler.Invoke(evt);
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
    }

    // =========================
    // STRATEGY
    // =========================

    public interface IImageLoadStrategy
    {
        byte[] Load(string href);
        string Name { get; }
    }

    public class FileSystemImageLoadStrategy : IImageLoadStrategy
    {
        public string Name => "File system";

        public byte[] Load(string href)
        {
            if (!File.Exists(href))
            {
                throw new FileNotFoundException("Image file not found.", href);
            }

            return File.ReadAllBytes(href);
        }
    }

    public class NetworkImageLoadStrategy : IImageLoadStrategy
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        public string Name => "Network";

        public byte[] Load(string href)
        {
            if (!Uri.TryCreate(href, UriKind.Absolute, out var uri))
            {
                throw new ArgumentException("Invalid URL.", nameof(href));
            }

            return _httpClient.GetByteArrayAsync(uri).GetAwaiter().GetResult();
        }
    }

    public static class ImageLoadStrategyFactory
    {
        public static IImageLoadStrategy Create(string href)
        {
            if (Uri.TryCreate(href, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttp ||
                 uri.Scheme == Uri.UriSchemeHttps))
            {
                return new NetworkImageLoadStrategy();
            }

            return new FileSystemImageLoadStrategy();
        }
    }

    // =========================
    // IMAGE NODE
    // =========================

    public class LightImageNode : LightElementNode
    {
        public string Href { get; }
        public string? Alt { get; }

        public byte[]? ImageData { get; private set; }
        public string? LoadSource { get; private set; }
        public string? LoadError { get; private set; }

        public bool IsLoaded => ImageData != null;

        public LightImageNode(string href, string? alt = null)
            : base("img", DisplayType.Inline, ClosingType.SelfClosing)
        {
            Href = href;
            Alt = alt;
        }

        public void Load()
        {
            try
            {
                var strategy = ImageLoadStrategyFactory.Create(Href);

                LoadSource = strategy.Name;
                ImageData = strategy.Load(Href);

                LoadError = null;
            }
            catch (Exception ex)
            {
                ImageData = null;
                LoadError = ex.Message;
            }
        }

        public override string OuterHTML()
        {
            string classAttr = CssClasses.Count > 0
                ? $" class=\"{string.Join(" ", CssClasses)}\""
                : string.Empty;

            string altAttr = !string.IsNullOrWhiteSpace(Alt)
                ? $" alt=\"{Alt}\""
                : string.Empty;

            return $"<img src=\"{Href}\"{altAttr}{classAttr} />";
        }

        public override string InnerHTML()
        {
            return string.Empty;
        }

        public override void Print(int indent = 0)
        {
            string spaces = new string(' ', indent);

            Console.WriteLine($"{spaces}{OuterHTML()}");

            if (IsLoaded)
            {
                Console.WriteLine($"{spaces}  [loaded from: {LoadSource}, bytes: {ImageData!.Length}]");
            }
            else if (LoadError != null)
            {
                Console.WriteLine($"{spaces}  [load error: {LoadError}]");
            }
        }

        public void ShowImageInfo()
        {
            Console.WriteLine($"Tag name: {TagName}");
            Console.WriteLine($"Href: {Href}");
            Console.WriteLine($"Alt: {(string.IsNullOrWhiteSpace(Alt) ? "none" : Alt)}");
            Console.WriteLine($"Loaded: {IsLoaded}");
            Console.WriteLine($"Load source: {(LoadSource ?? "not loaded")}");
            Console.WriteLine($"Bytes: {(ImageData?.Length.ToString() ?? "0")}");
            Console.WriteLine($"Error: {(LoadError ?? "none")}");
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;

            var page = new LightElementNode(
                "div",
                DisplayType.Block,
                ClosingType.WithClosingTag
            );

            page.AddCssClass("container");

            var title = new LightElementNode(
                "h1",
                DisplayType.Block,
                ClosingType.WithClosingTag
            );

            title.AddChild(new LightTextNode("LightHTML Demo"));

            var list = new LightElementNode(
                "ul",
                DisplayType.Block,
                ClosingType.WithClosingTag
            );

            list.AddCssClass("menu");

            var item1 = new LightElementNode(
                "li",
                DisplayType.Block,
                ClosingType.WithClosingTag
            );

            item1.AddChild(new LightTextNode("Home"));

            var item2 = new LightElementNode(
                "li",
                DisplayType.Block,
                ClosingType.WithClosingTag
            );

            item2.AddChild(new LightTextNode("About"));

            var item3 = new LightElementNode(
                "li",
                DisplayType.Block,
                ClosingType.WithClosingTag
            );

            item3.AddChild(new LightTextNode("Contacts"));

            list.AddChild(item1);
            list.AddChild(item2);
            list.AddChild(item3);

            // ====================================
            // ЛОКАЛЬНА КАРТИНКА
            // ====================================

            string localImagePath = Path.Combine(
                AppContext.BaseDirectory,
                "local-demo.png"
            );

            // Маленька PNG-картинка 1x1
            File.WriteAllBytes(
                localImagePath,
                Convert.FromBase64String(
                    "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO5L9XQAAAAASUVORK5CYII="
                )
            );

            var localImage = new LightImageNode(
                localImagePath,
                "Local image"
            );

            localImage.AddCssClass("logo");

            localImage.Load();

            // ====================================
            // КАРТИНКА З МЕРЕЖІ
            // ====================================

            var remoteImage = new LightImageNode(
                "https://httpbin.org/image/png",
                "Remote image"
            );

            remoteImage.AddCssClass("logo");

            remoteImage.Load();

            page.AddChild(title);
            page.AddChild(list);
            page.AddChild(localImage);
            page.AddChild(remoteImage);

            // ====================================
            // EVENTS
            // ====================================

            item1.AddEventListener("click", evt =>
            {
                Console.WriteLine(
                    $"[Event] '{evt.Type}' on <{evt.Target.TagName}> with text: '{evt.Target.InnerHTML()}'"
                );
            });

            item2.AddEventListener("click", evt =>
            {
                Console.WriteLine(
                    $"[Event] '{evt.Type}' on <{evt.Target.TagName}> — opening About page."
                );
            });

            list.AddEventListener("mouseover", evt =>
            {
                Console.WriteLine(
                    $"[Event] '{evt.Type}' on <{evt.Target.TagName}> — highlighting menu."
                );
            });

            localImage.AddEventListener("click", evt =>
            {
                Console.WriteLine(
                    $"[Event] '{evt.Type}' on local image."
                );
            });

            remoteImage.AddEventListener("click", evt =>
            {
                Console.WriteLine(
                    $"[Event] '{evt.Type}' on remote image."
                );
            });

            // ====================================
            // OUTPUT
            // ====================================

            Console.WriteLine("=== Tree output ===");
            page.Print();

            Console.WriteLine();

            Console.WriteLine("=== Local image info ===");
            localImage.ShowImageInfo();

            Console.WriteLine();

            Console.WriteLine("=== Remote image info ===");
            remoteImage.ShowImageInfo();

            Console.WriteLine();

            Console.WriteLine("=== Full OuterHTML ===");
            Console.WriteLine(page.OuterHTML());

            Console.WriteLine();

            Console.WriteLine("=== Simulated events ===");

            Console.WriteLine("-- Click on first menu item --");
            item1.DispatchEvent("click");

            Console.WriteLine();

            Console.WriteLine("-- Mouseover on list --");
            list.DispatchEvent("mouseover");

            Console.WriteLine();

            Console.WriteLine("-- Click on local image --");
            localImage.DispatchEvent("click", new
            {
                href = localImage.Href,
                timestamp = DateTime.UtcNow
            });

            Console.WriteLine();

            Console.WriteLine("-- Click on remote image --");
            remoteImage.DispatchEvent("click", new
            {
                href = remoteImage.Href,
                timestamp = DateTime.UtcNow
            });

            Console.WriteLine();

            Console.WriteLine("=== End of demo ===");
        }
    }
}
