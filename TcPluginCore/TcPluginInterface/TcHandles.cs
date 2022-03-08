using System;
using System.Collections.Generic;
using System.Threading;

namespace TcPluginInterface;

internal class RefCountObject
{
    public RefCountObject(object o)
    {
        Obj = o;
        RefCount = 1;
    }

    public object Obj { get; private set; }
    public int RefCount { get; private set; }

    public void Update(object o)
    {
        Obj = o;
        RefCount++;
    }
}

// Some TC plugin methods return handles as pointers to internal plugin structures.
// This class contains methods for TC plugin Handle management.
public static class TcHandles
{
    #region Handle Management

    private static readonly Dictionary<IntPtr, RefCountObject> _handleDictionary = new();

    private static int _lastHandle;
    private static readonly object _handleSyncObj = new();

    public static IntPtr AddHandle(object obj)
    {
        Monitor.Enter(_handleSyncObj);
        try
        {
            _lastHandle++;
            var handle = new IntPtr(_lastHandle);
            _handleDictionary.Add(handle, new RefCountObject(obj));
            return handle;
        }
        finally
        {
            Monitor.Exit(_handleSyncObj);
        }
    }

    public static void AddHandle(IntPtr handle, object obj)
    {
        Monitor.Enter(_handleSyncObj);
        try
        {
            _handleDictionary.Add(handle, new RefCountObject(obj));
        }
        finally
        {
            Monitor.Exit(_handleSyncObj);
        }
    }

    public static object GetObject(IntPtr handle)
    {
        Monitor.Enter(_handleSyncObj);
        try
        {
            return _handleDictionary.ContainsKey(handle) ? _handleDictionary[handle].Obj : null;
        }
        finally
        {
            Monitor.Exit(_handleSyncObj);
        }
    }

    public static int GetRefCount(IntPtr handle)
    {
        Monitor.Enter(_handleSyncObj);
        try
        {
            if (_handleDictionary.ContainsKey(handle))
            {
                return _handleDictionary[handle].RefCount;
            }
            else
            {
                return -1;
            }
        }
        finally
        {
            Monitor.Exit(_handleSyncObj);
        }
    }

    public static void UpdateHandle(IntPtr handle, object obj)
    {
        Monitor.Enter(_handleSyncObj);
        try
        {
            _handleDictionary[handle].Update(obj);
        }
        finally
        {
            Monitor.Exit(_handleSyncObj);
        }
    }

    public static int RemoveHandle(IntPtr handle)
    {
        Monitor.Enter(_handleSyncObj);
        try
        {
            if (_handleDictionary.ContainsKey(handle))
            {
                var result = _handleDictionary[handle].RefCount;
                _handleDictionary.Remove(handle);
                return result;
            }
            else
            {
                return -1;
            }
        }
        finally
        {
            Monitor.Exit(_handleSyncObj);
        }
    }

    #endregion Handle Management
}
