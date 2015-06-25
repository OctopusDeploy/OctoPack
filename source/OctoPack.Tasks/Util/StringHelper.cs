namespace OctoPack.Tasks.Util
{
	public static class StringHelper
	{
		public static bool IsNullOrWhiteSpace(string value)
		{
			if (value != null)
				value = value.Trim();

			return string.IsNullOrEmpty(value);
		}
	}
}
