﻿using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MiniCover
{
    public class HitTestMethod
    {
        private readonly object _lock = new object();

        internal HitTestMethod(MethodBase testMethod)
        {
            AssemblyName = testMethod.DeclaringType.Assembly.FullName;
            ClassName = testMethod.DeclaringType.Name;
            MethodName = testMethod.Name;
            AssemblyLocation = testMethod.DeclaringType.Assembly.Location;
            HitedInstructions = new Dictionary<int, int>();
        }

        [JsonConstructor]

        internal HitTestMethod(string assemblyName,
            string className,
            string methodName,
            string assemblyLocation,
            int counter, IDictionary<int, int> hitedInstructions)
        {
            AssemblyName = assemblyName;
            ClassName = className;
            MethodName = methodName;
            AssemblyLocation = assemblyLocation;
            Counter = counter;
            HitedInstructions = hitedInstructions;
        }

        public string AssemblyName { get; }
        public string ClassName { get; }
        public string MethodName { get; }
        public string AssemblyLocation { get; }
        public int Counter { get; private set; }
        public IDictionary<int, int> HitedInstructions { get; }

        public void HasHit(int id)
        {
            lock (_lock)
            {
                Counter++;
                if (this.HitedInstructions.TryGetValue(id, out var count))
                {
                    this.HitedInstructions[id] = count + 1;
                }
                else
                {
                    this.HitedInstructions[id] = 1;
                }
            }
        }

        public bool HasBeenHitBy(int instructionId)
        {
            return this.HitedInstructions.ContainsKey(instructionId);
        }

        public static IEnumerable<HitTestMethod> MergeDuplicates(IEnumerable<HitTestMethod> source, int instructionId)
        {
            return source
                .GroupBy(h => new { h.AssemblyName, h.ClassName, h.MethodName, h.AssemblyLocation })
                .Select(g => new HitTestMethod(
                    g.Key.AssemblyName,
                    g.Key.ClassName,
                    g.Key.MethodName,
                    g.Key.AssemblyLocation,
                    g.Sum(h => h.HitedInstructions[instructionId]),
                    new Dictionary<int, int>()
                )).ToArray();
        }
    }
}