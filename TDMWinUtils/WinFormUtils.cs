using System.ComponentModel;
using System.Windows.Forms;
using TDMUtils;

namespace TDMWinUtils
{
    public static class WinFormUtils
    {
        public class ContainerItem(object? value, string display)
        {
            public object? Value { get; } = value;
            public string Display { get; } = display;
            public override string ToString() => Display;
            public static ContainerItem[] ToContainerList<T>(IEnumerable<T> items, Func<T, string> Display) =>
                [.. items.Select(i => new ContainerItem(i, Display(i)))];
            public static ContainerItem[] ToContainerList<T>(IEnumerable<T> items, Func<T, object> Tags, Func<T, string> Display) =>
                [.. items.Select(i => new ContainerItem(Tags(i), Display(i)))];
        }
        public static T? GetSelectedContainerItem<T>(this ComboBox comboBox)
        {
            if (comboBox.SelectedItem is ContainerItem containerItem && containerItem.Value is T value)
                return value;
            return default;
        }
        public static T? GetSelectedContainerItem<T>(this ListBox comboBox)
        {
            if (comboBox.SelectedItem is ContainerItem containerItem && containerItem.Value is T value)
                return value;
            return default;
        }
        public static void AddBindingSource(this ListControl control, IEnumerable<object> data)
        {
            var bs = new BindingSource { DataSource = data };
            control.DataSource = bs;
        }
        public static void RefreshListControl(this ListControl control)
        {
            if (control.DataSource is BindingSource bs)
            {
                bs.ResetBindings(false);
                return;
            }
            var dataSource = control.DataSource;
            control.DataSource = null;
            control.DataSource = dataSource; 
        }
        public static void SafeInvoke(this Control control, Action action)
        {
            if (control.InvokeRequired)
                control.Invoke(action);
            else
                action();
        }
        public static string GetDescription(this Enum value) =>
        value.GetType().GetField(value.ToString())?.GetCustomAttributes(typeof(DescriptionAttribute), false)
             .OfType<DescriptionAttribute>()
             .FirstOrDefault()?.Description ?? value.ToString();

        public static void AppendString(this RichTextBox rtb, params ColoredString[] coloredStrings)
        {
            rtb.AppendString(() =>
            {
                string rtf = ColoredString.BuildColoredStringsRtf(coloredStrings, rtb.ForeColor);
                rtb.SelectedRtf = rtf;
            });
        }
        public static void AppendString(this RichTextBox rtb, string text, Color? color = null)
        {
            rtb.AppendString(() =>
            {
                if (color is not null)
                    rtb.SelectionColor = color.Value;
                rtb.AppendText(text + Environment.NewLine);
                rtb.SelectionColor = rtb.ForeColor;
            });
        }
        public static void AppendString(this RichTextBox rtb, Action appendAction)
        {
            bool autoScroll = rtb.SelectionStart == rtb.TextLength;

            rtb.SelectionStart = rtb.TextLength;
            appendAction();

            if (autoScroll)
            {
                rtb.SelectionStart = rtb.TextLength;
                rtb.ScrollToCaret();
            }
        }
    }
}
