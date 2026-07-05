using Microsoft.VisualStudio.TestTools.UnitTesting;
using LukeBot.Common;
using System.Collections.Generic;
using System;
using System.Text.RegularExpressions;


namespace LukeBot.Tests.Common
{
    [TestClass]
    public class TimeZoneDBTests
    {
        private static readonly Regex ALT_SUFFIX_PATTERN =
            new Regex("(?<base>[A-Z]+)\\((?<suffix>[A-Za-z]+)\\)");

        private void VerifySuffixedAlternative(string tz, string alt)
        {
            Assert.IsFalse(String.IsNullOrEmpty(alt), "Provided empty or null alternative");
            Match match = ALT_SUFFIX_PATTERN.Match(alt);

            Assert.IsTrue(match.Success, "Alternative abbreviation didn't match the test regex");
            Assert.IsTrue(match.Groups.ContainsKey("base") && match.Groups["base"].Success, "Failed to extract base timezone abbreviation");
            Assert.IsTrue(match.Groups.ContainsKey("suffix") && match.Groups["suffix"].Success, "Failed to extract abbreviation's suffix");

            Assert.AreEqual(tz, match.Groups["base"].Value, "Alternative's base abbreviation does not match the main timezone abbreviation");
            Assert.IsFalse(String.IsNullOrEmpty(match.Groups["suffix"].Value), "Extracted timezone suffix is null or empty");
        }

        private void VerifyAbbrevToIANA(TimeZoneShort tz)
        {
            Dictionary<string, string> ianadb = TimeZoneDB.GetAbbrevToIANA();
            Assert.IsTrue(ianadb.ContainsKey(tz.Abbreviation), "DST-related Timezone \"{0}\" did not have an entry in the abbreviation-to-IANA DB", tz.Abbreviation);

            string iana = ianadb[tz.Abbreviation];
            Assert.IsFalse(String.IsNullOrEmpty(iana), "IANA timezone is null or empty for abbreviation \"{0}\"", tz.Abbreviation);
            Assert.IsTrue(TimeZoneInfo.TryFindSystemTimeZoneById(iana, out TimeZoneInfo ianatz), "System's TZ database did not contain IANA Timezone \"{0}\"", iana);
            Assert.IsNotNull(ianatz, "Found IANA TimeZoneInfo is null");

            Assert.AreEqual(ianatz.BaseUtcOffset, tz.UTC, String.Format("DB TimeZone's \"{0}\" UTC offset {1} does not match IANA UTC base offset {2}", tz.Abbreviation, tz.UTC, ianatz.BaseUtcOffset));
        }

        [TestMethod]
        public void TimeZoneDB_DBCorrectness()
        {
            // Some quick correctness checks for the TimeZoneDB itself
            // Duplicates are already checked by constructing the DB
            Dictionary<string, TimeZoneShort> db = TimeZoneDB.GetTimeZoneDB();


            // Check if inner DB references are valid depending on the AlternativeKind
            foreach (string k in db.Keys)
            {
                TimeZoneShort v = db[k];

                Assert.AreEqual(k, v.Abbreviation, "Timezone's internal abbreviation does not match its database key");
                Assert.IsFalse(String.IsNullOrEmpty(v.FullName), "Timezone's full name is empty or null.");
                Assert.IsNotNull(v.Alternatives, "Timezone's alternatives are null"); // all references should have non-null Alternatives just in case
                // TODO check span

                switch (v.AlternativeKind)
                {
                case AlternativeKind.NONE:
                {
                    // No alternatives means we should see a 0-element string array and nothing else
                    Assert.AreEqual(0, v.Alternatives.Length, "Timezone with no alternatives should have a 0-element Alternatives array");
                    break;
                }
                case AlternativeKind.AMBIGUOUS:
                {
                    // Ambiguous alternatives mean we picked one variant but to get to the others
                    // they must be suffixed with (...). For that, check if all alternatives:
                    //  - Start with the same abbreviation as currently tested key (but with a "(...)" suffix)
                    //  - Exist in the dictionary (just existence is enough, this loop will do the inner check of them anyway)
                    Assert.IsTrue(v.Alternatives.Length > 0, "Timezone's ambiguous alternatives should have more than 0 elements.");
                    foreach (string alt in v.Alternatives)
                    {
                        Assert.IsNotNull(db[alt], "Timezone's alternative {0} not found in the database", alt);
                        VerifySuffixedAlternative(v.Abbreviation, alt);
                    }
                    break;
                }
                case AlternativeKind.DST_ALT:
                {
                    // DST-Alt kind denotes timezones which observe DST changes.
                    Assert.AreEqual(1, v.Alternatives.Length, "Timezone with DST_ALT alternative kind should only have one alternative");
                    Assert.IsNotNull(db[v.Alternatives[0]], "Timezone's alternative {0} not found in the database", v.Alternatives[0]);
                    VerifyAbbrevToIANA(v);
                    break;
                }
                case AlternativeKind.DST_ALIAS:
                {
                    Assert.AreEqual(2, v.Alternatives.Length, "Timezone with DST_ALIAS alternative kind should have exactly two alternatives");
                    Assert.IsNotNull(db[v.Alternatives[0]], "Timezone's alternative {0} not found in the database", v.Alternatives[0]);
                    Assert.IsNotNull(db[v.Alternatives[1]], "Timezone's alternative {0} not found in the database", v.Alternatives[1]);
                    VerifyAbbrevToIANA(v);
                    break;
                }
                }
            }
        }
    }
}
