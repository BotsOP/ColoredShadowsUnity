using System;
using System.Collections.Generic;
using ColoredShadows.Scripts;
using UnityEngine;

namespace ColoredShadow.Core.Scripts
{
    public static class CustomLightManager
    {
        private const int MAX_SIZE = 16384;
        public static int CustomLightCount => _customLights.Count;
        private static List<CustomLight> _customLights;
        private static List<Vector2Int> _shadowAtlasesSize;

        static CustomLightManager()
        {
            _customLights = new List<CustomLight>();
            _shadowAtlasesSize = new List<Vector2Int>();
            _shadowAtlasesSize.Add(new Vector2Int(MAX_SIZE, MAX_SIZE));
        }

        public static CustomLight GetCustomLight(int index)
        {
            return _customLights[index];
        }

        public static Vector2Int GetShadowAtlasSize(int index)
        {
            return _shadowAtlasesSize[index];
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
            Queue<CustomLight> customLightsQueue = new Queue<CustomLight>(_customLights);
            
            Heap<Square> availableSpaces = new Heap<Square>();
            availableSpaces.Insert(new Square(MAX_SIZE, MAX_SIZE, 0, 0));

            Queue<CustomLight> remainingLights = new Queue<CustomLight>();

            for (int i = 0; i < 5; i++)
            {
                int maxWidth = 0;
                int maxHeight = 0;
                while (availableSpaces.Count > 0 && customLightsQueue.Count > 0)
                {
                    CustomLight customLight = customLightsQueue.Dequeue();
                    if (Fits(customLight, availableSpaces.Peek))
                    {
                        Square square = availableSpaces.Pop();
                        
                        Square square1 = new Square(square.width, square.height - customLight.TextureHeight, square.minX, square.minY + customLight.TextureHeight);
                        if(square1.width > 0 && square1.height > 0)
                            availableSpaces.Insert(square1);
                        
                        Square square2 = new Square(square.width - customLight.TextureWidth, customLight.TextureHeight, square.minX + customLight.TextureWidth, square.minY);
                        if(square2.width > 0 && square2.height > 0)
                            availableSpaces.Insert(square2);
                        
                        customLight.shadowAtlasPosX = square.minX;
                        customLight.shadowAtlasPosY = square.minY;
                        customLight.shadowAtlasIndex = i;

                        if (square.minX + customLight.TextureWidth > maxWidth)
                            maxWidth = square.minX + customLight.TextureWidth;
                        if (square.minY + customLight.TextureHeight > maxHeight)
                            maxHeight = square.minY + customLight.TextureHeight;
                    }
                    else
                    {
                        remainingLights.Enqueue(customLight);
                    }
                }
                
                if(_shadowAtlasesSize.Count <= i)
                    _shadowAtlasesSize.Add(new Vector2Int(maxWidth, maxHeight));
                _shadowAtlasesSize[i] = new Vector2Int(maxWidth, maxHeight);
                
                if(customLightsQueue.Count <= 0)
                    break;

                customLightsQueue = new Queue<CustomLight>(remainingLights);
                remainingLights.Clear();
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
            public int SurfaceArea => width * height;
            public Square(int width, int height, int minX, int minY)
            {
                this.width = width;
                this.height = height;
                this.minX = minX;
                this.minY = minY;
            }
            public int CompareTo(Square other)
            {
                return SurfaceArea.CompareTo(other.SurfaceArea);
            }
        }
    }
    
    public enum HeapType { MinHeap, MaxHeap }
    public class Heap<T> where T : IComparable<T>
    {
        private readonly List<T> items = new List<T>();
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
