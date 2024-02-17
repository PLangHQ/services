# Services
Services that can replace build in functionality of plang programming language

You can inject the following services
- db - This inject IDbConnection
- settings - This injects ISettingsRepository
- caching - This injects IAppCache
- logger - This injects Microsoft.Extensions.Logging.ILogger
- llm - This inject ILlmService
- askuser - This inject IAskUserHandler
- encryption - This injects IEncryption
- archiver - This injects IArchiver

All injections follow same pattern (llm can be exception)

Here is an example of how you inject your own service into plang app.

```plang
Start
- inject db, npgsql/lib/net7.0/Npgsql.dll, global
- inject caching, redis/redis.dll, global
```
The parameter global indicates that it should be globally set for the application. If it is not defined, it only applies to the goal that is running.

Here you can see an example using OpenAiService li on build. You will need the OpenAiService dll in your `.services` folder

```plang
@llm=OpenAiService

Start
- write out 'hello world'
```
It follows a strict pattern of @llm=..., so that the plang builder and runtime can parse it in the code before any llm is needed.

Download the folder of the service you want to inject.

More information on [Services can be found in our documentation](https://github.com/PLangHQ/plang/blob/main/Documentation/Services.md)

