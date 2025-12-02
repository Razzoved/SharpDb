# SharpDb.EntityFrameworkCore

**SharpDb.EntityFrameworkCore** is a .NET library designed to provide extensions, helpers, and utilities for simplifying and enhancing the use of Entity Framework Core (EF Core) in your projects.

## Features
- **Comparers**: Custom comparers for EF Core entities.
- **Converters**: Custom converters for EF Core value conversions.
- **Entities**: Predefined entity classes and interfaces for common EF Core scenarios.
- **Interceptors**: EF Core interceptors for logging, auditing, and more.
- Custom dbContext wrapper **UnitOfWork** to manage transactions and repository patterns.
- Custom sql runner **EfcSqlRunner** for raw SQL execution.
- Topology aware **ApplyConfigurationsFromAssembly** implementation.

## Requirements
- **Target Framework**: .NET 8 or higher
- **Dependencies**: Entity Framework Core

## Installation
Either clone the repository and include the project in your solution, or install via NuGet.

## Usage
1. Add a reference to the library in your project.
2. Import the relevant namespaces to access the extensions, helpers, and utilities.
3. Use the provided tools to simplify your EF Core workflows.

## Contributing
This library is maintained as a personal project. Contributions are welcome. Please follow project defined settings and formatting.

## License
See the LICENSE file for details.

---

For questions or support, please contact any of the maintainers listed in the project.
