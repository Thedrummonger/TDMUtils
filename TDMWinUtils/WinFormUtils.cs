using System.ComponentModel;
using System.Windows.Forms;
using TDMUtils;

namespace TDMWinUtils
{
    public static class WinFormUtils
    {
        public static bool TryGetSelectedContainerItem<T>(this ComboBox comboBox, out T value)
        {
            if (comboBox.SelectedItem is EnumerableUtilities.ContainerItem c && c.Value is T v)
            {
                value = v;
                return true;
            }
            value = default!;
            return false;
        }

        public static bool TryGetSelectedContainerItem<T>(this ListBox listBox, out T value)
        {
            if (listBox.SelectedItem is EnumerableUtilities.ContainerItem c && c.Value is T v)
            {
                value = v;
                return true;
            }
            value = default!;
            return false;
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
                string rtf = ColoredString.BuildRtf(coloredStrings);
                rtb.SelectedRtf = rtf;
            });
        }
        public static void AppendString(this RichTextBox rtb, string text, Color? color = null)
        {
            rtb.AppendString(() =>
            {
                if (color is not null)
                    rtb.SelectionColor = color.Value;
                rtb.AppendText(text);
                rtb.SelectionColor = rtb.ForeColor;
            });
        }
        public static void AppendString(this RichTextBox rtb, ColoredString coloredStrings)
        {
            rtb.AppendString(() =>
            {
                foreach (var word in coloredStrings.Words)
                {
                    if (word.Color is not null)
                        rtb.SelectionColor = word.Color.Value;
                    rtb.AppendText(word.Text);
                    rtb.SelectionColor = rtb.ForeColor;
                }
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
        /// <summary>
        /// Determines whether a background thread is still running and the specified Windows Forms control
        /// is still safe to target for UI updates.
        /// </summary>
        /// <param name="thread">The worker thread associated with the UI update.</param>
        /// <param name="control">The Windows Forms control to validate.</param>
        /// <returns>True if the thread is alive and the control is not null, not disposed, and has a created handle; otherwise, false.</returns>
        public static bool IsWinFormsControlAccessible(Thread? thread, Control? control)
        {
            return thread?.IsAlive == true
                && control != null
                && !control.IsDisposed
                && control.IsHandleCreated;
        }
    }
}
