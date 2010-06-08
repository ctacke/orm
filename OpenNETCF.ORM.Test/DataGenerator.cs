using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using OpenNETCF.ORM.Test.Entities;
using System.IO;
using System.Reflection;

namespace OpenNETCF.ORM.Test
{
    class DataGenerator
    {
        private string[] m_dictionary;
        private int m_maxWordsInTitle = 5;

        private string[] FirstNames = new string[]
        {
            "John", "Thomas", "Aaron", "George", "Elbridge", "Daniel", "Martin", 
            "Richard", "Millard", "William", "Hannibal", "Andrew", "Schuyler", 
            "Henry"
        };

        private string[] LastNames = new string[]
        {
            "Adams", "Jefferson", "Burr", "Clinton", "Gerry",
            "Tompkins", "Calhoun", "Van Buren", "Johnson", "Tyler",
            "Dallas", "Fillmore", "King", "Breckinridge", "Hamlin",
            "Colfax", "Wilson", "Wheeler"

        };

        private string[] Dictionary
        {
            get
            {
                if (m_dictionary == null)
                {
                    List<string> list = new List<string>();

                    var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetName().CodeBase);
                    path = Path.Combine(path, "dictionary.txt");
                    using (var reader = File.OpenText(path))
                    {
                        while (reader.Peek() > 0)
                        {
                            var line = reader.ReadLine().Trim();
                            if (line.Length > 0)
                            {
                                list.Add(line);
                            }
                        }
                    }

                    m_dictionary = list.ToArray();
                }

                return m_dictionary;
            }
        }

        public Author[] GenerateAuthors(int count)
        {
            List<Author> list = new List<Author>();

            int f = 0;
            int l = 0;

            while (list.Count < count)
            {
                list.Add(new Author
                {
                    Name = string.Format("{0} {1}", FirstNames[f], LastNames[l])
                });

                f++;

                if (f > FirstNames.Length - 1)
                {
                    f = 0;
                    l++;
                }
            }

            return list.ToArray();
        }

        public Book[] GenerateBooks(int count)
        {
            Book[] books = new Book[count];

            var r = new Random(Environment.TickCount);

            for (int i = 0; i < count; i++)
            {
                string title = string.Empty;

                for (int w = 0; w < r.Next(m_maxWordsInTitle) + 1; w++)
                {
                    title += Dictionary[r.Next(Dictionary.Length - 1)];
                    title += ' ';
                }

                books[i] = new Book
                {
                    Title = title.TrimEnd(),
                    BookType = r.Next(1) == 0 ? BookType.Fiction : BookType.NonFiction
                };
            }

            return books;
        }
    }
}
