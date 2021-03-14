using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace UnityEditor.Experimental.TerrainAPI
{
    /// <summary>
    /// Class used to simulate input for automation testing purposes
    /// </summary>
    internal static class InputSimulationSystem
    {
        /// <summary>
        /// List of input events to be dispatched 
        /// </summary>
        static List<INPUT> m_inputList = new List<INPUT>();

        /// <summary>
        /// Adds a key down to the list of messages.
        /// </summary>
        internal static void AddKeyDown(VirtualKeyCode keyCode)
        {
            var down = new INPUT
            {
                Type = (UInt32) InputType.Keyboard,
                Data =
                {
                    Keyboard = new KEYBDINPUT
                    {
                        KeyCode = (UInt16) keyCode,
                        Scan = (UInt16)(NativeMethods.MapVirtualKey((UInt32)keyCode, 0) & 0xFFU),
                        Flags = IsExtendedKey(keyCode) ? (UInt32) KeyboardFlag.ExtendedKey : 0,
                        Time = 0,
                        ExtraInfo = IntPtr.Zero
                    }
                }
            };

            m_inputList.Add(down);
        }

        /// <summary>
        /// Adds a key up to the list of messages.
        /// </summary>
        internal static void AddKeyUp(VirtualKeyCode keyCode)
        {
            var up = new INPUT
            {
                Type = (UInt32) InputType.Keyboard,
                Data =
                {
                    Keyboard = new KEYBDINPUT
                    {
                        KeyCode = (UInt16) keyCode,
                        Scan = (UInt16)(NativeMethods.MapVirtualKey((UInt32)keyCode, 0) & 0xFFU),
                        Flags = (UInt32) (IsExtendedKey(keyCode)
                            ? KeyboardFlag.KeyUp | KeyboardFlag.ExtendedKey
                            : KeyboardFlag.KeyUp),
                        Time = 0,
                        ExtraInfo = IntPtr.Zero
                    }
                }
            };

            m_inputList.Add(up);
        }

        /// <summary>
        /// Simulate a keypress (KeyUp & Key Down)
        /// </summary>
        /// <param name="keyCode"> Virtual Keycode associated with a keys value  </param>
        /// <param name="instantDispatch"> Bool value determing whether the Input list should be instantly dispatched after simulating the key press</param>
        /// <param name="pressAmount"> Number of presses to be simulated</param>
        internal static void SimulateKeyPress(VirtualKeyCode keyCode, bool instantDispatch = false, int pressAmount = 1)
        {
            for (int i = 0; i < pressAmount; i++)
            {
                AddKeyDown(keyCode);
                AddKeyUp(keyCode);
            }

            if(instantDispatch)
            DispatchInput();
        }

        /// <summary>
        /// Dispatches the specified list of <see cref="INPUT"/> messages in their specified order by issuing a single called to <see cref="NativeMethods.SendInput"/>.
        /// </summary>
        /// <param name="m_inputList">The list of <see cref="INPUT"/> messages to be dispatched.</param>
        /// <exception cref="ArgumentException">If the <paramref name="m_inputList"/> array is empty.</exception>
        /// <exception cref="ArgumentNullException">If the <paramref name="m_inputList"/> array is null.</exception>
        /// <exception cref="Exception">If the any of the commands in the <paramref name="m_inputList"/> array could not be sent successfully.</exception>
        public static void DispatchInput()
        {
            if (m_inputList == null)
                throw new ArgumentNullException("inputs");
            if (m_inputList.Count == 0)
                throw new ArgumentException("The input array was empty", "inputs");
            var successful = NativeMethods.SendInput((UInt32)m_inputList.Count, m_inputList.ToArray(), Marshal.SizeOf(typeof (INPUT)));
            if (successful != m_inputList.Count)
                throw new Exception("Some simulated input commands were not sent successfully. The most common reason for this happening are the security features of Windows including User Interface Privacy Isolation (UIPI). Your application can only send commands to applications of the same or lower elevation. Similarly certain commands are restricted to Accessibility/UIAutomation applications. Refer to the project home page and the code samples for more information.");
            m_inputList.Clear();
        }

        /// <summary>
        /// Determines if the <see cref="VirtualKeyCode"/> is an ExtendedKey
        /// </summary>
        /// <param name="keyCode">Key code to check against</param>
        /// <returns>true if the key code is an extended key; otherwise, false.</returns>
        static bool IsExtendedKey(VirtualKeyCode keyCode)
        {
            if (keyCode == VirtualKeyCode.MENU ||
                keyCode == VirtualKeyCode.LMENU ||
                keyCode == VirtualKeyCode.RMENU ||
                keyCode == VirtualKeyCode.CONTROL ||
                keyCode == VirtualKeyCode.RCONTROL ||
                keyCode == VirtualKeyCode.INSERT ||
                keyCode == VirtualKeyCode.DELETE ||
                keyCode == VirtualKeyCode.HOME ||
                keyCode == VirtualKeyCode.END ||
                keyCode == VirtualKeyCode.PRIOR ||
                keyCode == VirtualKeyCode.NEXT ||
                keyCode == VirtualKeyCode.RIGHT ||
                keyCode == VirtualKeyCode.UP ||
                keyCode == VirtualKeyCode.LEFT ||
                keyCode == VirtualKeyCode.DOWN ||
                keyCode == VirtualKeyCode.NUMLOCK ||
                keyCode == VirtualKeyCode.CANCEL ||
                keyCode == VirtualKeyCode.SNAPSHOT ||
                keyCode == VirtualKeyCode.DIVIDE)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}