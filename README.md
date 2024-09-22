## Run a SQL server in a Docker container
1. Install Docker, [if using WSL](https://docs.docker.com/desktop/wsl/) or install Docker Desktop for Windows host
2. Follow [this guide](https://learn.microsoft.com/en-us/sql/linux/quickstart-install-connect-docker?view=sql-server-ver16&tabs=sqlcmd&pivots=cs1-bash) to run a SQL Server in a container

You can view the default credentials for the 'me' user
`sqlcmd config view --raw`

Then start a shell in the container
`docker exec -it sql1 'bash'`

Connect to the SQL server and enter the generated password.
`/opt/mssql-tools18/bin/sqlcmd -S localhost -U me -No`

Create a database
`CREATE DATABASE MyDatabase;`
`GO`

List databases
`SELECT name, database_id, create_date`
`FROM sys.databases;`
`GO`

If this is the first time using user-secrets for the project, run
`dotnet user-secrets init`

Store the connection string in user-secrets (replace passwrod, within the single quotes, with the generated password for 'me')
`dotnet user-secrets set DatabasePassword 'password'`

## Create a private key used to sign

Generate a random base-64 encoded string
`openssl rand -base64 12`
