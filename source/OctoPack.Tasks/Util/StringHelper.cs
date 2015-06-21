namespace OctoPack.Tasks.Util
{
	public static class StringHelper
	{
		public static bool IsNullOrEmpty(string value)
		{
			return string.IsNullOrEmpty(value.Trim());
		}
	}
}
