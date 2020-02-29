using System;
using System.Collections.Generic;

namespace osu_rx.Dependencies
{
    public class DependencyContainer
    {
        private static List<object> dependencies = new List<object>();

        public static void Cache<T>(T dependency) => dependencies.Add(dependency);

        public static T Get<T>()
        {
            var dependencyType = typeof(T);

            var dependency = dependencies.Find(d => d.GetType() == dependencyType);
            if (dependency != default)
                return (T)dependency;
            else if (dependency == null)
                throw new Exception($"Dependency of type {dependencyType} not cached!");
            else
                throw new Exception($"Dependency of type {dependencyType} not found!");
        }
    }
}
