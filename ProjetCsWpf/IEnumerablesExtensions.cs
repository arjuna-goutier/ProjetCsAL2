using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjetCsWpf
{
    public static class IEnumerablesExtensions
    {
        public delegate bool Condition<T>(T t);

        public static bool UniqueValues<T>(this IEnumerable<T> values, IEqualityComparer<T> comparer = null)
        {
            var set = new HashSet<T>(comparer);
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var item in values)
                if (!set.Add(item))
                    return false;
            return true;
        }

        public static ISet<T> ToSet<T>(this IEnumerable<T> source) {
            var set = new HashSet<T>();
            foreach (var element in source)
                set.Add(element);
            return set;
        }

        public static IEnumerable<IEnumerable<T>> SplitWhen<T>(this IEnumerable<T> source,Condition<T> condition) {
            var countToSkip = 0;
            var toReturn = new List<IEnumerable<T>>();
            while (countToSkip < source.Count()) {
                var value = source.Skip(countToSkip).TakeWhile((t) => !condition(t));
                if(value.Any())
                    toReturn.Add(value);
                countToSkip += value.Count() + 1;
            }
            return toReturn;
        }
        
        public static IEnumerable<IEnumerable<T>> SplitBy<T>(this IEnumerable<T> source, int span) {
            for (var countToSkip = 0; countToSkip < source.Count(); countToSkip += span)
                yield return source.Skip(countToSkip).Take(span);
        }

        public static IEnumerable<T> Sub<T>(this IEnumerable<T> source, int begin, int count) {
            return source.Skip(begin).Take(count);
        }

        public static T[][] ToArrayArray<T>(this IEnumerable<IEnumerable<T>> source) {
            return source.Select(line => line.ToArray()).ToArray();
        }

        public static string GetString(this IEnumerable<Case> source)
        {
            return source.Aggregate("", (c, n) => c + string.Format("({1},{2}) -> {0};", (n.Resolved ?  n.Value.ToString() : n.Hypotheses.Aggregate("",(cur,nex) => cur+ ","+nex)),n.X,n.Y));
        }
    }
}
