# 1.add packages: !!! this is for .net9
dotnet add package Microsoft.EntityFrameworkCore --version 9.*
dotnet add package Microsoft.EntityFrameworkCore.Design --version 9.*
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL --version 9.*
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package Serilog.AspNetCore
dotnet add package Swashbuckle.AspNetCore --version 6.6.2
dotnet add package DotNetEnv
dotnet add package Microsoft.AspNetCore.Identity
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer

# 2. restore project
dotnet restore

# 3. create an .env file from .env.example and change the database connection info

# 4. add migration & apply migration
dotnet ef migrations add InitialCreate
dotnet ef database update



