using System;

using NUnit.Framework;
using ProjectStats.Logic;

namespace ProjectStats.Tests {
    [TestFixture()]
    public class LogicTests {

        [TestCase(@"2012-03-24", @"3/25/2012")]
        [TestCase(@"2012-03-25", @"3/25/2012")]
        [TestCase(@"2012-03-26", @"4/1/2012")]
        public void WeekEndingTests(string input, string expected) {
            DateTime subject = DateTime.Parse(input);
            DateTime actual = ProjectStatsLogic.WeekEnding(subject);
            Assert.AreEqual(expected, actual.ToShortDateString());
        }
    }
}
