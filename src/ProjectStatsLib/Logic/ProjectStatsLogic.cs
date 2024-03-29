﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using ProjectStats.Model;
using System.Collections;

namespace ProjectStats.Logic {
    public class ProjectStatsLogic {

        private ProjectStatsLogic() { /* not creatable */ }

        #region Cycle Time (closed - created, binned counts)
        public static DataTable CycleTime(DataTable dtSource) {
            DataTable ret = ProjectStatsModelFactory.CreateCycleTimeTable();

            AddCycleTimeRow(dtSource, ret);
            AddCycleTimeRows(dtSource, ret, @"milestone");
            AddCycleTimeRows(dtSource, ret, @"user");
            return ret;
        }

        private static void AddCycleTimeRow(DataTable dtSource, DataTable dest) {

            DataRow[] rows;
            string filter = @"closed_at is not null";
            rows = dtSource.Select(filter);

            int[] values = CalculateCycleTime(rows);

            DataRow drStats = dest.NewRow();
            SetRowValues(drStats, @"overall", string.Empty, values);
            dest.Rows.Add(drStats);
        }

        private static void AddCycleTimeRows(DataTable dtSource, DataTable dest, string rollupField) {

            DataRow[] rows;
            foreach (var name in DistinctValues(dtSource.AsEnumerable(), rollupField)) {
                string filter = string.Format(@"closed_at is not null AND {0} = '{1}'", rollupField, name);
                rows = dtSource.Select(filter);

                int[] values = CalculateCycleTime(rows);

                DataRow drStats = dest.NewRow();
                SetRowValues(drStats, rollupField, name, values);
                dest.Rows.Add(drStats);
            }
        }

        private static void SetRowValues(DataRow drStats, string rollupField, string rollupValue, int[] values) {
            drStats[@"rolluptype"] = rollupField;
            drStats[@"rollupvalue"] = rollupValue;

            string fieldNameTemplate = @"bin{0}";
            for (int idx = 0; idx < values.Length; idx++) {
                drStats[string.Format(fieldNameTemplate, idx + 1)] = values[idx];
            }
        }

        private static int[] CalculateCycleTime(DataRow[] rows) {
            // Bins: 0 days, 1-2 days, 3-7 days, 8-14 days, 15-30 days, 31+
            int[] bins = { 1, 3, 8, 15, 31 };
            int[] values = new int[bins.Length + 1];

            foreach (DataRow dr in rows) {
                int idx = 0;
                int age = IssueAge(dr);
                if (age > -1) {
                    for (idx = 0; idx < bins.Length; idx++) {
                        if (age < bins[idx]) break;
                    }
                    values[idx] += 1;
                }
            }

            return values;
        }
        #endregion

        #region Productivity (closed count by week)
        public static DataTable Productivity(DataTable dtSource) {
            DataTable ret = ProjectStatsModelFactory.CreateCountByWeekTable(@"Productivity");

            AddProductivityRows(dtSource, ret);
            AddProductivityRows(dtSource, ret, @"milestone");
            AddProductivityRows(dtSource, ret, @"user");
            return ret;
        }

        private static void AddProductivityRows(DataTable dtSource, DataTable ret) {
            DataView dv = new DataView(dtSource, @"closed_at is not null", null, DataViewRowState.CurrentRows);
            AddProductivityRows(dv, ret, @"Overall", string.Empty);
        }

        private static void AddProductivityRows(DataTable dtSource, DataTable ret, string rollupField) {
            string filterTemplate = @"closed_at is not null and {0} = '{1}'";
            foreach (string rollupValue in DistinctValues(dtSource.AsEnumerable(), rollupField)) {
                DataView dv = new DataView(dtSource, string.Format(filterTemplate, rollupField, rollupValue), null, DataViewRowState.CurrentRows);
                AddProductivityRows(dv, ret, rollupField, rollupValue);
            }
        }

        private static void AddProductivityRows(DataView dv, DataTable dest, string rollupField, string rollupValue) {

            SortedDictionary<string, int> counts = new SortedDictionary<string, int>();

            foreach (DataRowView drv in dv) {
                AddToWeeklyCount(counts, drv[@"closed_at"]);
            }

            foreach (string key in counts.Keys) {
                    DataRow dr = dest.NewRow();
                    dr[@"count1"] = counts[key];
                    dr[@"date"] = key;
                    dr[@"rolluptype"] = rollupField;
                    dr[@"rollupvalue"] = rollupValue;
                    dest.Rows.Add(dr);
            }
        }

