﻿using System;
using System.Runtime.InteropServices;

namespace TcPluginInterface.Packer;

// Used as parameter type for OpenArchive method
[Serializable]
public class OpenArchiveData
{
    private readonly IntPtr _ptr;
    private TcOpenArchiveData _data;
    private TcOpenArchiveDataW _dataW;
    private bool _isUnicode;

    #region Constructors

    public OpenArchiveData(IntPtr ptr, bool isUnicode)
    {
        _ptr = ptr;
        _isUnicode = isUnicode;
        if (ptr == IntPtr.Zero)
            return;

        if (isUnicode)
        {
            _dataW = (TcOpenArchiveDataW)Marshal.PtrToStructure(ptr, typeof(TcOpenArchiveDataW));
            ArchiveName = _dataW.ArchiveName;
            Mode = (ArcOpenMode)_dataW.Mode;
        }
        else
        {
            _data = (TcOpenArchiveData)Marshal.PtrToStructure(ptr, typeof(TcOpenArchiveData));
            ArchiveName = _data.ArchiveName;
            Mode = (ArcOpenMode)_data.Mode;
        }
    }

    #endregion Constructors

    public void Update()
    {
        if (_ptr == IntPtr.Zero)
            return;

        if (_isUnicode)
        {
            _dataW.Result = (int)Result;
            Marshal.StructureToPtr(_dataW, _ptr, false);
        }
        else
        {
            _data.Result = (int)Result;
            Marshal.StructureToPtr(_data, _ptr, false);
        }
    }

    #region Properties

    public string ArchiveName { get; private set; }
    public ArcOpenMode Mode { get; private set; }
    public PackerResult Result { get; set; }

    //   may be used in the future
    //public string CmtBuf { get; set; }
    //public int CmtBufSize { get; set; }
    //public int CmtSize { get; set; }
    //public int CmtState { get; set; }

    #endregion Properties

    #region TC Structures

    [Serializable]
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct TcOpenArchiveDataW
    {
        [MarshalAs(UnmanagedType.LPWStr)] public string ArchiveName;
        public int Mode;
        public int Result;
        [MarshalAs(UnmanagedType.LPWStr)] public string CommentBuffer;
        public int CommentBufferSize;
        public int CommentSize;
        public int CommentState;
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    private struct TcOpenArchiveData
    {
        [MarshalAs(UnmanagedType.LPStr)] public string ArchiveName;
        public int Mode;
        public int Result;
        [MarshalAs(UnmanagedType.LPStr)] public string CommentBuffer;
        public int CommentBufferSize;
        public int CommentSize;
        public int CommentState;
    }

    #endregion TC Structures
}
