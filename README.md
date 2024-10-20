# EFQueryMapper

EFQueryMapper is a powerful library for type mapping, including support for Entity Framework's IQueryable interface. It allows for efficient mapping between entities and DTOs, streamlining the data transformation process in .NET applications. Mapping profiles are created only once and saved, which makes subsequent mapping operations very fast. If there is no need for any configuration between two types, you don't need to do anything in mapper. **Not even a 'CreateMap' required if no configuration needed**.

# Why use it over other mapping libraries?
- Entity Framework query mapping on the fly! Thanks to Expression API 🥂
- High performance by caching mapping expressions
- Provides customization abilities in a simple way
- Easy to use with the help of extension methods
- Works with **not only entityframework queries** via IQueryable interface, also any collection type and class can be mapped.

# Example
You can map directly using IQueryable interface. Here's a quick example:
```csharp
IQueryable<Person> peopleQuery = dbcontext.Person.Where(x=> x.Age >= 18);
List<PersonDTO> extensionQueryPeople = peopleQuery.Map<PersonDTO>(mapper).ToList();
```
Or you can go this way:
```csharp
List<PersonDTO> value = mapper.Map<Person, PersonDTO>(peopleQuery).ToList();
```

See also another extension example with Map method:
```csharp
 public virtual async Task<TEntity> Create<TEntity, TRequest>(TRequest request, bool save = true)
     where TRequest : class
     where TEntity : class
 {
     var entity = request.Map<TEntity>(queryMapper);
     .....
 }
```

# Installation

You can install the package via NuGet Package Manager:

```csharp
Install-Package EFQueryMapper
```
Or via .NET CLI:
```csharp
dotnet add package EFQueryMapper
```

# Usage

You could add it to DI this way:
```csharp
 services.AddQueryMapper<BookMapper>()
```

The mapper is registered as a singleton, allowing you to inject it wherever needed. For example, see the code below:
```csharp
 public BookRepository(BookContext bookContext, IQueryMapper queryMapper)
 {
     BookContext = bookContext;
     QueryMapper = queryMapper;
 }
```

Your custom mapper must be a subclass of **QueryMapper**. It is a MUST since QueryMapper itself already covers all the needed stuff and implements **IQueryMapper** readily with a flexible way. Let's take a look at an example:
```csharp
public class BookMapper : QueryMapper
{
    public BookMapper()
    {
        Configure<Person, PersonDTO>(config =>
        {
            config
            .Match(x => x.Firstname + " " + x.Lastname, dto => dto.Fullname)
            .UsingPublicConstructor(x => new PersonDTO(x.Firstname, x.Lastname))
            ;
        });

        Configure<Book, ReadBookResponse>(config =>
        {
            config
            .Match(x => x.Author.FirstName + " " + x.Author.LastName, y => y.AuthorName)
            .Match(x => x.CreatedBy.FirstName + " " + x.CreatedBy.LastName, y => y.CreatedByName)
            .UsingNonPublicConstructor(x => new ParameterContainer(x.AuthorId))
            ;
        });

        Configure<Note, ReadNoteResponse>(config =>
        {
            config
            .Match(x => x.User.FirstName + " " + x.User.LastName, y => y.UserName)
            .Match(x => x.User.ShareId, y => y.ShareId)
            ;
        });
    }
}
```
You see we have "**Configure**" method. It has a couple methods to extend mapping abilities:  

**Match**: Allows you to determine source type expression to use and the destination type's property/field to set. See below example:
```csharp
.Match(x => x.User.ShareId, y => y.ShareId)
```

**UsingPublicConstructor**: Allows you to use the **public constructor** you wish. If destination class has multiple constructors and you want to pick a certain one, this method would be useful. If you don't use and map straight, first constructor found will be used. See below example:

```csharp
.UsingPublicConstructor(x => new PersonDTO(x.Firstname, x.Lastname))
```

**Public constructors are prioritized while picking up constructor automatically.**

Lets say you have only one constructor and it has parameters. You don't need anything if parameter names are matched to related property/field. Imagine you have only this constructor:
```csharp
 private ReadBookResponse(int authorId)
 {
     AuthorId = authorId;
 }
```
Even if it is private, it is easily used with mapper. If you have multiple constructors, public ones have priority to be picked up by mapper. Since member names are matched, you don't have to make any configurations. If parameter namings don't match just use the method above.

If you need to use a **non-public constructor** of destination type and there are multiple constructors, see below example:
```csharp
  Configure<Book, ReadBookResponse>(config =>
  {
      config
      .Match(x => x.Author.FirstName + " " + x.Author.LastName, y => y.AuthorName)
      .Match(x => x.CreatedBy.FirstName + " " + x.CreatedBy.LastName, y => y.CreatedByName)
      .UsingNonPublicConstructor(x => new ParameterContainer(x.AuthorId))
      ;
  });
```
In **UsingNonPublicConstructor** you could add arguments you wish, ensure the argument types you added to ParameterContainer match the wished destination type constructor's argument types you want to use.  


# License

This project is licensed under the Apache License 2.0 - see the [LICENSE](./LICENSE.txt) file for details.


