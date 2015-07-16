using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Windows;

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

        public static IEnumerable<IEnumerable<T>> CartesianProduct<T>(this IEnumerable<T> source, int size)
        {
            if (size < 1) yield break;
            var enumerators = new IEnumerator<T>[size];
            for (var i = 0; i < size; ++i)
            {
                enumerators[i] = source.GetEnumerator();
                enumerators[i].MoveNext();
            }
            for (var i = 0; true; ++i)
            {
                if (i == size)
                    i = 0;

                yield return enumerators.Select(e => e.Current);
                if (!enumerators[i].MoveNext())
                    yield break;
            }
        }

        public static IEnumerable<T> Difference<T>(this IEnumerable<T> source, IEnumerable<T> other)
        {
            return from item in source
                   where !other.Contains(item)
                   select item;
        } 

        public static string GetString(this IEnumerable<Case> source)
        {
            return source.Aggregate("", (c, n) => c + string.Format("({1},{2}) -> {0};", (n.Resolved ?  n.Value.ToString() : n.Hypotheses.Aggregate("",(cur,nex) => cur+ ","+nex)),n.X,n.Y));
        }
        
        public static IEnumerable<IEnumerable<T>>  GetCombination<T>(this IEnumerable<T> source)
        {
            var values = source.ToList();
            var count = (int) Math.Pow(2, values.Count);
            for (var i = 1; i <= count - 1; i++)
            {
                var addMap = Convert
                                .ToString(i, 2)
                                .PadLeft(values.Count, '0')
                                .Select(c => c =='1')
                                .ToArray();
                var combination = new List<T>();

                for (var j = 0; j < addMap.Length; j++)
                    if (addMap[j])
                        combination.Add(values[j]);
                
                yield return combination;
            }
        }

        public static IEnumerable<T> Rely<T>(this IEnumerable<T> source, T toPut)
        {
            var i = source.GetEnumerator();
            var ok = i.MoveNext();
            while (ok)
            {
                yield return i.Current;
                ok = i.MoveNext();
                if (ok) {
                    yield return toPut;                    
                }
            }
        }

        public static IEnumerable<T> SelfEnum<T>(this T source) {
            yield return source;
        }
    }
}
