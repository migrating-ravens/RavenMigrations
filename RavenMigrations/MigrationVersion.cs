using System;

namespace RavenMigrations
{
    public class MigrationVersion : IComparable<MigrationVersion>
    {
        public long Major { get; private set; }
        public long Minor { get; private set; }
        public long Build { get; private set; }
        public long Revision { get; private set; }

        public MigrationVersion(long major, long minor = 0, long build = 0, long revision = 0)
        {
            Major = major;
            Minor = minor;
            Build = build;
            Revision = revision;
        }

        protected bool Equals(MigrationVersion other)
        {
            return Major == other.Major && Minor == other.Minor && Build == other.Build && Revision == other.Revision;
        }

        public int CompareTo(MigrationVersion other)
        {
            return
                Major != other.Major
                    ? Major.CompareTo(other.Major)
                    : Minor != other.Minor
                        ? Minor.CompareTo(other.Minor)
                        : Build != other.Build
                            ? Build.CompareTo(other.Build)
                            : Revision.CompareTo(other.Revision);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MigrationVersion) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Major.GetHashCode();
                hashCode = (hashCode*397) ^ Minor.GetHashCode();
                hashCode = (hashCode*397) ^ Build.GetHashCode();
                hashCode = (hashCode*397) ^ Revision.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(MigrationVersion left, MigrationVersion right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(MigrationVersion left, MigrationVersion right)
        {
            return !Equals(left, right);
        }

        public override string ToString()
        {
            return string.Format("{0}.{1}.{2}.{3}", Major, Minor, Build, Revision);
        }
    }
}