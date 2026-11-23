# RTT Client Manager

Add, edit and list clients, and export them to CSV.

## You need

- .NET SDK 10 (see `global.json`)
- SQL Server LocalDB

## Run it

The database is created automatically the first time the API starts.

**Visual Studio:** open `RTT.ClientManager.slnx`, choose the **MultiProject** profile and press F5.

**Command line:** start the API first, then the web app, each in its own terminal:

```bash
dotnet run --project RTT.Service
```

```bash
dotnet run --project RTT.Web
```

Then open http://localhost:5154.

## Settings

- Database connection: `RTT.Service/appsettings.Development.json`
- API address used by the web app: `RTT.Web/appsettings.json` (`ClientService:Url`)
