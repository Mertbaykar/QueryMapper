# EFQueryMapper

EFQueryMapper is a powerful library for seamless type mapping, including support for Entity Framework's IQueryable interface. It allows for efficient mapping between entities and DTOs, streamlining the data transformation process in .NET applications.

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

You can add it to DI this way:
```csharp
 services.AddQueryMapper<BookMapper>()
```
Your custom mapper must be a subclass of QueryMapper. It is a MUST since QueryMapper itself already covers all the heavy stuff and implements IQueryMapper readily with a flexible way. Let's take a look at an example:
```csharp
 public class BookMapper : QueryMapper
 {
     public BookMapper()
     {
         Configure<Person, PersonDTO>(config =>
         {
             config
             .Match(x => x.Firstname + " " + x.Lastname, dto => dto.Fullname)
             .UsingConstructor(x => new PersonDTO(x.Firstname, x.Lastname))
             ;
         });

         Configure<Book, ReadBookResponse>(config =>
         {
             config
             .Match(x => x.Author.FirstName + " " + x.Author.LastName, y => y.AuthorName)
             .Match(x => x.CreatedBy.FirstName + " " + x.CreatedBy.LastName, y => y.CreatedByName)
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

**UsingConstructor**: Allows you to use the constructor you wish. If destination class has multiple constructors and you want to pick a certain one, this method would be useful. If you don't use and map straight, first constructor found will be used. **Private constructors are not prioritized while picking up constructor automatically.**  

Lets say you have only one constructor and it has parameters. You don't need to use this method if parameter names are matched to related property/field. Imagine you have only this constructor:
```csharp
 private PersonDTO(string firstname)
 {
     Firstname = firstname;
 }
```
Even if it is private, it is easily used with mapper. If you have multiple constructors, the ones who are not private have priority to be picked up by mapper. Since member names are matched, you don't have to make any configurations. If namings don't match just use the method.

# Example
You can easily use Map method directly with IQueryable interface. Here's a quick example:
```csharp
IQueryable<Person> peopleQuery = dbcontext.Person.Where(x=> x.Age >= 18);
List<PersonDTO> extensionQueryPeople = peopleQuery.Map<PersonDTO>(mapper).ToList();
```
Or you can go this way:
```csharp
List<PersonDTO> value = mapper.Map<Person, PersonDTO>(peopleQuery).ToList();
```

# Features
- Supports Entity Framework IQueryable interface
- Seamless mapping between entities and DTOs
- Optimized for performance with the help of Expression API


# License

This project is licensed under the Apache License 2.0 - see the [LICENSE](./LICENSE.txt) file for details.


