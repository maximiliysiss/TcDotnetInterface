﻿using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace OY.TotalCommander.TcPluginInterface.Content;

[Serializable]
public class ContentValue
{
    private bool changed;
    private string strValue;

    public void CopyTo(IntPtr ptr)
    {
        if (ptr != IntPtr.Zero && changed)
        {
            switch (FieldType)
            {
                case ContentFieldType.Numeric32:
                    Marshal.WriteInt32(ptr, int.Parse(strValue));
                    break;
                case ContentFieldType.Numeric64:
                    Marshal.WriteInt64(ptr, long.Parse(strValue));
                    break;
                case ContentFieldType.NumericFloating:
                    string altStr = null;
                    var floatStr = strValue;
                    if (floatStr.Contains("|"))
                    {
                        var pos = floatStr.IndexOf("|", StringComparison.Ordinal);
                        altStr = floatStr.Substring(pos + 1);
                        floatStr = floatStr.Substring(0, pos);
                    }

                    Marshal.Copy(new[] { double.Parse(floatStr) }, 0, ptr, 1);
                    if (!string.IsNullOrEmpty(altStr))
                    {
                        // ??? ANSI or Unicode
                        var altOutput = new IntPtr(ptr.ToInt32() + sizeof(double));
                        Marshal.Copy((altStr + (char)0).ToCharArray(), 0, altOutput, altStr.Length + 1);
                    }

                    break;
                case ContentFieldType.Date:
                    var date = DateTime.Parse(strValue);
                    Marshal.Copy(new[] { (short)date.Year, (short)date.Month, (short)date.Day }, 0, ptr, 3);
                    break;
                case ContentFieldType.Time:
                    var time = DateTime.Parse(strValue);
                    Marshal.Copy(new[] { (short)time.Hour, (short)time.Minute, (short)time.Second }, 0, ptr, 3);
                    break;
                case ContentFieldType.Boolean:
                    Marshal.WriteInt32(ptr, bool.Parse(strValue) ? 1 : 0);
                    break;
                case ContentFieldType.MultipleChoice:
                case ContentFieldType.String:
                    TcUtils.WriteStringAnsi(strValue, ptr, 0);
                    break;
                case ContentFieldType.FullText:
                    // ??? can it be Unicode ???
                    TcUtils.WriteStringAnsi(strValue, ptr, 0);
                    break;
                case ContentFieldType.DateTime:
                    Marshal.WriteInt64(ptr, DateTime.Parse(strValue).ToFileTime());
                    break;
                case ContentFieldType.WideString:
                    TcUtils.WriteStringUni(strValue, ptr, 0);
                    break;
            }
        }
    }

    #region Properties

    public string StrValue
    {
        get => strValue;
        set
        {
            if (!(value ?? string.Empty).Equals(strValue ?? string.Empty))
            {
                strValue = value;
                changed = true;
            }
        }
    }

    public ContentFieldType FieldType { get; private set; }

    #endregion Properties

    #region Constructors

    public ContentValue(string value, ContentFieldType fieldType)
    {
        StrValue = value;
        FieldType = fieldType;
        changed = true;
    }

    public ContentValue(IntPtr ptr, ContentFieldType fieldType)
    {
        FieldType = fieldType;
        strValue = null;
        if (ptr != IntPtr.Zero)
        {
            switch (fieldType)
            {
                case ContentFieldType.Numeric32:
                    strValue = Marshal.ReadInt32(ptr).ToString(CultureInfo.InvariantCulture);
                    break;
                case ContentFieldType.Numeric64:
                    strValue = Marshal.ReadInt64(ptr).ToString(CultureInfo.InvariantCulture);
                    break;
                case ContentFieldType.NumericFloating:
                    var dv = new double[1];
                    Marshal.Copy(ptr, dv, 0, 1);
                    strValue = dv[0].ToString(CultureInfo.InvariantCulture);
                    break;
                case ContentFieldType.Date:
                    var date = new short[3];
                    Marshal.Copy(ptr, date, 0, 3);
                    // convert to standard short date format ("d")
                    strValue = new DateTime(date[0], date[1], date[2]).ToString(CultureInfo.InvariantCulture);
                    break;
                case ContentFieldType.Time:
                    var time = new short[3];
                    Marshal.Copy(ptr, time, 0, 3);
                    // convert to standard long time format ("T")
                    strValue = new DateTime(1, 1, 1, time[0], time[1], time[2]).ToString(CultureInfo.InvariantCulture);
                    break;
                case ContentFieldType.Boolean:
                    strValue = (Marshal.ReadInt32(ptr) != 0).ToString();
                    break;
                case ContentFieldType.MultipleChoice:
                case ContentFieldType.String:
                    strValue = Marshal.PtrToStringAnsi(ptr);
                    break;
                case ContentFieldType.DateTime:
                    // convert to General date/long time format ("G")
                    strValue = DateTime.FromFileTime(Marshal.ReadInt64(ptr)).ToString(CultureInfo.InvariantCulture);
                    break;
                case ContentFieldType.WideString:
                    strValue = Marshal.PtrToStringUni(ptr);
                    break;
            }
        }
    }

    #endregion Constructors
}
