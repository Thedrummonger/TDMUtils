using System.ComponentModel;
using System.Windows.Forms;
using TDMUtils;

namespace TDMWinUtils
{
    public static class WinFormUtils
    {
        public static T? GetSelectedContainerItem<T>(this ComboBox comboBox)
        {
            if (comboBox.SelectedItem is EnumerableUtilities.ContainerItem containerItem && containerItem.Value is T value)
                return value;
            return default;
        }
        public static T? GetSelectedContainerItem<T>(this ListBox comboBox)
        {
            if (comboBox.SelectedItem is EnumerableUtilities.ContainerItem containerItem && containerItem.Value is T value)
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
        public static void AppendString(this RichTextBox rtb, params ColoredString[] coloredStrings)
        {
            rtb.AppendString(() =>
            {
                string rtf = ColoredString.BuildRtf(coloredStrings, rtb.ForeColor);
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
