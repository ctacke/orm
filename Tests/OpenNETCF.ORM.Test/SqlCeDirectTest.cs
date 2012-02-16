using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlServerCe;
using OpenNETCF.ORM.Test.Entities;

namespace OpenNETCF.ORM.Test
{
    public class SqlCeDirectTest : ITestClass
    {
        private SqlCeConnection Connection { get; set; }
        private Dictionary<string, int> m_bookOrdinals = new Dictionary<string, int>();
        private Dictionary<string, int> m_authorOrdinals = new Dictionary<string, int>();

        public void Initialize()
        {
            Connection = new SqlCeConnection("Data source=pubs.sdf");
            Connection.Open();
        }

        private void CheckOrdinals(SqlCeResultSet results)
        {
            if (m_bookOrdinals.Count == 0)
            {
                for (int i = 0; i < results.FieldCount; i++)
                {
                    m_bookOrdinals.Add(results.GetName(i), i);
                }
            }
        }

        private void CheckAuthorOrdinals(SqlCeResultSet results)
        {
            if (m_authorOrdinals.Count == 0)
            {
                for (int i = 0; i < results.FieldCount; i++)
                {
                    m_authorOrdinals.Add(results.GetName(i), i);
                }
            }
        }

        public Book[] GetAllBooks()
        {
            var books = new List<Book>();

            using (SqlCeCommand cmd = new SqlCeCommand("SELECT * FROM Book", Connection))
            {
                using (var results = cmd.ExecuteResultSet(ResultSetOptions.Insensitive))
                {
                    while (results.Read())
                    {
                        if (m_bookOrdinals.Count == 0)
                        {
                            for (int i = 0; i < results.FieldCount; i++)
                            {
                                m_bookOrdinals.Add(results.GetName(i), i);
                            }
                        }

                        books.Add(new Book
                        {
                            BookID = results.GetInt32(m_bookOrdinals["BookID"]),
                            AuthorID = results.GetInt32(m_bookOrdinals["AuthorID"]),
                            Title = results.GetString(m_bookOrdinals["Title"])
                        });
                    }
                }
            }

            return books.ToArray();
        }

        public int GetAuthorCount()
        {
            return GetRowCount("Author");
        }

        public int GetBookCount()
        {
            return GetRowCount("Book");
        }

        private int GetRowCount(string tableName)
        {
            var books = new List<Book>();

            using (SqlCeCommand cmd = new SqlCeCommand(string.Format("SELECT COUNT(*) FROM {0}", tableName), Connection))
            {
                return (int)cmd.ExecuteScalar();
            }
        }

        public void TruncateBooks()
        {
            throw new NotImplementedException();
        }

        public Book[] GetBooksOfType(BookType type)
        {
            var books = new List<Book>();

            using (SqlCeCommand cmd = new SqlCeCommand(
                string.Format("SELECT * FROM Book WHERE BookType = {0}", (int)type), Connection))
            {
                using (var results = cmd.ExecuteResultSet(ResultSetOptions.Insensitive))
                {
                    CheckOrdinals(results);

                    while (results.Read())
                    {

                        books.Add(new Book
                        {
                            BookID = results.GetInt32(m_bookOrdinals["BookID"]),
                            AuthorID = results.GetInt32(m_bookOrdinals["AuthorID"]),
                            Title = results.GetString(m_bookOrdinals["Title"])
                        });
                    }
                }
            }

            return books.ToArray();
        }

        public Book GetBookById(int bookID)
        {
            Book book = null;

            using (SqlCeCommand cmd = new SqlCeCommand("SELECT * FROM Book WHERE BookID = @bookid", Connection))
            {
                cmd.Parameters.Add(new SqlCeParameter("@bookid", bookID));

                using (var results = cmd.ExecuteResultSet(ResultSetOptions.Insensitive))
                {
                    if (results.Read())
                    {
                        CheckOrdinals(results);

                        book = new Book
                        {
                            BookID = results.GetInt32(m_bookOrdinals["BookID"]),
                            AuthorID = results.GetInt32(m_bookOrdinals["AuthorID"]),
                            Title = results.GetString(m_bookOrdinals["Title"])
                        };
                    }
                }
            }

            return book;
        }

        public Author GetAuthorById(int id)
        {
            Author author = null;
            List<Book> books = new List<Book>();

            using (SqlCeCommand cmd = new SqlCeCommand("SELECT AuthorID, Name, BookID, Title, BookType FROM Author INNER JOIN Book ON Author.AuthorID = Book.AuthorID WHERE Author.AuthorID = @id",
                Connection))
            {
                cmd.Parameters.Add(new SqlCeParameter("@id", id));

                using (var results = cmd.ExecuteResultSet(ResultSetOptions.Insensitive))
                {
                    while (results.Read())
                    {
                        if (author == null)
                        {
                            author = new Author
                            {
                                AuthorID = (int)results["AuthorID"],
                                Name = (string)results["Name"]
                            };
                        }
                        else if (author.AuthorID != results.GetInt32(m_authorOrdinals["AuthorID"]))
                        {
                            // we're on a new author , so we're done
                            // (shoudln't happen unless we have more than 1 author with the same name)
                            break;
                        }

                        books.Add(new Book
                        {
                            BookID = (int)results["BookID"],
                            Title = (string)results["Title"],
                            BookType = (BookType)results["BookType"],
                            AuthorID = author.AuthorID
                        });
                    }
                }
            }

            author.Books = books.ToArray();

            return author;
        }

        public Author GetAuthorByName(string name)
        {
            Author author = null;
            List<Book> books = new List<Book>();

            string sql = string.Format("SELECT * FROM Author where Name = '{0}'", name);
            using (SqlCeCommand cmd = new SqlCeCommand(sql,
                Connection))
            {
                using (var results = cmd.ExecuteResultSet(ResultSetOptions.Insensitive))
                {
                    if (results.Read())
                    {
                        CheckAuthorOrdinals(results);

                        author = new Author
                        {
                            AuthorID = results.GetInt32(m_authorOrdinals["AuthorID"]),
                            Name = results.GetString(m_authorOrdinals["Name"])
                        };

                        sql = string.Format("SELECT * FROM Book WHERE AuthorID = {0}", author.AuthorID);

                        using (var bookcmd = new SqlCeCommand(sql, Connection))
                        using (var bookresults = bookcmd.ExecuteResultSet(ResultSetOptions.Insensitive))
                        {
                            CheckOrdinals(bookresults);

                            while (bookresults.Read())
                            {
                                books.Add(new Book
                                {
                                    BookID = bookresults.GetInt32(m_bookOrdinals["BookID"]),
                                    Title = bookresults.GetString(m_bookOrdinals["Title"]),
                                    BookType = (BookType)bookresults.GetInt32(m_bookOrdinals["BookType"]),
                                    AuthorID = author.AuthorID
                                });
                            }
                        }
                    }
                }
            }

            author.Books = books.ToArray();

            return author;
        }

        public Author[] GetAuthors(int count, int offset)
        {
            // TODO:
            return new Author[] { };
        }

        public void Insert(Author author)
        {
            // TODO:
            return;
        }

        public void Update(Author author)
        {
            // TODO:
            return;
        }
    }
}
