using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TDMUtils
{
    public static class ObjectWrappers
    {
        public class DisplayItem<T>(T value, string display)
        {
            public T Value { get; set; } = value;
            public string Display { get; set; } = display;
            public override string ToString() => Display;
        }

        public static List<DisplayItem<T>> EnumDisplayItemList<T>() where T : struct, Enum =>
            [.. EnumerableUtilities.EnumAsArray<T>().Select(e => new DisplayItem<T>(e, e.GetDescription()))];

        public static List<DisplayItem<T>> ToDisplayItemList<T>(this IEnumerable<T> items, Func<T, string> displaySelector) =>
            [.. items.Select(item => new DisplayItem<T>(item, displaySelector(item)))];
    }
}
