using System;

namespace BridgePatternShapes
{
    // Implementor
    public interface IRenderer
    {
        void Render(string shapeName);
    }

    // Concrete Implementor 1
    public class VectorRenderer : IRenderer
    {
        public void Render(string shapeName)
        {
            Console.WriteLine($"Drawing {shapeName} as vector graphics");
        }
    }

    // Concrete Implementor 2
    public class RasterRenderer : IRenderer
    {
        public void Render(string shapeName)
        {
            Console.WriteLine($"Drawing {shapeName} as pixels");
        }
    }

    // Abstraction
    public abstract class Shape
    {
        protected IRenderer renderer;

        protected Shape(IRenderer renderer)
        {
            this.renderer = renderer;
        }

        public abstract void Draw();
    }

    // Refined Abstraction: Circle
    public class Circle : Shape
    {
        public Circle(IRenderer renderer) : base(renderer) { }

        public override void Draw()
        {
            renderer.Render("Circle");
        }
    }

    // Refined Abstraction: Square
    public class Square : Shape
    {
        public Square(IRenderer renderer) : base(renderer) { }

        public override void Draw()
        {
            renderer.Render("Square");
        }
    }

    // Refined Abstraction: Triangle
    public class Triangle : Shape
    {
        public Triangle(IRenderer renderer) : base(renderer) { }

        public override void Draw()
        {
            renderer.Render("Triangle");
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            IRenderer vector = new VectorRenderer();
            IRenderer raster = new RasterRenderer();

            Shape circle = new Circle(vector);
            Shape square = new Square(raster);
            Shape triangle = new Triangle(raster);

            circle.Draw();    // Drawing Circle as vector graphics
            square.Draw();    // Drawing Square as pixels
            triangle.Draw(); // Drawing Triangle as pixels
        }
    }
}