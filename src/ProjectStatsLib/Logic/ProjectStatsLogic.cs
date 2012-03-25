using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using ProjectStats.Model;

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

            var distinctNames = (from row in dtSource.AsEnumerable()
                                 select row.Field<string>(rollupField)).Distinct();

            DataRow[] rows;
            foreach (var name in distinctNames) {
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
            int[] bins = { 1, 3, 8, 15 };
            int[] values = new int[bins.Length + 1];

            foreach (DataRow dr in rows) {
                int idx = 0;
                int age = IssueAge(dr);
                for (idx = 0; idx < bins.Length; idx++) {
                    if (age < bins[idx]) break;
                }
                values[idx] += 1;
            }

            return values;
        }

        private static int IssueAge(DataRow dr) {
            if (dr.IsNull(@"created_at")) return 0;
            if (dr.IsNull(@"closed_at")) return -1;
            DateTime created = DateTime.Parse(dr[@"created_at"].ToString());
            DateTime closed = DateTime.Parse(dr[@"closed_at"].ToString());

            return closed.Subtract(created).Days;
        }
        #endregion

        #region Productivity (closed count by week)
        public static DataTable Productivity(DataTable dtSource) {
            DataTable ret = ProjectStatsModelFactory.CreateCountByWeekTable();

            AddProductivityRows(dtSource, ret);
            AddProductivityRows(dtSource, ret, @"milestone");
            AddProductivityRows(dtSource, ret, @"user");
            return ret;
        }

        private static void AddProductivityRows(DataTable dtSource, DataTable ret) {
            DataView dv = new DataView(dtSource, @"closed_at is not null", @"closed_at", DataViewRowState.CurrentRows);

            DataRow dr = null;
            string currentDate = string.Empty;
            int count = 0;
            foreach (DataRowView drv in dv) {
                if (currentDate != drv[@"closed_at"].ToString()) {
                    // TODO Count
                }
            }
        }

        private static void AddProductivityRows(DataTable dtSource, DataTable ret, string rollupField) {
            throw new NotImplementedException();
        }

        public static DateTime WeekEnding(DateTime input) {
            DateTime EndOfWeek = input.AddDays(-(int)input.DayOfWeek);
            if (input.DayOfWeek != DayOfWeek.Sunday) 
                EndOfWeek = EndOfWeek.AddDays(7);
            return EndOfWeek;
        }

        #endregion
    }
}
