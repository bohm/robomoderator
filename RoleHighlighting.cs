using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace domovoj
{

    class RoleHighlighting
    {
        private string regexMatcher;

        public RoleHighlighting()
        {
            // Build the regex matching string from the roleset in Settings.
            StringBuilder sb = new StringBuilder();
            bool first = true;
            foreach (string DigitRole in Settings.LoudDigitRoles)
            {
                if (!first)
                {
                    sb.Append("|");
                }
                else
                {
                    first = false;
                }
                sb.Append(DigitRole);
            }

            foreach (string MetalRole in Settings.LoudMetalRoles)
            {
                if (!first)
                {
                    sb.Append("|");
                }
                else
                {
                    first = false;
                }

                sb.Append(MetalRole);
            }
            regexMatcher = sb.ToString();
        }

        public List<string> RolesToHighlight(string haystack)
        {
            List<string> ret = new List<string>();
            var matchCollection = Regex.Matches(haystack, regexMatcher);
            foreach(Match m in matchCollection)
            {
                ret.Add(m.Value);
            }

            return ret;
        }
    }
}
