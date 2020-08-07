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
	public class Connection
	{
		public enum ConnectionType
		{
			ParentChild,
			ParentParant,
			Marriage,
			FirstPerson
		}

		public ConnectionType connectionType;
		public Person personA;
		public Person personB;
	}

	public class ConnectionPath : Connection
	{
		/// <summary>
		/// If personA is first person in path = true, if personB = false
		/// </summary>
		public bool personOrder;

		public ConnectionPath() { }

		public ConnectionPath(Person A, Person B, ConnectionType ct, bool po)
		{
			personA = A;
			personB = B;
			connectionType = ct;
			personOrder = po;
		}

		/// <summary>
		/// Constructor for new ConnectionPath with Connection and new order
		/// </summary>
		/// <param name="c">Connection</param>
		/// <param name="p">PersonOrder</param>
		public ConnectionPath(Connection c, bool p)
		{
			personA = c.personA;
			personB = c.personB;
			connectionType = c.connectionType;
			personOrder = p;
		}
	}
	public class Person
	{
		public int id;
		public string firstName;
		public string lastName;

		public Person(string firstName, string lastName, int id)
		{
			this.firstName = firstName;
			this.lastName = lastName;
			this.id = id;
		}

		public Person() { }
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
				doc = Helpclass.Instance.LoadFileToDoc();
			}
			XmlNode root = doc.GetElementsByTagName("GEDCOM").Item(0);

			// Get Persons
			Helpclass.Instance.People = new List<Person>();
			XmlNodeList xmlPeople = root.SelectNodes("IndividualRec");
			foreach (XmlNode xmlPerson in xmlPeople)
			{
				string firstName = xmlPerson.SelectSingleNode("IndivName/NamePart[@Type='given name']").InnerText;
				string lastName = xmlPerson.SelectSingleNode("IndivName/NamePart[@Type='surname']").InnerText;
				int id = Convert.ToInt32(xmlPerson.SelectSingleNode("@Id").InnerText.Substring(1));
				Person person = new Person(firstName, lastName, id);
				Helpclass.Instance.People.Add(person);
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
					Connection ParentParent = new Connection { connectionType = Connection.ConnectionType.ParentParant, personA = Helpclass.Instance.GetPersonById(f), personB = Helpclass.Instance.GetPersonById(m) };
					connections.Add(ParentParent);
				}

				XmlNodeList xmlChilds = xmlFamiliy.SelectNodes("Child");
				foreach (XmlNode xmlChild in xmlChilds)
				{
					Connection ParentChild1 = new Connection { connectionType = Connection.ConnectionType.ParentChild };
					Connection ParentChild2 = new Connection { connectionType = Connection.ConnectionType.ParentChild };

					XmlNode c_ = xmlChild.SelectSingleNode("Link/@Ref");
					if (c_ != null && f != -1)
					{
						ParentChild1.personA = Helpclass.Instance.GetPersonById(f);
						ParentChild1.personB = Helpclass.Instance.GetPersonById(Convert.ToInt32(c_.InnerText.Substring(1)));
						connections.Add(ParentChild1);
					}
					if (c_ != null && m != -1)
					{
						ParentChild2.personA = Helpclass.Instance.GetPersonById(m);
						ParentChild2.personB = Helpclass.Instance.GetPersonById(Convert.ToInt32(c_.InnerText.Substring(1)));
						connections.Add(ParentChild2);
					}
				}
			}

			// Get Marriages
			XmlNodeList xmlMarriages = root.SelectNodes("EventRec[@Type='marriage']");
			foreach (XmlNode xmlMarriage in xmlMarriages)
			{
				Connection marriage = new Connection { connectionType = Connection.ConnectionType.Marriage };
				XmlNodeList people = xmlMarriage.SelectNodes("Participant/Link/@Ref");
				marriage.personA = Helpclass.Instance.GetPersonById(Convert.ToInt32(people[0].InnerText.Substring(1)));
				marriage.personB = Helpclass.Instance.GetPersonById(Convert.ToInt32(people[1].InnerText.Substring(1)));
				connections.Add(marriage);
			}


			// Get start person & end person
			Console.WriteLine("\nGeben Sie den Nachnamen ODER Vornamen für die Startperson ein.");
			Person FirstPerson;
			if (!Helpclass.Instance.Testphase) FirstPerson = Helpclass.Instance.ChoosePerson(Helpclass.Instance.People);
			else FirstPerson = Helpclass.Instance.People[200];
			Console.WriteLine("\nGeben Sie den Nachnamen ODER Vornamen für die Zielperson ein.");
			Person LastPerson;
			if (!Helpclass.Instance.Testphase) LastPerson = Helpclass.Instance.ChoosePerson(Helpclass.Instance.People);
			else LastPerson = Helpclass.Instance.People[300];
			Console.WriteLine(FirstPerson.firstName + " " + FirstPerson.lastName + " und " + LastPerson.firstName + " " + LastPerson.lastName + " wurden ausgewählt.");

			
			// Find connection
			
			List<List<ConnectionPath>> FamilyPaths = new List<List<ConnectionPath>>();
			ConnectionPath FirstPersonC = new ConnectionPath { connectionType = Connection.ConnectionType.FirstPerson, personA = FirstPerson, personB = FirstPerson, personOrder = true };
			FamilyPaths.Add(new List<ConnectionPath>());
			FamilyPaths[0].Add(FirstPersonC);

			// Iterieren, bis alle Pfade als letzte Person die Zielperson haben
			bool allLastPeopleFound = false;
			List<List<ConnectionPath>> newFamilyPaths = new List<List<ConnectionPath>>();
			while (!allLastPeopleFound)
			{
				// Alle Pfade durchgehen
				allLastPeopleFound = true;
				foreach (List<ConnectionPath> path in FamilyPaths)
				{
					// Letzte Person im aktuellen Pfad speichern
					Person lastPathPerson;  
					if (path[path.Count - 1].personOrder)
						lastPathPerson = path[path.Count - 1].personA;
					else
						lastPathPerson = path[path.Count - 1].personB;

					// Alle Connections zu lastPathPerson im aktuellen Pfad finden
					List<Connection> foundConnections = connections.FindAll(x => x.personA.id == lastPathPerson.id || x.personB.id == lastPathPerson.id);

					// alle zur letzten Pfadperson gefundenen Connections durchgehen
					bool FirstConnection = false;
					foreach (Connection foundConnection in foundConnections)
					{
						bool personOrderFoundConnection = lastPathPerson == foundConnection.personA;
						
						// Wenn letzte Person in einem Pfad nicht die Zielperson ist:
						if (lastPathPerson.id != LastPerson.id)
						{
							allLastPeopleFound = false;
							ConnectionPath newConnectionPath = new ConnectionPath(foundConnection, personOrderFoundConnection);

							// Pruefe, ob Person schon im Pfad vorhanden ist.
							bool PersonAlreadyInPath = false;
							foreach (ConnectionPath connectionPath in path)
								if (connectionPath.personA == lastPathPerson || connectionPath.personB == lastPathPerson)
									PersonAlreadyInPath = true;

							if (!PersonAlreadyInPath)
							{
								if (FirstConnection)
								{
									path.Add(newConnectionPath);
									FirstConnection = true;
								}
								else
								{
									newFamilyPaths.Add(path);
									newFamilyPaths[newFamilyPaths.Count - 1].RemoveAt(newFamilyPaths[newFamilyPaths.Count - 1].Count - 1);
									newFamilyPaths[newFamilyPaths.Count - 1].Add(newConnectionPath);
								}
							}
							else
							{

							}
						}
					}
				}
				foreach (List<ConnectionPath> newFamilyPath in newFamilyPaths)
				{
					FamilyPaths.Add(newFamilyPath);
				}
				newFamilyPaths.Clear();
			}
		}
	}

	public class Helpclass
	{
		#region properties
		public bool Testphase = false;

		private static Helpclass _instance;
		public static Helpclass Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = new Helpclass();
				}
				return _instance;
			}
		}

		public List<Person> People;
		#endregion

		public Person GetPersonById(int id)
		{
			return People.Find(x => x.id == id);
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
			Person chosenPerson;
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
