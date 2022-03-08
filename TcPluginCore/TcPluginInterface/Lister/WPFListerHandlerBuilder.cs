using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using WPFUserControl = System.Windows.Controls.UserControl;

namespace OY.TotalCommander.TcPluginInterface.Lister;

public class WPFListerHandlerBuilder : IListerHandlerBuilder
{
    private ElementHost elementHost;

    public ListerPlugin Plugin { get; set; }

    public IntPtr GetHandle(object listerControl, IntPtr parentHandle)
    {
        var listerHandle = IntPtr.Zero;
        if (listerControl != null)
        {
            if (listerControl is WPFUserControl)
            {
                var wpfControl = (WPFUserControl)listerControl;
                elementHost = new ElementHost
                {
                    Dock = DockStyle.Fill,
                    Child = wpfControl
                };
                wpfControl.KeyDown += wpfControl_KeyDown;
                elementHost.Focus();
                wpfControl.Focus();
                listerHandle = elementHost.Handle;
            }
            else
            {
                throw new Exception("Unexpected WPF control type: " + listerControl.GetType());
            }
        }

        return listerHandle;
    }

    #region Keyboard Handler

    private static readonly List<Key> SentToParentKeys = new()
    {
        Key.D1, Key.D2, Key.D3, Key.D4, Key.D5, Key.D6, Key.D7, Key.D8, // Options.Mode
        Key.N, Key.P,
        Key.A, Key.S, Key.V, Key.W, // Options.Text
        Key.F, Key.L, Key.C, // Options.Images
        Key.F2, Key.F5, Key.F7
    };

    private static readonly List<Key> SentToParentCtrlKeys = new()
    {
        Key.P //, Key.A, Key.C,
    };

    private void wpfControl_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Plugin.CloseParentWindow();
            e.Handled = true;
        }
        else if (SentToParentCtrlKeys.Contains(e.Key)
                 && (e.KeyboardDevice.Modifiers & ModifierKeys.Control) != 0
                 && (e.KeyboardDevice.Modifiers & ModifierKeys.Alt) == 0)
        {
            var code = KeyInterop.VirtualKeyFromKey(e.Key) | (int)Keys.Control;
            Plugin.SendKeyToParentWindow(code);
        }
        else if (SentToParentKeys.Contains(e.Key)
                 && (e.KeyboardDevice.Modifiers & ModifierKeys.Control) == 0
                 && (e.KeyboardDevice.Modifiers & ModifierKeys.Alt) == 0)
        {
            Plugin.SendKeyToParentWindow(KeyInterop.VirtualKeyFromKey(e.Key));
        }
    }

    #endregion Keyboard Handler
}
