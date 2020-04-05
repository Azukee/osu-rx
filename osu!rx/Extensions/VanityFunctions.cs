using System.Collections.Generic;

namespace osu_rx.Extensions
{
    public static class VanityFunctions
    {
        /// <summary>
        /// Safe copies a List, and doesn't reference the same object
        /// </summary>
        /// <param name="original"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static List<T> CopyList<T>(List<T> original)
        {
            List<T> newList = new List<T>();
            foreach(var item in original) 
                newList.Add(item);
            
            return newList;
        }
    }
}