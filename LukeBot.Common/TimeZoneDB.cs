using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

[assembly: InternalsVisibleTo("LukeBot.Tests")]

namespace LukeBot.Common
{
    public class TimeZoneDB
    {
        // used to fetch respective TimeZoneInfo from the System to determine its details about DST
        private static readonly Dictionary<string, string> ABBREV_TO_IANA = new ()
        {
            { "ACST",           "Australia/Darwin" },
            { "AET",            "Australia/Sydney" },
            { "AKST",           "America/Anchorage" },
            { "AST(Atlantic)",  "Atlantic/Bermuda" },
            { "AZOT",           "Atlantic/Azores" },
            { "CET",            "Europe/Berlin" },
            { "CHAST",          "Pacific/Chatham" },
            { "CLT",            "America/Santiago" },
            { "CST(Cuba)",      "America/Havana" },
            { "CT",             "America/Chicago" },
            { "EAST",           "Pacific/Easter" },
            { "EET",            "Europe/Athens" },
            { "ET",             "America/New_York" },
            { "HST",            "America/Adak" },
            { "IST(Israel)",    "Asia/Jerusalem" },
            { "LHST",           "Australia/Lord_Howe" },
            { "MET",            "Europe/Berlin" },
            { "MT",             "America/Denver" },
            { "NT",             "America/St_Johns" },
            { "NZST",           "Pacific/Auckland" },
            { "PMST",           "America/Miquelon" },
            { "PT",             "America/Los_Angeles" },
            { "WET",            "Europe/Lisbon" },
            { "WGT",            "America/Nuuk" },
        };

