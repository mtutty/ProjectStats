using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Collections.Specialized;
using System.Collections;

namespace ProjectStats.Model {
    public class ProjectStatsModelFactory {
        private ProjectStatsModelFactory() { /* not creatable */ }

        public static DataTable CreateIssuesTable() {
            Hashtable sd = new Hashtable();

            sd.Add(@"id", @"id");
            sd.Add("state", "state");
            sd.Add("milestone", "milestone.title");
            sd.Add("user", "user.login");
            sd.Add("created_at", "created_at");
            sd.Add("assignee", "assignee.login");
            sd.Add("updated_at", "updated_at");
            sd.Add("title", "title");
            sd.Add("labels", "labels[0].name");
            sd.Add("comments", "comments");
            sd.Add("number", "number");
            sd.Add("html_url", "html_url");
            sd.Add("pull_request", "pull_request.html_url");
            sd.Add("url", "url");
            sd.Add("closed_at", "closed_at");

            return CreateDataTable(@"Issues", sd);
        }

        public static DataTable CreateCycleTimeTable() {
            Hashtable sd = new Hashtable();

            sd.Add(@"rolluptype", @"rolluptype");
            sd.Add("rollupvalue", "rollupvalue");
            sd.Add("bin1", "bin1");
            sd.Add("bin2", "bin2");
            sd.Add("bin3", "bin3");
            sd.Add("bin4", "bin4");
            sd.Add("bin5", "bin5");
            sd.Add("bin6", "bin6");

            return CreateDataTable(@"CycleTime", sd);
        }

        public static DataTable CreateCountByWeekTable() {
            return CreateCountByWeekTable(@"CountByWeek");
        }

        public static DataTable CreateCountByWeekTable(string tableName) {
            Hashtable sd = new Hashtable();

            sd.Add(@"rolluptype", @"rolluptype");
            sd.Add("rollupvalue", "rollupvalue");
            sd.Add("date", "date");
            sd.Add("count1", "count1");
            sd.Add("count2", "count2");
            sd.Add("count3", "count3");

            return CreateDataTable(tableName, sd);
        }

        public static DataTable CreateDataTable(string TableName, Hashtable FieldList) {
            DataTable ret = new DataTable(TableName);

            foreach (string colName in FieldList.Keys) {
                ret.Columns.Add(colName).Caption = FieldList[colName].ToString();
            }
            return ret;
        }

    }
}
