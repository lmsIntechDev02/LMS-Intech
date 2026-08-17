using System;

namespace Repository.Common
{
    public static class Common
    {
        public static (string CountryName, string TimeZone) GetAttendacneTimeZone(string attendacnetimezone)
        {
            string shortCountryName = attendacnetimezone.Substring(0, 3);
            string timeZone = "";
            string countryName = "";
            
            
            if (attendacnetimezone.Substring(0, 4) == "NG-L")
            {
                timeZone = "W. Central Africa Standard Time";
                countryName = "Nigeria Lagos";
            }
            else if (attendacnetimezone.Substring(0, 4) == "NG-P")
            {
                timeZone = "W. Central Africa Standard Time";
                countryName = "Nigeria Port Harcourt";
            }
            else if (attendacnetimezone.Substring(0, 4) == "IN01")
            {
                timeZone = "Pakistan Standard Time";
                countryName = "Pakistan";
            }
            else if (attendacnetimezone.Substring(0, 4) == "IN03")
            {
                timeZone = "W. Central Africa Standard Time";
                countryName = "Nigeria";
            }
            else if (attendacnetimezone.Substring(0, 4) == "IN04")
            {
                timeZone = "W. Central Africa Standard Time";
                countryName = "Angola";
            }
            else if (attendacnetimezone.Substring(0, 4) == "IN05")
            {
                timeZone = "Arab Standard Time";
                countryName = "Saudi Arabia";
            }
            else if (attendacnetimezone.Substring(0, 4) == "IN07")
            {
                timeZone = "Arab Standard Time";
                countryName = "Iraq";
            }
            else if (attendacnetimezone.Substring(0, 4) == "IN08")
            {
                timeZone = "Arab Standard Time";
                countryName = "United Arab Emirates";
            }
            else
            {
                switch (shortCountryName)
                {
                    case "PK ":
                        timeZone = "Pakistan Standard Time";
                        countryName = "Pakistan";
                        break;
                    case "PAK":
                        timeZone = "Pakistan Standard Time";
                        countryName = "Pakistan";
                        break;
                    case "UAE":
                        timeZone = "Arab Standard Time";
                        countryName = "United Arab Emirates";
                        break;
                    case "KSA":
                        timeZone = "Arab Standard Time";
                        countryName = "Saudi Arabia";
                        break;
                    case "GBR":
                        timeZone = "GMT Standard Time";
                        countryName = "United Kingdom";
                        break;
                    case "USA":
                        timeZone = "Central Standard Time";
                        countryName = "United States";
                        break;
                    case "NGA":
                        timeZone = "W. Central Africa Standard Time";
                        countryName = "Nigeria";
                        break;
                    case "NG-":
                        timeZone = "W. Central Africa Standard Time";
                        countryName = "Nigeria";
                        break;
                    case "EGY":
                        timeZone = "Egypt Standard Time";
                        countryName = "Egypt";
                        break;
                    case "IRQ":
                        timeZone = "Arabic Standard Time";
                        countryName = "Iraq";
                        break;
                    case "OMN":
                        timeZone = "Arabian Standard Time";
                        countryName = "Oman";
                        break;
                    case "QAT":
                        timeZone = "Arab Standard Time";
                        countryName = "Qatar";
                        break;
                    case "AGO":
                        timeZone = "W. Central Africa Standard Time";
                        countryName = "Angola";
                        break;
                    case "KAZ":
                        timeZone = "West Asia Standard Time";
                        countryName = "Kazakhstan";
                        break;
                    // Add more cases for other countries
                    default:
                        timeZone = String.Empty; // Or handle the default case based on your requirements
                        countryName = String.Empty;
                        break;
                }
            }
                return (countryName, timeZone);
        }
        public static (string CountryName, string TimeZone) GetAttendacneTimeZone1(string attendacnetimezone)
        {
            //string devName =  attendacnetimezone.ToUpper().Trim();

            //attendacnetimezone= "IN08-UAE Reception-OUT";

            


            // Special handling for Nigeria
            // NG-L and NG-P both should return Nigeria
            // Nigeria special handling
            if (attendacnetimezone.Substring(0, 4) == "NG-L")
            {
                return ("Nigeria Lagos", "W. Central Africa Standard Time");
            }

            if (attendacnetimezone.Substring(0, 4) == "NG-P")
            {
                return ("Nigeria Port Harcourt", "W. Central Africa Standard Time");
            }
            //string shortCountryName = "";
            //string timeZone = "";
            //string countryName = "";

            //// Special Nigeria locations
            //if (devName.StartsWith("NG-L"))
            //{
            //    return ("Nigeria Lagos", "W. Central Africa Standard Time");
            //}

            //if (devName.StartsWith("NG-P"))
            //{
            //    return ("Nigeria Port Harcourt", "W. Central Africa Standard Time");
            //}

            //// Split values
            //string[] parts = devName
            //    .Replace("(", "")
            //    .Replace(")", "")
            //    .Split(new char[] { '-', ' '  }, StringSplitOptions.RemoveEmptyEntries);

            // Detect country code dynamically
            if (string.IsNullOrWhiteSpace(attendacnetimezone))
                return ("", "");

            string devName = attendacnetimezone.ToUpper().Trim();
            // Replace special characters with spaces
            devName = devName
                .Replace("_", " ")
                .Replace("-", " ")
                .Replace("=", " ")
                .Replace("'", " ")
                .Replace("(", " ")
                .Replace(")", " ")
                .Replace("/", " ")
                .Replace("\\", " ")
                .Replace(".", " ");

            // Remove only LAST IN / OUT
            devName = System.Text.RegularExpressions.Regex.Replace(
                devName,
                @"\s+(IN|OUT)\s*$",
                "",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // Ignore unwanted words
            string[] ignoreWords =
            {
        "RECEPTION",
        "DOOR",
        "BACKDOOR",
        "FACE",
        "FACTORY",
        "WAREHOUSE",
        "FACILITY",
        "OFFICE",
        "SERVER",
        "ROOM",
        "MAIN",
        "FLOOR",
        "GF",
        "FF",
        "PHC",
        "PH",
        "LO",
        "DEVICE",
        "PANEL",
        "SA",
        "NTC"
    };


            foreach (string word in ignoreWords)
            {
                devName = System.Text.RegularExpressions.Regex.Replace(
                    devName,
                    $@"\b{word}\b",
                    "",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }

            //devName = devName.Replace("(", "")
            //                 .Replace(")", "")
            //                 .Trim();
            devName = System.Text.RegularExpressions.Regex.Replace(devName, @"\s+", " ").Trim();
            string shortCountryName = "";
            string countryName = "";
            string timeZone = "";

            // Split values
            string[] parts = devName.Split(' ');

            //string shortCountryName = "";
            //string timeZone = "";
            //string countryName = "";

          

            // Split values
            //string[] parts = devName
            //    .Split(new char[] { '-', ' ' ,'_'}, StringSplitOptions.RemoveEmptyEntries);
            foreach (string part in parts)
            {
                switch (part)
                {
                    case "PK":
                    case "PAK":
                    case "UAE":
                    case "ARE":
                    case "KSA":
                    case "SAU":
                    case "USA":
                    case "US":
                    case "NGA":
                    case "NG":
                    case "GBR":
                    case "UK":
                    case "IRQ":
                    case "QAT":
                    case "OMN":
                    case "EGY":
                    case "AGO":
                    case "KAZ":
                    case "IND":
                    case "IN":
                    case "BGD":
                    case "CHN":
                    case "JPN":
                    case "DEU":
                    case "GER":
                    case "FRA":
                    case "CAN":
                    case "AUS":
                    case "TUR":
                    case "ZAF":
                    case "MYS":
                    case "SGP":
                    case "THA":
                    case "IDN":
                    case "BRA":
                    case "MEX":
                    case "RUS":
                    case "ITA":
                    case "ESP":
                    case "NLD":
                    case "CHE":
                    case "SWE":
                    case "NOR":
                    case "DNK":
                    case "NZL":
                    case "KOR":
                    case "LKA":
                    case "NPL":
                    case "AFG":

                        shortCountryName = part;
                        break;
                }

                if (!string.IsNullOrEmpty(shortCountryName))
                    break;
            }

            switch (shortCountryName)
            {
                case "PK":
                case "PAK":
                    countryName = "Pakistan";
                    timeZone = "Pakistan Standard Time";
                    break;

                case "UAE":
                case "ARE":
                    countryName = "United Arab Emirates";
                    timeZone = "Arabian Standard Time";
                    break;

                case "KSA":
                case "SAU":
                    countryName = "Saudi Arabia";
                    timeZone = "Arab Standard Time";
                    break;

                case "USA":
                case "US":
                    countryName = "United States";
                    timeZone = "Central Standard Time";
                    break;

                case "NGA":
                case "NG":
                    countryName = "Nigeria";
                    timeZone = "W. Central Africa Standard Time";
                    break;

                case "GBR":
                case "UK":
                    countryName = "United Kingdom";
                    timeZone = "GMT Standard Time";
                    break;

                case "IRQ":
                    countryName = "Iraq";
                    timeZone = "Arabic Standard Time";
                    break;

                case "QAT":
                    countryName = "Qatar";
                    timeZone = "Arab Standard Time";
                    break;

                case "OMN":
                    countryName = "Oman";
                    timeZone = "Arabian Standard Time";
                    break;

                case "EGY":
                    countryName = "Egypt";
                    timeZone = "Egypt Standard Time";
                    break;

                case "AGO":
                    countryName = "Angola";
                    timeZone = "W. Central Africa Standard Time";
                    break;

                case "KAZ":
                    countryName = "Kazakhstan";
                    timeZone = "West Asia Standard Time";
                    break;

                case "IND":
                case "IN":
                    countryName = "India";
                    timeZone = "India Standard Time";
                    break;

                case "BGD":
                    countryName = "Bangladesh";
                    timeZone = "Bangladesh Standard Time";
                    break;

                case "CHN":
                    countryName = "China";
                    timeZone = "China Standard Time";
                    break;

                case "JPN":
                    countryName = "Japan";
                    timeZone = "Tokyo Standard Time";
                    break;

                case "DEU":
                case "GER":
                    countryName = "Germany";
                    timeZone = "W. Europe Standard Time";
                    break;

                case "FRA":
                    countryName = "France";
                    timeZone = "Romance Standard Time";
                    break;

                case "CAN":
                    countryName = "Canada";
                    timeZone = "Canada Central Standard Time";
                    break;

                case "AUS":
                    countryName = "Australia";
                    timeZone = "AUS Eastern Standard Time";
                    break;

                case "TUR":
                    countryName = "Turkey";
                    timeZone = "Turkey Standard Time";
                    break;

                case "ZAF":
                    countryName = "South Africa";
                    timeZone = "South Africa Standard Time";
                    break;

                case "MYS":
                    countryName = "Malaysia";
                    timeZone = "Singapore Standard Time";
                    break;

                case "SGP":
                    countryName = "Singapore";
                    timeZone = "Singapore Standard Time";
                    break;

                case "THA":
                    countryName = "Thailand";
                    timeZone = "SE Asia Standard Time";
                    break;

                case "IDN":
                    countryName = "Indonesia";
                    timeZone = "SE Asia Standard Time";
                    break;

                case "BRA":
                    countryName = "Brazil";
                    timeZone = "E. South America Standard Time";
                    break;

                case "MEX":
                    countryName = "Mexico";
                    timeZone = "Central Standard Time (Mexico)";
                    break;

                case "RUS":
                    countryName = "Russia";
                    timeZone = "Russian Standard Time";
                    break;

                case "ITA":
                    countryName = "Italy";
                    timeZone = "W. Europe Standard Time";
                    break;

                case "ESP":
                    countryName = "Spain";
                    timeZone = "Romance Standard Time";
                    break;

                case "NLD":
                    countryName = "Netherlands";
                    timeZone = "W. Europe Standard Time";
                    break;

                case "CHE":
                    countryName = "Switzerland";
                    timeZone = "W. Europe Standard Time";
                    break;

                case "SWE":
                    countryName = "Sweden";
                    timeZone = "W. Europe Standard Time";
                    break;

                case "NOR":
                    countryName = "Norway";
                    timeZone = "W. Europe Standard Time";
                    break;

                case "DNK":
                    countryName = "Denmark";
                    timeZone = "Romance Standard Time";
                    break;

                case "NZL":
                    countryName = "New Zealand";
                    timeZone = "New Zealand Standard Time";
                    break;

                case "KOR":
                    countryName = "South Korea";
                    timeZone = "Korea Standard Time";
                    break;

                case "LKA":
                    countryName = "Sri Lanka";
                    timeZone = "Sri Lanka Standard Time";
                    break;

                case "NPL":
                    countryName = "Nepal";
                    timeZone = "Nepal Standard Time";
                    break;

                case "AFG":
                    countryName = "Afghanistan";
                    timeZone = "Afghanistan Standard Time";
                    break;

                default:
                    countryName = "";
                    timeZone = "";
                    break;
            }

            return (countryName, timeZone);
        }
        public static (string CountryName, string TimeZone) GetAttendacneTimeZone2(string attendacnetimezone)
        {
            string devName = attendacnetimezone.ToUpper();


            string shortCountryName = devName.Length >= 3
                ? devName.Substring(0, 3)
                : devName;

            string timeZone = String.Empty;
            string countryName = String.Empty;

            if (devName.StartsWith("NG-L"))
            {
                timeZone = "W. Central Africa Standard Time";
                countryName = "Nigeria Lagos";
            }
            else if (devName.StartsWith("NG-P"))
            {
                timeZone = "W. Central Africa Standard Time";
                countryName = "Nigeria Port Harcourt";
            }
            else
            {
               if(devName.Contains("-"))
                {
                    if (devName.Split('-').Length > 1)
                    {
                        shortCountryName = devName.Split('-')[1].ToString()+"-"+ devName.Split('-')[0].Substring(0, 1);
                    }
                    else
                    {
                        shortCountryName = devName.Split('-')[1].Split(' ')[0].ToString();
                    }
                    
                   
                }
                else  
                {
                    shortCountryName = devName.Split(' ')[1].ToString();
                }

                


                switch (shortCountryName)
                {
                    // Pakistan
                    case "PK ":
                    case "PAK":
                        timeZone = "Pakistan Standard Time";
                        countryName = "Pakistan";
                        break;

                    // UAE
                    case "UAE":
                    case "ARE":
                        timeZone = "Arabian Standard Time";
                        countryName = "United Arab Emirates";
                        break;

                    // Saudi Arabia
                    case "KSA":
                    case "SAU":
                        timeZone = "Arab Standard Time";
                        countryName = "Saudi Arabia";
                        break;

                    // United Kingdom
                    case "GBR":
                    case "UK ":
                        timeZone = "GMT Standard Time";
                        countryName = "United Kingdom";
                        break;

                    // United States
                    case "USA":
                        timeZone = "Central Standard Time";
                        countryName = "United States";
                        break;

                    // Nigeria
                    case "NGA":
                    case "NG-":
                        timeZone = "W. Central Africa Standard Time";
                        countryName = "Nigeria";
                        break;

                    // Egypt
                    case "EGY":
                        timeZone = "Egypt Standard Time";
                        countryName = "Egypt";
                        break;

                    // Iraq
                    case "IRQ":
                        timeZone = "Arabic Standard Time";
                        countryName = "Iraq";
                        break;

                    // Oman
                    case "OMN":
                        timeZone = "Arabian Standard Time";
                        countryName = "Oman";
                        break;

                    // Qatar
                    case "QAT":
                        timeZone = "Arab Standard Time";
                        countryName = "Qatar";
                        break;

                    // Angola
                    case "AGO":
                        timeZone = "W. Central Africa Standard Time";
                        countryName = "Angola";
                        break;

                    // Kazakhstan
                    case "KAZ":
                        timeZone = "West Asia Standard Time";
                        countryName = "Kazakhstan";
                        break;

                    // India
                    case "IND":
                        timeZone = "India Standard Time";
                        countryName = "India";
                        break;

                    // Bangladesh
                    case "BGD":
                        timeZone = "Bangladesh Standard Time";
                        countryName = "Bangladesh";
                        break;

                    // China
                    case "CHN":
                        timeZone = "China Standard Time";
                        countryName = "China";
                        break;

                    // Japan
                    case "JPN":
                        timeZone = "Tokyo Standard Time";
                        countryName = "Japan";
                        break;

                    // Germany
                    case "DEU":
                    case "GER":
                        timeZone = "W. Europe Standard Time";
                        countryName = "Germany";
                        break;

                    // France
                    case "FRA":
                        timeZone = "Romance Standard Time";
                        countryName = "France";
                        break;

                    // Canada
                    case "CAN":
                        timeZone = "Canada Central Standard Time";
                        countryName = "Canada";
                        break;

                    // Australia
                    case "AUS":
                        timeZone = "AUS Eastern Standard Time";
                        countryName = "Australia";
                        break;

                    // Turkey
                    case "TUR":
                        timeZone = "Turkey Standard Time";
                        countryName = "Turkey";
                        break;

                    // South Africa
                    case "ZAF":
                        timeZone = "South Africa Standard Time";
                        countryName = "South Africa";
                        break;

                    // Malaysia
                    case "MYS":
                        timeZone = "Singapore Standard Time";
                        countryName = "Malaysia";
                        break;

                    // Singapore
                    case "SGP":
                        timeZone = "Singapore Standard Time";
                        countryName = "Singapore";
                        break;

                    // Thailand
                    case "THA":
                        timeZone = "SE Asia Standard Time";
                        countryName = "Thailand";
                        break;

                    // Indonesia
                    case "IDN":
                        timeZone = "SE Asia Standard Time";
                        countryName = "Indonesia";
                        break;

                    // Brazil
                    case "BRA":
                        timeZone = "E. South America Standard Time";
                        countryName = "Brazil";
                        break;

                    // Mexico
                    case "MEX":
                        timeZone = "Central Standard Time (Mexico)";
                        countryName = "Mexico";
                        break;

                    // Russia
                    case "RUS":
                        timeZone = "Russian Standard Time";
                        countryName = "Russia";
                        break;

                    // Italy
                    case "ITA":
                        timeZone = "W. Europe Standard Time";
                        countryName = "Italy";
                        break;

                    // Spain
                    case "ESP":
                        timeZone = "Romance Standard Time";
                        countryName = "Spain";
                        break;

                    // Netherlands
                    case "NLD":
                        timeZone = "W. Europe Standard Time";
                        countryName = "Netherlands";
                        break;

                    // Switzerland
                    case "CHE":
                        timeZone = "W. Europe Standard Time";
                        countryName = "Switzerland";
                        break;

                    // Sweden
                    case "SWE":
                        timeZone = "W. Europe Standard Time";
                        countryName = "Sweden";
                        break;

                    // Norway
                    case "NOR":
                        timeZone = "W. Europe Standard Time";
                        countryName = "Norway";
                        break;

                    // Denmark
                    case "DNK":
                        timeZone = "Romance Standard Time";
                        countryName = "Denmark";
                        break;

                    // New Zealand
                    case "NZL":
                        timeZone = "New Zealand Standard Time";
                        countryName = "New Zealand";
                        break;

                    // South Korea
                    case "KOR":
                        timeZone = "Korea Standard Time";
                        countryName = "South Korea";
                        break;

                    // Sri Lanka
                    case "LKA":
                        timeZone = "Sri Lanka Standard Time";
                        countryName = "Sri Lanka";
                        break;

                    // Nepal
                    case "NPL":
                        timeZone = "Nepal Standard Time";
                        countryName = "Nepal";
                        break;

                    // Afghanistan
                    case "AFG":
                        timeZone = "Afghanistan Standard Time";
                        countryName = "Afghanistan";
                        break;

                    default:
                        timeZone = "";
                        countryName = "";
                        break;
                }
            }

            return (countryName, timeZone);
        } 
        
        public  static void GetAttendacneTimeZone22()
        {
            string[] attendanceLocations =
  {
    "PK First Floor-IN",
    "PK Face Reception-OUT",
    "IN01-PK First Floor B-OUT",
    "IN01-PK First Floor B-IN",
    "IN01-PK_GF Face Reception-IN",
    "IN05-KSA-Riyadh-IN",
    "KSA Riyadh IN",
    "PK BackDoor-IN",
    "PK Reception-OUT",
    "PK First Floor B-OUT",
    "KSA Factory NTC",
    "NG-PH Main Door",
    "IN05-KSA Factory NTC",
    "IN01-PK BackDoor-IN",
    "IN01-PK_GF Face Reception-OUT",
    "IN03-NG-PHC-GF-OUT",
    "PK Reception-IN",
    "KSA Factory-IN",
    "NG-LO BackDoor-IN",
    "IN07-IRQ-Face Reception-IN",
    "NG-PH Reception-IN",
    "UAE Factory Device",
    "NG-PHC Reception OUT",
    "NG-PH Reception-Out",
    "IN01-PK First Floor A-OUT",
    "IN03-NG-LO Reception-IN",
    "IN05-KSA Reception-OUT",
    "IN03-NG-LO BackDoor-Out",
    "IN03-NG-PHC Reception-IN",
    "IN03-NG-PHC-GF-IN",
    "PK First Floor-OUT",
    "NG-LO Reception-IN",
    "KSA Facility to Office-IN",
    "IN07-IRQ-Face Reception-OUT",
    "PK BackDoor-OUT",
    "NG-LO BackDoor-Out",
    "UAE Warehouse Door",
    "NG-PHC Reception-IN",
    "IN01-PK Face Reception-IN",
    "IN03-NG-PHC Reception-OUT",
    "IN03-NG-PHC-FF-OUT",
    "IN05-ACS Facility Office-IN",
    "PK First Floor B-IN",
    "NG-PHC Door-OUT",
    "IN03-NG-LO Reception-Out",
    "IN03-NG-PHC-FF-IN",
    "NG-PH Door-Out",
    "PK Face Reception-IN",
    "KSA Riyadh Office OUT",
    "IN01-PK BackDoor-OUT",
    "KSA Reception-OUT",
    "UAE Reception-IN",
    "KSA Factory-Out",
    "IN05-KSA Factory-OUT",
    "IN08-UAE Reception-OUT",
    "KSA Reception-IN",
    "IN05-KSA Factory-IN",
    "Panel Room PK",
    "UAE Reception-OUT",
    "PK First Floor A-IN",
    "IN01-PK Face Reception-OUT",
    "NG-PH Door-IN",
    "KSA Riyadh Office IN",
    "NG-PHC Reception IN",
    "IN01-PK First Floor A-IN",
    "NG-PH Main Door (SA)",
    "PK Server Room",
    "PK First Floor A-OUT",
    "IN08-UAE Warehouse Door",
    "IN08-UAE Reception-IN",
    "NG-LO Reception-Out",
    "IN05-ACS-Facility Office-IN",
    "IN05-KSA Reception-IN",
    "IN05-KSA Factory-NTC"
};

            foreach (string item in attendanceLocations)
            {
                var result = GetAttendacneTimeZone(item);

                 
            }
        }
    }
}