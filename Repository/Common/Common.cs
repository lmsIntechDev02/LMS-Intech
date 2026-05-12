using System;

namespace Repository.Common
{
    public static class Common
    {
        public static (string CountryName, string TimeZone) GetAttendacneTimeZone(string attendacnetimezone)
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
    }
}