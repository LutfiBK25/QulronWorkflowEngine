# Domain.ProcessEngine.Enums
Has Module Types, Action Types(Done on Modules), Field Types, Execution Result

# Domain.Entities
Has Application, Module, Module Types

# Infrastructure.ProcessEngine.Execution.ModuleCache :
Has the cache for each module and type in the applications, Its where they are stored for quick access

# Infrastructure.ProcessEngine.Execution.ExecutionSession
# Infrastructure.ProcessEngine.Execution.ExecutionFrame
# Infrastructure.ProcessEngine.Execution.ActionResult


# Infrastructure.ProcessEngine.Parsing.FieldParser
# Infrastructure.ProcessEngine.Parsing.ReturnParser


## Execution Logics:
# Infrastructure.ProcessEngine.Executors.ProcessExecutor
Execution Logic for ProcessModule, and Each Step Action Type

# Infrastructure.ProcessEngine.Executors.DatabaseExecutor
Execution Logic for DatabaseActionModule

##


# Infrastructure.ProcessEngine.Services.ApplicationLoaderService
Loads Application Modules into the cache


## Testing Redis
# Test connection
docker exec -it processengine-redis redis-cli ping
# Should return: PONG
