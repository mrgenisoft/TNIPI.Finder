using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace TNIPI.Finder
{
    public enum WellLogType : int { Continuous, Discrete };

    [Serializable]
    public sealed class WellHdrRecord
    {
        public string Uwi, Name, Cluster, Class, Operator, SurveyTool, NorthReference;
        public DateTime SpudDate, FinishDate, SurveyDate;
        public double BottomMd, BottomTvd, Elevation, Nx, Ny, MagneticCorrection;
    }

    [Serializable]
    public sealed class WellTrajRecord
    {
        public double MD, Inclination, Azimuth;

        public WellTrajRecord(double md, double incl, double azim)
        {
            MD = md;
            Inclination = incl;
            Azimuth = azim;
        }
    }

    [Serializable]
    public sealed class WellLogRecord
    {
        public double MD;
        public object Value;

        public WellLogRecord(double md, object value)
        {
            MD = md;
            Value = value;
        }
    }

    [Serializable]
    public sealed class WellStateRecord
    {
        public string Uwi, TypeDescription, StateDescription, MethodDescription;
        public int Type, State, Method;
        public DateTime TimeStamp;
    }

    [Serializable]
    public sealed class WellTopRecord
    {
        public string Uwi, Layer;
        public double Top, Base;
    }

    [Serializable]
    public sealed class WellLogRecordList : List<WellLogRecord>
    {
    }

    public interface IFinder
    {
        bool IsClient64bit();
        void SetConnectionParameters(string sid, string user, string pwd, string proj, string wlrlt, string uwimask);
        void OpenConnection();
        void CloseConnection();
        string Project { get; }
        string UwiMask { get; }
        string ServerVersion { get; }
        ICollection<WellHdrRecord> WellList { get; }
        ICollection<WellTrajRecord> GetDirectionalSurvey(string uwi);
        IDictionary<string, WellLogRecordList> GetWellLogs(string uwi, IDictionary<string, WellLogType> logColumnDict);
        ICollection<WellStateRecord> GetWellState(ICollection<string> uwiList);
        ICollection<WellTopRecord> GetWellTops(ICollection<string> uwiList);
    }
}
