using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace FindFamilyConnection
{
	struct Person
	{
		public int id;
		public string firstName;
		public string lastName;

		public Person(string firstName, string lastName, int id) : this()
		{
			this.firstName = firstName;
			this.lastName = lastName;
			this.id = id;
		}
	}

	enum ConnectionType
	{
		ParentChild,
		ParentParant,
		Marriage
	}

	struct Connection
	{
		public ConnectionType connectionType;
		public int person1;
		public int person2;
	}

	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("Start FindFamilyConnection\n");
			Console.WriteLine("Familiendaten müssen als \"Gedcom XML 6.0\"-Datei exportiert sein.\nGeben Sie den Pfad zur XML-Datei ein!");

			string filepath = Console.ReadLine();
			FileStream fs = File.OpenRead(filepath);
			XmlDocument doc = new XmlDocument();
			doc.Load(fs);
			XmlNode root = doc.GetElementsByTagName("GEDCOM").Item(0);

			// Get Persons
			List<Person> People = new List<Person>();
			XmlNodeList xmlPeople = root.SelectNodes("IndividualRec");
			foreach (XmlNode xmlPerson in xmlPeople)
			{
				string firstName = xmlPerson.SelectSingleNode("IndivName/NamePart[@Type='given name']").InnerText;
				string lastName = xmlPerson.SelectSingleNode("IndivName/NamePart[@Type='surname']").InnerText;
				int id = Convert.ToInt32(xmlPerson.SelectSingleNode("@Id").InnerText.Substring(1));
				Person person = new Person(firstName, lastName, id);
				People.Add(person);
			}

			List<Connection> connections = new List<Connection>();

			// Get Childs
			XmlNodeList xmlFamilies = root.SelectNodes("FamilyRec");
			foreach(XmlNode xmlFamiliy in xmlFamilies)
			{
				int f = -1, m = -1;
				XmlNode m_ = xmlFamiliy.SelectSingleNode("WifeMoth/Link/@Ref");
				if(m_!=null) m = Convert.ToInt32(m_.InnerText.Substring(1));
						
				XmlNode f_ = xmlFamiliy.SelectSingleNode("HusbFath/Link/@Ref");
				if(f_!=null) f = Convert.ToInt32(f_.InnerText.Substring(1));

				if (m != -1 && f != -1)
				{
					Connection ParentParent = new Connection { connectionType = ConnectionType.ParentParant, person1 = f, person2 = m };
					connections.Add(ParentParent);
				}
				
				XmlNodeList xmlChilds = xmlFamiliy.SelectNodes("Child");
				foreach(XmlNode xmlChild in xmlChilds)
				{
					Connection ParentChild1 = new Connection { connectionType = ConnectionType.ParentChild };
					Connection ParentChild2 = new Connection { connectionType = ConnectionType.ParentChild };

					XmlNode c_ = xmlChild.SelectSingleNode("Link/@Ref");
					if (c_ != null && f != -1)
					{
						ParentChild1.person1 = f;
						ParentChild1.person2 = Convert.ToInt32(c_.InnerText.Substring(1));
						connections.Add(ParentChild1);
					}
					if (c_ != null && m != -1)
					{
						ParentChild2.person1 = m;
						ParentChild2.person2 = Convert.ToInt32(c_.InnerText.Substring(1));
						connections.Add(ParentChild2);
					}
				}
			}

			// Get Marriages
			XmlNodeList xmlMarriages = root.SelectNodes("EventRec[@Type='marriage']");
			foreach(XmlNode xmlMarriage in xmlMarriages)
			{
				Connection marriage = new Connection { connectionType = ConnectionType.Marriage };
				XmlNodeList people = xmlMarriage.SelectNodes("Participant/Link/@Ref");
				marriage.person1 = Convert.ToInt32(people[0].InnerText.Substring(1));
				marriage.person2 = Convert.ToInt32(people[1].InnerText.Substring(1));
				connections.Add(marriage);
			}
			
		}
	}
}
