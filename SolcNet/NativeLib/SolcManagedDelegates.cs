﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace SolcNet.NativeLib
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void NativeReadFileCallbackDelegate(
        [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8StringMarshaler))]
            string path,
        [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8StringMarshaler))]
            ref string contents,
        [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8StringMarshaler))]
            ref string error);

    [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8StringMarshalerNoCleanup))]
    public delegate string CompileStandardDelegate(string input, NativeReadFileCallbackDelegate readCallback);

    [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8StringMarshalerNoCleanup))]
    public delegate string CompileJsonDelegate(string input, bool optimize);

    [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8StringMarshalerNoCleanup))]
    delegate string CompileJsonMultiDelegate(string input, bool optimize);

    [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8StringMarshalerNoCleanup))]
    delegate string CompileJsonCallbackDelegate(string input, bool optimize, NativeReadFileCallbackDelegate readCallback);

    [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8StringMarshalerNoCleanup))]
    delegate string LicenseDelegate();

    [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8StringMarshalerNoCleanup))]
    delegate string VersionDelegate();
}
