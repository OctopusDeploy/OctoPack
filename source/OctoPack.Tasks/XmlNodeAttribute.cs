namespace OctoPack.Tasks
{
	public class XmlNodeAttribute
	{
		public XmlNodeAttribute()
		{
		}

		public XmlNodeAttribute(string name, string value)
		{
			this.Name = name;
			this.Value = value;
		}
		public string Name { get; set; }
		public string Value { get; set; }
	}
}