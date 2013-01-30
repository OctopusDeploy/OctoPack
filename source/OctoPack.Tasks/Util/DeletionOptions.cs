using System;

namespace OctoPack.Tasks.Util
{
    public class DeletionOptions : IEquatable<DeletionOptions>
    {
        public static DeletionOptions TryThreeTimes { get { return new DeletionOptions { RetryAttempts = 3, ThrowOnFailure = true };}}
        public static DeletionOptions TryThreeTimesIgnoreFailure { get { return new DeletionOptions { RetryAttempts = 3, ThrowOnFailure = false };}}

        DeletionOptions()
        {
            SleepBetweenAttemptsMilliseconds = 100;
        }

        public int RetryAttempts { get; private set; }
        public int SleepBetweenAttemptsMilliseconds { get; private set; }
        public bool ThrowOnFailure { get; private set; }

        public bool Equals(DeletionOptions other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return RetryAttempts == other.RetryAttempts && SleepBetweenAttemptsMilliseconds == other.SleepBetweenAttemptsMilliseconds && ThrowOnFailure.Equals(other.ThrowOnFailure);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((DeletionOptions) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = RetryAttempts;
                hashCode = (hashCode*397) ^ SleepBetweenAttemptsMilliseconds;
                hashCode = (hashCode*397) ^ ThrowOnFailure.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(DeletionOptions left, DeletionOptions right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(DeletionOptions left, DeletionOptions right)
        {
            return !Equals(left, right);
        }
    }
}