        private static readonly Dictionary<string, TimeZoneShort> DATABASE = new ()
        {
            { "ACDT",               new TimeZoneShort("ACDT", "Australian Central Daylight Saving Time", new TimeSpan(+10, 30, 0), AlternativeKind.NONE, new string[0]) },
            { "ACST",               new TimeZoneShort("ACST", "Australian Central Standard Time", new TimeSpan(+09, 30, 0), AlternativeKind.DST_ALT, new string[] { "ACDT" } ) },
            { "ACT(Acre)",          new TimeZoneShort("ACT(Acre)", "Acre Time", new TimeSpan(-05, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "ACT(ASEAN)",         new TimeZoneShort("ACT(ASEAN)", "ASEAN Common Time (proposed)", new TimeSpan(+08, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "ACT",                new TimeZoneShort("ACT", "Acre Time", new TimeSpan(-05, 00, 0), AlternativeKind.AMBIGUOUS, new string[] { "ACT(Acre)", "ACT(ASEAN)" } ) },
            { "ACWST",              new TimeZoneShort("ACWST", "Australian Central Western Standard Time (unofficial)", new TimeSpan(+08, 45, 0), AlternativeKind.NONE, new string[0]) },
            { "ADT",                new TimeZoneShort("ADT", "Atlantic Daylight Time", new TimeSpan(-03, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "AEDT",               new TimeZoneShort("AEDT", "Australian Eastern Daylight Saving Time", new TimeSpan(+11, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "AEST",               new TimeZoneShort("AEST", "Australian Eastern Standard Time", new TimeSpan(+10, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "AET",                new TimeZoneShort("AET", "Australian Eastern Time", new TimeSpan(+10, 00, 0), AlternativeKind.DST_ALIAS, new string[] { "AEST", "AEDT" } ) },
            { "AFT",                new TimeZoneShort("AFT", "Afghanistan Time", new TimeSpan(+04, 30, 0), AlternativeKind.NONE, new string[0]) },
            { "AKDT",               new TimeZoneShort("AKDT", "Alaska Daylight Time", new TimeSpan(-08, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "AKST",               new TimeZoneShort("AKST", "Alaska Standard Time", new TimeSpan(-09, 00, 0), AlternativeKind.DST_ALT, new string[] { "AKDT" } ) },
            { "ALMT",               new TimeZoneShort("ALMT", "Alma-Ata Time", new TimeSpan(+06, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "AMST",               new TimeZoneShort("AMST", "Amazon Summer Time (Brazil)", new TimeSpan(-03, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "AMT(Amazon)",        new TimeZoneShort("AMT(Amazon)", "Amazon Time (Brazil)", new TimeSpan(-04, 00, 0), AlternativeKind.NONE, new string[0]) }, // Brazil does not observe DST since 2019
            { "AMT(Armenia)",       new TimeZoneShort("AMT(Armenia)", "Armenia Time", new TimeSpan(+04, 00, 0), AlternativeKind.NONE, new string[0] ) },
            { "AMT",                new TimeZoneShort("AMT", "Armenia Time", new TimeSpan(+04, 00, 0), AlternativeKind.AMBIGUOUS, new string[] { "AMT(Amazon)", "AMT(Armenia)" } ) },
            { "ANAT",               new TimeZoneShort("ANAT", "Anadyr Time", new TimeSpan(+12, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "AQTT",               new TimeZoneShort("AQTT", "Aqtobe Time", new TimeSpan(+05, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "ART",                new TimeZoneShort("ART", "Argentina Time", new TimeSpan(-03, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "AST(Arabia)",        new TimeZoneShort("AST(Arabia)", "Arabia Standard Time", new TimeSpan(+03, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "AST(Atlantic)",      new TimeZoneShort("AST(Atlantic)", "Atlantic Standard Time", new TimeSpan(-04, 00, 0), AlternativeKind.DST_ALT, new string[] { "ADT" } ) },
            { "AST",                new TimeZoneShort("AST", "Atlantic Standard Time", new TimeSpan(-04, 00, 0), AlternativeKind.AMBIGUOUS, new string[] { "AST(Arabia)", "AST(Atlantic)" } ) },
            { "AWST",               new TimeZoneShort("AWST", "Australian Western Standard Time", new TimeSpan(+08, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "AZOST",              new TimeZoneShort("AZOST", "Azores Summer Time", new TimeSpan(+00, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "AZOT",               new TimeZoneShort("AZOT", "Azores Standard Time", new TimeSpan(-01, 00, 0), AlternativeKind.DST_ALT, new string[] { "AZOST" } ) },
            { "AZT",                new TimeZoneShort("AZT", "Azerbaijan Time", new TimeSpan(+04, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "BNT",                new TimeZoneShort("BNT", "Brunei Time", new TimeSpan(+08, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "BIOT",               new TimeZoneShort("BIOT", "British Indian Ocean Time", new TimeSpan(+06, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "BIT",                new TimeZoneShort("BIT", "Baker Island Time", new TimeSpan(-12, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "BOT",                new TimeZoneShort("BOT", "Bolivia Time", new TimeSpan(-04, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "BRST",               new TimeZoneShort("BRST", "Brasília Summer Time", new TimeSpan(-02, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "BRT",                new TimeZoneShort("BRT", "Brasília Time", new TimeSpan(-03, 00, 0), AlternativeKind.NONE, new string[0]) }, // Brazil does not observe DST
            { "BST(Bangladesh)",    new TimeZoneShort("BST(Bangladesh)", "Bangladesh Standard Time", new TimeSpan(+06, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "BST(Bougainville)",  new TimeZoneShort("BST(Bougainville)", "Bougainville Standard Time", new TimeSpan(+11, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "BST(British)",       new TimeZoneShort("BST(British)", "British Summer Time", new TimeSpan(+01, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "BST",                new TimeZoneShort("BST", "British Summer Time", new TimeSpan(+01, 00, 0), AlternativeKind.AMBIGUOUS, new string[] { "BST(Bangladesh)", "BST(Bougainville)", "BST(British)" } ) },
            { "BTT",                new TimeZoneShort("BTT", "Bhutan Time", new TimeSpan(+06, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "CAT",                new TimeZoneShort("CAT", "Central Africa Time", new TimeSpan(+02, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "CCT",                new TimeZoneShort("CCT", "Cocos Islands Time", new TimeSpan(+06, 30, 0), AlternativeKind.NONE, new string[0]) },
            { "CDT(Central)",       new TimeZoneShort("CDT(Central)", "Central Daylight Time", new TimeSpan(-05, 00, 0), AlternativeKind.NONE, new string[0] ) },
            { "CDT(Cuba)",          new TimeZoneShort("CDT(Cuba)", "Cuba Daylight Time", new TimeSpan(-04, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "CDT",                new TimeZoneShort("CDT", "Central Daylight Time", new TimeSpan(-05, 00, 0), AlternativeKind.AMBIGUOUS, new string[] { "CDT(Cuba)", "CDT(Central)" } ) },
            { "CEST",               new TimeZoneShort("CEST", "Central European Summer Time", new TimeSpan(+02, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "CET",                new TimeZoneShort("CET", "Central European Time", new TimeSpan(+01, 00, 0), AlternativeKind.DST_ALT, new string[] { "CEST" } ) },
            { "CHADT",              new TimeZoneShort("CHADT", "Chatham Daylight Time", new TimeSpan(+13, 45, 0), AlternativeKind.NONE, new string[0]) },
            { "CHAST",              new TimeZoneShort("CHAST", "Chatham Standard Time", new TimeSpan(+12, 45, 0), AlternativeKind.DST_ALT, new string[] { "CHADT" } ) },
            { "CHOT",               new TimeZoneShort("CHOT", "Choibalsan Standard Time", new TimeSpan(+08, 00, 0), AlternativeKind.NONE, new string[0]) }, // Mongolia does not observe DST
            { "CHOST",              new TimeZoneShort("CHOST", "Choibalsan Summer Time", new TimeSpan(+09, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "CHST",               new TimeZoneShort("CHST", "Chamorro Standard Time", new TimeSpan(+10, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "CHUT",               new TimeZoneShort("CHUT", "Chuuk Time", new TimeSpan(+10, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "CIST",               new TimeZoneShort("CIST", "Clipperton Island Standard Time", new TimeSpan(-08, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "CKT",                new TimeZoneShort("CKT", "Cook Island Time", new TimeSpan(-10, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "CLST",               new TimeZoneShort("CLST", "Chile Summer Time", new TimeSpan(-03, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "CLT",                new TimeZoneShort("CLT", "Chile Standard Time", new TimeSpan(-04, 00, 0), AlternativeKind.DST_ALT, new string[] { "CLST" } ) },
            { "COST",               new TimeZoneShort("COST", "Colombia Summer Time", new TimeSpan(-04, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "COT",                new TimeZoneShort("COT", "Colombia Time", new TimeSpan(-05, 00, 0), AlternativeKind.NONE, new string[0]) }, // Colombia does not observe DST
            { "CST(Central)",       new TimeZoneShort("CST(Central)", "Central Standard Time", new TimeSpan(-06, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "CST(China)",         new TimeZoneShort("CST(China)", "China Standard Time", new TimeSpan(+08, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "CST(Cuba)",          new TimeZoneShort("CST(Cuba)", "Cuba Standard Time", new TimeSpan(-05, 00, 0), AlternativeKind.DST_ALT, new string[] { "CDT(Cuba)" } ) },
            { "CST",                new TimeZoneShort("CST", "Central Standard Time", new TimeSpan(-06, 00, 0), AlternativeKind.AMBIGUOUS, new string[] { "CST(Central)", "CST(China)", "CST(Cuba)" } ) },
            { "CT",                 new TimeZoneShort("CT", "Central Time", new TimeSpan(-06, 00, 0), AlternativeKind.DST_ALIAS, new string[] { "CST", "CDT" } ) },
            { "CVT",                new TimeZoneShort("CVT", "Cape Verde Time", new TimeSpan(-01, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "CWST",               new TimeZoneShort("CWST", "Central Western Standard Time (Australia, unofficial)", new TimeSpan(+08, 45, 0), AlternativeKind.NONE, new string[0]) },
            { "CXT",                new TimeZoneShort("CXT", "Christmas Island Time", new TimeSpan(+07, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "DAVT",               new TimeZoneShort("DAVT", "Davis Time", new TimeSpan(+07, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "DDUT",               new TimeZoneShort("DDUT", "Dumont d'Urville Time (in French Antarctic station)", new TimeSpan(+10, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "DFT",                new TimeZoneShort("DFT", "AIX-specific equivalent of Central European Time", new TimeSpan(+01, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "EASST",              new TimeZoneShort("EASST", "Easter Island Summer Time", new TimeSpan(-05, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "EAST",               new TimeZoneShort("EAST", "Easter Island Standard Time", new TimeSpan(-06, 00, 0), AlternativeKind.DST_ALT, new string[] { "EASST" } ) },
            { "EAT",                new TimeZoneShort("EAT", "East Africa Time", new TimeSpan(+03, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "ECT(Carribean)",     new TimeZoneShort("ECT(Carribean)", "Eastern Caribbean Time", new TimeSpan(-04, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "ECT(Ecuador)",       new TimeZoneShort("ECT(Ecuador)", "Ecuador Time", new TimeSpan(-05, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "ECT",                new TimeZoneShort("ECT", "Ecuador Time", new TimeSpan(-05, 00, 0), AlternativeKind.AMBIGUOUS, new string[] { "ECT(Carribean)", "ECT(Ecuador)" } ) },
            { "EDT",                new TimeZoneShort("EDT", "Eastern Daylight Time", new TimeSpan(-04, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "EEST",               new TimeZoneShort("EEST", "Eastern European Summer Time", new TimeSpan(+03, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "EET",                new TimeZoneShort("EET", "Eastern European Time", new TimeSpan(+02, 00, 0), AlternativeKind.DST_ALT, new string[] { "EEST" } ) },
            // EGT and EGST are no longer used since 2023 (all Greenland except for Pituffik Base uses West Greenland Time)
            { "EST",                new TimeZoneShort("EST", "Eastern Standard Time", new TimeSpan(-05, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "ET",                 new TimeZoneShort("ET", "Eastern Time", new TimeSpan(-05, 00, 0), AlternativeKind.DST_ALIAS, new string[] { "EST", "EDT" } ) },
            { "FET",                new TimeZoneShort("FET", "Further-eastern European Time", new TimeSpan(+03, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "FJT",                new TimeZoneShort("FJT", "Fiji Time", new TimeSpan(+12, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "FKST",               new TimeZoneShort("FKST", "Falkland Islands Summer Time", new TimeSpan(-03, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "FKT",                new TimeZoneShort("FKT", "Falkland Islands Time", new TimeSpan(-04, 00, 0), AlternativeKind.NONE, new string[0]) }, // Falkland Islands don't observe DST anymore
            { "FNT",                new TimeZoneShort("FNT", "Fernando de Noronha Time", new TimeSpan(-02, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "GALT",               new TimeZoneShort("GALT", "Galápagos Time", new TimeSpan(-06, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "GAMT",               new TimeZoneShort("GAMT", "Gambier Islands Time", new TimeSpan(-09, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "GET",                new TimeZoneShort("GET", "Georgia Standard Time", new TimeSpan(+04, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "GFT",                new TimeZoneShort("GFT", "French Guiana Time", new TimeSpan(-03, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "GILT",               new TimeZoneShort("GILT", "Gilbert Island Time", new TimeSpan(+12, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "GIT",                new TimeZoneShort("GIT", "Gambier Island Time", new TimeSpan(-09, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "GMT",                new TimeZoneShort("GMT", "Greenwich Mean Time", new TimeSpan(+00, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "GST(Georgia)",       new TimeZoneShort("GST(Georgia)", "South Georgia and the South Sandwich Islands Time", new TimeSpan(-02, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "GST(Gulf)",          new TimeZoneShort("GST(Gulf)", "Gulf Standard Time", new TimeSpan(+04, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "GST",                new TimeZoneShort("GST", "Gulf Standard Time", new TimeSpan(+04, 00, 0), AlternativeKind.AMBIGUOUS, new string[] { "GST(Georgia)", "GST(Gulf)" } ) },
            { "GYT",                new TimeZoneShort("GYT", "Guyana Time", new TimeSpan(-04, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "HDT",                new TimeZoneShort("HDT", "Hawaii–Aleutian Daylight Time", new TimeSpan(-09, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "HAEC",               new TimeZoneShort("HAEC", "Heure Avancée d'Europe Centrale", new TimeSpan(+02, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "HST",                new TimeZoneShort("HST", "Hawaii–Aleutian Standard Time", new TimeSpan(-10, 00, 0), AlternativeKind.DST_ALT, new string[] { "HDT" } ) },
            { "HKT",                new TimeZoneShort("HKT", "Hong Kong Time", new TimeSpan(+08, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "HMT",                new TimeZoneShort("HMT", "Heard and McDonald Islands Time", new TimeSpan(+05, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "HOVST",              new TimeZoneShort("HOVST", "Hovd Summer Time", new TimeSpan(+08, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "HOVT",               new TimeZoneShort("HOVT", "Hovd Time", new TimeSpan(+07, 00, 0), AlternativeKind.NONE, new string[0]) }, // Hovd does not observe DST anymore
            { "ICT",                new TimeZoneShort("ICT", "Indochina Time", new TimeSpan(+07, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "IDLW",               new TimeZoneShort("IDLW", "International Date Line West time zone", new TimeSpan(-12, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "IDT",                new TimeZoneShort("IDT", "Israel Daylight Time", new TimeSpan(+03, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "IOT",                new TimeZoneShort("IOT", "Indian Ocean Time", new TimeSpan(+06, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "IRDT",               new TimeZoneShort("IRDT", "Iran Daylight Time", new TimeSpan(+04, 30, 0), AlternativeKind.NONE, new string[0]) },
            { "IRKT",               new TimeZoneShort("IRKT", "Irkutsk Time", new TimeSpan(+08, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "IRST",               new TimeZoneShort("IRST", "Iran Standard Time", new TimeSpan(+03, 30, 0), AlternativeKind.NONE, new string[0]) }, // Iran does not observe DST anymore
            { "IST(Indian)",        new TimeZoneShort("IST(Indian)", "Indian Standard Time", new TimeSpan(+05, 30, 0), AlternativeKind.NONE, new string[0]) },
            { "IST(Irish)",         new TimeZoneShort("IST(Irish)", "Irish Standard Time", new TimeSpan(+01, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "IST(Israel)",        new TimeZoneShort("IST(Israel)", "Israel Standard Time", new TimeSpan(+02, 00, 0), AlternativeKind.DST_ALT, new string[] { "IDT" } ) },
            { "IST",                new TimeZoneShort("IST", "Irish Standard Time", new TimeSpan(+01, 00, 0), AlternativeKind.AMBIGUOUS, new string[] { "IST(Indian)", "IST(Irish)", "IST(Israel)" } ) },
            { "JST",                new TimeZoneShort("JST", "Japan Standard Time", new TimeSpan(+09, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "KALT",               new TimeZoneShort("KALT", "Kaliningrad Time", new TimeSpan(+02, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "KGT",                new TimeZoneShort("KGT", "Kyrgyzstan Time", new TimeSpan(+06, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "KOST",               new TimeZoneShort("KOST", "Kosrae Time", new TimeSpan(+11, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "KRAT",               new TimeZoneShort("KRAT", "Krasnoyarsk Time", new TimeSpan(+07, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "KST",                new TimeZoneShort("KST", "Korea Standard Time", new TimeSpan(+09, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "LHST(Standard)",     new TimeZoneShort("LHST(Standard)", "Lord Howe Standard Time", new TimeSpan(+10, 30, 0), AlternativeKind.NONE, new string[0]) },
            { "LHST(Summer)",       new TimeZoneShort("LHST(Summer)", "Lord Howe Summer Time", new TimeSpan(+11, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "LHST",               new TimeZoneShort("LHST", "Lord Howe Standard Time", new TimeSpan(+10, 30, 0), AlternativeKind.DST_ALIAS, new string[] { "LHST(Standard)", "LHST(Summer)" } ) },
            { "LINT",               new TimeZoneShort("LINT", "Line Islands Time", new TimeSpan(+14, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "MAGT",               new TimeZoneShort("MAGT", "Magadan Time", new TimeSpan(+12, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "MART",               new TimeZoneShort("MART", "Marquesas Islands Time", new TimeSpan(-09, 30, 0), AlternativeKind.NONE, new string[0]) },
            { "MAWT",               new TimeZoneShort("MAWT", "Mawson Station Time", new TimeSpan(+05, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "MDT",                new TimeZoneShort("MDT", "Mountain Daylight Time", new TimeSpan(-06, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "MET",                new TimeZoneShort("MET", "Middle European Time", new TimeSpan(+01, 00, 0), AlternativeKind.DST_ALT, new string[] { "MEST" } ) },
            { "MEST",               new TimeZoneShort("MEST", "Middle European Summer Time", new TimeSpan(+02, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "MHT",                new TimeZoneShort("MHT", "Marshall Islands Time", new TimeSpan(+12, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "MIST",               new TimeZoneShort("MIST", "Macquarie Island Station Time", new TimeSpan(+11, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "MIT",                new TimeZoneShort("MIT", "Marquesas Islands Time", new TimeSpan(-09, 30, 0), AlternativeKind.NONE, new string[0]) },
            { "MMT",                new TimeZoneShort("MMT", "Myanmar Standard Time", new TimeSpan(+06, 30, 0), AlternativeKind.NONE, new string[0]) },
            { "MSK",                new TimeZoneShort("MSK", "Moscow Time", new TimeSpan(+03, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "MST(Malaysian)",     new TimeZoneShort("MST(Malaysian)", "Malaysian Standard Time", new TimeSpan(+08, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "MST(Mountain)",      new TimeZoneShort("MST(Mountain)", "Mountain Standard Time", new TimeSpan(-07, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "MST",                new TimeZoneShort("MST", "Mountain Standard Time", new TimeSpan(-07, 00, 0), AlternativeKind.AMBIGUOUS, new string[] { "MST(Mountain)", "MST(Malaysian)" } ) },
            { "MT",                 new TimeZoneShort("MT", "Mountain Time", new TimeSpan(-07, 00, 0), AlternativeKind.DST_ALIAS, new string[] { "MST", "MDT" } ) },
            { "MUT",                new TimeZoneShort("MUT", "Mauritius Time", new TimeSpan(+04, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "MVT",                new TimeZoneShort("MVT", "Maldives Time", new TimeSpan(+05, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "MYT",                new TimeZoneShort("MYT", "Malaysia Time", new TimeSpan(+08, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "NCT",                new TimeZoneShort("NCT", "New Caledonia Time", new TimeSpan(+11, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "NDT",                new TimeZoneShort("NDT", "Newfoundland Daylight Time", new TimeSpan(-02,-30, 0), AlternativeKind.NONE, new string[0]) },
            { "NFT",                new TimeZoneShort("NFT", "Norfolk Island Time", new TimeSpan(+11, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "NOVT",               new TimeZoneShort("NOVT", "Novosibirsk Time", new TimeSpan(+07, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "NPT",                new TimeZoneShort("NPT", "Nepal Time", new TimeSpan(+05, 45, 0), AlternativeKind.NONE, new string[0]) },
            { "NST",                new TimeZoneShort("NST", "Newfoundland Standard Time", new TimeSpan(-03,-30, 0), AlternativeKind.NONE, new string[0]) },
            { "NT",                 new TimeZoneShort("NT", "Newfoundland Time", new TimeSpan(-03,-30, 0), AlternativeKind.DST_ALIAS, new string[] { "NST", "NDT" } ) },
            { "NUT",                new TimeZoneShort("NUT", "Niue Time", new TimeSpan(-11, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "NZDT",               new TimeZoneShort("NZDT", "New Zealand Daylight Time", new TimeSpan(+13, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "NZDST",              new TimeZoneShort("NZDST", "New Zealand Daylight Saving Time", new TimeSpan(+13, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "NZST",               new TimeZoneShort("NZST", "New Zealand Standard Time", new TimeSpan(+12, 00, 0), AlternativeKind.DST_ALT, new string[] { "NZDT" } ) },
            { "OMST",               new TimeZoneShort("OMST", "Omsk Time", new TimeSpan(+06, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "ORAT",               new TimeZoneShort("ORAT", "Oral Time", new TimeSpan(+05, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "PDT",                new TimeZoneShort("PDT", "Pacific Daylight Time", new TimeSpan(-07, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "PET",                new TimeZoneShort("PET", "Peru Time", new TimeSpan(-05, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "PETT",               new TimeZoneShort("PETT", "Kamchatka Time", new TimeSpan(+12, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "PGT",                new TimeZoneShort("PGT", "Papua New Guinea Time", new TimeSpan(+10, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "PHOT",               new TimeZoneShort("PHOT", "Phoenix Island Time", new TimeSpan(+13, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "PHT",                new TimeZoneShort("PHT", "Philippine Time", new TimeSpan(+08, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "PHST",               new TimeZoneShort("PHST", "Philippine Standard Time", new TimeSpan(+08, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "PKT",                new TimeZoneShort("PKT", "Pakistan Standard Time", new TimeSpan(+05, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "PMDT",               new TimeZoneShort("PMDT", "Saint Pierre and Miquelon Daylight Time", new TimeSpan(-02, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "PMST",               new TimeZoneShort("PMST", "Saint Pierre and Miquelon Standard Time", new TimeSpan(-03, 00, 0), AlternativeKind.DST_ALT, new string[] { "PMDT" } ) },
            { "PONT",               new TimeZoneShort("PONT", "Pohnpei Standard Time", new TimeSpan(+11, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "PST",                new TimeZoneShort("PST", "Pacific Standard Time", new TimeSpan(-08, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "PT",                 new TimeZoneShort("PT", "Pacific Time", new TimeSpan(-08, 00, 0), AlternativeKind.DST_ALIAS, new string[] { "PST", "PDT" } ) },
            { "PWT",                new TimeZoneShort("PWT", "Palau Time", new TimeSpan(+09, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "PYST",               new TimeZoneShort("PYST", "Paraguay Summer Time", new TimeSpan(-03, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "PYT",                new TimeZoneShort("PYT", "Paraguay Time", new TimeSpan(-03, 00, 0), AlternativeKind.NONE, new string[0]) }, // Paraguay does not observe DST anymore, remained on PYST
            { "RET",                new TimeZoneShort("RET", "Réunion Time", new TimeSpan(+04, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "ROTT",               new TimeZoneShort("ROTT", "Rothera Research Station Time", new TimeSpan(-03, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "SAKT",               new TimeZoneShort("SAKT", "Sakhalin Island Time", new TimeSpan(+11, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "SAMT",               new TimeZoneShort("SAMT", "Samara Time", new TimeSpan(+04, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "SAST",               new TimeZoneShort("SAST", "South African Standard Time", new TimeSpan(+02, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "SBT",                new TimeZoneShort("SBT", "Solomon Islands Time", new TimeSpan(+11, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "SCT",                new TimeZoneShort("SCT", "Seychelles Time", new TimeSpan(+04, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "SDT",                new TimeZoneShort("SDT", "Samoa Daylight Time", new TimeSpan(-10, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "SGT",                new TimeZoneShort("SGT", "Singapore Time", new TimeSpan(+08, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "SLST",               new TimeZoneShort("SLST", "Sri Lanka Standard Time", new TimeSpan(+05, 30, 0), AlternativeKind.NONE, new string[0]) },
            { "SRET",               new TimeZoneShort("SRET", "Srednekolymsk Time", new TimeSpan(+11, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "SRT",                new TimeZoneShort("SRT", "Suriname Time", new TimeSpan(-03, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "SST",                new TimeZoneShort("SST", "Samoa Standard Time", new TimeSpan(-11, 00, 0), AlternativeKind.NONE, new string[0]) }, // Samoa does not observe DST anymore
            { "SYOT",               new TimeZoneShort("SYOT", "Showa Station Time", new TimeSpan(+03, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "TAHT",               new TimeZoneShort("TAHT", "Tahiti Time", new TimeSpan(-10, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "THA",                new TimeZoneShort("THA", "Thailand Standard Time", new TimeSpan(+07, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "TFT",                new TimeZoneShort("TFT", "French Southern and Antarctic Time", new TimeSpan(+05, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "TJT",                new TimeZoneShort("TJT", "Tajikistan Time", new TimeSpan(+05, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "TKT",                new TimeZoneShort("TKT", "Tokelau Time", new TimeSpan(+13, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "TLT",                new TimeZoneShort("TLT", "Timor Leste Time", new TimeSpan(+09, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "TMT",                new TimeZoneShort("TMT", "Turkmenistan Time", new TimeSpan(+05, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "TRT",                new TimeZoneShort("TRT", "Turkey Time", new TimeSpan(+03, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "TOT",                new TimeZoneShort("TOT", "Tonga Time", new TimeSpan(+13, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "TST",                new TimeZoneShort("TST", "Taiwan Standard Time", new TimeSpan(+08, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "TVT",                new TimeZoneShort("TVT", "Tuvalu Time", new TimeSpan(+12, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "ULAST",              new TimeZoneShort("ULAST", "Ulaanbaatar Summer Time", new TimeSpan(+09, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "ULAT",               new TimeZoneShort("ULAT", "Ulaanbaatar Standard Time", new TimeSpan(+08, 00, 0), AlternativeKind.NONE, new string[0]) }, // Mongolia does not observe DST anymore
            { "UTC",                new TimeZoneShort("UTC", "Coordinated Universal Time", new TimeSpan(+00, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "UYST",               new TimeZoneShort("UYST", "Uruguay Summer Time", new TimeSpan(-02, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "UYT",                new TimeZoneShort("UYT", "Uruguay Standard Time", new TimeSpan(-03, 00, 0), AlternativeKind.NONE, new string[0]) }, // Uruguay does not observe DST anymore
            { "UZT",                new TimeZoneShort("UZT", "Uzbekistan Time", new TimeSpan(+05, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "VET",                new TimeZoneShort("VET", "Venezuelan Standard Time", new TimeSpan(-04, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "VLAT",               new TimeZoneShort("VLAT", "Vladivostok Time", new TimeSpan(+10, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "VOLT",               new TimeZoneShort("VOLT", "Volgograd Time", new TimeSpan(+03, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "VOST",               new TimeZoneShort("VOST", "Vostok Station Time", new TimeSpan(+06, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "VUT",                new TimeZoneShort("VUT", "Vanuatu Time", new TimeSpan(+11, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "WAKT",               new TimeZoneShort("WAKT", "Wake Island Time", new TimeSpan(+12, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "WAST",               new TimeZoneShort("WAST", "West Africa Summer Time", new TimeSpan(+02, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "WAT",                new TimeZoneShort("WAT", "West Africa Time", new TimeSpan(+01, 00, 0), AlternativeKind.NONE, new string[0]) }, // West Africa does not observe DST anymore
            { "WEST",               new TimeZoneShort("WEST", "Western European Summer Time", new TimeSpan(+01, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "WET",                new TimeZoneShort("WET", "Western European Time", new TimeSpan(+00, 00, 0), AlternativeKind.DST_ALT, new string[] { "WEST" } ) },
            { "WIB",                new TimeZoneShort("WIB", "Western Indonesian Time", new TimeSpan(+07, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "WIT",                new TimeZoneShort("WIT", "Eastern Indonesian Time", new TimeSpan(+09, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "WITA",               new TimeZoneShort("WITA", "Central Indonesia Time", new TimeSpan(+08, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "WGST",               new TimeZoneShort("WGST", "West Greenland Summer Time", new TimeSpan(-01, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "WGT",                new TimeZoneShort("WGT", "West Greenland Time", new TimeSpan(-02, 00, 0), AlternativeKind.DST_ALT, new string[] { "WGST" } ) },
            { "WST",                new TimeZoneShort("WST", "Western Standard Time", new TimeSpan(+08, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "YAKT",               new TimeZoneShort("YAKT", "Yakutsk Time", new TimeSpan(+09, 00, 0), AlternativeKind.NONE, new string[0]) },
            { "YEKT",               new TimeZoneShort("YEKT", "Yekaterinburg Time", new TimeSpan(+05, 00, 0), AlternativeKind.NONE, new string[0]) },
        };

        public static TimeZoneShort GetFromAbbreviation(string abbrev)
        {
            return DATABASE[abbrev];
        }

        public static TimeZoneShort TryGetFromAbbreviation(string abbrev)
        {
            TimeZoneShort ret = null;

            if (DATABASE.ContainsKey(abbrev))
                ret = DATABASE[abbrev];

            return ret;
        }

        public static string GetDSTDetails(string abbrev)
        {
            return ABBREV_TO_IANA[abbrev];
        }

        // TEST METHODS
        internal static Dictionary<string, TimeZoneShort> GetTimeZoneDB() => DATABASE;
        internal static Dictionary<string, string> GetAbbrevToIANA() => ABBREV_TO_IANA;
    }
}
