# RaceDay — ST10472632_PROG6212_POE_PART1_GROUP4

**Student Number:** ST10472632

## 1. Project Overview

RaceDay is a web-based event management system designed for road running, walking and cycling events in South Africa.

The system allows participants to discover events, register, manage their profiles, enrol in events and view race results.

Event organisers can create and manage events, categories, participant enrolments and race results.

The application was developed across three project parts, progressing from system planning and database design to API development and a web-based MVC application.

## 2. Technologies Used

- C#
- ASP.NET Core Web API
- ASP.NET Core MVC
- .NET 10
- Entity Framework Core
- Microsoft SQL Server
- Cookie-based authentication
- Role-based authorisation
- Azure Blob Storage
- Bootstrap and CSS
- Swagger UI
- xUnit
- SQLite In-Memory
- Git and GitHub
- GitHub Actions
- Docker and Docker Compose

## 3. System Features

### 3.1 Participant Features

- Register and log in.
- View available events.
- View event details and categories.
- Enrol in events.
- View personal enrolments.
- View race results.
- Update personal profile information.
- Upload a profile picture.

### 3.2 Organiser Features

- Log in as an organiser.
- View the organiser dashboard.
- Create, update and delete events.
- Manage event categories.
- View participant enrolments.
- Record and update race results.
- Manage event information.

### 3.3 System Features

- Cookie-based authentication.
- Role-based access control.
- API and MVC architecture.
- SQL Server database integration.
- Azure Blob Storage image uploads.
- Responsive user interface.
- Automated build and test validation.

## 4. Project Development

### Part 1 — Planning and Database Design

Part 1 focused on planning the RaceDay system before implementation.

Deliverables included:

- Entity Relationship Diagram (ERD).
- RESTful API Endpoint Plan.
- SQL Server Database Script.
- Database testing documentation.
- Initial GitHub Actions validation workflow.

### Part 2 — RESTful Web API

Part 2 implemented the backend using ASP.NET Core Web API.

The API provides functionality for:

- Authentication and user profiles.
- Event management.
- Category management.
- Participant enrolments.
- Race result management.
- Role-based authorisation.
- Database operations using Entity Framework Core.

Swagger UI was used to document and test the API endpoints.

### Part 3 — MVC Web Application

Part 3 extends the API by introducing an ASP.NET Core MVC web application.

The MVC application communicates with the RESTful API through HTTP requests rather than accessing the database directly.

The implementation includes:

- Participant and organiser interfaces.
- Role-specific navigation.
- Organiser dashboard.
- Event and category management.
- Participant enrolment management.
- Results management.
- Participant profile management.
- Azure Blob Storage integration for profile images.
- Docker configuration for the MVC application.
- GitHub Actions validation for Part 3.

## 5. Database

RaceDay uses Microsoft SQL Server.

The database includes the following main entities:

- User
- UserProfile
- Event
- Category
- Enrolment
- Result

Entity Framework Core is used to manage database operations and relationships.

The database connection must be configured for the local SQL Server installation.

Example connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=RaceDayDB;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

Replace `YOUR_SERVER` with the appropriate SQL Server instance.

Do not commit passwords or sensitive connection details to GitHub.

## 6. Azure Blob Storage

RaceDay integrates Azure Blob Storage for image uploads.

The API handles image uploads and returns an image URL that the MVC application can use to display the uploaded image.

The implementation includes:

- Azure Blob Storage integration.
- Image upload through the API.
- File type validation.
- File size validation.
- Image URL storage in the user profile.
- Displaying uploaded profile pictures.

The Azure Storage connection string must be configured securely using .NET User Secrets or another appropriate configuration method.

Example:

```powershell
dotnet user-secrets set "ConnectionStrings:AzureStorage" "YOUR_AZURE_STORAGE_CONNECTION_STRING" --project RaceDay.API
```

Never publish your actual Azure Storage connection string.

## 7. Running the Application

### Prerequisites

Install the following:

- .NET 10 SDK.
- Microsoft SQL Server or SQL Server Express.
- Visual Studio.
- Git.

### Step 1: Clone the repository

Clone the RaceDay repository:

```bash
git clone https://github.com/Jaycrown7/ST10472632_PROG6212_POE_PART1_GROUP4.git
```

Open the solution:

`RaceDay.slnx`

### Step 2: Restore dependencies

```bash
dotnet restore RaceDay.slnx
```

### Step 3: Configure the database

Configure the SQL Server connection string and ensure the RaceDayDB database is available.

### Step 4: Run the API

```bash
dotnet run --project RaceDay.API/RaceDay.API.csproj
```

The API is configured to use:

- HTTP: `http://localhost:5021`
- HTTPS: `https://localhost:7038`

Swagger UI can be accessed at:

`http://localhost:5021/swagger`

### Step 5: Run the MVC application

Open a second terminal and execute:

```bash
dotnet run --project RaceDay.MVC/RaceDay.MVC.csproj
```

Open the MVC application using the address displayed in the terminal.

**Important:** The API must be running while using the MVC application.

## 8. Docker Configuration

The repository includes:

- `RaceDay.MVC/Dockerfile`
- `docker-compose.yml`

The Docker Compose configuration builds and runs the MVC application.

The API must also be running and accessible from the Docker container.

From the repository root, execute:

```bash
docker compose up --build
```

The MVC application is configured to use port `8080`.

Open:

`http://localhost:8080`

The Docker configuration uses `host.docker.internal` to communicate with the API running on the host machine.

Docker Desktop must be installed and running.

## 9. Authentication and Authorisation

RaceDay uses cookie-based authentication.

The application supports two roles:

- Participant
- Organiser

Authentication claims identify the logged-in user and their assigned role.

Role-based authorisation protects restricted API operations and ensures users can only access permitted functionality.

## 10. Unit Testing

The solution includes a testing project:

`RaceDay.Tests`

The tests use xUnit and SQLite In-Memory where appropriate.

To execute the tests:

```bash
dotnet test RaceDay.slnx
```

## 11. GitHub Actions — Continuous Integration

GitHub Actions automates project validation.

The repository contains the following workflows:

- `.github/workflows/part1-validation.yml`
- `.github/workflows/part2-validation.yml`
- `.github/workflows/part3-validation.yml`

The Part 3 workflow:

1. Checks out the repository.
2. Sets up the .NET 10 SDK.
3. Restores project dependencies.
4. Builds the solution, including MVC.
5. Executes the automated unit tests.

The Part 3 workflow runs on pushes and pull requests targeting the `part3-mvc` branch.

The successful GitHub Actions run confirms that the configured build and test pipeline completed successfully.

## 12. Version Control

Git and GitHub were used throughout development.

The project was developed using separate branches to organise the different implementation stages.

- `part2-api` — RESTful API implementation.
- `part3-mvc` — MVC application and Part 3 implementation.

Commits document development progress, feature implementation, bug fixes and project improvements.

## 13. Demonstration Videos

### Part 1

https://youtu.be/YCT6UP938xA

### Part 3

[]

## 14. Project Conclusion

RaceDay progressed from an initial system design into a functional event management application.

The project combines ASP.NET Core Web API, MVC, SQL Server, Entity Framework Core, cookie-based authentication and Azure Blob Storage.

The final implementation provides separate functionality for participants and organisers while using an API-based architecture to manage application data.

GitHub Actions provides automated build and test validation, while Docker configuration supports containerisation of the MVC application.