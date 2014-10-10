using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace RavenMigrations
{
    public class VersionFromFullNameParser
    {
        private static readonly Regex VersionRegex = new Regex(@"^(?<fragments>\D\D?((\d+)(\W|_|$)){1,4})", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
        private static readonly Regex DigitRegex = new Regex(@"\D{0,2}((?<digits>(\d+))(\W|_|$))", RegexOptions.Compiled | RegexOptions.ExplicitCapture);

        public static MigrationVersion ParseFullName(string fullName)
        {
            var numbers =
                fullName
                    .Split('.').SkipWhile(n => !VersionRegex.IsMatch(n))
                    .SelectMany(part => VersionRegex.Matches(part).Cast<Match>().Select(m => m.Groups["fragments"].Value))
                    .SelectMany(fragment => DigitRegex.Matches(fragment).Cast<Match>().Select(m => m.Groups["digits"].Value))
                    .Select(Int64.Parse)
                    .ToArray();
            if (numbers.Length == 1) return new MigrationVersion(0, numbers[0]);
            if (numbers.Length == 0)
                throw new VersionFromFullNameParser.InvalidVersionException(String.Format("No numbers found for version in '{0}'", fullName));
            if (numbers.Length > 4)
                throw new VersionFromFullNameParser.InvalidVersionException(String.Format("Name for versioning contains too many version numbers: '{0}'", fullName));

            // pad out any missing numbers
            Array.Resize(ref numbers, 4);

            return new MigrationVersion(numbers[0], numbers[1], numbers[2], numbers[3]);
        }

        [Serializable]
        public class InvalidVersionException : Exception
        {
            public InvalidVersionException()
            {
            }

            public InvalidVersionException(string message)
                : base(message)
            {
            }

            public InvalidVersionException(string message, Exception innerException)
                : base(message, innerException)
            {
            }
        }
    }
}