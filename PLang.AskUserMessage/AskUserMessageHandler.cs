using PLang.Exceptions.AskUser;
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

		public AskUserMessageHandler(IEngine engine, IPseudoRuntime pseudoRuntime, PLangAppContext context, MemoryStack memoryStack)
		{
			this.engine = engine;
			this.pseudoRuntime = pseudoRuntime;
			this.context = context;
			this.memoryStack = memoryStack;
		}

		public async Task<bool> Handle(AskUserException ex)
		{
			// user should add step into his Events.goal or Start.goal
			// - set static variable 'AskUserMessageAddress'='npub123....'
			var adminAddress = memoryStack.Get<string>("AskUserMessageAddress", true);

			Dictionary<string, object?> parameters = [];
			parameters.Add("content", ex.Message);
			parameters.Add("to", adminAddress);

			await pseudoRuntime.RunGoal(engine, context, Path.DirectorySeparatorChar.ToString(), ".services/AskUserMessage/SendMessage", parameters);

			// Wait for the response
			while (true)
			{
				var answer = memoryStack.Get<string?>("AskUserResponse");
				if (!string.IsNullOrEmpty(answer))
				{
					// the exception contain link to method that should be called when user answers
					// using InvokeCallback, it will call the method and send in the answer.
					await ex.InvokeCallback(answer);
					memoryStack.Put("AskUserResponse", null);
					return true;
				}

				await Task.Delay(1000);
			}

		}
	}
}
