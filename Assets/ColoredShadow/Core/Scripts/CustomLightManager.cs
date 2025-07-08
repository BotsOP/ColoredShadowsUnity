using System;
using System.Collections.Generic;
using ColoredShadows.Scripts;
using UnityEngine;

namespace ColoredShadow.Core.Scripts
{
    public static class CustomLightManager
    {
        private const int MAX_SIZE = 16320;
        public static int CustomLightCount => _customLights.Count;
        private static List<CustomLight> _customLights;
        private static Vector2Int _shadowAtlasesSize;
        private static RectanglePacker rectanglePacker;

        static CustomLightManager()
        {
            _customLights = new List<CustomLight>();
            rectanglePacker = new RectanglePacker();
        }

        public static CustomLight GetCustomLight(int index)
        {
            return _customLights[index];
        }

        public static Vector2Int GetShadowAtlasSize()
        {
            return _shadowAtlasesSize;
        }

        public static void AddCustomLight(CustomLight customLight)
        {
            _customLights.Add(customLight);
            RefreshShadowAtlas();
        }

        public static void RemoveCustomLight(CustomLight customLight)
        {
            _customLights.Remove(customLight);
            RefreshShadowAtlas();
        }

        public static void RefreshShadowAtlas()
        {
            _customLights.Sort((a, b) => b.TextureSurfaceArea.CompareTo(a.TextureSurfaceArea));
            // rectanglePacker.PackRectangles(_customLights, out int width, out int height);
            // _shadowAtlasesSize = new Vector2Int(width, height);
            // return;
            Queue<CustomLight> customLightsQueue = new Queue<CustomLight>(_customLights);
            
            Heap<Square> availableSpaces = new Heap<Square>(HeapType.MaxHeap);
            availableSpaces.Insert(new Square(MAX_SIZE, MAX_SIZE, 0, 0));

            Queue<CustomLight> remainingLights = new Queue<CustomLight>();

            int maxWidth = 0;
            int maxHeight = 0;
            while (availableSpaces.Count > 0 && customLightsQueue.Count > 0)
            {
                CustomLight customLight = customLightsQueue.Dequeue();
                bool foundSpot = false;
                for (int j = 0; j < availableSpaces.Count; j++)
                {
                    if (!Fits(customLight, availableSpaces.items[j]))
                        continue;

                    foundSpot = true;
                    Square square = availableSpaces.Pop();
                    Debug.Log($"{square.minX} {square.minY}");
                    
                    Square square1 = new Square(square.width, square.height - customLight.TextureHeight, square.minX, square.minY + customLight.TextureHeight);
                    if(square1.width > 0 && square1.height > 0)
                        availableSpaces.Insert(square1);
                    
                    Square square2 = new Square(square.width - customLight.TextureWidth, customLight.TextureHeight, square.minX + customLight.TextureWidth, square.minY);
                    if(square2.width > 0 && square2.height > 0)
                        availableSpaces.Insert(square2);
                    
                    customLight.shadowAtlasPosX = square.minX;
                    customLight.shadowAtlasPosY = square.minY;

                    if (square.minX + customLight.TextureWidth > maxWidth)
                        maxWidth = square.minX + customLight.TextureWidth;
                    if (square.minY + customLight.TextureHeight > maxHeight)
                        maxHeight = square.minY + customLight.TextureHeight;
                    
                    break;
                }
                
                if(!foundSpot)
                    remainingLights.Enqueue(customLight);
                
                if(customLightsQueue.Count <= 0)
                    break;
            }

            _shadowAtlasesSize = new Vector2Int(maxWidth, maxHeight);
            
            foreach (CustomLight remainingLight in remainingLights)
            {
                Debug.Log($"Couldnt find space for {remainingLight.gameObject.name}");
            }
        }

        private static bool Fits(CustomLight customLight, Square square)
        {
            return customLight.TextureWidth <= square.width && customLight.TextureHeight <= square.height;
        }
        
        private struct Square : IComparable<Square>
        {
            public int width;
            public int height;
            public int minX;
            public int minY;
            // private int SurfaceArea => width * height;
            private float DistBottomLeftCorner => Vector2Int.Distance(Vector2Int.zero, new Vector2Int(minX, minY));
            public Square(int width, int height, int minX, int minY)
            {
                this.width = width;
                this.height = height;
                this.minX = minX;
                this.minY = minY;
            }
            public int CompareTo(Square other)
            {
                Debug.Log($"1: min pos: {minX} {minY} dist: {DistBottomLeftCorner} 2: min pos: {other.minX} {other.minY} dist:  {other.DistBottomLeftCorner}");
                return DistBottomLeftCorner.CompareTo(other.DistBottomLeftCorner);
            }
        }
    }
    
    public enum HeapType { MinHeap, MaxHeap }
    public class Heap<T> where T : IComparable<T>
    {
        public List<T> items = new List<T>();
        private readonly HeapType type;

        public T Peek => items.Count > 0 ? items[0] : throw new InvalidOperationException("Heap is empty.");
        public int Count => items.Count;

        public Heap(HeapType type = HeapType.MinHeap)
        {
            this.type = type;
        }

        public void Insert(T item)
        {
            items.Add(item);
            int i = items.Count - 1;
            bool isMax = (type == HeapType.MaxHeap);
            while (i > 0)
            {
                int p = (i - 1) / 2;
                if ((items[i].CompareTo(items[p]) > 0) ^ isMax)
                {
                    (items[i], items[p]) = (items[p], items[i]);
                    i = p;
                }
                else break;
            }
        }

