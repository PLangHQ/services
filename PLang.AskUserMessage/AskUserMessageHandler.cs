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
			var adminAddress = memoryStack.Get<string>("AdminAddress", true);

			Dictionary<string, object?> parameters = [];
			parameters.Add("content", ex.Message);
			parameters.Add("to", adminAddress);

			await pseudoRuntime.RunGoal(engine, context, Path.DirectorySeparatorChar.ToString(), ".services/AskUserMessage/SendMessage", parameters);

			while (true)
			{
				var answer = memoryStack.Get<string?>("AskUserResponse");
				if (!string.IsNullOrEmpty(answer))
				{
					await ex.InvokeCallback(answer);
					memoryStack.Put("AskUserResponse", null);
					return true;
				}

				await Task.Delay(1000);
			}

		}
	}
}
