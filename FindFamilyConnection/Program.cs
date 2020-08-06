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
	public struct Person
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
		Marriage,
		FirstPerson
	}

	struct Connection
	{
		public ConnectionType connectionType;
		public int person1;
		public int person2;
	}

	public class Program
	{

		static void Main(string[] args)
		{
			Console.WriteLine("Start FindFamilyConnection\n");
			Console.WriteLine("Familiendaten müssen als \"Gedcom XML 6.0\"-Datei exportiert sein.\nGeben Sie den Pfad zur XML-Datei ein!");
			XmlDocument doc = new XmlDocument();
			while (!doc.HasChildNodes)
			{
				doc = helpclass.Instance.LoadFileToDoc();
			}
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
			foreach (XmlNode xmlFamiliy in xmlFamilies)
			{
				int f = -1, m = -1;
				XmlNode m_ = xmlFamiliy.SelectSingleNode("WifeMoth/Link/@Ref");
				if (m_ != null) m = Convert.ToInt32(m_.InnerText.Substring(1));

				XmlNode f_ = xmlFamiliy.SelectSingleNode("HusbFath/Link/@Ref");
				if (f_ != null) f = Convert.ToInt32(f_.InnerText.Substring(1));

				if (m != -1 && f != -1)
				{
					Connection ParentParent = new Connection { connectionType = ConnectionType.ParentParant, person1 = f, person2 = m };
					connections.Add(ParentParent);
				}

				XmlNodeList xmlChilds = xmlFamiliy.SelectNodes("Child");
				foreach (XmlNode xmlChild in xmlChilds)
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
			foreach (XmlNode xmlMarriage in xmlMarriages)
			{
				Connection marriage = new Connection { connectionType = ConnectionType.Marriage };
				XmlNodeList people = xmlMarriage.SelectNodes("Participant/Link/@Ref");
				marriage.person1 = Convert.ToInt32(people[0].InnerText.Substring(1));
				marriage.person2 = Convert.ToInt32(people[1].InnerText.Substring(1));
				connections.Add(marriage);
			}


			// Get start person & end person
			Console.WriteLine("\nGeben Sie den Nachnamen ODER Vornamen für die Startperson ein.");
			Person FirstPerson;
			if (!helpclass.Instance.Testphase) FirstPerson = helpclass.Instance.ChoosePerson(People);
			else FirstPerson = People[200];
			Console.WriteLine("\nGeben Sie den Nachnamen ODER Vornamen für die Zielperson ein.");
			Person LastPerson;
			if (!helpclass.Instance.Testphase) LastPerson = helpclass.Instance.ChoosePerson(People);
			else LastPerson = People[300];
			Console.WriteLine(FirstPerson.firstName + " " + FirstPerson.lastName + " und " + LastPerson.firstName + " " + LastPerson.lastName + " wurden ausgewählt.");

			// Find connection
			//List<List<Connection>> FamilyPaths = new List<List<Connection>>();
			List<Connection> FamilyPath1 = new List<Connection>();
			Connection FirstPersonC = new Connection { connectionType = ConnectionType.FirstPerson, person1 = FirstPerson.id, person2 = FirstPerson.id };
			FamilyPath1.Add(FirstPersonC);
			List<Connection> firstPersonConnections = connections.FindAll(x => x.person1 == FirstPerson.id || x.person2 == FirstPerson.id);
			foreach(Connection connection in firstPersonConnections)
			{
				int otherPersonId = 0;
				if (connection.person1 == FirstPerson.id)
					otherPersonId = connection.person2;
				else
					otherPersonId = connection.person1;
				var test = FamilyPath1.FindAll(x => x.person1 == otherPersonId || x.person2 == otherPersonId);
				//if (FamilyPaths[0].Find(x=>x.person1==otherPersonId||x.person2==otherPersonId)==null)
				var test2 = FamilyPath1.FindAll(x => x.person1 == otherPersonId || x.person2 == otherPersonId);

			}
		}
	}

	public class helpclass
	{
		public bool Testphase = true;

		private static helpclass _instance;
		public static helpclass Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = new helpclass();
				}
				return _instance;
			}
		}

		public string NameInput()
		{
			string input = "";
			do
			{
				ConsoleKeyInfo k = Console.ReadKey(true);
				if (k.Key == ConsoleKey.Enter)
					break;
				if (k.Key == ConsoleKey.Backspace)
				{
					Console.Write("\b \b");
					input = input.Remove(input.Length - 1);
				}
				else
				{
					Console.Write(k.KeyChar);
					input += k.KeyChar;
				}
			} while (true);
			return input;
		}

		public Person ChoosePerson(List<Person> people)
		{
			List<Person> containingPeople = new List<Person>();
			while (true)
			{
				string nameInput = NameInput();
				containingPeople.Clear();
				foreach (Person person in people)
					if (person.firstName.Contains(nameInput) || person.lastName.Contains(nameInput))
						containingPeople.Add(person);
				if (containingPeople.Count != 0) break;
				Console.WriteLine("\nName konnte nicht gefunden werden! Neuer Versuch:");
			}
			Person chosenPerson = new Person(); ;
			if (containingPeople.Count > 1)
			{
				Console.WriteLine();
				for (int i = 0; i < containingPeople.Count; i++)
					Console.WriteLine(i + 1 + "\t" + containingPeople[i].firstName + " " + containingPeople[i].lastName);
				Console.WriteLine("Wählen Sie anhand der Zahl eine der aufgelisteten Personen aus!");
				int z = Convert.ToInt32(Console.ReadLine());
				chosenPerson = containingPeople[z - 1];
			}
			else
				chosenPerson = containingPeople[0];
			return chosenPerson;
		}

		public XmlDocument LoadFileToDoc()
		{
			XmlDocument doc = new XmlDocument();
			try
			{
				string filepath;
				if (Testphase) filepath = "P:\\Sonstiges\\Ahnen\\Export\\XML\\Ahnen_04.xml";
				else filepath = Console.ReadLine();
				FileStream fs = File.OpenRead(filepath);
				doc.Load(fs);
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
			}
			return doc;
		}

	}
}
