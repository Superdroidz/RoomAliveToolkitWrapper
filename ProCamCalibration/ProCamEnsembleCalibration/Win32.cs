using System;
using System.Runtime.InteropServices;

namespace RoomAliveToolkit
{

    public class Win32
    {
        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr _aligned_malloc(UIntPtr size, UIntPtr alignment);

        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr _aligned_free(IntPtr memblock);

        [DllImport("Kernel32.dll", EntryPoint = "RtlZeroMemory", SetLastError = false)]
        public static extern bool ZeroMemory(IntPtr Destination, UIntPtr Length);

        [DllImport("kernel32.dll", EntryPoint = "RtlMoveMemory", CallingConvention = CallingConvention.Cdecl)]
        public static extern void CopyMemory(IntPtr Destination, IntPtr Source, UIntPtr Length);

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern short GetAsyncKeyState(int vkey);

    }

}