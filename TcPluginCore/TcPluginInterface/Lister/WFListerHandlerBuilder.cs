using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace TcPluginInterface.Lister;

[Serializable]
public class WfListerHandlerBuilder : IListerHandlerBuilder
{
    public ListerPlugin Plugin { get; set; }

    public IntPtr GetHandle(object listerControl, IntPtr parentHandle)
    {
        var listerHandle = IntPtr.Zero;
        if (listerControl == null)
            return listerHandle;

        if (listerControl is UserControl userControl)
        {
            if (Plugin.FocusedControl is Control control)
                control.KeyDown += wfControl_KeyDown;

            listerHandle = userControl.Handle;
        }
        else
        {
            throw new Exception($"Unexpected WinForms control type: {listerControl.GetType()}");
        }

        return listerHandle;
    }

    #region Keyboard Handler

    private static readonly List<Keys> _sentToParentKeys = new()
    {
        Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5, Keys.D6, Keys.D7, Keys.D8, // Options.Mode
        Keys.N, Keys.P,
        Keys.A, Keys.S, Keys.V, Keys.W, // Options.Text
        Keys.F, Keys.L, Keys.C, // Options.Images
        Keys.F2, Keys.F5, Keys.F7
    };

    private static readonly List<Keys> _sentToParentCtrlKeys = new()
    {
        Keys.A, Keys.C, Keys.P
    };

    private void wfControl_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            Plugin.CloseParentWindow();
            e.Handled = true;
        }
        else if (_sentToParentCtrlKeys.Contains(e.KeyCode) && e.Control && !e.Alt)
        {
            var code = e.KeyValue | (int)Keys.Control;
            Plugin.SendKeyToParentWindow(code);
        }
        else if (_sentToParentKeys.Contains(e.KeyCode) && !e.Control && !e.Alt)
        {
            Plugin.SendKeyToParentWindow(e.KeyValue);
        }
    }

    #endregion Keyboard Handler
}
