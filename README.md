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

Here's a quick example of how to use EFQueryMapper with Entity Framework:
```csharp
IQueryable<Person> peopleQuery = dbcontext.Person.Where(x=> x.Age >= 18);
List<PersonDTO> extensionQueryPeople = peopleQuery.Map<PersonDTO>(mapper).ToList();
```
OR
```csharp
List<PersonDTO> value = mapper.Map<Person, PersonDTO>(peopleQuery).ToList();
```

# Features
- Supports Entity Framework IQueryable interface
- Seamless mapping between entities and DTOs
- Optimized for performance with the help of Expression API


# License

This project is licensed under the Apache License 2.0 - see the [LICENSE](./LICENSE.txt) file for details.


