using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenNETCF.ORM;
using ReferenceSample.Entities;

namespace ReferenceSample
{
    class Program
    {
        static void Main(string[] args)
        {
            var store = new DataService();

            store.AddBook(null, null, null);

            var authors = new List<Person>();
            var illustrators = new List<Person>();

            authors.Add(store.AddPerson("Alexadre", "Dumas"));
            authors.Add(store.AddPerson("Richard", "Castle"));
            illustrators.Add(store.AddPerson("Stan", "Lee"));

            store.AddBook("Title", authors.ToArray(), illustrators.ToArray());
        }
    }

    public class DataService
    {
        private SqlCeDataStore m_store;

        public DataService()
        {
            m_store = new SqlCeDataStore("MyData.sdf");

            m_store.DeleteStore();

            if (!m_store.StoreExists)
            {
                m_store.CreateStore();
            }

            m_store.AddType<Book>();
            m_store.AddType<Person>();

            m_store.EnsureCompatibility();
        }

        public Person AddPerson(string firstName, string lastName)
        {
            var person = new Person()
            {
                FirstName = firstName,
                LastName = lastName
            };

            m_store.Insert(person);

            return person;
        }

        public Book AddBook(string title, Person[] authors, Person[] illustrators)
        {
            var book = new Book()
            {
                Title = title,
                Authors = authors,
                Illustrators = illustrators
            };

            m_store.Insert(book);

            return book;
        }

        public Book[] GetAllBooks()
        {
            return m_store.Select<Book>().ToArray();
        }
    }
}
