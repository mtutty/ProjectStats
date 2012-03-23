using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using ProjectStats.Model;

namespace ProjectStats.Logic {
    public class ProjectStatsLogic {

        private ProjectStatsLogic() { /* not creatable */ }

        public static DataTable CycleTime(DataTable dtSource) {
            DataTable ret = ProjectStatsModelFactory.CreateCycleTimeTable();

            DataRow drStats = ret.NewRow();

            drStats[@"rolluptype"] = string.Empty;
            drStats[@"rollupvalue"] = string.Empty;

            int[] bins = {
                1, 3, 8, 15
            };

            int[] values = { 0, 0, 0, 0, 0 };

            // Bins: 0 days, 1-2 days, 3-7 days, 8-14 days, 15-30 days, 31+
            foreach (DataRow dr in dtSource.Select(@"closed_at is not null")) {
                int idx = 0;
                int age = IssueAge(dr);
                for (idx = 0; idx < bins.Length; idx++) {
                    // TODO
                }
            }
            ret.Rows.Add(drStats);

            return ret;
        }

        private static int IssueAge(DataRow dr) {
            if (dr.IsNull(@"created_at")) return 0;
            if (dr.IsNull(@"closed_at")) return -1;
            DateTime created = DateTime.Parse(dr[@"created_at"].ToString());
            DateTime closed = DateTime.Parse(dr[@"closed_at"].ToString());

            return closed.Subtract(created).Days;
        }
    }
}