        public T Pop()
        {
            if (items.Count == 0) throw new InvalidOperationException("Heap is empty.");
            T root = items[0];
            int last = items.Count - 1;
            items[0] = items[last];
            items.RemoveAt(last);
            int idx = 0;
            bool isMax = (type == HeapType.MaxHeap);
            while (true)
            {
                int l = 2 * idx + 1, r = 2 * idx + 2, target = idx;
                if (l < items.Count && ((items[l].CompareTo(items[target]) > 0) ^ isMax))
                    target = l;
                if (r < items.Count && ((items[r].CompareTo(items[target]) > 0) ^ isMax))
                    target = r;
                if (target == idx) break;
                (items[idx], items[target]) = (items[target], items[idx]);
                idx = target;
            }
            return root;
        }
    }
}

public class RectanglePacker
{
    private struct Shelf
    {
        public int y;
        public int height;
        public int usedWidth;
        public int availableWidth;
        
        public Shelf(int y, int height, int totalWidth)
        {
            this.y = y;
            this.height = height;
            this.usedWidth = 0;
            this.availableWidth = totalWidth;
        }
    }

    private List<Shelf> shelves;
    private int atlasWidth;
    private int atlasHeight;
    private int currentHeight;

    public RectanglePacker()
    {
        shelves = new List<Shelf>(50); // Pre-allocate for performance
    }

    /// <summary>
    /// Packs rectangles into the smallest possible atlas size
    /// </summary>
    /// <param name="rectangles">Pre-sorted rectangles from largest to smallest surface area</param>
    /// <param name="finalAtlasWidth">Output: final atlas width</param>
    /// <param name="finalAtlasHeight">Output: final atlas height</param>
    /// <returns>True if packing was successful</returns>
    public bool PackRectangles(List<CustomLight> rectangles, out int finalAtlasWidth, out int finalAtlasHeight)
    {
        if (rectangles == null || rectangles.Count == 0)
        {
            finalAtlasWidth = 0;
            finalAtlasHeight = 0;
            return true;
        }

        // Find the minimum possible atlas width based on the largest rectangle
        int minWidth = GetMinimumAtlasWidth(rectangles);
        
        // Try progressively larger atlas sizes until we find one that works
        for (int tryWidth = minWidth; tryWidth <= 4096; tryWidth = GetNextPowerOfTwo(tryWidth))
        {
            if (TryPackWithWidth(rectangles, tryWidth, out int height))
            {
                finalAtlasWidth = tryWidth;
                finalAtlasHeight = GetNextPowerOfTwo(height);
                return true;
            }
        }

        finalAtlasWidth = 0;
        finalAtlasHeight = 0;
        return false;
    }

    private bool TryPackWithWidth(List<CustomLight> rectangles, int width, out int height)
    {
        // Reset state
        shelves.Clear();
        atlasWidth = width;
        currentHeight = 0;

        // Try to pack all rectangles
        for (int i = 0; i < rectangles.Count; i++)
        {
            CustomLight rect = rectangles[i];
            if (!PackRectangle(rect))
            {
                height = 0;
                return false;
            }
        }

        height = currentHeight;
        return true;
    }

    private bool PackRectangle(CustomLight rect)
    {
        int rectWidth = rect.TextureWidth;
        int rectHeight = rect.TextureHeight;

        // Try to fit on existing shelves first
        for (int i = 0; i < shelves.Count; i++)
        {
            Shelf shelf = shelves[i];
            
            // Check if rectangle fits on this shelf
            if (rectWidth <= shelf.availableWidth && rectHeight <= shelf.height)
            {
                // Place rectangle on this shelf
                rect.shadowAtlasPosX = shelf.usedWidth;
                rect.shadowAtlasPosY = shelf.y;
                
                // Update shelf
                shelf.usedWidth += rectWidth;
                shelf.availableWidth -= rectWidth;
                shelves[i] = shelf;
                
                return true;
            }
        }

        // Need to create a new shelf
        if (rectWidth > atlasWidth)
        {
            return false; // Rectangle too wide
        }

        // Create new shelf
        Shelf newShelf = new Shelf(currentHeight, rectHeight, atlasWidth);
        
        // Place rectangle on new shelf
        rect.shadowAtlasPosX = 0;
        rect.shadowAtlasPosY = currentHeight;
        
        // Update shelf
        newShelf.usedWidth = rectWidth;
        newShelf.availableWidth = atlasWidth - rectWidth;
        
        shelves.Add(newShelf);
        currentHeight += rectHeight;
        
        return true;
    }

    private int GetMinimumAtlasWidth(List<CustomLight> rectangles)
    {
        int maxWidth = 0;
        long totalArea = 0;
        
        for (int i = 0; i < rectangles.Count; i++)
        {
            CustomLight rect = rectangles[i];
            if (rect.TextureWidth > maxWidth)
                maxWidth = rect.TextureWidth;
            totalArea += (long)rect.TextureWidth * rect.TextureHeight;
        }
        
        // Atlas width must be at least as wide as the widest rectangle
        // and should accommodate the total area
        int minWidthFromArea = Mathf.CeilToInt(Mathf.Sqrt(totalArea));
        return Mathf.Max(maxWidth, minWidthFromArea);
    }

    private int GetNextPowerOfTwo(int value)
    {
        if (value <= 0) return 1;
        
        value--;
        value |= value >> 1;
        value |= value >> 2;
        value |= value >> 4;
        value |= value >> 8;
        value |= value >> 16;
        value++;
        
        return value;
    }
}