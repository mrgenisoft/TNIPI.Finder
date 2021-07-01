using System;
using System.Text;
using System.Data;
using System.Data.Common;
using System.Data.OracleClient;
using System.Collections;
using System.Collections.Generic;

namespace TNIPI.Finder
{
    public class FinderAccess : MarshalByRefObject, IFinder
    {
        private const double DoubleTolerance = 1e-6d;

        private const double WellLogStep = 0.1d;

        private OracleConnection conn;
        private string project, wlrltable, uwimask;

        public FinderAccess()
        {
            conn = new OracleConnection();
        }

        private string FillStringList(IEnumerable<string> strList)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string str in strList)
                sb.AppendFormat("{0}, ", str);
            return sb.ToString().TrimEnd(", ".ToCharArray());
        }

        public bool IsClient64bit()
        {
            try
            {
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                process.StartInfo = new System.Diagnostics.ProcessStartInfo("tnsping.exe");
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.UseShellExecute = false;

                if (!process.Start())
                    throw new Exception("Error while starting TNS Ping");

                System.IO.StreamReader output = process.StandardOutput;
                process.WaitForExit(5000);

                if (process.HasExited)
                {
                    string result = output.ReadToEnd().ToLower();
                    if (result.Contains("32-bit"))
                        return false;
                    else if (result.Contains("64-bit"))
                        return true;
                    else
                        throw new Exception("Unknown Oracle client architecture");
                }
                else
                    throw new Exception("TNS Ping not responding");
            }
            catch (System.ComponentModel.Win32Exception w32exc)
            {
                throw new Exception("TNS Ping not found", w32exc);
            }
            catch (Exception exc)
            {
                throw exc;
            }
        }

        public void SetConnectionParameters(string sid, string user, string pwd, string proj, string wlrlt, string uwimask)
        {
            conn.ConnectionString = string.Format("Data Source={0};User ID={1};Password={2};", sid, user, pwd);
            this.project = proj;
            this.wlrltable = wlrlt;
            this.uwimask = uwimask;
        }

        public void OpenConnection()
        {
            conn.Open();
        }

        public void CloseConnection()
        {
            conn.Close();
        }

        public string Project
        {
            get { return project; }
        }

        public string UwiMask
        {
            get { return uwimask; }
        }

        public string ServerVersion
        {
            get { return conn.ServerVersion; }
        }

        public ICollection<WellHdrRecord> WellList
        {
            get { return GetWellList(); }
        }

        private int GetReaderInt32(IDataReader reader, int index)
        {
            if (!reader.IsDBNull(index))
                return reader.GetInt32(index);
            else
                return int.MinValue;
        }

        private float GetReaderFloat(IDataReader reader, int index)
        {
            if (!reader.IsDBNull(index))
                return reader.GetFloat(index);
            else
                return float.NaN;
        }

        private double GetReaderDouble(IDataReader reader, int index)
        {
            if (!reader.IsDBNull(index))
                return reader.GetDouble(index);
            else
                return double.NaN;
        }

        private DateTime GetReaderDateTime(IDataReader reader, int index)
        {
            if (!reader.IsDBNull(index))
                return reader.GetDateTime(index);
            else
                return DateTime.MinValue;
        }

        private string GetReaderString(IDataReader reader, int index)
        {
            if (!reader.IsDBNull(index))
                return reader.GetString(index);
            else
                return string.Empty;
        }

        enum WellHdrColumns : int { UWI, NAME, NUMBER, CLASS, OPERATOR, SPUD_DATE, FIN_DRILL, DRILLERS_TD, TVD, ELEVATION, NODE_X, NODE_Y, SURVEY_DATE, SURVEY_TOOL, NORTH, CORRECTION };
        private ICollection<WellHdrRecord> GetWellList()
        {
            string cmdText;
            if (string.IsNullOrEmpty(uwimask))
                cmdText = string.Format("SELECT WH.UWI UWI, WELL_NAME, WELL_NUMBER, CLASS, OPERATOR, SPUD_DATE, FIN_DRILL, " +
                "DRILLERS_TD, TVD, ELEVATION, NODE_X, NODE_Y, SURVEY_DATE, REMARKS SURVEY_TOOL, NORTH_REFERENCE, DECLINATION_CORRECTION " +
                "FROM {0}.WELL_HDR WH, {0}.WELL_DIR_SRVY_HDR WDSH, {0}.NODES N " +
                "WHERE WH.UWI = WDSH.UWI AND WH.NODE_ID = N.NODE_ID AND PREFERRED_FLAG = 'Y' " +
                "ORDER BY UWI",
                project);
            else
                cmdText = string.Format("SELECT WH.UWI UWI, WELL_NAME, WELL_NUMBER, CLASS, OPERATOR, SPUD_DATE, FIN_DRILL, " +
                "DRILLERS_TD, TVD, ELEVATION, NODE_X, NODE_Y, SURVEY_DATE, REMARKS SURVEY_TOOL, NORTH_REFERENCE, DECLINATION_CORRECTION " +
                "FROM {0}.WELL_HDR WH, {0}.WELL_DIR_SRVY_HDR WDSH, {0}.NODES N " +
                "WHERE WH.UWI = WDSH.UWI AND WH.NODE_ID = N.NODE_ID AND PREFERRED_FLAG = 'Y' AND WH.UWI LIKE '{1}' " +
                "ORDER BY UWI",
                project, uwimask);

            DbCommand cmd = conn.CreateCommand();
            cmd.CommandText = cmdText;
            DbDataReader reader = cmd.ExecuteReader(CommandBehavior.KeyInfo);

            if (!reader.HasRows)
                throw new Exception("Wells not found");

            List<WellHdrRecord> wellList = new List<WellHdrRecord>();
            while (reader.Read())
            {
                WellHdrRecord wellRec = new WellHdrRecord();
                
                wellRec.Uwi = GetReaderString(reader, (int)WellHdrColumns.UWI);
                wellRec.Name = GetReaderString(reader, (int)WellHdrColumns.NAME);
                wellRec.Cluster = GetReaderString(reader, (int)WellHdrColumns.NUMBER);
                wellRec.Class = GetReaderString(reader, (int)WellHdrColumns.CLASS);
                wellRec.Operator = GetReaderString(reader, (int)WellHdrColumns.OPERATOR);
                wellRec.SpudDate = GetReaderDateTime(reader, (int)WellHdrColumns.SPUD_DATE);
                wellRec.FinishDate = GetReaderDateTime(reader, (int)WellHdrColumns.FIN_DRILL);
                wellRec.BottomMd = GetReaderDouble(reader, (int)WellHdrColumns.DRILLERS_TD);
                wellRec.BottomTvd = GetReaderDouble(reader, (int)WellHdrColumns.TVD);
                wellRec.Elevation = GetReaderDouble(reader, (int)WellHdrColumns.ELEVATION);
                wellRec.Nx = GetReaderDouble(reader, (int)WellHdrColumns.NODE_X);
                wellRec.Ny = GetReaderDouble(reader, (int)WellHdrColumns.NODE_Y);
                wellRec.SurveyDate = GetReaderDateTime(reader, (int)WellHdrColumns.SURVEY_DATE);
                wellRec.SurveyTool = GetReaderString(reader, (int)WellHdrColumns.SURVEY_TOOL);
                wellRec.NorthReference = GetReaderString(reader, (int)WellHdrColumns.NORTH);
                wellRec.MagneticCorrection = GetReaderDouble(reader, (int)WellHdrColumns.CORRECTION);

                if (double.IsNaN(wellRec.MagneticCorrection))
                    wellRec.MagneticCorrection = 0.0d;
                wellRec.MagneticCorrection *= Math.PI / 180.0d;
                
                wellList.Add(wellRec);
            }

            return wellList;
        }

        enum DirSrvyColumns : int { MD, INCL, AZIM };
        public ICollection<WellTrajRecord> GetDirectionalSurvey(string uwi)
        {
            DbCommand cmd = conn.CreateCommand();
            cmd.CommandText = string.Format("SELECT MD, DEVIATION_ANGLE, AZIMUTH " +
                "FROM {0}.WELL_DIR_SRVY_PTS WDSP " +
                "WHERE WDSP.UWI = '{1}' AND (WDSP.UWI, WDSP.SOURCE, WDSP.DIR_SRVY_ID) IN " +
                "(SELECT UWI, SOURCE, DIR_SRVY_ID FROM {0}.WELL_DIR_SRVY_HDR WHERE UWI = '{1}' AND PREFERRED_FLAG = 'Y') " +
                "ORDER BY MD",
                project, uwi);
            DbDataReader reader = cmd.ExecuteReader(CommandBehavior.KeyInfo);

            //if (!reader.HasRows)
            //    throw new Exception("Directional survey not found");

            List<WellTrajRecord> traj = new List<WellTrajRecord>();
            while (reader.Read())
            {
                double md, incl, azim;

                md = GetReaderDouble(reader, (int)DirSrvyColumns.MD);
                incl = GetReaderDouble(reader, (int)DirSrvyColumns.INCL);
                incl *= Math.PI / 180.0d;
                azim = GetReaderDouble(reader, (int)DirSrvyColumns.AZIM);
                azim *= Math.PI / 180.0d;

                if (incl < 0.0d * Math.PI || incl > Math.PI)
                    throw new Exception("Inclination angle not in range 0...180");

                if (azim < -2.0d * Math.PI || azim > 2.0d * Math.PI)
                    throw new Exception("Azimuth not in range -360...+360");

                traj.Add(new WellTrajRecord(md, incl, azim));
            }

            return traj;
        }

        enum WellLogColumns : int { TOP, BASE };
        public IDictionary<string, WellLogRecordList> GetWellLogs(string uwi, IDictionary<string, WellLogType> logColumnDict)
        {
            DbCommand cmd = conn.CreateCommand();
            cmd.CommandText = string.Format("SELECT TOP, BASE, {0} " +
                "FROM {1}.{2} WLRL, CLASS.SAT_DESC SD " +
                "WHERE UWI = '{3}' AND WLRL.SATURATION = SD.SATURATION " +
                "ORDER BY TOP",
                FillStringList(logColumnDict.Keys), project, wlrltable, uwi);
            DbDataReader reader = cmd.ExecuteReader(CommandBehavior.KeyInfo);

            //if (!reader.HasRows)
            //    throw new Exception("Well logs not found");

            Dictionary<string, WellLogRecordList> wlrDict = new Dictionary<string, WellLogRecordList>();
            foreach (string colName in logColumnDict.Keys)
                wlrDict.Add(colName, new WellLogRecordList());

            bool flag = true;
            double md = double.MaxValue;
            while (reader.Read())
            {
                double mtop, mbase;

                mtop = GetReaderDouble(reader, (int)WellLogColumns.TOP);
                mbase = GetReaderDouble(reader, (int)WellLogColumns.BASE);

                if (flag)
                    md = mtop;

                int index = 2;
                double mdcnt = md;
                foreach (string colName in logColumnDict.Keys)
                {
                    WellLogType type = logColumnDict[colName];
                    object val;
                    if (type == WellLogType.Continuous)
                        val = GetReaderFloat(reader, index++);
                    else
                        val = GetReaderInt32(reader, index++);

                    mdcnt = md;
                    while (!flag && mdcnt < (mtop - DoubleTolerance))
                    {
                        WellLogRecord wlr = new WellLogRecord(mdcnt, null);
                        wlrDict[colName].Add(wlr);
                        mdcnt += WellLogStep;
                    }

                    while (mdcnt < (mbase - DoubleTolerance))
                    {
                        WellLogRecord wlr = new WellLogRecord(mdcnt, val);
                        wlrDict[colName].Add(wlr);
                        mdcnt += WellLogStep;
                    }
                }

                md = mdcnt;
                flag = false;
            }

            return wlrDict;
        }

        enum WellFondColumns : int { UWI, DATE_D, TYPE, TYPE_DESC, STATE, STATE_DESC, METHOD, METHOD_DESC };
        public ICollection<WellStateRecord> GetWellState(ICollection<string> uwiList)
        {
            string cmdText;
            if (string.IsNullOrEmpty(uwimask))
                cmdText = string.Format("SELECT UWI, DATE_D, WF.TYPE TYPE, TD.NAME_RUS TYPE_DESC, WF.STATE STATE, SD.NAME_RUS STATE_DESC, WF.METHOD METHOD, MD.NAME_RUS METHOD_DESC " +
                "FROM {0}.WELL_FOND WF, CLASS.TYPE_DESC TD, CLASS.STATE_DESC SD, CLASS.METHOD_DESC MD " +
                "WHERE (UWI, DATE_D) IN (SELECT UWI, MAX(DATE_D) FROM {0}.WELL_FOND GROUP BY UWI) " +
                "AND WF.TYPE = TD.TYPE AND WF.STATE = SD.STATE AND WF.METHOD = MD.METHOD " +
                "ORDER BY UWI",
                project);
            else
                cmdText = string.Format("SELECT UWI, DATE_D, WF.TYPE TYPE, TD.NAME_RUS TYPE_DESC, WF.STATE STATE, SD.NAME_RUS STATE_DESC, WF.METHOD METHOD, MD.NAME_RUS METHOD_DESC " +
                "FROM {0}.WELL_FOND WF, CLASS.TYPE_DESC TD, CLASS.STATE_DESC SD, CLASS.METHOD_DESC MD " +
                "WHERE (UWI, DATE_D) IN (SELECT UWI, MAX(DATE_D) FROM {0}.WELL_FOND GROUP BY UWI) " +
                "AND WF.TYPE = TD.TYPE AND WF.STATE = SD.STATE AND WF.METHOD = MD.METHOD AND UWI LIKE '{1}' " +
                "ORDER BY UWI",
                project, uwimask);

            DbCommand cmd = conn.CreateCommand();
            cmd.CommandText = cmdText;
            DbDataReader reader = cmd.ExecuteReader(CommandBehavior.KeyInfo);

            //if (!reader.HasRows)
            //    throw new Exception("Well state not found");

            List<WellStateRecord> wsrList = new List<WellStateRecord>();
            while (reader.Read())
            {
                DateTime date_d;
                string uwi, type_desc, state_desc, method_desc;
                int type, state, method;

                uwi = GetReaderString(reader, (int)WellFondColumns.UWI);
                date_d = GetReaderDateTime(reader, (int)WellFondColumns.DATE_D);
                type = GetReaderInt32(reader, (int)WellFondColumns.TYPE);
                type_desc = GetReaderString(reader, (int)WellFondColumns.TYPE_DESC);
                state = GetReaderInt32(reader, (int)WellFondColumns.STATE);
                state_desc = GetReaderString(reader, (int)WellFondColumns.STATE_DESC);
                method = GetReaderInt32(reader, (int)WellFondColumns.METHOD);
                method_desc = GetReaderString(reader, (int)WellFondColumns.METHOD_DESC);

                if (!uwiList.Contains(uwi))
                    continue;

                WellStateRecord wsr = new WellStateRecord();
                wsr.Uwi = uwi;
                wsr.TimeStamp = date_d;
                wsr.Type = type;
                wsr.TypeDescription = type_desc;
                wsr.State = state;
                wsr.StateDescription = state_desc;
                wsr.Method = method;
                wsr.MethodDescription = method_desc;
                wsrList.Add(wsr);
            }

            return wsrList;
        }

        enum WellTopColumns : int { UWI, LAYER_NAME, TOP, BASE };
        public ICollection<WellTopRecord> GetWellTops(ICollection<string> uwiList)
        {
            string cmdText;
            if (string.IsNullOrEmpty(uwimask))
                cmdText = string.Format("SELECT UWI, LAYER_NAME, MIN(TOP) TOP, MAX(BASE) BASE " +
                "FROM {0}.{1} " +
                "WHERE LAYER_NAME NOT LIKE 'GAP%' AND LAYER_NAME <> 'UNKNOWN'" +
                "GROUP BY UWI, LAYER_NAME " +
                "ORDER BY UWI, TOP",
                project, wlrltable);
            else
                cmdText = string.Format("SELECT UWI, LAYER_NAME, MIN(TOP) TOP, MAX(BASE) BASE " +
                "FROM {0}.{1} " +
                "WHERE UWI LIKE '{2}' AND LAYER_NAME NOT LIKE 'GAP%' AND LAYER_NAME <> 'UNKNOWN'" +
                "GROUP BY UWI, LAYER_NAME " +
                "ORDER BY UWI, TOP",
                project, wlrltable, uwimask);

            DbCommand cmd = conn.CreateCommand();
            cmd.CommandText = cmdText;
            DbDataReader reader = cmd.ExecuteReader(CommandBehavior.KeyInfo);
            
            //if (!reader.HasRows)
            //    throw new Exception("Well tops not found");

            List<WellTopRecord> wtrList = new List<WellTopRecord>();
            while (reader.Read())
            {
                string uwi, layer;
                double mtop, mbase;

                uwi = GetReaderString(reader, (int)WellTopColumns.UWI);
                layer = GetReaderString(reader, (int)WellTopColumns.LAYER_NAME);
                mtop = GetReaderDouble(reader, (int)WellTopColumns.TOP);
                mbase = GetReaderDouble(reader, (int)WellTopColumns.BASE);

                if (!uwiList.Contains(uwi))
                    continue;

                WellTopRecord wtr = new WellTopRecord();
                wtr.Uwi = uwi;
                wtr.Layer = layer;
                wtr.Top = mtop;
                wtr.Base = mbase;
                wtrList.Add(wtr);
            }

            return wtrList;
        }
    }
}
