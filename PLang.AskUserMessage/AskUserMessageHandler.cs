using Microsoft.Extensions.Logging;
using PLang.Errors;
using PLang.Errors.AskUser;
using PLang.Interfaces;
using PLang.Runtime;

namespace PLang.AskUserMessage
{
	public class AskUserMessageHandler : IAskUserHandler
	{
		private readonly IEngine engine;
		private readonly IPseudoRuntime pseudoRuntime;
		private readonly PLangAppContext context;
		private readonly MemoryStack memoryStack;
		private readonly ILogger logger;
		private IEngine? goalEngine = null;
		public AskUserMessageHandler(IEngine engine, IPseudoRuntime pseudoRuntime, PLangAppContext context, MemoryStack memoryStack, ILogger logger)
		{
			this.engine = engine;
			this.pseudoRuntime = pseudoRuntime;
			this.context = context;
			this.memoryStack = memoryStack;
			this.logger = logger;
		}

		public async Task<(bool, IError?)> Handle(AskUserError ex)
		{
			// user should add step into his Events.goal or Start.goal
			// - set static variable 'AskUserMessageAddress'='npub123....'
			var adminAddress = memoryStack.Get<string>("AskUserMessageAddress", true);
			if (string.IsNullOrEmpty(adminAddress))
			{
				logger.LogWarning(@$"%AskUserMessageAddress% is empty so no message will be sent. Content is:
{ex.Message}
");
				return (false, null);
				
			}
			Dictionary<string, object?> parameters = [];
			parameters.Add("content", ex.Message);
			parameters.Add("to", adminAddress);

			InstallServiceGoal();
			
			if (goalEngine == null) goalEngine = engine;

			var runGoalTask = pseudoRuntime.RunGoal(goalEngine, context, ".services/PLang.AskUserMessage", "SendMessage.goal", parameters);
			await runGoalTask;

			if (runGoalTask.Exception != null) return (false, new Error("Errur running goal", Exception: runGoalTask.Exception));

			goalEngine = runGoalTask.Result.engine;
			var engineMemoryStack = goalEngine.GetMemoryStack();
			// Wait for the response
			while (true)
			{
				
				var answer = engineMemoryStack.Get<string?>("AskUserResponse");
				engineMemoryStack.Remove("AskUserResponse");

				if (!string.IsNullOrEmpty(answer))
				{
					// the exception contain pointer to method that should be called when user answers
					// using InvokeCallback, it will call the method and send in the answer.
					// checkout https://github.com/PLangHQ/plang/blob/main/PLang/Exceptions/AskUser/Database/AskUserDatabaseType.cs
					// to see how the InvokeCallback uses LLM to map the answer from the user to a class.

					var task = ex.InvokeCallback([answer]);
					await task;

					if (task.Exception != null) throw task.Exception;

					
					return (true, null);
				}

				await Task.Delay(1000);
			}

		}


		private void InstallServiceGoal()
		{
			
		}
	}
}
