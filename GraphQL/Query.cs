namespace Server.GraphQL;

public class Query
{
    public Book GetBook() =>
        new Book
        {
            Title = "Book #1",
            Author = new Author
            {
                Name = "Author #1"
            }
        };

    public Author GetAuthor() =>
        new Author
        {
            Name = "Author #1"
        };
}