        #endregion

        #region Backlog (total + opened - closed by week)
        public static DataTable Backlog(DataTable dtSource) {
            DataTable ret = ProjectStatsModelFactory.CreateCountByWeekTable(@"Backlog");

            AddBacklogRows(dtSource, ret);
            AddBacklogRows(dtSource, ret, @"milestone");
            AddBacklogRows(dtSource, ret, @"user");
            return ret;
        }

        private static void AddBacklogRows(DataTable dtSource, DataTable ret) {
            DataView dv = new DataView(dtSource);
            AddBacklogRows(dv, ret, @"Overall", string.Empty);
        }

        private static void AddBacklogRows(DataTable dtSource, DataTable ret, string rollupField) {
            string filterTemplate = @"{0} = '{1}'";
            foreach (string rollupValue in DistinctValues(dtSource.AsEnumerable(), rollupField)) {
                DataView dv = new DataView(dtSource, string.Format(filterTemplate, rollupField, rollupValue), null, DataViewRowState.CurrentRows);
                AddBacklogRows(dv, ret, rollupField, rollupValue);
            }
        }

        private static void AddBacklogRows(DataView dv, DataTable dest, string rollupField, string rollupValue) {

            SortedDictionary<string, int> opened = new SortedDictionary<string, int>();
            SortedDictionary<string, int> closed = new SortedDictionary<string, int>();

            foreach (DataRowView drv in dv) {
                AddToWeeklyCount(opened, drv[@"created_at"]);
                AddToWeeklyCount(closed, drv[@"closed_at"]);
            }

            int runningTotal = 0;
            foreach (string key in opened.Keys) {
                DataRow dr = dest.NewRow();

                int o = opened.ContainsKey(key) ? opened[key] : 0;
                dr[@"count1"] = o;

                int c = closed.ContainsKey(key) ? closed[key] : 0;
                dr[@"count2"] = c;

                runningTotal = runningTotal + o - c;
                dr[@"count3"] = runningTotal;
                
                dr[@"date"] = key;
                dr[@"rolluptype"] = rollupField;
                dr[@"rollupvalue"] = rollupValue;
                dest.Rows.Add(dr);
            }
        }

        #endregion


        #region Common Functions
        private static void AddToWeeklyCount(SortedDictionary<string, int> counts, object rawDate) {
            DateTime? wkEnding = WeekEnding(rawDate);
            if (wkEnding != null) {
                string key = wkEnding.Value.ToString(@"yyyy-MM-dd");
                if (counts.ContainsKey(key))
                    counts[key] += 1;
                else
                    counts.Add(key, 1);
            }
        }

        public static DateTime? WeekEnding(object rawInput) {
            if (rawInput == null ||
                string.IsNullOrEmpty(rawInput.ToString())) return null;
            return WeekEnding(DateTime.Parse(rawInput.ToString()));
        }

        public static DateTime? WeekEnding(DateTime input) {
            DateTime EndOfWeek = input.AddDays(-(int)input.DayOfWeek);
            if (input.DayOfWeek != DayOfWeek.Sunday)
                EndOfWeek = EndOfWeek.AddDays(7);
            return new DateTime(EndOfWeek.Year, EndOfWeek.Month, EndOfWeek.Day);
        }

        private static int IssueAge(DataRow dr) {
            if (! HasFieldValue(dr[@"created_at"])) return 0;
            if (! HasFieldValue(dr[@"closed_at"])) return -1;
            DateTime created = DateTime.Parse(dr[@"created_at"].ToString());
            DateTime closed = DateTime.Parse(dr[@"closed_at"].ToString());

            return closed.Subtract(created).Days;
        }

        private static bool HasFieldValue(object rawValue) {
            if (rawValue == null ||
                string.IsNullOrEmpty(rawValue.ToString())) return false;
            return true;
        }

        public static IEnumerable<string> DistinctValues(IEnumerable<DataRow> source, string groupField) {
            var ret = (from row in source
                                 select row.Field<string>(groupField)).Distinct();

            return ret;
        }
        #endregion

    }
}
